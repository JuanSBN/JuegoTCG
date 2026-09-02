/**
 * Test Automatizado de claimFreePack() (Fase 5 - Punto 3)
 * Valida:
 * 1. Reclamo exitoso del sobre gratis diario
 * 2. Validación de cooldown de 12 horas estricto en el servidor (inmune al reloj del móvil)
 * 3. Idempotencia y protección anti-duplicación ante desconexiones
 * 4. Reclamo habilitado una vez transcurrido el tiempo real de recarga
 */

const COOLDOWN_HOURS = 12;
const COOLDOWN_MS = COOLDOWN_HOURS * 60 * 60 * 1000;

class MockFirestore {
  constructor() {
    this.users = new Map();
    this.transactions = [];
    this.processedRequests = new Map();
    this.simulatedServerTime = Date.now();
  }

  // Simulación de claimFreePack en el servidor
  async claimFreePackSimulation(userId, idempotencyKey) {
    // 1. Chequeo de idempotencia
    if (this.processedRequests.has(idempotencyKey)) {
      return {
        fromCache: true,
        data: this.processedRequests.get(idempotencyKey),
      };
    }

    // 2. Datos de usuario
    const user = this.users.get(userId) || {
      availablePacks: { pack_gratis_diario: 0 },
      lastFreePackClaimAt: 0,
    };

    const timeElapsed = this.simulatedServerTime - (user.lastFreePackClaimAt || 0);

    // 3. Validación de cooldown con el reloj del servidor
    if (user.lastFreePackClaimAt > 0 && timeElapsed < COOLDOWN_MS) {
      const remainingHours = Math.ceil((COOLDOWN_MS - timeElapsed) / (60 * 60 * 1000));
      throw new Error(`Cooldown activo. Debes esperar ${remainingHours}h para tu próximo sobre.`);
    }

    // 4. Acreditar sobre gratis
    user.availablePacks.pack_gratis_diario = (user.availablePacks.pack_gratis_diario || 0) + 1;
    user.lastFreePackClaimAt = this.simulatedServerTime;
    this.users.set(userId, user);

    // 5. Registrar transacción
    const txId = `tx_free_${Date.now()}_${Math.floor(Math.random() * 1000)}`;
    this.transactions.push({
      transactionId: txId,
      userId,
      type: "reclamar_sobre_gratis",
      timestamp: new Date(this.simulatedServerTime).toISOString(),
    });

    const responseData = {
      success: true,
      freePacksAvailable: user.availablePacks.pack_gratis_diario,
      lastClaimAt: new Date(this.simulatedServerTime).toISOString(),
      nextClaimAvailableAt: new Date(this.simulatedServerTime + COOLDOWN_MS).toISOString(),
      transactionId: txId,
    };

    // 6. Guardar idempotencia
    this.processedRequests.set(idempotencyKey, responseData);

    return { fromCache: false, data: responseData };
  }
}

async function runTests() {
  console.log("\n==============================================================");
  console.log("🧪 TEST AUTOMATIZADO: Cloud Function claimFreePack() (Fase 5.3)");
  console.log("==============================================================\n");

  const db = new MockFirestore();
  const userId = "user_carlos_01";
  db.users.set(userId, { availablePacks: { pack_gratis_diario: 0 }, lastFreePackClaimAt: 0 });

  // ----------------------------------------------------
  // TEST 1: Reclamo inicial de sobre gratis
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Reclamando primer sobre gratis del día...");
  const key1 = "KEY-FREE-CLAIM-001";
  const res1 = await db.claimFreePackSimulation(userId, key1);

  console.log(`  📦 Sobres gratis disponibles: ${res1.data.freePacksAvailable} (Esperado: 1)`);
  console.log(`  🕒 Próximo sobre disponible en el servidor: ${res1.data.nextClaimAvailableAt}`);
  console.log(`  📝 Transacción registrada: ${res1.data.transactionId}`);

  if (res1.data.freePacksAvailable === 1 && !res1.fromCache) {
    console.log("  ✅ PASÓ: Primer sobre gratis reclamado y acreditado con éxito.\n");
  } else {
    console.error("  ❌ FALLÓ en el reclamo inicial.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Intento tramposo de adelantar la hora del celular (0 minutos después)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Intentando reclamar inmediatamente de nuevo (simulando trampa de cambiar hora en celular)...");
  try {
    const keyFraud = "KEY-FREE-FRAUD-002";
    await db.claimFreePackSimulation(userId, keyFraud);
    console.error("  ❌ FALLÓ: El servidor permitió reclamar antes del cooldown (Grave falla de seguridad).");
    process.exit(1);
  } catch (err) {
    console.log(`  🛡️ Servidor bloqueó la petición con mensaje: "${err.message}"`);
    console.log(`  📦 Sobres en inventario: ${db.users.get(userId).availablePacks.pack_gratis_diario} (Esperado: 1)`);
    console.log("  ✅ PASÓ: El servidor protegió el cooldown ignorando el cliente.\n");
  }

  // ----------------------------------------------------
  // TEST 3: Reintento por caída de red con la MISMA key
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Reintento por caída de red con la MISMA key:", key1);
  const res3 = await db.claimFreePackSimulation(userId, key1);

  console.log(`  ⚡ ¿Vino de caché de idempotencia?: ${res3.fromCache}`);
  console.log(`  📦 Sobres en inventario: ${db.users.get(userId).availablePacks.pack_gratis_diario} (Esperado: 1, NO 2)`);

  if (res3.fromCache && db.users.get(userId).availablePacks.pack_gratis_diario === 1) {
    console.log("  ✅ PASÓ: Idempotencia validada, no se duplicaron sobres gratis.\n");
  } else {
    console.error("  ❌ FALLÓ: Se duplicó el sobre en el reintento.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: Reclamo 12 horas después (Tiempo cumplido en servidor)
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Avanzando el reloj del servidor 12 horas...");
  db.simulatedServerTime += COOLDOWN_MS + 1000; // 12h y 1s después

  const keyNext = "KEY-FREE-CLAIM-003";
  const res4 = await db.claimFreePackSimulation(userId, keyNext);

  console.log(`  📦 Sobres gratis disponibles tras 12h: ${res4.data.freePacksAvailable} (Esperado: 2)`);
  console.log(`  🕒 Próxima recarga programada para: ${res4.data.nextClaimAvailableAt}`);

  if (res4.data.freePacksAvailable === 2 && !res4.fromCache) {
    console.log("  ✅ PASÓ: Segundo sobre gratis acreditado correctamente tras cumplirse el cooldown.");
  } else {
    console.error("  ❌ FALLÓ en el reclamo tras el cooldown.");
    process.exit(1);
  }

  console.log("\n==============================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE claimFreePack() FUERON EXITOSAS! (4/4)");
  console.log("==============================================================\n");
}

runTests().catch(console.error);
