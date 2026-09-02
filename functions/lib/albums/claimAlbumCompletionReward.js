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
exports.claimAlbumCompletionReward = exports.ALBUM_REWARDS = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const idempotency_1 = require("../utils/idempotency");
const appCheck_1 = require("../utils/appCheck");
// Configuración de recompensa del Álbum Piloto (GDD 5.3)
exports.ALBUM_REWARDS = {
    "album_piloto_01": { coins: 250, packType: "pack_mitico_garantizado" },
};
/**
 * Cloud Function: claimAlbumCompletionReward (Fase 7.2)
 * Valida en el servidor que el jugador posea todas las cartas del álbum antes de otorgar el premio mayor.
 */
exports.claimAlbumCompletionReward = functions.https.onCall(async (data, context) => {
    // 0. Validar App Check
    (0, appCheck_1.validateAppCheck)(context, "claimAlbumCompletionReward");
    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para reclamar la recompensa del álbum.");
    }
    const { albumId, idempotencyKey } = data;
    if (!albumId || !idempotencyKey) {
        throw new functions.https.HttpsError("invalid-argument", "Los parámetros 'albumId' y 'idempotencyKey' son obligatorios.");
    }
    // 2. Comprobar Idempotencia
    const cachedRecord = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
        return cachedRecord.result;
    }
    // 3. Ejecutar transacción atómica de Firestore
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 3a. Re-verificar idempotencia en transacción
        const processedRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
        const processedDoc = await transaction.get(processedRef);
        if (processedDoc.exists) {
            return processedDoc.data()?.result;
        }
        // 3b. Leer perfil de usuario
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
        const userDoc = await transaction.get(userRef);
        if (!userDoc.exists) {
            throw new functions.https.HttpsError("not-found", "El perfil del usuario no existe.");
        }
        const userData = userDoc.data() || {};
        const completedAlbums = userData.completedAlbums || {};
        // 3c. Validar que no haya sido reclamado previamente
        if (completedAlbums[albumId]) {
            throw new functions.https.HttpsError("already-exists", "Ya has reclamado la recompensa por completar este álbum.");
        }
        // 3d. Obtener el catálogo total de cartas del álbum
        const catalogQuery = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.CARDS_CATALOG)
            .where("albumId", "==", albumId)
            .get();
        let requiredCardIds = [];
        if (!catalogQuery.empty) {
            requiredCardIds = catalogQuery.docs.map((d) => d.id);
        }
        else {
            // Fallback al catálogo piloto oficial de 10 cartas
            requiredCardIds = ["LD", "VJ", "EH", "KM", "PE", "LY", "JB", "RO", "MS", "KDB"];
        }
        // 3e. Verificar que el jugador tenga en su inventario todas las cartas requeridas
        const userCardsSnapshot = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
            .doc(userId)
            .collection(firebase_1.COLLECTIONS.USER_COLLECTION)
            .where("albumId", "==", albumId)
            .get();
        const userCardIds = new Set(userCardsSnapshot.docs.map((d) => d.id));
        const missingCards = requiredCardIds.filter((id) => !userCardIds.has(id));
        if (missingCards.length > 0) {
            throw new functions.https.HttpsError("failed-precondition", `Álbum incompleto. Te faltan ${missingCards.length} cartas para completarlo (${userCardIds.size}/${requiredCardIds.length}).`);
        }
        // 3f. Acreditar recompensas
        const rewardConfig = exports.ALBUM_REWARDS[albumId] || { coins: 200, packType: "pack_mitico_garantizado" };
        const currentCoins = userData.coins ?? 0;
        const newCoinsTotal = currentCoins + rewardConfig.coins;
        transaction.update(userRef, {
            coins: newCoinsTotal,
            [`completedAlbums.${albumId}`]: {
                completedAt: firebase_1.FieldValue.serverTimestamp(),
                rewardCoins: rewardConfig.coins,
                rewardPackType: rewardConfig.packType,
            },
            [`availablePacks.${rewardConfig.packType}`]: firebase_1.FieldValue.increment(1),
        });
        // 3g. Registrar en log de transacciones
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
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
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        const response = {
            success: true,
            albumId,
            rewardCoins: rewardConfig.coins,
            rewardPackType: rewardConfig.packType,
            newCoinsTotal,
            completedAt: new Date().toISOString(),
            transactionId,
        };
        // 3h. Guardar idempotencia
        (0, idempotency_1.recordIdempotencyInTransaction)(transaction, idempotencyKey, userId, "claimAlbumCompletionReward", response);
        return response;
    });
});
//# sourceMappingURL=claimAlbumCompletionReward.js.map