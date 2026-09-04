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
exports.getCollectionRanking = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const appCheck_1 = require("../utils/appCheck");
/**
 * Cloud Function Callable: getCollectionRanking
 * Consulta el ranking por Poder de Colección (GDD Sección 7.2)
 * utilizando el campo cacheado 'collectionPower' en la colección 'users'.
 */
exports.getCollectionRanking = functions.https.onCall(async (data, context) => {
    (0, appCheck_1.validateAppCheck)(context, "getCollectionRanking");
    const authUid = context.auth?.uid || "anonymous_user";
    const mode = data?.mode || "global";
    const maxLimit = Math.min(Math.max(data?.limit || 20, 1), 100);
    let rankingItems = [];
    if (mode === "friends" && authUid !== "anonymous_user") {
        // 1. Ranking de Amigos: obtener IDs de amigos confirmados
        const friendsSnap = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
            .doc(authUid)
            .collection("friends")
            .get();
        const friendUids = new Set([authUid]);
        friendsSnap.forEach((doc) => {
            friendUids.add(doc.id);
        });
        // Consultar perfiles de amigos
        const userDocs = await Promise.all(Array.from(friendUids).map((fUid) => firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(fUid).get()));
        const list = [];
        userDocs.forEach((docSnap) => {
            if (docSnap.exists) {
                const uData = docSnap.data();
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
    }
    else {
        // 2. Ranking Global
        const usersSnap = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.USERS)
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
});
//# sourceMappingURL=getCollectionRanking.js.map