import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface FriendEntry {
  friendUid: string;
  displayName: string;
  photoUrl: string;
  friendCode: string;
  level: number;
  collectionPower: number;
  albumProgress: number;
  addedAt?: string;
}

export interface FriendRequestEntry {
  requestId: string;
  fromUid: string;
  fromName: string;
  fromPhotoUrl: string;
  fromCode: string;
  createdAt?: string;
}

export interface SocialDataResponse {
  friendCode: string;
  friendCount: number;
  pendingRequestsCount: number;
  friends: FriendEntry[];
  pendingRequests: FriendRequestEntry[];
}

/**
 * Genera un código de amigo único de 8 caracteres en formato FC-XXXX o XXXX-XXXX (ej: FC-8294)
 */
export function generateRandomFriendCode(): string {
  const chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Sin 0/O ni 1/I para evitar confusiones visuales
  let code = "FC-";
  for (let i = 0; i < 4; i++) {
    code += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return code;
}

/**
 * Cloud Function: getSocialData
 * Recupera el código de amigo del usuario (generándolo si es nuevo), su lista de amigos confirmados
 * y las solicitudes de amistad pendientes recibidas.
 */
export const getSocialData = functions.https.onCall(
  async (data: any, context: functions.https.CallableContext): Promise<SocialDataResponse> => {
    validateAppCheck(context, "getSocialData");

    const userId = context.auth?.uid;
    if (!userId) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para consultar datos sociales."
      );
    }

    const userRef = db.collection(COLLECTIONS.USERS).doc(userId);
    const userDoc = await userRef.get();
    let userData = userDoc.data() || {};

    // Si el usuario no tiene friendCode aún, generarlo de forma única
    let friendCode = userData.friendCode;
    if (!friendCode) {
      let unique = false;
      while (!unique) {
        const candidate = generateRandomFriendCode();
        const existing = await db
          .collection(COLLECTIONS.USERS)
          .where("friendCode", "==", candidate)
          .limit(1)
          .get();

        if (existing.empty) {
          friendCode = candidate;
          unique = true;
        }
      }

      await userRef.set(
        {
          friendCode,
          friendCount: userData.friendCount || 0,
          pendingRequestsCount: userData.pendingRequestsCount || 0,
          updatedAt: FieldValue.serverTimestamp(),
        },
        { merge: true }
      );
    }

    // 1. Obtener lista de amigos
    const friendsSnap = await userRef.collection(COLLECTIONS.FRIENDS).get();
    const friends: FriendEntry[] = friendsSnap.docs.map((doc) => {
      const d = doc.data();
      return {
        friendUid: d.friendUid || doc.id,
        displayName: d.displayName || "Entrenador",
        photoUrl: d.photoUrl || "",
        friendCode: d.friendCode || "",
        level: d.playerLevel || d.level || 1,
        collectionPower: d.collectionPower || 0,
        albumProgress: d.albumProgress || 0,
        addedAt: d.addedAt ? d.addedAt.toDate().toISOString() : undefined,
      };
    });

    // 2. Obtener solicitudes pendientes recibidas
    const requestsSnap = await db
      .collection(COLLECTIONS.FRIEND_REQUESTS)
      .where("toUid", "==", userId)
      .where("status", "==", "pending")
      .orderBy("createdAt", "desc")
      .get();

    const pendingRequests: FriendRequestEntry[] = requestsSnap.docs.map((doc) => {
      const d = doc.data();
      return {
        requestId: doc.id,
        fromUid: d.fromUid,
        fromName: d.fromName || "Entrenador",
        fromPhotoUrl: d.fromPhotoUrl || "",
        fromCode: d.fromCode || "",
        createdAt: d.createdAt ? d.createdAt.toDate().toISOString() : undefined,
      };
    });

    return {
      friendCode,
      friendCount: friends.length,
      pendingRequestsCount: pendingRequests.length,
      friends,
      pendingRequests,
    };
  }
);
