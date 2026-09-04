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
exports.updateListingPrice = exports.cancelListing = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Cloud Function Callable: cancelListing
 * Cancela una publicación activa en el mercado y reintegra la carta reservada al vendedor (TDD 2.11).
 * RESTRICCIÓN: Exclusiva para el vendedor original (sellerUid === callerUid).
 */
exports.cancelListing = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "cancelListing");
    const callerUid = context.auth?.uid;
    if (!callerUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para cancelar una publicación.");
    }
    const { listingId } = data;
    if (!listingId || typeof listingId !== "string" || !listingId.trim()) {
        throw new functions.https.HttpsError("invalid-argument", "El identificador del listado es obligatorio.");
    }
    const listingRef = firebase_1.db.collection(firebase_1.COLLECTIONS.MARKET_LISTINGS).doc(listingId);
    return await firebase_1.db.runTransaction(async (transaction) => {
        const listingSnap = await transaction.get(listingRef);
        if (!listingSnap.exists) {
            throw new functions.https.HttpsError("not-found", "El listado de mercado no existe.");
        }
        const listing = listingSnap.data();
        // 1. Restringido estrictamente al vendedor original
        if (listing.sellerUid !== callerUid) {
            throw new functions.https.HttpsError("permission-denied", "Solo el vendedor que publicó la carta puede cancelarla.");
        }
        // 2. Revalidar que siga activo
        if (listing.status !== "activo") {
            throw new functions.https.HttpsError("failed-precondition", `No se puede cancelar el listado porque su estado actual es '${listing.status}'.`);
        }
        const cardId = listing.cardId;
        const cardName = listing.cardName || `Carta ${cardId}`;
        const rarity = listing.rarity || "comun";
        const quantity = Math.max(1, listing.quantity || 1);
        // 3. Reintegro atómico de la carta al inventario del vendedor
        const sellerCardRef = firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
            .doc(callerUid)
            .collection(firebase_1.COLLECTIONS.USER_COLLECTION)
            .doc(cardId);
        const sellerCardSnap = await transaction.get(sellerCardRef);
        if (sellerCardSnap.exists) {
            transaction.update(sellerCardRef, {
                quantity: firebase_1.FieldValue.increment(quantity),
            });
        }
        else {
            transaction.set(sellerCardRef, {
                cardId,
                name: cardName,
                rarity,
                quantity,
                dateObtained: firebase_1.FieldValue.serverTimestamp(),
            });
        }
        // 4. Actualizar estado del listado a 'cancelado'
        transaction.update(listingRef, {
            status: "cancelado",
            closedAt: firebase_1.FieldValue.serverTimestamp(),
        });
        console.log(`[cancelListing] Listado ${listingId} cancelado por ${callerUid}. ${quantity}x ${cardName} reintegradas al inventario.`);
        return {
            success: true,
            listingId,
            cardId,
            cardName,
            quantityRestored: quantity,
            message: `Has retirado ${cardName} del mercado. La carta ha sido devuelta a tu colección.`,
        };
    });
});
/**
 * Cloud Function Callable: updateListingPrice
 * Modifica el precio por carta de una publicación activa (TDD 2.11).
 * RESTRICCIÓN: Exclusiva para el vendedor original (sellerUid === callerUid).
 */
exports.updateListingPrice = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "updateListingPrice");
    const callerUid = context.auth?.uid;
    if (!callerUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para modificar el precio de una publicación.");
    }
    const { listingId, newPricePerCard } = data;
    if (!listingId || typeof listingId !== "string" || !listingId.trim()) {
        throw new functions.https.HttpsError("invalid-argument", "El identificador del listado es obligatorio.");
    }
    if (typeof newPricePerCard !== "number" ||
        newPricePerCard <= 0 ||
        !Number.isInteger(newPricePerCard)) {
        throw new functions.https.HttpsError("invalid-argument", "El nuevo precio debe ser un número entero mayor a 0.");
    }
    const listingRef = firebase_1.db.collection(firebase_1.COLLECTIONS.MARKET_LISTINGS).doc(listingId);
    return await firebase_1.db.runTransaction(async (transaction) => {
        const listingSnap = await transaction.get(listingRef);
        if (!listingSnap.exists) {
            throw new functions.https.HttpsError("not-found", "El listado de mercado no existe.");
        }
        const listing = listingSnap.data();
        // 1. Restringido estrictamente al vendedor original
        if (listing.sellerUid !== callerUid) {
            throw new functions.https.HttpsError("permission-denied", "Solo el vendedor que publicó la carta puede modificar su precio.");
        }
        // 2. Revalidar que siga activo
        if (listing.status !== "activo") {
            throw new functions.https.HttpsError("failed-precondition", `No se puede modificar el precio porque el listado está '${listing.status}'.`);
        }
        const oldPrice = listing.pricePerCard;
        // 3. Actualizar precio
        transaction.update(listingRef, {
            pricePerCard: newPricePerCard,
            updatedAt: firebase_1.FieldValue.serverTimestamp(),
        });
        console.log(`[updateListingPrice] Precio del listado ${listingId} actualizado de ${oldPrice} a ${newPricePerCard} monedas por ${callerUid}.`);
        return {
            success: true,
            listingId,
            cardId: listing.cardId,
            oldPrice,
            newPrice: newPricePerCard,
            message: `El precio ha sido actualizado a ${newPricePerCard} monedas.`,
        };
    });
});
//# sourceMappingURL=manageListing.js.map