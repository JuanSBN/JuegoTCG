import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { getCachedIdempotentResult, recordIdempotencyInTransaction } from "../utils/idempotency";
import { validateAppCheck } from "../utils/appCheck";
import {
  generatePackCards,
  CardCatalogEntry,
  DEFAULT_RARITY_WEIGHTS,
  RarityWeights,
  RARITY_POWER_POINTS,
  CardRarity,
} from "./generateCardsRNG";

export interface OpenPackRequest {
  packId: string;
  idempotencyKey: string;
}

export interface CardResultItem extends CardCatalogEntry {
  isNew: boolean;
  quantityAfter: number;
}

export interface OpenPackResponse {
  success: boolean;
  packId: string;
  cards: CardResultItem[];
  coinsRemaining: number;
  newCollectionPower: number;
  transactionId: string;
}

/**
 * Cloud Function: openPack
 * Executes server-side authoritative pack opening, weighted RNG, atomic deduction,
 * inventory crediting and idempotency caching.
 */
export const openPack = functions.https.onCall(
  async (data: OpenPackRequest, context: functions.https.CallableContext): Promise<OpenPackResponse> => {
    // 0. Verify App Check (TDD 2.7)
    validateAppCheck(context, "openPack");

    // 1. Verify Authentication
    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para abrir sobres."
      );
    }

    const { packId, idempotencyKey } = data;
    if (!packId || !idempotencyKey) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "Los parámetros 'packId' y 'idempotencyKey' son obligatorios."
      );
    }

    // 2. Check Idempotency (Fast path from cache before acquiring database locks)
    const cachedRecord = await getCachedIdempotentResult<OpenPackResponse>(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
      console.log(`[openPack] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
      return cachedRecord.result;
    }

    // 3. Execute in Atomic Firestore Transaction
    return await db.runTransaction(async (transaction) => {
      // 3a. Re-verify idempotency inside the active transaction
      const processedRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
      const processedDoc = await transaction.get(processedRef);
      if (processedDoc.exists) {
        return processedDoc.data()?.result as OpenPackResponse;
      }

      // 3b. Read user data
      const userRef = db.collection(COLLECTIONS.USERS).doc(userId);
      const userDoc = await transaction.get(userRef);

      if (!userDoc.exists) {
        throw new functions.https.HttpsError(
          "not-found",
          "El perfil del usuario no existe en la base de datos."
        );
      }

      const userData = userDoc.data() || {};
      const currentCoins = userData.coins ?? 0;
      let currentPower = userData.collectionPower ?? 0;
      const userPacks = userData.availablePacks || {};
      const packsCount = userPacks[packId] ?? 0;

      // 3c. Read pack definition
      const packRef = db.collection(COLLECTIONS.PACKS).doc(packId);
      const packDoc = await transaction.get(packRef);

      let costAmount = 0;
      let costType = "sobres_disponibles";
      let weights: RarityWeights = DEFAULT_RARITY_WEIGHTS;
      let albumId = "album_piloto_01";

      if (packDoc.exists) {
        const packData = packDoc.data() || {};
        costAmount = packData.costAmount || 0;
        costType = packData.costType || "moneda";
        weights = packData.rarityWeights || DEFAULT_RARITY_WEIGHTS;
        albumId = packData.albumId || "album_piloto_01";
      }

      // Validate cost/availability
      if (costType === "moneda") {
        if (costAmount > 0 && currentCoins < costAmount) {
          throw new functions.https.HttpsError(
            "failed-precondition",
            `Monedas insuficientes. Necesitas ${costAmount} monedas.`
          );
        }
      } else {
        if (packsCount <= 0 && currentCoins < 100) {
          throw new functions.https.HttpsError(
            "failed-precondition",
            "No tienes sobres de este tipo disponibles."
          );
        }
      }

      // 3d. Fetch cards catalog for this album
      const catalogQuery = await db
        .collection(COLLECTIONS.CARDS_CATALOG)
        .where("albumId", "==", albumId)
        .get();

      let catalog: CardCatalogEntry[] = [];
      if (!catalogQuery.empty) {
        catalog = catalogQuery.docs.map((d: any) => ({
          cardId: d.id,
          ...(d.data() as Omit<CardCatalogEntry, "cardId">),
        }));
      } else {
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
      const rolledCards = generatePackCards(catalog, 5, weights);

      // 3f. Update user's collection & calculate power
      const cardsResult: CardResultItem[] = [];
      let powerGained = 0;

      for (const card of rolledCards) {
        const cardRef = db
          .collection(COLLECTIONS.USERS)
          .doc(userId)
          .collection(COLLECTIONS.USER_COLLECTION)
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
            dateObtained: FieldValue.serverTimestamp(),
          });
          powerGained += RARITY_POWER_POINTS[card.rarity as CardRarity] || 1;
        } else {
          transaction.update(cardRef, {
            quantity: FieldValue.increment(1),
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
      const userUpdates: any = {
        collectionPower: currentPower + powerGained,
        packsOpenedTotal: FieldValue.increment(1),
        lastPackOpenedAt: FieldValue.serverTimestamp(),
      };

      if (costType === "moneda" && costAmount > 0) {
        newCoins = currentCoins - costAmount;
        userUpdates.coins = newCoins;
      } else if (packsCount > 0) {
        userUpdates[`availablePacks.${packId}`] = FieldValue.increment(-1);
      }

      transaction.update(userRef, userUpdates);

      // 3h. Register audit transaction log
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
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
        timestamp: FieldValue.serverTimestamp(),
      });

      // 3i. Build final response
      const response: OpenPackResponse = {
        success: true,
        packId,
        cards: cardsResult,
        coinsRemaining: newCoins,
        newCollectionPower: currentPower + powerGained,
        transactionId,
      };

      // 3j. Record Idempotency atomically
      recordIdempotencyInTransaction(
        transaction,
        idempotencyKey,
        userId,
        "openPack",
        response
      );

      return response;
    });
  }
);
