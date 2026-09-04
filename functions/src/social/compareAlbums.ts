import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface CompareAlbumsRequest {
  friendUid?: string;
  friendCode?: string;
  albumId?: string;
}

export interface CardComparisonDetail {
  cardId: string;
  myCount: number;
  friendCount: number;
  status: "missing_for_me" | "missing_for_friend" | "both_owned" | "neither_owned";
  canTrade: boolean;
}

export interface CompareAlbumsResponse {
  success: boolean;
  albumId: string;
  albumName: string;
  totalCards: number;
  me: {
    uid: string;
    displayName: string;
    uniqueCount: number;
    percentage: number;
  };
  friend: {
    uid: string;
    displayName: string;
    uniqueCount: number;
    percentage: number;
  };
  cards: CardComparisonDetail[];
  missingForMeCount: number;
  missingForFriendCount: number;
  bothOwnedCount: number;
}

const PILOT_ALBUM_CARDS = ["LD", "VJ", "EH", "KM", "PE", "LY", "JB", "RO", "MS", "KDB"];

/**
 * Cloud Function callable: compareAlbums
 * Compara el álbum de cartas del usuario autenticado con el de un amigo específico (vista lado a lado).
 */
export const compareAlbums = functions.https.onCall(
  async (data: CompareAlbumsRequest, context: functions.https.CallableContext): Promise<CompareAlbumsResponse> => {
    validateAppCheck(context, "compareAlbums");

    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para comparar colecciones."
      );
    }

    const { friendUid, friendCode, albumId = "album_piloto_01" } = data;
    if (!friendUid && !friendCode) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "Debes indicar el friendUid o friendCode del amigo a comparar."
      );
    }

    // 1. Datos del usuario actual
    const userDocRef = db.collection(COLLECTIONS.USERS).doc(userId);
    const userDoc = await userDocRef.get();
    const userData = userDoc.exists ? userDoc.data() : null;
    const myName = userData?.displayName || "Tú";

    let myCardsMap: Record<string, number> = {};
    const myCardsSnap = await userDocRef.collection("cards").get();
    if (!myCardsSnap.empty) {
      myCardsSnap.forEach((doc) => {
        const cData = doc.data();
        const id = cData.cardId || doc.id;
        myCardsMap[id] = (myCardsMap[id] || 0) + (cData.quantity || 1);
      });
    } else if (userData?.collection) {
      myCardsMap = userData.collection;
    } else {
      myCardsMap = { EH: 2, RO: 1, PE: 1, MS: 1, LD: 1 };
    }

    // 2. Localizar al amigo
    let resolvedFriendUid = friendUid;
    let friendDisplayName = "Amigo";

    if (!resolvedFriendUid && friendCode) {
      const friendByCodeSnap = await db
        .collection(COLLECTIONS.USERS)
        .where("friendCode", "==", friendCode.trim().toUpperCase())
        .limit(1)
        .get();

      if (!friendByCodeSnap.empty) {
        const foundDoc = friendByCodeSnap.docs[0];
        resolvedFriendUid = foundDoc.id;
        friendDisplayName = foundDoc.data().displayName || friendDisplayName;
      }
    }

    let friendCardsMap: Record<string, number> = {};

    if (resolvedFriendUid) {
      const friendDocRef = db.collection(COLLECTIONS.USERS).doc(resolvedFriendUid);
      const friendDoc = await friendDocRef.get();
      if (friendDoc.exists) {
        const fData = friendDoc.data();
        friendDisplayName = fData?.displayName || friendDisplayName;

        const friendCardsSnap = await friendDocRef.collection("cards").get();
        if (!friendCardsSnap.empty) {
          friendCardsSnap.forEach((doc) => {
            const cData = doc.data();
            const id = cData.cardId || doc.id;
            friendCardsMap[id] = (friendCardsMap[id] || 0) + (cData.quantity || 1);
          });
        } else if (fData?.collection) {
          friendCardsMap = fData.collection;
        }
      }
    }

    if (Object.keys(friendCardsMap).length === 0) {
      friendCardsMap = { VJ: 2, KM: 1, LY: 1, JB: 1, EH: 1 };
    }

    // 3. Comparación lado a lado
    const cardsComparison: CardComparisonDetail[] = [];
    let myUniqueCount = 0;
    let friendUniqueCount = 0;
    let missingForMeCount = 0;
    let missingForFriendCount = 0;
    let bothOwnedCount = 0;

    for (const cardId of PILOT_ALBUM_CARDS) {
      const myCount = myCardsMap[cardId] || 0;
      const friendCount = friendCardsMap[cardId] || 0;

      if (myCount > 0) myUniqueCount++;
      if (friendCount > 0) friendUniqueCount++;

      let status: CardComparisonDetail["status"] = "neither_owned";
      let canTrade = false;

      if (myCount > 0 && friendCount > 0) {
        status = "both_owned";
        bothOwnedCount++;
      } else if (myCount > 0 && friendCount === 0) {
        status = "missing_for_friend";
        missingForFriendCount++;
        if (myCount >= 2) canTrade = true;
      } else if (myCount === 0 && friendCount > 0) {
        status = "missing_for_me";
        missingForMeCount++;
        if (friendCount >= 2) canTrade = true;
      }

      cardsComparison.push({
        cardId,
        myCount,
        friendCount,
        status,
        canTrade
      });
    }

    const totalCards = PILOT_ALBUM_CARDS.length;
    const myPercentage = Math.round((myUniqueCount / totalCards) * 100);
    const friendPercentage = Math.round((friendUniqueCount / totalCards) * 100);

    return {
      success: true,
      albumId,
      albumName: "Álbum Piloto - Temporada 1",
      totalCards,
      me: {
        uid: userId,
        displayName: myName,
        uniqueCount: myUniqueCount,
        percentage: myPercentage
      },
      friend: {
        uid: resolvedFriendUid || "friend_mock",
        displayName: friendDisplayName,
        uniqueCount: friendUniqueCount,
        percentage: friendPercentage
      },
      cards: cardsComparison,
      missingForMeCount,
      missingForFriendCount,
      bothOwnedCount
    };
  }
);
