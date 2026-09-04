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
exports.recalculateCollectionPower = exports.recalculateCollectionPowerTrigger = exports.RARITY_POWER_POINTS_TABLE = void 0;
exports.calculateUserCollectionPower = calculateUserCollectionPower;
exports.updateCachedCollectionPower = updateCachedCollectionPower;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Tabla oficial de puntos por rareza para el Poder de Colección (GDD Sección 7.2)
 * Comun: 1
 * Especial / Poco común: 2
 * Épica / Rara: 4
 * Legendaria: 8
 * Mítica: 15
 * Full Art: 25
 */
exports.RARITY_POWER_POINTS_TABLE = {
    comun: 1,
    especial: 2,
    poco_comun: 2,
    epica: 4,
    rara: 4,
    legendaria: 8,
    mitica: 15,
    full_art: 25,
};
/**
 * Calcula el poder de colección de un usuario sumando los puntos fijos por rareza
 * de cada carta ÚNICA obtenida (los duplicados no suman puntos extra, según GDD 7.2).
 */
async function calculateUserCollectionPower(userId) {
    const cardsSnap = await firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId).collection("cards").get();
    let totalPower = 0;
    let uniqueCount = 0;
    cardsSnap.forEach((doc) => {
        const card = doc.data();
        const qty = card.quantity || 0;
        if (qty > 0) {
            uniqueCount++;
            const rarityKey = (card.rarity || "comun").toString().toLowerCase();
            const points = exports.RARITY_POWER_POINTS_TABLE[rarityKey] || 1;
            totalPower += points;
        }
    });
    return { totalPower, uniqueCount };
}
/**
 * Actualiza el campo cacheado 'collectionPower' en el documento de usuario en users/{userId}
 * para evitar lecturas masivas en consultas de rankings y listas de amigos (TDD Sección 6).
 */
async function updateCachedCollectionPower(userId) {
    const { totalPower, uniqueCount } = await calculateUserCollectionPower(userId);
    await firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId).set({
        collectionPower: totalPower,
        uniqueCardsCount: uniqueCount,
        lastPowerRecalculatedAt: firebase_1.FieldValue.serverTimestamp(),
    }, { merge: true });
    return totalPower;
}
/**
 * Trigger de Firestore (TDD Sección 6):
 * Se dispara automáticamente cuando cambia userCollection de un jugador
 * (nueva carta obtenida por openPack o por comprar en el mercado) y actualiza collectionPower.
 */
exports.recalculateCollectionPowerTrigger = functions.firestore
    .document(`${firebase_1.COLLECTIONS.USERS}/{userId}/cards/{cardId}`)
    .onWrite(async (change, context) => {
    const userId = context.params.userId;
    if (!userId)
        return null;
    try {
        const newPower = await updateCachedCollectionPower(userId);
        console.log(`[Trigger] collectionPower recalculado para usuario ${userId}: ${newPower} pts`);
        return { success: true, userId, newPower };
    }
    catch (error) {
        console.error(`[Trigger] Error recalculando collectionPower para ${userId}:`, error);
        return null;
    }
});
/**
 * Cloud Function Callable: recalculateCollectionPower
 * Permite al cliente solicitar manualmente un recálculo o sincronización del poder de colección.
 */
exports.recalculateCollectionPower = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "recalculateCollectionPower");
    const authUid = context.auth?.uid;
    if (!authUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para recalcular el poder de colección.");
    }
    const userId = data?.targetUserId || authUid;
    const newPower = await updateCachedCollectionPower(userId);
    return {
        success: true,
        userId,
        collectionPower: newPower,
    };
});
//# sourceMappingURL=recalculateCollectionPower.js.map