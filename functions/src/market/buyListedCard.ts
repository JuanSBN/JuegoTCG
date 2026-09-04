import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface BuyListedCardRequest {
  listingId: string;
}

export interface BuyListedCardResponse {
  success: boolean;
  listingId: string;
  cardId: string;
  cardName: string;
  pricePaid: number;
  quantity: number;
  buyerNewCoins: number;
  message: string;
}

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
export const buyListedCard = functions.https.onCall(
  async (data: BuyListedCardRequest, context: functions.https.CallableContext): Promise<BuyListedCardResponse> => {
    validateAppCheck(context, "buyListedCard");

    const buyerUid = context.auth?.uid;
    if (!buyerUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para comprar cartas en el mercado."
      );
    }

    const { listingId } = data;
    if (!listingId || typeof listingId !== "string" || !listingId.trim()) {
      throw new functions.https.HttpsError("invalid-argument", "El identificador del listado es obligatorio.");
    }

    const listingRef = db.collection(COLLECTIONS.MARKET_LISTINGS).doc(listingId);
    const buyerUserRef = db.collection(COLLECTIONS.USERS).doc(buyerUid);

    return await db.runTransaction(async (transaction) => {
      // 1. Leer listado
      const listingSnap = await transaction.get(listingRef);
      if (!listingSnap.exists) {
        throw new functions.https.HttpsError("not-found", "El listado de mercado no existe.");
      }

      const listing = listingSnap.data()!;

      // 2. Revalidar status activo
      if (listing.status !== "activo") {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `El listado ya no está disponible para compra (Estado actual: ${listing.status}).`
        );
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
        throw new functions.https.HttpsError(
          "invalid-argument",
          "No puedes comprar tus propios listados en el mercado."
        );
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
        throw new functions.https.HttpsError(
          "failed-precondition",
          `Monedas insuficientes. Necesitas ${totalPrice} monedas pero tienes ${buyerCoins}.`
        );
      }

      // 5. Leer documento del vendedor para acreditar monedas
      const sellerUserRef = db.collection(COLLECTIONS.USERS).doc(sellerUid);
      const sellerSnap = await transaction.get(sellerUserRef);
      if (!sellerSnap.exists) {
        throw new functions.https.HttpsError("not-found", "Perfil del vendedor no encontrado.");
      }

      // 6. Leer inventario de cartas del comprador para transferir la carta
      const buyerCardRef = db
        .collection(COLLECTIONS.USERS)
        .doc(buyerUid)
        .collection(COLLECTIONS.USER_COLLECTION)
        .doc(cardId);

      const buyerCardSnap = await transaction.get(buyerCardRef);

      // --- MUTACIONES ATÓMICAS SIMULTÁNEAS ---

      // A. Descontar monedas del comprador
      const buyerNewCoins = buyerCoins - totalPrice;
      transaction.update(buyerUserRef, {
        coins: FieldValue.increment(-totalPrice),
      });

      // B. Acreditar 100% de monedas al vendedor (sin comisión del estudio, GDD 7.1)
      transaction.update(sellerUserRef, {
        coins: FieldValue.increment(totalPrice),
      });

      // C. Mover carta a userCollection del comprador
      if (buyerCardSnap.exists) {
        transaction.update(buyerCardRef, {
          quantity: FieldValue.increment(quantity),
        });
      } else {
        transaction.set(buyerCardRef, {
          cardId,
          name: cardName,
          rarity,
          quantity,
          dateObtained: FieldValue.serverTimestamp(),
        });
      }

      // D. Marcar listado como 'vendido'
      transaction.update(listingRef, {
        status: "vendido",
        buyerUid,
        buyerDisplayName,
        soldAt: FieldValue.serverTimestamp(),
        closedAt: FieldValue.serverTimestamp(),
      });

      // E. Registrar auditoría inmutable en transactions
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
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
        timestamp: FieldValue.serverTimestamp(),
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
  }
);
