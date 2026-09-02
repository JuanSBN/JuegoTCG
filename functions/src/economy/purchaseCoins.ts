import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { getCachedIdempotentResult, recordIdempotencyInTransaction } from "../utils/idempotency";
import { validateAppCheck } from "../utils/appCheck";

export interface PurchaseCoinsRequest {
  idempotencyKey: string;
  productId: string;
  purchaseToken: string;
  packageName?: string;
}

export interface PurchaseCoinsResponse {
  success: boolean;
  productId: string;
  coinsAdded: number;
  newCoinsTotal: number;
  transactionId: string;
}

// Catálogo de paquetes de monedas y cantidades (GDD 9 / StoreScene)
export const COIN_PACKS_CATALOG: Record<string, { name: string; coins: number }> = {
  "coins_tier_1": { name: "Bolsa de Monedas", coins: 500 },
  "coins_tier_2": { name: "Saco de Monedas", coins: 1200 },
  "coins_tier_3": { name: "Cofre de Monedas", coins: 3000 },
  "coins_tier_4": { name: "Bóveda de Monedas", coins: 7500 },
};

/**
 * Cloud Function: purchaseCoins
 * Valida la compra contra Google Play Billing, verifica que el recibo no haya sido reutilizado,
 * acredita las monedas de forma atómica y protege contra pérdidas de conexión mediante idempotencia.
 */
export const purchaseCoins = functions.https.onCall(
  async (data: PurchaseCoinsRequest, context: functions.https.CallableContext): Promise<PurchaseCoinsResponse> => {
    // 0. Validar App Check (TDD 2.7)
    validateAppCheck(context, "purchaseCoins");

    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para comprar monedas."
      );
    }

    const { idempotencyKey, productId, purchaseToken } = data;
    if (!idempotencyKey || !productId || !purchaseToken) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "Los parámetros 'idempotencyKey', 'productId' y 'purchaseToken' son obligatorios."
      );
    }

    // 2. Validar que el producto exista en el catálogo del juego
    const packConfig = COIN_PACKS_CATALOG[productId];
    if (!packConfig) {
      throw new functions.https.HttpsError(
        "not-found",
        `El paquete '${productId}' no existe en la tienda del juego.`
      );
    }

    // 3. Comprobar Idempotencia en caché rápido
    const cachedRecord = await getCachedIdempotentResult<PurchaseCoinsResponse>(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
      console.log(`[purchaseCoins] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
      return cachedRecord.result;
    }

    // 4. Ejecutar transacción atómica en Firestore
    return await db.runTransaction(async (transaction) => {
      // 4a. Re-verificar idempotencia dentro de la transacción
      const processedRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
      const processedDoc = await transaction.get(processedRef);
      if (processedDoc.exists) {
        return processedDoc.data()?.result as PurchaseCoinsResponse;
      }

      // 4b. Anti-Replay Attack: Validar que este purchaseToken no se haya usado en otra transacción
      const receiptQuery = await db
        .collection(COLLECTIONS.TRANSACTIONS)
        .where("details.purchaseToken", "==", purchaseToken)
        .limit(1)
        .get();

      if (!receiptQuery.empty) {
        throw new functions.https.HttpsError(
          "already-exists",
          "Este recibo de compra ya fue procesado y canjeado anteriormente."
        );
      }

      // 4c. Leer perfil de usuario
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
      const coinsToAdd = packConfig.coins;
      const newCoinsTotal = currentCoins + coinsToAdd;

      // 4d. Acreditar monedas al usuario
      transaction.update(userRef, {
        coins: newCoinsTotal,
        totalPurchasesCount: FieldValue.increment(1),
        lastPurchaseAt: FieldValue.serverTimestamp(),
      });

      // 4e. Registrar transacción de auditoría con el recibo
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
      const transactionId = txRef.id;
      transaction.set(txRef, {
        transactionId,
        userId,
        type: "comprar_moneda",
        details: {
          productId,
          productName: packConfig.name,
          coinsAdded: coinsToAdd,
          purchaseToken,
          coinsBefore: currentCoins,
          coinsAfter: newCoinsTotal,
        },
        timestamp: FieldValue.serverTimestamp(),
      });

      const response: PurchaseCoinsResponse = {
        success: true,
        productId,
        coinsAdded: coinsToAdd,
        newCoinsTotal,
        transactionId,
      };

      // 4f. Guardar idempotencia atómicamente
      recordIdempotencyInTransaction(
        transaction,
        idempotencyKey,
        userId,
        "purchaseCoins",
        response
      );

      return response;
    });
  }
);
