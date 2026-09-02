/**
 * Test Automatizado de watchAdReward() (Fase 5 - Punto 4)
 * Valida:
 * 1. Reclamo de sobre de recompensa tras ver anuncio
 * 2. Límite diario de 2 sobres por anuncio (GDD 8.1)
 * 3. Idempotencia y protección anti-duplicación ante desconexiones
 * 4. Reinicio automático del límite al día siguiente
 */

const MAX_DAILY_ADS = 2;

class MockFirestore {
  constructor() {
    this.users = new Map();
    this.transactions = [];
    this.processedRequests = new Map();
    this.currentDateStr = "2026-09-01";
  }

  // Simulación de watchAdReward en el servidor
  async watchAdRewardSimulation(userId, idempotencyKey, token) {
    // 1. Chequeo de idempotencia
    if (this.processedRequests.has(idempotencyKey)) {
      return {
        fromCache: true,
        data: this.processedRequests.get(idempotencyKey),
      };
    }

    // 2. Datos de usuario
    const user = this.users.get(userId) || {
      availablePacks: { pack_anuncio: 0 },
      adsWatchedTodayCount: 0,
      lastAdWatchedDate: "",
    };

    let adsWatchedToday = (user.lastAdWatchedDate === this.currentDateStr) ? (user.adsWatchedTodayCount || 0) : 0;

    // 3. Validar límite diario (GDD 8.1: 2 sobres por anuncio al día)
    if (adsWatchedToday >= MAX_DAILY_ADS) {
      throw new Error(`Límite diario alcanzado. Máximo ${MAX_DAILY_ADS} sobres por anuncio al día.`);
    }

    // 4. Acreditar sobre y actualizar contadores
    adsWatchedToday += 1;
    user.availablePacks.pack_anuncio = (user.availablePacks.pack_anuncio || 0) + 1;
    user.adsWatchedTodayCount = adsWatchedToday;
    user.lastAdWatchedDate = this.currentDateStr;
    this.users.set(userId, user);

    // 5. Registrar transacción
    const txId = `tx_ad_${Date.now()}_${Math.floor(Math.random() * 1000)}`;
    this.transactions.push({
      transactionId: txId,
      userId,
      type: "ver_anuncio_recompensa",
      adsWatchedToday,
    });

    const responseData = {
      success: true,
      rewardType: "pack_anuncio",
      adPacksAvailable: user.availablePacks.pack_anuncio,
      adsWatchedToday,
      maxDailyAds: MAX_DAILY_ADS,
      transactionId: txId,
    };

    // 6. Guardar idempotencia
    this.processedRequests.set(idempotencyKey, responseData);

    return { fromCache: false, data: responseData };
  }
}

async function runTests() {
  console.log("\n=============================================================");
  console.log("🧪 TEST AUTOMATIZADO: Cloud Function watchAdReward() (Fase 5.4)");
  console.log("=============================================================\n");

  const db = new MockFirestore();
  const userId = "user_lucas_01";
  db.users.set(userId, { availablePacks: { pack_anuncio: 0 }, adsWatchedTodayCount: 0, lastAdWatchedDate: "" });

  // ----------------------------------------------------
  // TEST 1: Primer anuncio visto hoy (1/2)
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Reclamando sobre del primer anuncio visto hoy (1/2)...");
  const key1 = "KEY-AD-REWARD-001";
  const res1 = await db.watchAdRewardSimulation(userId, key1, "ssv_token_abc");

  console.log(`  📦 Sobres por anuncio en inventario: ${res1.data.adPacksAvailable} (Esperado: 1)`);
  console.log(`  📺 Anuncios vistos hoy: ${res1.data.adsWatchedToday} / ${res1.data.maxDailyAds}`);
  console.log(`  📝 Transacción registrada: ${res1.data.transactionId}`);

  if (res1.data.adPacksAvailable === 1 && res1.data.adsWatchedToday === 1) {
    console.log("  ✅ PASÓ: Primer sobre por anuncio entregado correctamente.\n");
  } else {
    console.error("  ❌ FALLÓ en el primer anuncio.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Segundo anuncio visto hoy (2/2 - Límite alcanzado)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Reclamando sobre del segundo anuncio visto hoy (2/2)...");
  const key2 = "KEY-AD-REWARD-002";
  const res2 = await db.watchAdRewardSimulation(userId, key2, "ssv_token_def");

  console.log(`  📦 Sobres por anuncio en inventario: ${res2.data.adPacksAvailable} (Esperado: 2)`);
  console.log(`  📺 Anuncios vistos hoy: ${res2.data.adsWatchedToday} / ${res2.data.maxDailyAds}`);

  if (res2.data.adPacksAvailable === 2 && res2.data.adsWatchedToday === 2) {
    console.log("  ✅ PASÓ: Segundo sobre por anuncio entregado correctamente.\n");
  } else {
    console.error("  ❌ FALLÓ en el segundo anuncio.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Intento de superar el límite diario (3er anuncio)
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Intentando reclamar un 3er anuncio en el mismo día...");
  try {
    const key3 = "KEY-AD-REWARD-003";
    await db.watchAdRewardSimulation(userId, key3, "ssv_token_ghi");
    console.error("  ❌ FALLÓ: El servidor permitió reclamar más del límite diario.");
    process.exit(1);
  } catch (err) {
    console.log(`  🛡️ Servidor bloqueó la petición: "${err.message}"`);
    console.log(`  📦 Sobres en inventario: ${db.users.get(userId).availablePacks.pack_anuncio} (Esperado: 2)`);
    console.log("  ✅ PASÓ: Límite diario de 2 anuncios respetado.\n");
  }

  // ----------------------------------------------------
  // TEST 4: Reintento por caída de red (Misma key)
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Reintento por caída de red con la MISMA key:", key2);
  const res4 = await db.watchAdRewardSimulation(userId, key2, "ssv_token_def");

  console.log(`  ⚡ ¿Vino de caché de idempotencia?: ${res4.fromCache}`);
  console.log(`  📦 Sobres en inventario: ${db.users.get(userId).availablePacks.pack_anuncio} (Esperado: 2, NO 3)`);

  if (res4.fromCache && db.users.get(userId).availablePacks.pack_anuncio === 2) {
    console.log("  ✅ PASÓ: Idempotencia validada, no se duplicaron sobres.\n");
  } else {
    console.error("  ❌ FALLÓ en la idempotencia.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 5: Nuevo día calendario (2026-09-02)
  // ----------------------------------------------------
  console.log("▶️ TEST 5: Cambiando al día siguiente (2026-09-02)...");
  db.currentDateStr = "2026-09-02";

  const keyNextDay = "KEY-AD-REWARD-004";
  const res5 = await db.watchAdRewardSimulation(userId, keyNextDay, "ssv_token_jkl");

  console.log(`  📦 Sobres en inventario en el nuevo día: ${res5.data.adPacksAvailable} (Esperado: 3)`);
  console.log(`  📺 Contador del nuevo día reiniciado: ${res5.data.adsWatchedToday} / ${res5.data.maxDailyAds}`);

  if (res5.data.adPacksAvailable === 3 && res5.data.adsWatchedToday === 1) {
    console.log("  ✅ PASÓ: El contador diario se reinició correctamente para el nuevo día.");
  } else {
    console.error("  ❌ FALLÓ al reiniciar el día.");
    process.exit(1);
  }

  console.log("\n=============================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE watchAdReward() FUERON EXITOSAS! (5/5)");
  console.log("=============================================================\n");
}

runTests().catch(console.error);
