import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, FieldValue } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface SendFriendRequestInput {
  friendCode: string;
}

export interface SendFriendRequestResult {
  success: boolean;
  message: string;
  autoAccepted?: boolean;
  targetUser?: {
    uid: string;
    displayName: string;
    friendCode: string;
    photoUrl?: string;
  };
}

/**
 * Cloud Function: sendFriendRequest
 * Permite a un jugador enviar una solicitud de amistad a otro mediante su código de amigo (TDD 5.1, 5.8).
 * Valida que el código exista, que no sea auto-solicitud, y que no sean amigos previamente.
 */
export const sendFriendRequest = functions.https.onCall(
  async (data: SendFriendRequestInput, context: functions.https.CallableContext): Promise<SendFriendRequestResult> => {
    // 0. Validar App Check
    validateAppCheck(context, "sendFriendRequest");

    // 1. Validar autenticación
    const fromUid = context.auth?.uid;
    if (!fromUid) {
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Debes iniciar sesión para agregar amigos."
      );
    }

    // 2. Validar y normalizar código
    const rawCode = data?.friendCode;
    if (!rawCode || typeof rawCode !== "string" || !rawCode.trim()) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "El código de amigo es obligatorio."
      );
    }

    const normalizedCode = rawCode.trim().toUpperCase();

    // 3. Buscar usuario destinatario por su código de amigo
    const targetQuery = await db
      .collection(COLLECTIONS.USERS)
      .where("friendCode", "==", normalizedCode)
      .limit(1)
      .get();

    if (targetQuery.empty) {
      throw new functions.https.HttpsError(
        "not-found",
        "No se encontró ningún jugador con ese código de amigo."
      );
    }

    const targetDoc = targetQuery.docs[0];
    const targetUid = targetDoc.id;
    const targetData = targetDoc.data();

    // 4. Bloquear auto-agregado
    if (targetUid === fromUid) {
      throw new functions.https.HttpsError(
        "invalid-argument",
        "No puedes agregarte a ti mismo como amigo."
      );
    }

    // 5. Verificar si ya son amigos
    const existingFriendDoc = await db
      .collection(COLLECTIONS.USERS)
      .doc(fromUid)
      .collection(COLLECTIONS.FRIENDS)
      .doc(targetUid)
      .get();

    if (existingFriendDoc.exists) {
      throw new functions.https.HttpsError(
        "already-exists",
        `Ya eres amigo de ${targetData.displayName || "este jugador"}.`
      );
    }

    // 6. Obtener datos del remitente
    const fromUserDoc = await db.collection(COLLECTIONS.USERS).doc(fromUid).get();
    const fromUserData = fromUserDoc.data() || {};

    const directRequestId = `${fromUid}_${targetUid}`;
    const reverseRequestId = `${targetUid}_${fromUid}`;

    // 7. Verificar si ya existe solicitud directa pendiente
    const directReqDoc = await db.collection(COLLECTIONS.FRIEND_REQUESTS).doc(directRequestId).get();
    if (directReqDoc.exists && directReqDoc.data()?.status === "pending") {
      throw new functions.https.HttpsError(
        "already-exists",
        "Ya enviaste una solicitud de amistad a este jugador."
      );
    }

    // 8. Verificar si existe solicitud inversa pendiente (auto-aceptación mutua)
    const reverseReqDoc = await db.collection(COLLECTIONS.FRIEND_REQUESTS).doc(reverseRequestId).get();
    if (reverseReqDoc.exists && reverseReqDoc.data()?.status === "pending") {
      // Ambos intentaron agregarse -> Aceptar de inmediato en transacción atómica
      await db.runTransaction(async (transaction) => {
        const now = FieldValue.serverTimestamp();

        // Marcar solicitud inversa como aceptada
        transaction.update(db.collection(COLLECTIONS.FRIEND_REQUESTS).doc(reverseRequestId), {
          status: "accepted",
          updatedAt: now,
        });

        // Crear amigo en usuario A
        const friendARef = db.collection(COLLECTIONS.USERS).doc(fromUid).collection(COLLECTIONS.FRIENDS).doc(targetUid);
        transaction.set(friendARef, {
          friendUid: targetUid,
          displayName: targetData.displayName || "Entrenador",
          photoUrl: targetData.photoUrl || "",
          friendCode: targetData.friendCode || normalizedCode,
          collectionPower: targetData.collectionPower || 0,
          playerLevel: targetData.playerLevel || 1,
          addedAt: now,
        });

        // Crear amigo en usuario B
        const friendBRef = db.collection(COLLECTIONS.USERS).doc(targetUid).collection(COLLECTIONS.FRIENDS).doc(fromUid);
        transaction.set(friendBRef, {
          friendUid: fromUid,
          displayName: fromUserData.displayName || "Entrenador",
          photoUrl: fromUserData.photoUrl || "",
          friendCode: fromUserData.friendCode || "",
          collectionPower: fromUserData.collectionPower || 0,
          playerLevel: fromUserData.playerLevel || 1,
          addedAt: now,
        });

        // Actualizar contadores
        transaction.update(db.collection(COLLECTIONS.USERS).doc(fromUid), {
          friendCount: FieldValue.increment(1),
        });
        transaction.update(db.collection(COLLECTIONS.USERS).doc(targetUid), {
          friendCount: FieldValue.increment(1),
          pendingRequestsCount: FieldValue.increment(-1),
        });
      });

      return {
        success: true,
        autoAccepted: true,
        message: `¡${targetData.displayName || "El jugador"} también te había enviado una solicitud! Ahora son amigos.`,
        targetUser: {
          uid: targetUid,
          displayName: targetData.displayName || "Entrenador",
          friendCode: targetData.friendCode || normalizedCode,
          photoUrl: targetData.photoUrl || "",
        },
      };
    }

    // 9. Crear nueva solicitud de amistad pendiente
    const now = FieldValue.serverTimestamp();
    await db.runTransaction(async (transaction) => {
      const requestRef = db.collection(COLLECTIONS.FRIEND_REQUESTS).doc(directRequestId);
      transaction.set(requestRef, {
        requestId: directRequestId,
        fromUid,
        fromName: fromUserData.displayName || "Entrenador",
        fromPhotoUrl: fromUserData.photoUrl || "",
        fromCode: fromUserData.friendCode || "",
        toUid: targetUid,
        toName: targetData.displayName || "Entrenador",
        toCode: targetData.friendCode || normalizedCode,
        status: "pending",
        createdAt: now,
        updatedAt: now,
      });

      // Incrementar solicitudes pendientes en el destinatario
      transaction.update(db.collection(COLLECTIONS.USERS).doc(targetUid), {
        pendingRequestsCount: FieldValue.increment(1),
      });
    });

    return {
      success: true,
      autoAccepted: false,
      message: `¡Solicitud de amistad enviada a ${targetData.displayName || "jugador"}!`,
      targetUser: {
        uid: targetUid,
        displayName: targetData.displayName || "Entrenador",
        friendCode: targetData.friendCode || normalizedCode,
        photoUrl: targetData.photoUrl || "",
      },
    };
  }
);
