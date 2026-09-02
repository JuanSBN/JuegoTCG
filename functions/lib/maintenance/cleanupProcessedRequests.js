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
exports.triggerManualCleanup = exports.cleanupProcessedRequests = exports.IDEMPOTENCY_RETENTION_MS = exports.IDEMPOTENCY_RETENTION_HOURS = void 0;
exports.executeCleanupProcessedRequests = executeCleanupProcessedRequests;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
// Tiempo de retención de idempotencia (TDD 2.6: 48 horas de antigüedad)
exports.IDEMPOTENCY_RETENTION_HOURS = 48;
exports.IDEMPOTENCY_RETENTION_MS = exports.IDEMPOTENCY_RETENTION_HOURS * 60 * 60 * 1000;
/**
 * Función interna de limpieza que purga registros de idempotencia antiguos en lotes.
 */
async function executeCleanupProcessedRequests(retentionMs = exports.IDEMPOTENCY_RETENTION_MS) {
    const cutoffTimestamp = new Date(Date.now() - retentionMs);
    const cutoffFirestoreTimestamp = firebase_1.Timestamp.fromDate(cutoffTimestamp);
    const snapshot = await firebase_1.db
        .collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS)
        .where("createdAt", "<", cutoffFirestoreTimestamp)
        .limit(500)
        .get();
    if (snapshot.empty) {
        console.log(`[cleanupProcessedRequests] No hay registros antiguos para eliminar (Cutoff: ${cutoffTimestamp.toISOString()}).`);
        return { deletedCount: 0, cutoffDate: cutoffTimestamp.toISOString() };
    }
    const batch = firebase_1.db.batch();
    snapshot.docs.forEach((doc) => {
        batch.delete(doc.ref);
    });
    await batch.commit();
    console.log(`[cleanupProcessedRequests] Se eliminaron con éxito ${snapshot.size} registros expirados.`);
    return {
        deletedCount: snapshot.size,
        cutoffDate: cutoffTimestamp.toISOString(),
    };
}
/**
 * Scheduled Cloud Function: cleanupProcessedRequests
 * Se ejecuta automáticamente todos los días a medianoche (00:00 UTC) para purgar registros de más de 48 horas.
 */
exports.cleanupProcessedRequests = functions.pubsub
    .schedule("every 24 hours")
    .onRun(async () => {
    console.log("[cleanupProcessedRequests] Iniciando rutina de limpieza programada de 48h...");
    const result = await executeCleanupProcessedRequests();
    console.log(`[cleanupProcessedRequests] Limpieza finalizada. Documentos eliminados: ${result.deletedCount}`);
});
/**
 * Endpoint manual HTTPS (callable) para pruebas y mantenimiento por administradores.
 */
exports.triggerManualCleanup = functions.https.onCall(async (data, context) => {
    return await executeCleanupProcessedRequests();
});
//# sourceMappingURL=cleanupProcessedRequests.js.map