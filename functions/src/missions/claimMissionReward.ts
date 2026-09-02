import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { getCachedIdempotentResult, recordIdempotencyInTransaction } from "../utils/idempotency";
import { validateAppCheck } from "../utils/appCheck";

export interface MissionDefinition {
  missionId: string;
  title: string;
  type: "diaria" | "progreso";
  totalRequired: number;
  rewardCoins: number;
  rewardPacks?: string;
}

// Catálogo de Misiones Oficiales del MVP (GDD 8 y 10)
export const MVP_MISSIONS_CATALOG: Record<string, MissionDefinition> = {
  "m_open_pack": {
    missionId: "m_open_pack",
    title: "Abre 1 sobre",
    type: "diaria",
    totalRequired: 1,
    rewardCoins: 50,
  },
  "m_get_rare": {
    missionId: "m_get_rare",
    title: "Consigue 1 carta rara o superior",
    type: "diaria",
    totalRequired: 1,
    rewardCoins: 100,
  },
  "m_market_action": {
    missionId: "m_market_action",
    title: "Vende o publica 1 carta en el mercado",
    type: "diaria",
    totalRequired: 1,
    rewardCoins: 75,
  },
  "m_trade_friend": {
    missionId: "m_trade_friend",
    title: "Intercambia 1 carta con un amigo",
    type: "diaria",
    totalRequired: 1,
    rewardCoins: 100,
  },
};

export interface ClaimMissionRewardRequest {
  idempotencyKey: string;
  missionId: string;
}

export interface ClaimMissionRewardResponse {
  success: boolean;
  missionId: string;
  coinsRewarded: number;
  newCoinsTotal: number;
  transactionId: string;
}

/**
 * Cloud Function: claimMissionReward (Fase 4.5)
 * Valida del lado del servidor que la misión esté completada y no haya sido reclamada previamente.
 * Acredita la recompensa de forma atómica y protege contra duplicaciones con idempotencia.
 */
export const claimMissionReward = functions.https.onCall(
  async (data: ClaimMissionRewardRequest, context: functions.https.CallableContext): Promise<ClaimMissionRewardResponse> => {
    // 0. Validar App Check
    validateAppCheck(context, "claimMissionReward");

    // 1. Validar Autenticación
    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para reclamar recompensas de misiones."
      );
    }

    const { idempotencyKey, missionId } = data;
    if (!idempotencyKey || !missionId) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "Los parámetros 'idempotencyKey' y 'missionId' son obligatorios."
      );
    }

    // 2. Validar que la misión exista en el catálogo
    const missionDef = MVP_MISSIONS_CATALOG[missionId];
    if (!missionDef) {
      throw new functions.https.HttpsError(
        "not-found",
        `La misión '${missionId}' no existe en el catálogo.`
      );
    }

    // 3. Comprobar Idempotencia en caché
    const cachedRecord = await getCachedIdempotentResult<ClaimMissionRewardResponse>(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
      return cachedRecord.result;
    }

    // 4. Ejecutar transacción atómica de Firestore
    return await db.runTransaction(async (transaction) => {
      // 4a. Re-verificar idempotencia en la transacción
      const processedRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
      const processedDoc = await transaction.get(processedRef);
      if (processedDoc.exists) {
        return processedDoc.data()?.result as ClaimMissionRewardResponse;
      }

      // 4b. Leer documento de misión del usuario (userMissions/{userId}_{missionId})
      const userMissionDocId = `${userId}_${missionId}`;
      const missionRef = db.collection(COLLECTIONS.USER_MISSIONS).doc(userMissionDocId);
      const missionDoc = await transaction.get(missionRef);

      if (!missionDoc.exists) {
        throw new functions.https.HttpsError(
          "failed-precondition",
          "Aún no has iniciado o completado esta misión."
        );
      }

      const missionData = missionDoc.data() || {};
      const currentProgress = missionData.currentProgress ?? 0;
      const isClaimed = missionData.claimed === true;

      if (isClaimed) {
        throw new functions.https.HttpsError(
          "already-exists",
          "La recompensa de esta misión ya fue reclamada."
        );
      }

      if (currentProgress < missionDef.totalRequired) {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `Misión incompleta. Progreso actual: ${currentProgress}/${missionDef.totalRequired}.`
        );
      }

      // 4c. Leer perfil del usuario y sumar recompensa
      const userRef = db.collection(COLLECTIONS.USERS).doc(userId);
      const userDoc = await transaction.get(userRef);

      if (!userDoc.exists) {
        throw new functions.https.HttpsError(
          "not-found",
          "El perfil del usuario no existe."
        );
      }

      const userData = userDoc.data() || {};
      const currentCoins = userData.coins ?? 0;
      const coinsToAdd = missionDef.rewardCoins;
      const newCoinsTotal = currentCoins + coinsToAdd;

      // 4d. Actualizar estado de la misión
      transaction.update(missionRef, {
        claimed: true,
        claimedAt: FieldValue.serverTimestamp(),
      });

      // 4e. Acreditar monedas al usuario
      transaction.update(userRef, {
        coins: newCoinsTotal,
        missionsCompletedCount: FieldValue.increment(1),
      });

      // 4f. Registrar transacción de auditoría
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
      const transactionId = txRef.id;
      transaction.set(txRef, {
        transactionId,
        userId,
        type: "reclamar_mision",
        details: {
          missionId,
          missionTitle: missionDef.title,
          coinsRewarded: coinsToAdd,
        },
        timestamp: FieldValue.serverTimestamp(),
      });

      const response: ClaimMissionRewardResponse = {
        success: true,
        missionId,
        coinsRewarded: coinsToAdd,
        newCoinsTotal,
        transactionId,
      };

      // 4g. Guardar idempotencia atómicamente
      recordIdempotencyInTransaction(
        transaction,
        idempotencyKey,
        userId,
        "claimMissionReward",
        response
      );

      return response;
    });
  }
);
