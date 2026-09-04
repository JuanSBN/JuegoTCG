import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface CancelListingRequest {
  listingId: string;
}

export interface CancelListingResponse {
  success: boolean;
  listingId: string;
  cardId: string;
  cardName: string;
  quantityRestored: number;
  message: string;
}

export interface UpdateListingPriceRequest {
  listingId: string;
  newPricePerCard: number;
}

export interface UpdateListingPriceResponse {
  success: boolean;
  listingId: string;
  cardId: string;
  oldPrice: number;
  newPrice: number;
  message: string;
}

/**
 * Cloud Function Callable: cancelListing
 * Cancela una publicación activa en el mercado y reintegra la carta reservada al vendedor (TDD 2.11).
 * RESTRICCIÓN: Exclusiva para el vendedor original (sellerUid === callerUid).
 */
export const cancelListing = functions.https.onCall(
  async (data: CancelListingRequest, context: functions.https.CallableContext): Promise<CancelListingResponse> => {
    validateAppCheck(context, "cancelListing");

    const callerUid = context.auth?.uid;
    if (!callerUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para cancelar una publicación."
      );
    }

    const { listingId } = data;
    if (!listingId || typeof listingId !== "string" || !listingId.trim()) {
      throw new functions.https.HttpsError("invalid-argument", "El identificador del listado es obligatorio.");
    }

    const listingRef = db.collection(COLLECTIONS.MARKET_LISTINGS).doc(listingId);

    return await db.runTransaction(async (transaction) => {
      const listingSnap = await transaction.get(listingRef);
      if (!listingSnap.exists) {
        throw new functions.https.HttpsError("not-found", "El listado de mercado no existe.");
      }

      const listing = listingSnap.data()!;

      // 1. Restringido estrictamente al vendedor original
      if (listing.sellerUid !== callerUid) {
        throw new functions.https.HttpsError(
          "permission-denied",
          "Solo el vendedor que publicó la carta puede cancelarla."
        );
      }

      // 2. Revalidar que siga activo
      if (listing.status !== "activo") {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `No se puede cancelar el listado porque su estado actual es '${listing.status}'.`
        );
      }

      const cardId = listing.cardId;
      const cardName = listing.cardName || `Carta ${cardId}`;
      const rarity = listing.rarity || "comun";
      const quantity = Math.max(1, listing.quantity || 1);

      // 3. Reintegro atómico de la carta al inventario del vendedor
      const sellerCardRef = db
        .collection(COLLECTIONS.USERS)
        .doc(callerUid)
        .collection(COLLECTIONS.USER_COLLECTION)
        .doc(cardId);

      const sellerCardSnap = await transaction.get(sellerCardRef);
      if (sellerCardSnap.exists) {
        transaction.update(sellerCardRef, {
          quantity: FieldValue.increment(quantity),
        });
      } else {
        transaction.set(sellerCardRef, {
          cardId,
          name: cardName,
          rarity,
          quantity,
          dateObtained: FieldValue.serverTimestamp(),
        });
      }

      // 4. Actualizar estado del listado a 'cancelado'
      transaction.update(listingRef, {
        status: "cancelado",
        closedAt: FieldValue.serverTimestamp(),
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
  }
);

/**
 * Cloud Function Callable: updateListingPrice
 * Modifica el precio por carta de una publicación activa (TDD 2.11).
 * RESTRICCIÓN: Exclusiva para el vendedor original (sellerUid === callerUid).
 */
export const updateListingPrice = functions.https.onCall(
  async (data: UpdateListingPriceRequest, context: functions.https.CallableContext): Promise<UpdateListingPriceResponse> => {
    validateAppCheck(context, "updateListingPrice");

    const callerUid = context.auth?.uid;
    if (!callerUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para modificar el precio de una publicación."
      );
    }

    const { listingId, newPricePerCard } = data;
    if (!listingId || typeof listingId !== "string" || !listingId.trim()) {
      throw new functions.https.HttpsError("invalid-argument", "El identificador del listado es obligatorio.");
    }

    if (
      typeof newPricePerCard !== "number" ||
      newPricePerCard <= 0 ||
      !Number.isInteger(newPricePerCard)
    ) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El nuevo precio debe ser un número entero mayor a 0."
      );
    }

    const listingRef = db.collection(COLLECTIONS.MARKET_LISTINGS).doc(listingId);

    return await db.runTransaction(async (transaction) => {
      const listingSnap = await transaction.get(listingRef);
      if (!listingSnap.exists) {
        throw new functions.https.HttpsError("not-found", "El listado de mercado no existe.");
      }

      const listing = listingSnap.data()!;

      // 1. Restringido estrictamente al vendedor original
      if (listing.sellerUid !== callerUid) {
        throw new functions.https.HttpsError(
          "permission-denied",
          "Solo el vendedor que publicó la carta puede modificar su precio."
        );
      }

      // 2. Revalidar que siga activo
      if (listing.status !== "activo") {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `No se puede modificar el precio porque el listado está '${listing.status}'.`
        );
      }

      const oldPrice = listing.pricePerCard;

      // 3. Actualizar precio
      transaction.update(listingRef, {
        pricePerCard: newPricePerCard,
        updatedAt: FieldValue.serverTimestamp(),
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
  }
);
