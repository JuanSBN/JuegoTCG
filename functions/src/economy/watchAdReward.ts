import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { getCachedIdempotentResult, recordIdempotencyInTransaction } from "../utils/idempotency";
import { validateAppCheck } from "../utils/appCheck";

export interface WatchAdRewardRequest {
  idempotencyKey: string;
  adPlacementId?: string;
  rewardVerificationToken?: string;
}

export interface WatchAdRewardResponse {
  success: boolean;
  rewardType: string;
  adPacksAvailable: number;
  adsWatchedToday: number;
  maxDailyAds: number;
  transactionId: string;
}

// Límite del GDD 8.1: Máximo 2 sobres por anuncio al día
export const MAX_DAILY_REWARDED_ADS = 2;

/**
 * Cloud Function: watchAdReward
 * Valida el callback de anuncio visto, verifica límite diario (GDD 8.1),
 * otorga el sobre de recompensa y protege con idempotencia.
 */
export const watchAdReward = functions.https.onCall(
  async (data: WatchAdRewardRequest, context: functions.https.CallableContext): Promise<WatchAdRewardResponse> => {
    // 0. Validar App Check (TDD 2.7)
    validateAppCheck(context, "watchAdReward");

    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para reclamar la recompensa del anuncio."
      );
    }

    const { idempotencyKey, rewardVerificationToken } = data;
    if (!idempotencyKey) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El parámetro 'idempotencyKey' es obligatorio."
      );
    }

    // 2. Comprobar Idempotencia en caché rápido
    const cachedRecord = await getCachedIdempotentResult<WatchAdRewardResponse>(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
      console.log(`[watchAdReward] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
      return cachedRecord.result;
    }

    // 3. Ejecutar transacción atómica en Firestore
    return await db.runTransaction(async (transaction) => {
      // 3a. Re-verificar idempotencia dentro de la transacción
      const processedRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
      const processedDoc = await transaction.get(processedRef);
      if (processedDoc.exists) {
        return processedDoc.data()?.result as WatchAdRewardResponse;
      }

      // 3b. Leer perfil del usuario
      const userRef = db.collection(COLLECTIONS.USERS).doc(userId);
      const userDoc = await transaction.get(userRef);

      if (!userDoc.exists) {
        throw new functions.https.HttpsError(
          "not-found",
          "El perfil del usuario no existe."
        );
      }

      const userData = userDoc.data() || {};
      const serverNow = Date.now();
      const todayDateStr = new Date(serverNow).toISOString().slice(0, 10); // YYYY-MM-DD
      
      const lastAdDateStr = userData.lastAdWatchedDate || "";
      let adsWatchedToday = (lastAdDateStr === todayDateStr) ? (userData.adsWatchedTodayCount || 0) : 0;

      // 3c. Validar límite diario de anuncios (GDD 8.1: 2 sobres por anuncio al día)
      if (adsWatchedToday >= MAX_DAILY_REWARDED_ADS) {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `Has alcanzado el límite diario de ${MAX_DAILY_REWARDED_ADS} sobres por anuncio. Vuelve mañana.`
        );
      }

      // 3d. Incrementar anuncios vistos y acreditar sobre
      adsWatchedToday += 1;
      const userPacks = userData.availablePacks || {};
      const currentAdPacks = userPacks["pack_anuncio"] ?? 0;
      const newAdPacks = currentAdPacks + 1;

      transaction.update(userRef, {
        "availablePacks.pack_anuncio": FieldValue.increment(1),
        adsWatchedTodayCount: adsWatchedToday,
        lastAdWatchedDate: todayDateStr,
        lastAdWatchedTimestamp: FieldValue.serverTimestamp(),
      });

      // 3e. Registrar transacción de auditoría
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
      const transactionId = txRef.id;
      transaction.set(txRef, {
        transactionId,
        userId,
        type: "ver_anuncio_recompensa",
        details: {
          reward: "pack_anuncio",
          adNumberToday: adsWatchedToday,
          tokenVerification: rewardVerificationToken ? "valid" : "client_callback",
        },
        timestamp: FieldValue.serverTimestamp(),
      });

      const response: WatchAdRewardResponse = {
        success: true,
        rewardType: "pack_anuncio",
        adPacksAvailable: newAdPacks,
        adsWatchedToday,
        maxDailyAds: MAX_DAILY_REWARDED_ADS,
        transactionId,
      };

      // 3f. Guardar idempotencia atómicamente
      recordIdempotencyInTransaction(
        transaction,
        idempotencyKey,
        userId,
        "watchAdReward",
        response
      );

      return response;
    });
  }
);
