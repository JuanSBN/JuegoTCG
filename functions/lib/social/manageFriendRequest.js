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
exports.rejectFriendRequest = exports.acceptFriendRequest = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Cloud Function: acceptFriendRequest
 * Acepta una solicitud de amistad pendiente mediante una transacción atómica que enlaza a ambos usuarios
 * en sus respectivas subcolecciones de amigos (TDD 5.8, GDD 7).
 */
exports.acceptFriendRequest = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "acceptFriendRequest");
    const callerUid = context.auth?.uid;
    if (!callerUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para aceptar solicitudes.");
    }
    const { requestId } = data;
    if (!requestId || typeof requestId !== "string") {
        throw new functions.https.HttpsError("invalid-argument", "El ID de solicitud es obligatorio.");
    }
    return await firebase_1.db.runTransaction(async (transaction) => {
        const requestRef = firebase_1.db.collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS).doc(requestId);
        const requestDoc = await transaction.get(requestRef);
        if (!requestDoc.exists) {
            throw new functions.https.HttpsError("not-found", "La solicitud de amistad no existe.");
        }
        const reqData = requestDoc.data();
        // Validar que el usuario actual sea el destinatario
        if (reqData.toUid !== callerUid) {
            throw new functions.https.HttpsError("permission-denied", "No tienes permiso para responder a esta solicitud.");
        }
        // Validar que siga pendiente
        if (reqData.status !== "pending") {
            throw new functions.https.HttpsError("failed-precondition", `Esta solicitud ya fue ${reqData.status === "accepted" ? "aceptada" : "rechazada"}.`);
        }
        const fromUid = reqData.fromUid;
        // Leer datos actualizados de ambos usuarios
        const fromUserRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(fromUid);
        const toUserRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(callerUid);
        const [fromUserSnap, toUserSnap] = await Promise.all([
            transaction.get(fromUserRef),
            transaction.get(toUserRef),
        ]);
        const fromUserData = fromUserSnap.data() || {};
        const toUserData = toUserSnap.data() || {};
        const now = firebase_1.FieldValue.serverTimestamp();
        // 1. Marcar solicitud como aceptada
        transaction.update(requestRef, {
            status: "accepted",
            updatedAt: now,
        });
        // 2. Crear amigo en destinatario (callerUid)
        const toFriendRef = firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
            .doc(callerUid)
            .collection(firebase_1.COLLECTIONS.FRIENDS)
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
        const fromFriendRef = firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
            .doc(fromUid)
            .collection(firebase_1.COLLECTIONS.FRIENDS)
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
            friendCount: firebase_1.FieldValue.increment(1),
            pendingRequestsCount: firebase_1.FieldValue.increment(-1),
        });
        transaction.update(fromUserRef, {
            friendCount: firebase_1.FieldValue.increment(1),
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
});
/**
 * Cloud Function: rejectFriendRequest
 * Rechaza una solicitud de amistad pendiente y actualiza los contadores.
 */
exports.rejectFriendRequest = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "rejectFriendRequest");
    const callerUid = context.auth?.uid;
    if (!callerUid) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para rechazar solicitudes.");
    }
    const { requestId } = data;
    if (!requestId || typeof requestId !== "string") {
        throw new functions.https.HttpsError("invalid-argument", "El ID de solicitud es obligatorio.");
    }
    return await firebase_1.db.runTransaction(async (transaction) => {
        const requestRef = firebase_1.db.collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS).doc(requestId);
        const requestDoc = await transaction.get(requestRef);
        if (!requestDoc.exists) {
            throw new functions.https.HttpsError("not-found", "La solicitud de amistad no existe.");
        }
        const reqData = requestDoc.data();
        if (reqData.toUid !== callerUid) {
            throw new functions.https.HttpsError("permission-denied", "No tienes permiso para rechazar esta solicitud.");
        }
        if (reqData.status !== "pending") {
            throw new functions.https.HttpsError("failed-precondition", "Esta solicitud ya fue procesada anteriormente.");
        }
        const now = firebase_1.FieldValue.serverTimestamp();
        transaction.update(requestRef, {
            status: "rejected",
            updatedAt: now,
        });
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(callerUid);
        transaction.update(userRef, {
            pendingRequestsCount: firebase_1.FieldValue.increment(-1),
        });
        return {
            success: true,
            message: "Solicitud rechazada.",
        };
    });
});
//# sourceMappingURL=manageFriendRequest.js.map