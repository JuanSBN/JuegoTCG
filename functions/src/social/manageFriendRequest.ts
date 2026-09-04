import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface ManageFriendRequestInput {
  requestId: string;
}

export interface ManageFriendRequestResult {
  success: boolean;
  message: string;
  friend?: {
    uid: string;
    displayName: string;
    friendCode: string;
    photoUrl?: string;
  };
}

/**
 * Cloud Function: acceptFriendRequest
 * Acepta una solicitud de amistad pendiente mediante una transacción atómica que enlaza a ambos usuarios
 * en sus respectivas subcolecciones de amigos (TDD 5.8, GDD 7).
 */
export const acceptFriendRequest = functions.https.onCall(
  async (data: ManageFriendRequestInput, context: functions.https.CallableContext): Promise<ManageFriendRequestResult> => {
    validateAppCheck(context, "acceptFriendRequest");

    const callerUid = context.auth?.uid;
    if (!callerUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para aceptar solicitudes."
      );
    }

    const { requestId } = data;
    if (!requestId || typeof requestId !== "string") {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El ID de solicitud es obligatorio."
      );
    }

    return await db.runTransaction(async (transaction) => {
      const requestRef = db.collection(COLLECTIONS.FRIEND_REQUESTS).doc(requestId);
      const requestDoc = await transaction.get(requestRef);

      if (!requestDoc.exists) {
        throw new functions.https.HttpsError(
          "not-found",
          "La solicitud de amistad no existe."
        );
      }

      const reqData = requestDoc.data()!;

      // Validar que el usuario actual sea el destinatario
      if (reqData.toUid !== callerUid) {
        throw new functions.https.HttpsError(
          "permission-denied",
          "No tienes permiso para responder a esta solicitud."
        );
      }

      // Validar que siga pendiente
      if (reqData.status !== "pending") {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `Esta solicitud ya fue ${reqData.status === "accepted" ? "aceptada" : "rechazada"}.`
        );
      }

      const fromUid = reqData.fromUid;

      // Leer datos actualizados de ambos usuarios
      const fromUserRef = db.collection(COLLECTIONS.USERS).doc(fromUid);
      const toUserRef = db.collection(COLLECTIONS.USERS).doc(callerUid);

      const [fromUserSnap, toUserSnap] = await Promise.all([
        transaction.get(fromUserRef),
        transaction.get(toUserRef),
      ]);

      const fromUserData = fromUserSnap.data() || {};
      const toUserData = toUserSnap.data() || {};

      const now = FieldValue.serverTimestamp();

      // 1. Marcar solicitud como aceptada
      transaction.update(requestRef, {
        status: "accepted",
        updatedAt: now,
      });

      // 2. Crear amigo en destinatario (callerUid)
      const toFriendRef = db
        .collection(COLLECTIONS.USERS)
        .doc(callerUid)
        .collection(COLLECTIONS.FRIENDS)
        .doc(fromUid);

      transaction.set(toFriendRef, {
        friendUid: fromUid,
        displayName: fromUserData.displayName || reqData.fromName || "Entrenador",
        photoUrl: fromUserData.photoUrl || reqData.fromPhotoUrl || "",
        friendCode: fromUserData.friendCode || reqData.fromCode || "",
        collectionPower: fromUserData.collectionPower || 0,
        playerLevel: fromUserData.playerLevel || 1,
        addedAt: now,
      });

      // 3. Crear amigo en remitente (fromUid)
      const fromFriendRef = db
        .collection(COLLECTIONS.USERS)
        .doc(fromUid)
        .collection(COLLECTIONS.FRIENDS)
        .doc(callerUid);

      transaction.set(fromFriendRef, {
        friendUid: callerUid,
        displayName: toUserData.displayName || reqData.toName || "Entrenador",
        photoUrl: toUserData.photoUrl || reqData.toPhotoUrl || "",
        friendCode: toUserData.friendCode || reqData.toCode || "",
        collectionPower: toUserData.collectionPower || 0,
        playerLevel: toUserData.playerLevel || 1,
        addedAt: now,
      });

      // 4. Actualizar contadores
      transaction.update(toUserRef, {
        friendCount: FieldValue.increment(1),
        pendingRequestsCount: FieldValue.increment(-1),
      });

      transaction.update(fromUserRef, {
        friendCount: FieldValue.increment(1),
      });

      return {
        success: true,
        message: `¡Solicitud aceptada! Ahora eres amigo de ${fromUserData.displayName || reqData.fromName || "este jugador"}.`,
        friend: {
          uid: fromUid,
          displayName: fromUserData.displayName || reqData.fromName || "Entrenador",
          friendCode: fromUserData.friendCode || reqData.fromCode || "",
          photoUrl: fromUserData.photoUrl || reqData.fromPhotoUrl || "",
        },
      };
    });
  }
);

/**
 * Cloud Function: rejectFriendRequest
 * Rechaza una solicitud de amistad pendiente y actualiza los contadores.
 */
export const rejectFriendRequest = functions.https.onCall(
  async (data: ManageFriendRequestInput, context: functions.https.CallableContext): Promise<ManageFriendRequestResult> => {
    validateAppCheck(context, "rejectFriendRequest");

    const callerUid = context.auth?.uid;
    if (!callerUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para rechazar solicitudes."
      );
    }

    const { requestId } = data;
    if (!requestId || typeof requestId !== "string") {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El ID de solicitud es obligatorio."
      );
    }

    return await db.runTransaction(async (transaction) => {
      const requestRef = db.collection(COLLECTIONS.FRIEND_REQUESTS).doc(requestId);
      const requestDoc = await transaction.get(requestRef);

      if (!requestDoc.exists) {
        throw new functions.https.HttpsError(
          "not-found",
          "La solicitud de amistad no existe."
        );
      }

      const reqData = requestDoc.data()!;

      if (reqData.toUid !== callerUid) {
        throw new functions.https.HttpsError(
          "permission-denied",
          "No tienes permiso para rechazar esta solicitud."
        );
      }

      if (reqData.status !== "pending") {
        throw new functions.https.HttpsError(
          "failed-precondition",
          "Esta solicitud ya fue procesada anteriormente."
        );
      }

      const now = FieldValue.serverTimestamp();

      transaction.update(requestRef, {
        status: "rejected",
        updatedAt: now,
      });

      const userRef = db.collection(COLLECTIONS.USERS).doc(callerUid);
      transaction.update(userRef, {
        pendingRequestsCount: FieldValue.increment(-1),
      });

      return {
        success: true,
        message: "Solicitud rechazada.",
      };
    });
  }
);
