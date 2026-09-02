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
exports.claimFreePack = exports.FREE_PACK_COOLDOWN_MS = exports.FREE_PACK_COOLDOWN_HOURS = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
const idempotency_1 = require("../utils/idempotency");
const appCheck_1 = require("../utils/appCheck");
// Cooldown de recarga: 12 horas entre sobres gratis (2 sobres gratis por día según GDD 8.1)
exports.FREE_PACK_COOLDOWN_HOURS = 12;
exports.FREE_PACK_COOLDOWN_MS = exports.FREE_PACK_COOLDOWN_HOURS * 60 * 60 * 1000;
/**
 * Cloud Function: claimFreePack
 * Reclama el sobre gratis diario validando el cooldown en el servidor (Timestamp de Firestore)
 * e idempotencia para evitar duplicaciones.
 */
exports.claimFreePack = functions.https.onCall(async (data, context) => {
    // 0. Validar App Check (TDD 2.7)
    (0, appCheck_1.validateAppCheck)(context, "claimFreePack");
    // 1. Validar autenticación
    const userId = context.auth?.uid;
    if (!userId) {
        throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para reclamar tu sobre gratis.");
    }
    const { idempotencyKey } = data;
    if (!idempotencyKey) {
        throw new functions.https.HttpsError("invalid-argument", "El parámetro 'idempotencyKey' es obligatorio.");
    }
    // 2. Comprobar Idempotencia en caché rápido
    const cachedRecord = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    if (cachedRecord.cached && cachedRecord.result) {
        console.log(`[claimFreePack] Devolviendo respuesta en caché para idempotencyKey: ${idempotencyKey}`);
        return cachedRecord.result;
    }
    // 3. Ejecutar transacción atómica de Firestore
    return await firebase_1.db.runTransaction(async (transaction) => {
        // 3a. Re-verificar idempotencia dentro de la transacción
        const processedRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
        const processedDoc = await transaction.get(processedRef);
        if (processedDoc.exists) {
            return processedDoc.data()?.result;
        }
        // 3b. Leer documento de usuario
        const userRef = firebase_1.db.collection(firebase_1.COLLECTIONS.USERS).doc(userId);
        const userDoc = await transaction.get(userRef);
        if (!userDoc.exists) {
            throw new functions.https.HttpsError("not-found", "El perfil del usuario no existe.");
        }
        const userData = userDoc.data() || {};
        const serverNow = Date.now();
        const lastClaimTimestamp = userData.lastFreePackClaimAt?.toMillis?.() || 0;
        const timeElapsed = serverNow - lastClaimTimestamp;
        // 3c. Validar cooldown del lado del servidor (reloj real de Firestore)
        if (lastClaimTimestamp > 0 && timeElapsed < exports.FREE_PACK_COOLDOWN_MS) {
            const remainingMs = exports.FREE_PACK_COOLDOWN_MS - timeElapsed;
            const remainingHours = Math.ceil(remainingMs / (60 * 60 * 1000));
            throw new functions.https.HttpsError("failed-precondition", `Tu sobre gratis aún no está listo. Vuelve en ${remainingHours}h para tu próximo sobre.`);
        }
        // 3d. Acreditar sobre gratis al inventario
        const userPacks = userData.availablePacks || {};
        const currentFreePacks = userPacks["pack_gratis_diario"] ?? 0;
        const newFreePacks = currentFreePacks + 1;
        transaction.update(userRef, {
            "availablePacks.pack_gratis_diario": firebase_1.FieldValue.increment(1),
            lastFreePackClaimAt: firebase_1.FieldValue.serverTimestamp(),
        });
        // 3e. Registrar transacción en log de auditoría
        const txRef = firebase_1.db.collection(firebase_1.COLLECTIONS.TRANSACTIONS).doc();
        const transactionId = txRef.id;
        transaction.set(txRef, {
            transactionId,
            userId,
            type: "reclamar_sobre_gratis",
            details: {
                packType: "pack_gratis_diario",
                cooldownHours: exports.FREE_PACK_COOLDOWN_HOURS,
            },
            timestamp: firebase_1.FieldValue.serverTimestamp(),
        });
        const nextClaimDate = new Date(serverNow + exports.FREE_PACK_COOLDOWN_MS).toISOString();
        const response = {
            success: true,
            freePacksAvailable: newFreePacks,
            lastClaimAt: new Date(serverNow).toISOString(),
            nextClaimAvailableAt: nextClaimDate,
            cooldownSecondsRemaining: Math.floor(exports.FREE_PACK_COOLDOWN_MS / 1000),
            transactionId,
        };
        // 3f. Guardar idempotencia atómicamente
        (0, idempotency_1.recordIdempotencyInTransaction)(transaction, idempotencyKey, userId, "claimFreePack", response);
        return response;
    });
});
//# sourceMappingURL=claimFreePack.js.map