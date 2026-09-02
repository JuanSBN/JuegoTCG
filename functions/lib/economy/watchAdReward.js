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
exports.watchAdReward = exports.MAX_DAILY_REWARDED_ADS = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const idempotency_1 = require("../utils/idempotency");
const appCheck_1 = require("../utils/appCheck");
// Límite del GDD 8.1: Máximo 2 sobres por anuncio al día
exports.MAX_DAILY_REWARDED_ADS = 2;
/**
 * Cloud Function: watchAdReward
 * Valida el callback de anuncio visto, verifica límite diario (GDD 8.1),
 * otorga el sobre de recompensa y protege con idempotencia.
 */
exports.watchAdReward = functions.https.onCall(async (data, context) => {
    // 0. Validar App Check (TDD 2.7)
    (0, appCheck_1.validateAppCheck)(context, "watchAdReward");
    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para reclamar la recompensa del anuncio.");
    }
    const { idempotencyKey, rewardVerificationToken } = data;
    if (!idempotencyKey) {
        throw new functions.https.HttpsError("invalid-argument", "El parámetro 'idempotencyKey' es obligatorio.");
    }
    // 2. Comprobar Idempotencia en caché rápido
    const cachedRecord = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
        console.log(`[watchAdReward] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
        return cachedRecord.result;
    }
    // 3. Ejecutar transacción atómica en Firestore
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 3a. Re-verificar idempotencia dentro de la transacción
        const processedRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
        const processedDoc = await transaction.get(processedRef);
        if (processedDoc.exists) {
            return processedDoc.data()?.result;
        }
        // 3b. Leer perfil del usuario
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
        const userDoc = await transaction.get(userRef);
        if (!userDoc.exists) {
            throw new functions.https.HttpsError("not-found", "El perfil del usuario no existe.");
        }
        const userData = userDoc.data() || {};
        const serverNow = Date.now();
        const todayDateStr = new Date(serverNow).toISOString().slice(0, 10); // YYYY-MM-DD
        const lastAdDateStr = userData.lastAdWatchedDate || "";
        let adsWatchedToday = (lastAdDateStr === todayDateStr) ? (userData.adsWatchedTodayCount || 0) : 0;
        // 3c. Validar límite diario de anuncios (GDD 8.1: 2 sobres por anuncio al día)
        if (adsWatchedToday >= exports.MAX_DAILY_REWARDED_ADS) {
            throw new functions.https.HttpsError("failed-precondition", `Has alcanzado el límite diario de ${exports.MAX_DAILY_REWARDED_ADS} sobres por anuncio. Vuelve mañana.`);
        }
        // 3d. Incrementar anuncios vistos y acreditar sobre
        adsWatchedToday += 1;
        const userPacks = userData.availablePacks || {};
        const currentAdPacks = userPacks["pack_anuncio"] ?? 0;
        const newAdPacks = currentAdPacks + 1;
        transaction.update(userRef, {
            "availablePacks.pack_anuncio": firebase_1.FieldValue.increment(1),
            adsWatchedTodayCount: adsWatchedToday,
            lastAdWatchedDate: todayDateStr,
            lastAdWatchedTimestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        // 3e. Registrar transacción de auditoría
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
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
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        const response = {
            success: true,
            rewardType: "pack_anuncio",
            adPacksAvailable: newAdPacks,
            adsWatchedToday,
            maxDailyAds: exports.MAX_DAILY_REWARDED_ADS,
            transactionId,
        };
        // 3f. Guardar idempotencia atómicamente
        (0, idempotency_1.recordIdempotencyInTransaction)(transaction, idempotencyKey, userId, "watchAdReward", response);
        return response;
    });
});
//# sourceMappingURL=watchAdReward.js.map