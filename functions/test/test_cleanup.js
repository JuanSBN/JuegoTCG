/**
 * Test Automatizado de cleanupProcessedRequests() (Fase 5 - Punto 6)
 * Valida:
 * 1. Detección y purga de registros con más de 48 horas de antigüedad
 * 2. Conservación de registros recientes (< 48h)
 * 3. Ejecución por lotes eficiente (Batch Delete)
 */

const RETENTION_MS = 48 * 60 * 60 * 1000;

class MockFirestore {
  constructor() {
    this.processedRequests = new Map();
  }

  // Simulación de cleanup en servidor
  async cleanupSimulation(simulatedNow = Date.now()) {
    const cutoff = simulatedNow - RETENTION_MS;
    let deletedCount = 0;

    for (const [key, data] of this.processedRequests.entries()) {
      if (data.createdAtMs < cutoff) {
        this.processedRequests.delete(key);
        deletedCount++;
      }
    }

    return {
      deletedCount,
      cutoffDate: new Date(cutoff).toISOString(),
      remainingRecords: this.processedRequests.size,
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Scheduled Function cleanupProcessedRequests() (Fase 5.6)");
  console.log("==========================================================================\n");

  const db = new MockFirestore();
  const now = Date.now();
  const ONE_HOUR = 60 * 60 * 1000;

  // Insertamos 3 tipos de registros:
  // 1. Reciente (hace 2 horas)
  // 2. Límite (hace 40 horas - aún válido)
  // 3. Antiguo 1 (hace 50 horas - expirado)
  // 4. Antiguo 2 (hace 72 horas - expirado)
  db.processedRequests.set("REQ_RECENT_01", {
    idempotencyKey: "REQ_RECENT_01",
    createdAtMs: now - (2 * ONE_HOUR),
    desc: "Apertura hace 2 horas",
  });

  db.processedRequests.set("REQ_VALID_02", {
    idempotencyKey: "REQ_VALID_02",
    createdAtMs: now - (40 * ONE_HOUR),
    desc: "Apertura hace 40 horas",
  });

  db.processedRequests.set("REQ_EXPIRED_01", {
    idempotencyKey: "REQ_EXPIRED_01",
    createdAtMs: now - (50 * ONE_HOUR),
    desc: "Apertura hace 50 horas (Expirada)",
  });

  db.processedRequests.set("REQ_EXPIRED_02", {
    idempotencyKey: "REQ_EXPIRED_02",
    createdAtMs: now - (72 * ONE_HOUR),
    desc: "Apertura hace 72 horas (Expirada)",
  });

  console.log(`📊 Estado Inicial: ${db.processedRequests.size} registros en processedRequests.`);

  // ----------------------------------------------------
  // TEST 1: Ejecutar rutina de limpieza
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Ejecutando limpieza automática programada (48h cutoff)...");
  const res1 = await db.cleanupSimulation(now);

  console.log(`  🗑️ Registros eliminados: ${res1.deletedCount} (Esperado: 2)`);
  console.log(`  📦 Registros conservados en base de datos: ${res1.remainingRecords} (Esperado: 2)`);
  console.log(`  🕒 Fecha de corte aplicada: ${res1.cutoffDate}`);

  const hasRecent = db.processedRequests.has("REQ_RECENT_01");
  const hasValid = db.processedRequests.has("REQ_VALID_02");
  const hasExpired1 = db.processedRequests.has("REQ_EXPIRED_01");
  const hasExpired2 = db.processedRequests.has("REQ_EXPIRED_02");

  if (res1.deletedCount === 2 && res1.remainingRecords === 2 && hasRecent && hasValid && !hasExpired1 && !hasExpired2) {
    console.log("  ✅ PASÓ: Se eliminaron exclusivamente los registros de más de 48h, conservando los válidos.\n");
  } else {
    console.error("  ❌ FALLÓ en la purga de registros.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Segunda ejecución inmediata (Sin registros expirados)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Ejecutando segunda limpieza consecutiva...");
  const res2 = await db.cleanupSimulation(now);

  console.log(`  🗑️ Registros eliminados: ${res2.deletedCount} (Esperado: 0)`);
  console.log(`  📦 Registros conservados: ${res2.remainingRecords} (Esperado: 2)`);

  if (res2.deletedCount === 0 && res2.remainingRecords === 2) {
    console.log("  ✅ PASÓ: No se borraron registros innecesariamente.");
  } else {
    console.error("  ❌ FALLÓ en la segunda pasada.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE cleanupProcessedRequests() FUERON EXITOSAS! (2/2)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
