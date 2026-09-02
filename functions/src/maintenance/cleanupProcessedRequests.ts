import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS, Timestamp } from "../firebase";

// Tiempo de retención de idempotencia (TDD 2.6: 48 horas de antigüedad)
export const IDEMPOTENCY_RETENTION_HOURS = 48;
export const IDEMPOTENCY_RETENTION_MS = IDEMPOTENCY_RETENTION_HOURS * 60 * 60 * 1000;

/**
 * Función interna de limpieza que purga registros de idempotencia antiguos en lotes.
 */
export async function executeCleanupProcessedRequests(
  retentionMs: number = IDEMPOTENCY_RETENTION_MS
): Promise<{ deletedCount: number; cutoffDate: string }> {
  const cutoffTimestamp = new Date(Date.now() - retentionMs);
  const cutoffFirestoreTimestamp = Timestamp.fromDate(cutoffTimestamp);

  const snapshot = await db
    .collection(COLLECTIONS.PROCESSED_REQUESTS)
    .where("createdAt", "<", cutoffFirestoreTimestamp)
    .limit(500)
    .get();

  if (snapshot.empty) {
    console.log(`[cleanupProcessedRequests] No hay registros antiguos para eliminar (Cutoff: ${cutoffTimestamp.toISOString()}).`);
    return { deletedCount: 0, cutoffDate: cutoffTimestamp.toISOString() };
  }

  const batch = db.batch();
  snapshot.docs.forEach((doc: any) => {
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
export const cleanupProcessedRequests = functions.pubsub
  .schedule("every 24 hours")
  .onRun(async () => {
    console.log("[cleanupProcessedRequests] Iniciando rutina de limpieza programada de 48h...");
    const result = await executeCleanupProcessedRequests();
    console.log(`[cleanupProcessedRequests] Limpieza finalizada. Documentos eliminados: ${result.deletedCount}`);
  });

/**
 * Endpoint manual HTTPS (callable) para pruebas y mantenimiento por administradores.
 */
export const triggerManualCleanup = functions.https.onCall(async (data: any, context: functions.https.CallableContext) => {
  return await executeCleanupProcessedRequests();
});
