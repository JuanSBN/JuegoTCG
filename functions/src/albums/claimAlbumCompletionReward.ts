import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { getCachedIdempotentResult, recordIdempotencyInTransaction } from "../utils/idempotency";
import { validateAppCheck } from "../utils/appCheck";

export interface ClaimAlbumRewardRequest {
  albumId: string;
  idempotencyKey: string;
}

export interface ClaimAlbumRewardResponse {
  success: boolean;
  albumId: string;
  rewardCoins: number;
  rewardPackType: string;
  newCoinsTotal: number;
  completedAt: string;
  transactionId: string;
}

// Configuración de recompensa del Álbum Piloto (GDD 5.3)
export const ALBUM_REWARDS: Record<string, { coins: number; packType: string }> = {
  "album_piloto_01": { coins: 250, packType: "pack_mitico_garantizado" },
};

/**
 * Cloud Function: claimAlbumCompletionReward (Fase 7.2)
 * Valida en el servidor que el jugador posea todas las cartas del álbum antes de otorgar el premio mayor.
 */
export const claimAlbumCompletionReward = functions.https.onCall(
  async (data: ClaimAlbumRewardRequest, context: functions.https.CallableContext): Promise<ClaimAlbumRewardResponse> => {
    // 0. Validar App Check
    validateAppCheck(context, "claimAlbumCompletionReward");

    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para reclamar la recompensa del álbum."
      );
    }

    const { albumId, idempotencyKey } = data;
    if (!albumId || !idempotencyKey) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "Los parámetros 'albumId' y 'idempotencyKey' son obligatorios."
      );
    }

    // 2. Comprobar Idempotencia
    const cachedRecord = await getCachedIdempotentResult<ClaimAlbumRewardResponse>(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
      return cachedRecord.result;
    }

    // 3. Ejecutar transacción atómica de Firestore
    return await db.runTransaction(async (transaction) => {
      // 3a. Re-verificar idempotencia en transacción
      const processedRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
      const processedDoc = await transaction.get(processedRef);
      if (processedDoc.exists) {
        return processedDoc.data()?.result as ClaimAlbumRewardResponse;
      }

      // 3b. Leer perfil de usuario
      const userRef = db.collection(COLLECTIONS.USERS).doc(userId);
      const userDoc = await transaction.get(userRef);

      if (!userDoc.exists) {
        throw new functions.https.HttpsError(
          "not-found",
          "El perfil del usuario no existe."
        );
      }

      const userData = userDoc.data() || {};
      const completedAlbums = userData.completedAlbums || {};

      // 3c. Validar que no haya sido reclamado previamente
      if (completedAlbums[albumId]) {
        throw new functions.https.HttpsError(
          "already-exists",
          "Ya has reclamado la recompensa por completar este álbum."
        );
      }

      // 3d. Obtener el catálogo total de cartas del álbum
      const catalogQuery = await db
        .collection(COLLECTIONS.CARDS_CATALOG)
        .where("albumId", "==", albumId)
        .get();

      let requiredCardIds: string[] = [];
      if (!catalogQuery.empty) {
        requiredCardIds = catalogQuery.docs.map((d: any) => d.id);
      } else {
        // Fallback al catálogo piloto oficial de 10 cartas
        requiredCardIds = ["LD", "VJ", "EH", "KM", "PE", "LY", "JB", "RO", "MS", "KDB"];
      }

      // 3e. Verificar que el jugador tenga en su inventario todas las cartas requeridas
      const userCardsSnapshot = await db
        .collection(COLLECTIONS.USERS)
        .doc(userId)
        .collection(COLLECTIONS.USER_COLLECTION)
        .where("albumId", "==", albumId)
        .get();

      const userCardIds = new Set(userCardsSnapshot.docs.map((d: any) => d.id));
      const missingCards = requiredCardIds.filter((id) => !userCardIds.has(id));

      if (missingCards.length > 0) {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `Álbum incompleto. Te faltan ${missingCards.length} cartas para completarlo (${userCardIds.size}/${requiredCardIds.length}).`
        );
      }

      // 3f. Acreditar recompensas
      const rewardConfig = ALBUM_REWARDS[albumId] || { coins: 200, packType: "pack_mitico_garantizado" };
      const currentCoins = userData.coins ?? 0;
      const newCoinsTotal = currentCoins + rewardConfig.coins;

      transaction.update(userRef, {
        coins: newCoinsTotal,
        [`completedAlbums.${albumId}`]: {
          completedAt: FieldValue.serverTimestamp(),
          rewardCoins: rewardConfig.coins,
          rewardPackType: rewardConfig.packType,
        },
        [`availablePacks.${rewardConfig.packType}`]: FieldValue.increment(1),
      });

      // 3g. Registrar en log de transacciones
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
      const transactionId = txRef.id;
      transaction.set(txRef, {
        transactionId,
        userId,
        type: "completar_album",
        details: {
          albumId,
          rewardCoins: rewardConfig.coins,
          rewardPackType: rewardConfig.packType,
        },
        timestamp: FieldValue.serverTimestamp(),
      });

      const response: ClaimAlbumRewardResponse = {
        success: true,
        albumId,
        rewardCoins: rewardConfig.coins,
        rewardPackType: rewardConfig.packType,
        newCoinsTotal,
        completedAt: new Date().toISOString(),
        transactionId,
      };

      // 3h. Guardar idempotencia
      recordIdempotencyInTransaction(
        transaction,
        idempotencyKey,
        userId,
        "claimAlbumCompletionReward",
        response
      );

      return response;
    });
  }
);
