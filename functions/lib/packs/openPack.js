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
exports.openPack = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const idempotency_1 = require("../utils/idempotency");
const appCheck_1 = require("../utils/appCheck");
const generateCardsRNG_1 = require("./generateCardsRNG");
/**
 * Cloud Function: openPack
 * Executes server-side authoritative pack opening, weighted RNG, atomic deduction,
 * inventory crediting and idempotency caching.
 */
exports.openPack = functions.https.onCall(async (data, context) => {
    // 0. Verify App Check (TDD 2.7)
    (0, appCheck_1.validateAppCheck)(context, "openPack");
    // 1. Verify Authentication
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para abrir sobres.");
    }
    const { packId, idempotencyKey } = data;
    if (!packId || !idempotencyKey) {
        throw new functions.https.HttpsError("invalid-argument", "Los parámetros 'packId' y 'idempotencyKey' son obligatorios.");
    }
    // 2. Check Idempotency (Fast path from cache before acquiring database locks)
    const cachedRecord = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
        console.log(`[openPack] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
        return cachedRecord.result;
    }
    // 3. Execute in Atomic Firestore Transaction
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 3a. Re-verify idempotency inside the active transaction
        const processedRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
        const processedDoc = await transaction.get(processedRef);
        if (processedDoc.exists) {
            return processedDoc.data()?.result;
        }
        // 3b. Read user data
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
        const userDoc = await transaction.get(userRef);
        if (!userDoc.exists) {
            throw new functions.https.HttpsError("not-found", "El perfil del usuario no existe en la base de datos.");
        }
        const userData = userDoc.data() || {};
        const currentCoins = userData.coins ?? 0;
        let currentPower = userData.collectionPower ?? 0;
        const userPacks = userData.availablePacks || {};
        const packsCount = userPacks[packId] ?? 0;
        // 3c. Read pack definition
        const packRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PACKS).doc(packId);
        const packDoc = await transaction.get(packRef);
        let costAmount = 0;
        let costType = "sobres_disponibles";
        let weights = generateCardsRNG_1.DEFAULT_RARITY_WEIGHTS;
        let albumId = "album_piloto_01";
        if (packDoc.exists) {
            const packData = packDoc.data() || {};
            costAmount = packData.costAmount || 0;
            costType = packData.costType || "moneda";
            weights = packData.rarityWeights || generateCardsRNG_1.DEFAULT_RARITY_WEIGHTS;
            albumId = packData.albumId || "album_piloto_01";
        }
        // Validate cost/availability
        if (costType === "moneda") {
            if (costAmount > 0 && currentCoins < costAmount) {
                throw new functions.https.HttpsError("failed-precondition", `Monedas insuficientes. Necesitas ${costAmount} monedas.`);
            }
        }
        else {
            if (packsCount <= 0 && currentCoins < 100) {
                throw new functions.https.HttpsError("failed-precondition", "No tienes sobres de este tipo disponibles.");
            }
        }
        // 3d. Fetch cards catalog for this album
        const catalogQuery = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.CARDS_CATALOG)
            .where("albumId", "==", albumId)
            .get();
        let catalog = [];
        if (!catalogQuery.empty) {
            catalog = catalogQuery.docs.map((d) => ({
                cardId: d.id,
                ...d.data(),
            }));
        }
        else {
            // Fallback default catalog if database is not yet seeded
            catalog = [
                { cardId: "LD", name: "Luis Díaz", initials: "LD", rarity: "mitica", team: "Liverpool", position: "DEL", albumId },
                { cardId: "VJ", name: "Vinicius Jr.", initials: "VJ", rarity: "rara", team: "Madrid", position: "DEL", albumId },
                { cardId: "EH", name: "Haaland", initials: "EH", rarity: "comun", team: "Manchester", position: "DEL", albumId },
                { cardId: "KM", name: "Mbappé", initials: "KM", rarity: "poco_comun", team: "Madrid", position: "DEL", albumId },
                { cardId: "PE", name: "Pedri", initials: "PE", rarity: "rara", team: "Barcelona", position: "MED", albumId },
                { cardId: "RO", name: "Rodri", initials: "RO", rarity: "comun", team: "Manchester", position: "MED", albumId },
                { cardId: "LY", name: "Lamine Yamal", initials: "LY", rarity: "mitica", team: "Barcelona", position: "DEL", albumId },
                { cardId: "JB", name: "Bellingham", initials: "JB", rarity: "rara", team: "Madrid", position: "MED", albumId },
                { cardId: "MS", name: "Salah", initials: "MS", rarity: "poco_comun", team: "Liverpool", position: "DEL", albumId },
                { cardId: "KDB", name: "De Bruyne", initials: "KDB", rarity: "rara", team: "Manchester", position: "MED", albumId },
            ];
        }
        // 3e. Generate 5 cards with weighted RNG
        const rolledCards = (0, generateCardsRNG_1.generatePackCards)(catalog, 5, weights);
        // 3f. Update user's collection & calculate power
        const cardsResult = [];
        let powerGained = 0;
        for (const card of rolledCards) {
            const cardRef = firebase_1.db
                .collection(firebase_1.COLLECTIONS.USERS)
                .doc(userId)
                .collection(firebase_1.COLLECTIONS.USER_COLLECTION)
                .doc(card.cardId);
            const cardDoc = await transaction.get(cardRef);
            const isNew = !cardDoc.exists;
            const currentQty = isNew ? 0 : cardDoc.data()?.quantity || 0;
            const newQty = currentQty + 1;
            if (isNew) {
                transaction.set(cardRef, {
                    cardId: card.cardId,
                    quantity: 1,
                    rarity: card.rarity,
                    albumId: card.albumId,
                    dateObtained: firebase_1.FieldValue.serverTimestamp(),
                });
                powerGained += generateCardsRNG_1.RARITY_POWER_POINTS[card.rarity] || 1;
            }
            else {
                transaction.update(cardRef, {
                    quantity: firebase_1.FieldValue.increment(1),
                });
            }
            cardsResult.push({
                ...card,
                isNew,
                quantityAfter: newQty,
            });
        }
        // 3g. Deduct cost / pack count
        let newCoins = currentCoins;
        const userUpdates = {
            collectionPower: currentPower + powerGained,
            packsOpenedTotal: firebase_1.FieldValue.increment(1),
            lastPackOpenedAt: firebase_1.FieldValue.serverTimestamp(),
        };
        if (costType === "moneda" && costAmount > 0) {
            newCoins = currentCoins - costAmount;
            userUpdates.coins = newCoins;
        }
        else if (packsCount > 0) {
            userUpdates[`availablePacks.${packId}`] = firebase_1.FieldValue.increment(-1);
        }
        transaction.update(userRef, userUpdates);
        // 3h. Register audit transaction log
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
        const transactionId = txRef.id;
        transaction.set(txRef, {
            transactionId,
            userId,
            type: "abrir_sobre",
            details: {
                packId,
                costType,
                costAmount,
                cardIds: cardsResult.map((c) => c.cardId),
                powerGained,
            },
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        // 3i. Build final response
        const response = {
            success: true,
            packId,
            cards: cardsResult,
            coinsRemaining: newCoins,
            newCollectionPower: currentPower + powerGained,
            transactionId,
        };
        // 3j. Record Idempotency atomically
        (0, idempotency_1.recordIdempotencyInTransaction)(transaction, idempotencyKey, userId, "openPack", response);
        return response;
    });
});
//# sourceMappingURL=openPack.js.map