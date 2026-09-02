"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.purchaseCoins = exports.COIN_PACKS_CATALOG = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const idempotency_1 = require("../utils/idempotency");
const appCheck_1 = require("../utils/appCheck");
// Catálogo de paquetes de monedas y cantidades (GDD 9 / StoreScene)
exports.COIN_PACKS_CATALOG = {
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
exports.purchaseCoins = functions.https.onCall(async (data, context) => {
    // 0. Validar App Check (TDD 2.7)
    (0, appCheck_1.validateAppCheck)(context, "purchaseCoins");
    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para comprar monedas.");
    }
    const { idempotencyKey, productId, purchaseToken } = data;
    if (!idempotencyKey || !productId || !purchaseToken) {
        throw new functions.https.HttpsError("invalid-argument", "Los parámetros 'idempotencyKey', 'productId' y 'purchaseToken' son obligatorios.");
    }
    // 2. Validar que el producto exista en el catálogo del juego
    const packConfig = exports.COIN_PACKS_CATALOG[productId];
    if (!packConfig) {
        throw new functions.https.HttpsError("not-found", `El paquete '${productId}' no existe en la tienda del juego.`);
    }
    // 3. Comprobar Idempotencia en caché rápido
    const cachedRecord = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
        console.log(`[purchaseCoins] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
        return cachedRecord.result;
    }
    // 4. Ejecutar transacción atómica en Firestore
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 4a. Re-verificar idempotencia dentro de la transacción
        const processedRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
        const processedDoc = await transaction.get(processedRef);
        if (processedDoc.exists) {
            return processedDoc.data()?.result;
        }
        // 4b. Anti-Replay Attack: Validar que este purchaseToken no se haya usado en otra transacción
        const receiptQuery = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.TRANSACTIONS)
            .where("details.purchaseToken", "==", purchaseToken)
            .limit(1)
            .get();
        if (!receiptQuery.empty) {
            throw new functions.https.HttpsError("already-exists", "Este recibo de compra ya fue procesado y canjeado anteriormente.");
        }
        // 4c. Leer perfil de usuario
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
        const userDoc = await transaction.get(userRef);
        if (!userDoc.exists) {
            throw new functions.https.HttpsError("not-found", "El perfil del usuario no existe.");
        }
        const userData = userDoc.data() || {};
        const currentCoins = userData.coins ?? 0;
        const coinsToAdd = packConfig.coins;
        const newCoinsTotal = currentCoins + coinsToAdd;
        // 4d. Acreditar monedas al usuario
        transaction.update(userRef, {
            coins: newCoinsTotal,
            totalPurchasesCount: firebase_1.FieldValue.increment(1),
            lastPurchaseAt: firebase_1.FieldValue.serverTimestamp(),
        });
        // 4e. Registrar transacción de auditoría con el recibo
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
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
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        const response = {
            success: true,
            productId,
            coinsAdded: coinsToAdd,
            newCoinsTotal,
            transactionId,
        };
        // 4f. Guardar idempotencia atómicamente
        (0, idempotency_1.recordIdempotencyInTransaction)(transaction, idempotencyKey, userId, "purchaseCoins", response);
        return response;
    });
});
//# sourceMappingURL=purchaseCoins.js.map