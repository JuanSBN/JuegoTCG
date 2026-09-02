import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { getCachedIdempotentResult, recordIdempotencyInTransaction } from "../utils/idempotency";
import { validateAppCheck } from "../utils/appCheck";

export interface ClaimFreePackRequest {
  idempotencyKey: string;
}

export interface ClaimFreePackResponse {
  success: boolean;
  freePacksAvailable: number;
  lastClaimAt: string;
  nextClaimAvailableAt: string;
  cooldownSecondsRemaining: number;
  transactionId: string;
}

// Cooldown de recarga: 12 horas entre sobres gratis (2 sobres gratis por día según GDD 8.1)
export const FREE_PACK_COOLDOWN_HOURS = 12;
export const FREE_PACK_COOLDOWN_MS = FREE_PACK_COOLDOWN_HOURS * 60 * 60 * 1000;

/**
 * Cloud Function: claimFreePack
 * Reclama el sobre gratis diario validando el cooldown en el servidor (Timestamp de Firestore)
 * e idempotencia para evitar duplicaciones.
 */
export const claimFreePack = functions.https.onCall(
  async (data: ClaimFreePackRequest, context: functions.https.CallableContext): Promise<ClaimFreePackResponse> => {
    // 0. Validar App Check (TDD 2.7)
    validateAppCheck(context, "claimFreePack");

    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para reclamar tu sobre gratis."
      );
    }

    const { idempotencyKey } = data;
    if (!idempotencyKey) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El parámetro 'idempotencyKey' es obligatorio."
      );
    }

    // 2. Comprobar Idempotencia en caché rápido
    const cachedRecord = await getCachedIdempotentResult<ClaimFreePackResponse>(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
      console.log(`[claimFreePack] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
      return cachedRecord.result;
    }

    // 3. Ejecutar transacción atómica de Firestore
    return await db.runTransaction(async (transaction) => {
      // 3a. Re-verificar idempotencia dentro de la transacción
      const processedRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
      const processedDoc = await transaction.get(processedRef);
      if (processedDoc.exists) {
        return processedDoc.data()?.result as ClaimFreePackResponse;
      }

      // 3b. Leer documento de usuario
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
      const lastClaimTimestamp = userData.lastFreePackClaimAt?.toMillis?.() || 0;
      const timeElapsed = serverNow - lastClaimTimestamp;

      // 3c. Validar cooldown del lado del servidor (reloj real de Firestore)
      if (lastClaimTimestamp > 0 && timeElapsed < FREE_PACK_COOLDOWN_MS) {
        const remainingMs = FREE_PACK_COOLDOWN_MS - timeElapsed;
        const remainingHours = Math.ceil(remainingMs / (60 * 60 * 1000));
        throw new functions.https.HttpsError(
          "failed-precondition",
          `Tu sobre gratis aún no está listo. Vuelve en ${remainingHours}h para tu próximo sobre.`
        );
      }

      // 3d. Acreditar sobre gratis al inventario
      const userPacks = userData.availablePacks || {};
      const currentFreePacks = userPacks["pack_gratis_diario"] ?? 0;
      const newFreePacks = currentFreePacks + 1;

      transaction.update(userRef, {
        "availablePacks.pack_gratis_diario": FieldValue.increment(1),
        lastFreePackClaimAt: FieldValue.serverTimestamp(),
      });

      // 3e. Registrar transacción en log de auditoría
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
      const transactionId = txRef.id;
      transaction.set(txRef, {
        transactionId,
        userId,
        type: "reclamar_sobre_gratis",
        details: {
          packType: "pack_gratis_diario",
          cooldownHours: FREE_PACK_COOLDOWN_HOURS,
        },
        timestamp: FieldValue.serverTimestamp(),
      });

      const nextClaimDate = new Date(serverNow + FREE_PACK_COOLDOWN_MS).toISOString();

      const response: ClaimFreePackResponse = {
        success: true,
        freePacksAvailable: newFreePacks,
        lastClaimAt: new Date(serverNow).toISOString(),
        nextClaimAvailableAt: nextClaimDate,
        cooldownSecondsRemaining: Math.floor(FREE_PACK_COOLDOWN_MS / 1000),
        transactionId,
      };

      // 3f. Guardar idempotencia atómicamente
      recordIdempotencyInTransaction(
        transaction,
        idempotencyKey,
        userId,
        "claimFreePack",
        response
      );

      return response;
    });
  }
);
