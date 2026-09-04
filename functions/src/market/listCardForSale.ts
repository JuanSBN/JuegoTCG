import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";
import { MarketListing } from "./marketTypes";

export interface ListCardForSaleRequest {
  cardId: string;
  pricePerCard: number;
  quantity?: number;
}

export interface ListCardForSaleResponse {
  success: boolean;
  listingId: string;
  cardId: string;
  cardName: string;
  rarity: string;
  quantity: number;
  pricePerCard: number;
  message: string;
}

// Catálogo de respaldo para cartas piloto si la BD no está poblada
const PILOT_CARDS_CATALOG: Record<string, { name: string; rarity: string }> = {
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
export const listCardForSale = functions.https.onCall(
  async (data: ListCardForSaleRequest, context: functions.https.CallableContext): Promise<ListCardForSaleResponse> => {
    validateAppCheck(context, "listCardForSale");

    const sellerUid = context.auth?.uid;
    if (!sellerUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para publicar cartas en el mercado."
      );
    }

    const { cardId, pricePerCard } = data;
    const quantity = Math.max(1, Math.floor(data.quantity || 1));

    if (!cardId || typeof cardId !== "string" || !cardId.trim()) {
      throw new functions.https.HttpsError("invalid-argument", "El identificador de la carta es obligatorio.");
    }

    if (typeof pricePerCard !== "number" || pricePerCard <= 0 || !Number.isInteger(pricePerCard)) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El precio por carta debe ser un número entero mayor a 0."
      );
    }

    // 1. Obtener nombre del vendedor
    const sellerDoc = await db.collection(COLLECTIONS.USERS).doc(sellerUid).get();
    const sellerDisplayName = sellerDoc.data()?.displayName || "Entrenador";

    // 2. Transacción Atómica de Reserva y Publicación (TDD 2.11)
    const userCardRef = db
      .collection(COLLECTIONS.USERS)
      .doc(sellerUid)
      .collection(COLLECTIONS.USER_COLLECTION)
      .doc(cardId);

    const listingRef = db.collection(COLLECTIONS.MARKET_LISTINGS).doc();
    const listingId = listingRef.id;

    const result = await db.runTransaction(async (transaction) => {
      // Revalidar inventario in-situ
      const userCardSnap = await transaction.get(userCardRef);
      if (!userCardSnap.exists) {
        throw new functions.https.HttpsError(
          "failed-precondition",
          "No posees esta carta en tu colección para poder publicarla."
        );
      }

      const cardData = userCardSnap.data() || {};
      const currentQty = cardData.quantity || 0;

      if (currentQty < quantity) {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `No tienes suficientes copias de esta carta (Tienes: ${currentQty}, Solicitadas: ${quantity}).`
        );
      }

      // Obtener metadatos de la carta
      let cardName = cardData.name || PILOT_CARDS_CATALOG[cardId]?.name || `Carta ${cardId}`;
      let rarity = cardData.rarity || PILOT_CARDS_CATALOG[cardId]?.rarity || "comun";

      // RESERVA ATÓMICA: Descontar carta de userCollection (TDD 2.11)
      if (currentQty === quantity) {
        transaction.delete(userCardRef);
      } else {
        transaction.update(userCardRef, {
          quantity: FieldValue.increment(-quantity),
        });
      }

      // Crear documento en marketListings con status 'activo'
      const newListing: MarketListing = {
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
        createdAt: FieldValue.serverTimestamp(),
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
  }
);
