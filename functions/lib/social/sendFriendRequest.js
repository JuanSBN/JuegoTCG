"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.sendFriendRequest = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Cloud Function: sendFriendRequest
 * Permite a un jugador enviar una solicitud de amistad a otro mediante su código de amigo (TDD 5.1, 5.8).
 * Valida que el código exista, que no sea auto-solicitud, y que no sean amigos previamente.
 */
exports.sendFriendRequest = functions.https.onCall(async (data, context) => {
    // 0. Validar App Check
    (0, appCheck_1.validateAppCheck)(context, "sendFriendRequest");
    // 1. Validar autenticación
    const fromUid = context.auth?.uid;
    if (!fromUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para agregar amigos.");
    }
    // 2. Validar y normalizar código
    const rawCode = data?.friendCode;
    if (!rawCode || typeof rawCode !== "string" || !rawCode.trim()) {
        throw new functions.https.HttpsError("invalid-argument", "El código de amigo es obligatorio.");
    }
    const normalizedCode = rawCode.trim().toUpperCase();
    // 3. Buscar usuario destinatario por su código de amigo
    const targetQuery = await firebase_1.db
        .collection(firebase_1.COLLECTIONS.USERS)
        .where("friendCode", "==", normalizedCode)
        .limit(1)
        .get();
    if (targetQuery.empty) {
        throw new functions.https.HttpsError("not-found", "No se encontró ningún jugador con ese código de amigo.");
    }
    const targetDoc = targetQuery.docs[0];
    const targetUid = targetDoc.id;
    const targetData = targetDoc.data();
    // 4. Bloquear auto-agregado
    if (targetUid === fromUid) {
        throw new functions.https.HttpsError("invalid-argument", "No puedes agregarte a ti mismo como amigo.");
    }
    // 5. Verificar si ya son amigos
    const existingFriendDoc = await firebase_1.db
        .collection(firebase_1.COLLECTIONS.USERS)
        .doc(fromUid)
        .collection(firebase_1.COLLECTIONS.FRIENDS)
        .doc(targetUid)
        .get();
    if (existingFriendDoc.exists) {
        throw new functions.https.HttpsError("already-exists", `Ya eres amigo de ${targetData.displayName || "este jugador"}.`);
    }
    // 6. Obtener datos del remitente
    const fromUserDoc = await firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(fromUid).get();
    const fromUserData = fromUserDoc.data() || {};
    const directRequestId = `${fromUid}_${targetUid}`;
    const reverseRequestId = `${targetUid}_${fromUid}`;
    // 7. Verificar si ya existe solicitud directa pendiente
    const directReqDoc = await firebase_1.db.collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS).doc(directRequestId).get();
    if (directReqDoc.exists && directReqDoc.data()?.status === "pending") {
        throw new functions.https.HttpsError("already-exists", "Ya enviaste una solicitud de amistad a este jugador.");
    }
    // 8. Verificar si existe solicitud inversa pendiente (auto-aceptación mutua)
    const reverseReqDoc = await firebase_1.db.collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS).doc(reverseRequestId).get();
    if (reverseReqDoc.exists && reverseReqDoc.data()?.status === "pending") {
        // Ambos intentaron agregarse -> Aceptar de inmediato en transacción atómica
        await firebase_1.db.runTransaction(async (transaction) => {
            const now = firebase_1.FieldValue.serverTimestamp();
            // Marcar solicitud inversa como aceptada
            transaction.update(firebase_1.db.collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS).doc(reverseRequestId), {
                status: "accepted",
                updatedAt: now,
            });
            // Crear amigo en usuario A
            const friendARef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(fromUid).collection(firebase_1.COLLECTIONS.FRIENDS).doc(targetUid);
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
            const friendBRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(targetUid).collection(firebase_1.COLLECTIONS.FRIENDS).doc(fromUid);
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
            transaction.update(firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(fromUid), {
                friendCount: firebase_1.FieldValue.increment(1),
            });
            transaction.update(firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(targetUid), {
                friendCount: firebase_1.FieldValue.increment(1),
                pendingRequestsCount: firebase_1.FieldValue.increment(-1),
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
    const now = firebase_1.FieldValue.serverTimestamp();
    await firebase_1.db.runTransaction(async (transaction) => {
        const requestRef = firebase_1.db.collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS).doc(directRequestId);
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
        transaction.update(firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(targetUid), {
            pendingRequestsCount: firebase_1.FieldValue.increment(1),
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
});
//# sourceMappingURL=sendFriendRequest.js.map