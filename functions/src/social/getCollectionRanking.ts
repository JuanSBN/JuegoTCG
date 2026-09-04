import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface RankingPlayerItem {
  rank: number;
  uid: string;
  displayName: string;
  photoUrl: string;
  collectionPower: number;
  level: number;
  isMe: boolean;
}

export interface GetCollectionRankingRequest {
  mode?: "friends" | "global";
  limit?: number;
}

export interface GetCollectionRankingResponse {
  success: boolean;
  mode: "friends" | "global";
  myRank: number;
  myPower: number;
  items: RankingPlayerItem[];
}

/**
 * Cloud Function Callable: getCollectionRanking
 * Consulta el ranking por Poder de Colección (GDD Sección 7.2)
 * utilizando el campo cacheado 'collectionPower' en la colección 'users'.
 */
export const getCollectionRanking = functions.https.onCall(
  async (data: GetCollectionRankingRequest, context: functions.https.CallableContext): Promise<GetCollectionRankingResponse> => {
    validateAppCheck(context, "getCollectionRanking");

    const authUid = context.auth?.uid || "anonymous_user";
    const mode = data?.mode || "global";
    const maxLimit = Math.min(Math.max(data?.limit || 20, 1), 100);

    let rankingItems: RankingPlayerItem[] = [];

    if (mode === "friends" && authUid !== "anonymous_user") {
      // 1. Ranking de Amigos: obtener IDs de amigos confirmados
      const friendsSnap = await db
        .collection(COLLECTIONS.USERS)
        .doc(authUid)
        .collection("friends")
        .get();

      const friendUids = new Set<string>([authUid]);
      friendsSnap.forEach((doc) => {
        friendUids.add(doc.id);
      });

      // Consultar perfiles de amigos
      const userDocs = await Promise.all(
        Array.from(friendUids).map((fUid) => db.collection(COLLECTIONS.USERS).doc(fUid).get())
      );

      const list: Array<{ uid: string; displayName: string; photoUrl: string; collectionPower: number; level: number }> = [];
      userDocs.forEach((docSnap) => {
        if (docSnap.exists) {
          const uData = docSnap.data()!;
          list.push({
            uid: docSnap.id,
            displayName: uData.displayName || "Entrenador",
            photoUrl: uData.photoUrl || "",
            collectionPower: uData.collectionPower || 0,
            level: uData.level || 1,
          });
        }
      });

      // Ordenar por collectionPower descendente
      list.sort((a, b) => b.collectionPower - a.collectionPower);

      rankingItems = list.slice(0, maxLimit).map((item, idx) => ({
        rank: idx + 1,
        uid: item.uid,
        displayName: item.displayName,
        photoUrl: item.photoUrl,
        collectionPower: item.collectionPower,
        level: item.level,
        isMe: item.uid === authUid,
      }));
    } else {
      // 2. Ranking Global
      const usersSnap = await db
        .collection(COLLECTIONS.USERS)
        .orderBy("collectionPower", "desc")
        .limit(maxLimit)
        .get();

      let currentRank = 1;
      usersSnap.forEach((docSnap) => {
        const uData = docSnap.data();
        rankingItems.push({
          rank: currentRank++,
          uid: docSnap.id,
          displayName: uData.displayName || "Entrenador",
          photoUrl: uData.photoUrl || "",
          collectionPower: uData.collectionPower || 0,
          level: uData.level || 1,
          isMe: docSnap.id === authUid,
        });
      });
    }

    // Fallback con datos de demostración si la base de datos está vacía
    if (rankingItems.length === 0) {
      rankingItems = [
        { rank: 1, uid: "friend_gs", displayName: "GoldenShot_7", photoUrl: "", collectionPower: 9120, level: 24, isMe: false },
        { rank: 2, uid: "friend_ec", displayName: "ElChampion", photoUrl: "", collectionPower: 6840, level: 18, isMe: false },
        { rank: 3, uid: authUid, displayName: "Tú", photoUrl: "", collectionPower: 5430, level: 15, isMe: true },
        { rank: 4, uid: "friend_ma", displayName: "MiAmigo_01", photoUrl: "", collectionPower: 4250, level: 12, isMe: false },
        { rank: 5, uid: "friend_ff", displayName: "FutbolFan_22", photoUrl: "", collectionPower: 2180, level: 8, isMe: false },
      ];
    }

    const myItem = rankingItems.find((x) => x.isMe);
    const myRank = myItem ? myItem.rank : 3;
    const myPower = myItem ? myItem.collectionPower : 5430;

    return {
      success: true,
      mode,
      myRank,
      myPower,
      items: rankingItems,
    };
  }
);
