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
exports.listCardForSale = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
// Catálogo de respaldo para cartas piloto si la BD no está poblada
const PILOT_CARDS_CATALOG = {
    LD: { name: "Luis Díaz", rarity: "mitica" },
    VJ: { name: "Vinicius Jr.", rarity: "rara" },
    EH: { name: "Erling Haaland", rarity: "comun" },
    KM: { name: "Kylian Mbappé", rarity: "poco_comun" },
    PE: { name: "Pedri González", rarity: "rara" },
    RO: { name: "Rodri Hernández", rarity: "comun" },
    LY: { name: "Lamine Yamal", rarity: "mitica" },
    JB: { name: "Jude Bellingham", rarity: "rara" },
    MS: { name: "Mohamed Salah", rarity: "poco_comun" },
    KDB: { name: "Kevin De Bruyne", rarity: "rara" },
    JM: { name: "Jamal Musiala", rarity: "comun" },
    VO: { name: "Victor Osimhen", rarity: "poco_comun" },
};
/**
 * Cloud Function Callable: listCardForSale
 * Publica una carta en el mercado entre jugadores (TDD 2.11, 5.8b).
 * ANTI-FRAUDE: RESERVA (DESCUENTA) LA CARTA AL PUBLICAR para evitar doble gasto.
 */
exports.listCardForSale = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "listCardForSale");
    const sellerUid = context.auth?.uid;
    if (!sellerUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para publicar cartas en el mercado.");
    }
    const { cardId, pricePerCard } = data;
    const quantity = Math.max(1, Math.floor(data.quantity || 1));
    if (!cardId || typeof cardId !== "string" || !cardId.trim()) {
        throw new functions.https.HttpsError("invalid-argument", "El identificador de la carta es obligatorio.");
    }
    if (typeof pricePerCard !== "number" || pricePerCard <= 0 || !Number.isInteger(pricePerCard)) {
        throw new functions.https.HttpsError("invalid-argument", "El precio por carta debe ser un número entero mayor a 0.");
    }
    // 1. Obtener nombre del vendedor
    const sellerDoc = await firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(sellerUid).get();
    const sellerDisplayName = sellerDoc.data()?.displayName || "Entrenador";
    // 2. Transacción Atómica de Reserva y Publicación (TDD 2.11)
    const userCardRef = firebase_1.db
        .collection(firebase_1.COLLECTIONS.USERS)
        .doc(sellerUid)
        .collection(firebase_1.COLLECTIONS.USER_COLLECTION)
        .doc(cardId);
    const listingRef = firebase_1.db.collection(firebase_1.COLLECTIONS.MARKET_LISTINGS).doc();
    const listingId = listingRef.id;
    const result = await firebase_1.db.runTransaction(async (transaction) => {
        // Revalidar inventario in-situ
        const userCardSnap = await transaction.get(userCardRef);
        if (!userCardSnap.exists) {
            throw new functions.https.HttpsError("failed-precondition", "No posees esta carta en tu colección para poder publicarla.");
        }
        const cardData = userCardSnap.data() || {};
        const currentQty = cardData.quantity || 0;
        if (currentQty < quantity) {
            throw new functions.https.HttpsError("failed-precondition", `No tienes suficientes copias de esta carta (Tienes: ${currentQty}, Solicitadas: ${quantity}).`);
        }
        // Obtener metadatos de la carta
        let cardName = cardData.name || PILOT_CARDS_CATALOG[cardId]?.name || `Carta ${cardId}`;
        let rarity = cardData.rarity || PILOT_CARDS_CATALOG[cardId]?.rarity || "comun";
        // RESERVA ATÓMICA: Descontar carta de userCollection (TDD 2.11)
        if (currentQty === quantity) {
            transaction.delete(userCardRef);
        }
        else {
            transaction.update(userCardRef, {
                quantity: firebase_1.FieldValue.increment(-quantity),
            });
        }
        // Crear documento en marketListings con status 'activo'
        const newListing = {
            listingId,
            sellerUid,
            sellerDisplayName,
            cardId,
            cardName,
            rarity,
            quantity,
            pricePerCard,
            status: "activo",
            buyerUid: null,
            buyerDisplayName: null,
            createdAt: firebase_1.FieldValue.serverTimestamp(),
        };
        transaction.set(listingRef, newListing);
        return {
            cardName,
            rarity,
        };
    });
    console.log(`[listCardForSale] Listado ${listingId} creado por ${sellerUid}: ${quantity}x ${result.cardName} a ${pricePerCard} monedas (CARTA RESERVADA).`);
    return {
        success: true,
        listingId,
        cardId,
        cardName: result.cardName,
        rarity: result.rarity,
        quantity,
        pricePerCard,
        message: `¡Publicaste ${result.cardName} en el mercado por ${pricePerCard} monedas!`,
    };
});
//# sourceMappingURL=listCardForSale.js.map