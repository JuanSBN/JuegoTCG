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
exports.getSocialData = void 0;
exports.generateRandomFriendCode = generateRandomFriendCode;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Genera un código de amigo único de 8 caracteres en formato FC-XXXX o XXXX-XXXX (ej: FC-8294)
 */
function generateRandomFriendCode() {
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
exports.getSocialData = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "getSocialData");
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para consultar datos sociales.");
    }
    const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
    const userDoc = await userRef.get();
    let userData = userDoc.data() || {};
    // Si el usuario no tiene friendCode aún, generarlo de forma única
    let friendCode = userData.friendCode;
    if (!friendCode) {
        let unique = false;
        while (!unique) {
            const candidate = generateRandomFriendCode();
            const existing = await firebase_1.db
                .collection(firebase_1.COLLECTIONS.USERS)
                .where("friendCode", "==", candidate)
                .limit(1)
                .get();
            if (existing.empty) {
                friendCode = candidate;
                unique = true;
            }
        }
        await userRef.set({
            friendCode,
            friendCount: userData.friendCount || 0,
            pendingRequestsCount: userData.pendingRequestsCount || 0,
            updatedAt: firebase_1.FieldValue.serverTimestamp(),
        }, { merge: true });
    }
    // 1. Obtener lista de amigos
    const friendsSnap = await userRef.collection(firebase_1.COLLECTIONS.FRIENDS).get();
    const friends = friendsSnap.docs.map((doc) => {
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
    const requestsSnap = await firebase_1.db
        .collection(firebase_1.COLLECTIONS.FRIEND_REQUESTS)
        .where("toUid", "==", userId)
        .where("status", "==", "pending")
        .orderBy("createdAt", "desc")
        .get();
    const pendingRequests = requestsSnap.docs.map((doc) => {
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
});
//# sourceMappingURL=getSocialData.js.map