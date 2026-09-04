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
exports.buyListedCard = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Cloud Function Callable: buyListedCard
 * Compra una carta listada en el mercado entre jugadores (TDD 2.11, GDD 7.1).
 * TRANSACCIÓN ATÓMICA DE FIRESTORE:
 * 1. Revalida que el listado siga con status 'activo'.
 * 2. Bloquea auto-compra (comprador != vendedor).
 * 3. Revalida saldo suficiente de monedas del comprador.
 * 4. Descuenta monedas del comprador.
 * 5. Acredita el 100% de las monedas al vendedor (sin comisión del estudio, GDD 7.1).
 * 6. Transfiere la carta reservada a userCollection del comprador.
 * 7. Marca el listado como 'vendido'.
 * 8. Registra auditoría en /transactions.
 */
exports.buyListedCard = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "buyListedCard");
    const buyerUid = context.auth?.uid;
    if (!buyerUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para comprar cartas en el mercado.");
    }
    const { listingId } = data;
    if (!listingId || typeof listingId !== "string" || !listingId.trim()) {
        throw new functions.https.HttpsError("invalid-argument", "El identificador del listado es obligatorio.");
    }
    const listingRef = firebase_1.db.collection(firebase_1.COLLECTIONS.MARKET_LISTINGS).doc(listingId);
    const buyerUserRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(buyerUid);
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 1. Leer listado
        const listingSnap = await transaction.get(listingRef);
        if (!listingSnap.exists) {
            throw new functions.https.HttpsError("not-found", "El listado de mercado no existe.");
        }
        const listing = listingSnap.data();
        // 2. Revalidar status activo
        if (listing.status !== "activo") {
            throw new functions.https.HttpsError("failed-precondition", `El listado ya no está disponible para compra (Estado actual: ${listing.status}).`);
        }
        const sellerUid = listing.sellerUid;
        const cardId = listing.cardId;
        const cardName = listing.cardName || `Carta ${cardId}`;
        const rarity = listing.rarity || "comun";
        const quantity = Math.max(1, listing.quantity || 1);
        const pricePerCard = listing.pricePerCard;
        const totalPrice = pricePerCard * quantity;
        // 3. Bloquear auto-compra
        if (buyerUid === sellerUid) {
            throw new functions.https.HttpsError("invalid-argument", "No puedes comprar tus propios listados en el mercado.");
        }
        // 4. Leer documento del comprador
        const buyerSnap = await transaction.get(buyerUserRef);
        if (!buyerSnap.exists) {
            throw new functions.https.HttpsError("not-found", "Perfil del comprador no encontrado.");
        }
        const buyerData = buyerSnap.data() || {};
        const buyerCoins = buyerData.coins || 0;
        const buyerDisplayName = buyerData.displayName || "Comprador";
        if (buyerCoins < totalPrice) {
            throw new functions.https.HttpsError("failed-precondition", `Monedas insuficientes. Necesitas ${totalPrice} monedas pero tienes ${buyerCoins}.`);
        }
        // 5. Leer documento del vendedor para acreditar monedas
        const sellerUserRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(sellerUid);
        const sellerSnap = await transaction.get(sellerUserRef);
        if (!sellerSnap.exists) {
            throw new functions.https.HttpsError("not-found", "Perfil del vendedor no encontrado.");
        }
        // 6. Leer inventario de cartas del comprador para transferir la carta
        const buyerCardRef = firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
            .doc(buyerUid)
            .collection(firebase_1.COLLECTIONS.USER_COLLECTION)
            .doc(cardId);
        const buyerCardSnap = await transaction.get(buyerCardRef);
        // --- MUTACIONES ATÓMICAS SIMULTÁNEAS ---
        // A. Descontar monedas del comprador
        const buyerNewCoins = buyerCoins - totalPrice;
        transaction.update(buyerUserRef, {
            coins: firebase_1.FieldValue.increment(-totalPrice),
        });
        // B. Acreditar 100% de monedas al vendedor (sin comisión del estudio, GDD 7.1)
        transaction.update(sellerUserRef, {
            coins: firebase_1.FieldValue.increment(totalPrice),
        });
        // C. Mover carta a userCollection del comprador
        if (buyerCardSnap.exists) {
            transaction.update(buyerCardRef, {
                quantity: firebase_1.FieldValue.increment(quantity),
            });
        }
        else {
            transaction.set(buyerCardRef, {
                cardId,
                name: cardName,
                rarity,
                quantity,
                dateObtained: firebase_1.FieldValue.serverTimestamp(),
            });
        }
        // D. Marcar listado como 'vendido'
        transaction.update(listingRef, {
            status: "vendido",
            buyerUid,
            buyerDisplayName,
            soldAt: firebase_1.FieldValue.serverTimestamp(),
            closedAt: firebase_1.FieldValue.serverTimestamp(),
        });
        // E. Registrar auditoría inmutable en transactions
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
        transaction.set(txRef, {
            transactionId: txRef.id,
            type: "market_purchase",
            listingId,
            sellerUid,
            buyerUid,
            cardId,
            cardName,
            rarity,
            quantity,
            pricePaid: totalPrice,
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        console.log(`[buyListedCard] ¡Listado ${listingId} comprado atómicamente por ${buyerUid}! ${quantity}x ${cardName} por ${totalPrice} monedas pagadas a ${sellerUid}.`);
        return {
            success: true,
            listingId,
            cardId,
            cardName,
            pricePaid: totalPrice,
            quantity,
            buyerNewCoins,
            message: `¡Has comprado a ${cardName} por ${totalPrice} monedas!`,
        };
    });
});
//# sourceMappingURL=buyListedCard.js.map