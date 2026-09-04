import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

/**
 * Tabla oficial de puntos por rareza para el Poder de Colección (GDD Sección 7.2)
 * Comun: 1
 * Especial / Poco común: 2
 * Épica / Rara: 4
 * Legendaria: 8
 * Mítica: 15
 * Full Art: 25
 */
export const RARITY_POWER_POINTS_TABLE: Record<string, number> = {
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
export async function calculateUserCollectionPower(userId: string): Promise<{ totalPower: number; uniqueCount: number }> {
  const cardsSnap = await db.collection(COLLECTIONS.USERS).doc(userId).collection("cards").get();

  let totalPower = 0;
  let uniqueCount = 0;

  cardsSnap.forEach((doc) => {
    const card = doc.data();
    const qty = card.quantity || 0;
    if (qty > 0) {
      uniqueCount++;
      const rarityKey = (card.rarity || "comun").toString().toLowerCase();
      const points = RARITY_POWER_POINTS_TABLE[rarityKey] || 1;
      totalPower += points;
    }
  });

  return { totalPower, uniqueCount };
}

/**
 * Actualiza el campo cacheado 'collectionPower' en el documento de usuario en users/{userId}
 * para evitar lecturas masivas en consultas de rankings y listas de amigos (TDD Sección 6).
 */
export async function updateCachedCollectionPower(userId: string): Promise<number> {
  const { totalPower, uniqueCount } = await calculateUserCollectionPower(userId);

  await db.collection(COLLECTIONS.USERS).doc(userId).set(
    {
      collectionPower: totalPower,
      uniqueCardsCount: uniqueCount,
      lastPowerRecalculatedAt: FieldValue.serverTimestamp(),
    },
    { merge: true }
  );

  return totalPower;
}

/**
 * Trigger de Firestore (TDD Sección 6):
 * Se dispara automáticamente cuando cambia userCollection de un jugador
 * (nueva carta obtenida por openPack o por comprar en el mercado) y actualiza collectionPower.
 */
export const recalculateCollectionPowerTrigger = functions.firestore
  .document(`${COLLECTIONS.USERS}/{userId}/cards/{cardId}`)
  .onWrite(async (change, context) => {
    const userId = context.params.userId;
    if (!userId) return null;

    try {
      const newPower = await updateCachedCollectionPower(userId);
      console.log(`[Trigger] collectionPower recalculado para usuario ${userId}: ${newPower} pts`);
      return { success: true, userId, newPower };
    } catch (error) {
      console.error(`[Trigger] Error recalculando collectionPower para ${userId}:`, error);
      return null;
    }
  });

/**
 * Cloud Function Callable: recalculateCollectionPower
 * Permite al cliente solicitar manualmente un recálculo o sincronización del poder de colección.
 */
export const recalculateCollectionPower = functions.https.onCall(
  async (data: { targetUserId?: string }, context: functions.https.CallableContext) => {
    validateAppCheck(context, "recalculateCollectionPower");

    const authUid = context.auth?.uid;
    if (!authUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para recalcular el poder de colección."
      );
    }

    const userId = data?.targetUserId || authUid;
    const newPower = await updateCachedCollectionPower(userId);

    return {
      success: true,
      userId,
      collectionPower: newPower,
    };
  }
);
