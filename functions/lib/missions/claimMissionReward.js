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
exports.claimMissionReward = exports.MVP_MISSIONS_CATALOG = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const idempotency_1 = require("../utils/idempotency");
const appCheck_1 = require("../utils/appCheck");
// Catálogo de Misiones Oficiales del MVP (GDD 8 y 10)
exports.MVP_MISSIONS_CATALOG = {
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
/**
 * Cloud Function: claimMissionReward (Fase 4.5)
 * Valida del lado del servidor que la misión esté completada y no haya sido reclamada previamente.
 * Acredita la recompensa de forma atómica y protege contra duplicaciones con idempotencia.
 */
exports.claimMissionReward = functions.https.onCall(async (data, context) => {
    // 0. Validar App Check
    (0, appCheck_1.validateAppCheck)(context, "claimMissionReward");
    // 1. Validar Autenticación
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para reclamar recompensas de misiones.");
    }
    const { idempotencyKey, missionId } = data;
    if (!idempotencyKey || !missionId) {
        throw new functions.https.HttpsError("invalid-argument", "Los parámetros 'idempotencyKey' y 'missionId' son obligatorios.");
    }
    // 2. Validar que la misión exista en el catálogo
    const missionDef = exports.MVP_MISSIONS_CATALOG[missionId];
    if (!missionDef) {
        throw new functions.https.HttpsError("not-found", `La misión '${missionId}' no existe en el catálogo.`);
    }
    // 3. Comprobar Idempotencia en caché
    const cachedRecord = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
        return cachedRecord.result;
    }
    // 4. Ejecutar transacción atómica de Firestore
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 4a. Re-verificar idempotencia en la transacción
        const processedRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
        const processedDoc = await transaction.get(processedRef);
        if (processedDoc.exists) {
            return processedDoc.data()?.result;
        }
        // 4b. Leer documento de misión del usuario (userMissions/{userId}_{missionId})
        const userMissionDocId = `${userId}_${missionId}`;
        const missionRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USER_MISSIONS).doc(userMissionDocId);
        const missionDoc = await transaction.get(missionRef);
        if (!missionDoc.exists) {
            throw new functions.https.HttpsError("failed-precondition", "Aún no has iniciado o completado esta misión.");
        }
        const missionData = missionDoc.data() || {};
        const currentProgress = missionData.currentProgress ?? 0;
        const isClaimed = missionData.claimed === true;
        if (isClaimed) {
            throw new functions.https.HttpsError("already-exists", "La recompensa de esta misión ya fue reclamada.");
        }
        if (currentProgress < missionDef.totalRequired) {
            throw new functions.https.HttpsError("failed-precondition", `Misión incompleta. Progreso actual: ${currentProgress}/${missionDef.totalRequired}.`);
        }
        // 4c. Leer perfil del usuario y sumar recompensa
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
        const userDoc = await transaction.get(userRef);
        if (!userDoc.exists) {
            throw new functions.https.HttpsError("not-found", "El perfil del usuario no existe.");
        }
        const userData = userDoc.data() || {};
        const currentCoins = userData.coins ?? 0;
        const coinsToAdd = missionDef.rewardCoins;
        const newCoinsTotal = currentCoins + coinsToAdd;
        // 4d. Actualizar estado de la misión
        transaction.update(missionRef, {
            claimed: true,
            claimedAt: firebase_1.FieldValue.serverTimestamp(),
        });
        // 4e. Acreditar monedas al usuario
        transaction.update(userRef, {
            coins: newCoinsTotal,
            missionsCompletedCount: firebase_1.FieldValue.increment(1),
        });
        // 4f. Registrar transacción de auditoría
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
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
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        const response = {
            success: true,
            missionId,
            coinsRewarded: coinsToAdd,
            newCoinsTotal,
            transactionId,
        };
        // 4g. Guardar idempotencia atómicamente
        (0, idempotency_1.recordIdempotencyInTransaction)(transaction, idempotencyKey, userId, "claimMissionReward", response);
        return response;
    });
});
//# sourceMappingURL=claimMissionReward.js.map