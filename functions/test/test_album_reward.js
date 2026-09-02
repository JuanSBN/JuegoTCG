/**
 * Test Automatizado de Recompensa de Álbum Completo (Fase 7.2)
 * Valida:
 * 1. Rechazo en servidor si el álbum no tiene el 100% de cartas requeridas.
 * 2. Entrega atómica del gran premio al completar 10/10 cartas (+250 monedas + 1 Sobre Mítico).
 * 3. Prevención de reclamos duplicados del mismo álbum.
 * 4. Idempotencia ante reintentos de red.
 */

class MockAlbumRewardEngine {
  constructor() {
    this.users = new Map();
    this.userCollections = new Map(); // userId -> Set of cardIds
    this.processedRequests = new Map();
    this.transactions = [];
    this.catalogCards = ["LD", "VJ", "EH", "KM", "PE", "LY", "JB", "RO", "MS", "KDB"];
  }

  // Simulación de claimAlbumCompletionReward en Cloud Functions
  async claimAlbumReward(userId, albumId, idempotencyKey) {
    if (this.processedRequests.has(idempotencyKey)) {
      return { fromCache: true, response: this.processedRequests.get(idempotencyKey) };
    }

    const user = this.users.get(userId) || { coins: 100, completedAlbums: {}, availablePacks: {} };
    if (user.completedAlbums[albumId]) {
      throw new Error("Ya has reclamado la recompensa por completar este álbum.");
    }

    const userCards = this.userCollections.get(userId) || new Set();
    const missingCards = this.catalogCards.filter((id) => !userCards.has(id));

    if (missingCards.length > 0) {
      throw new Error(`Álbum incompleto. Te faltan ${missingCards.length} cartas (${userCards.size}/${this.catalogCards.length}).`);
    }

    // Acreditar gran premio
    const rewardCoins = 250;
    const rewardPackType = "pack_mitico_garantizado";

    user.coins += rewardCoins;
    user.availablePacks[rewardPackType] = (user.availablePacks[rewardPackType] || 0) + 1;
    user.completedAlbums[albumId] = {
      completedAt: new Date().toISOString(),
      rewardCoins,
      rewardPackType,
    };

    this.users.set(userId, user);

    const response = {
      success: true,
      albumId,
      rewardCoins,
      rewardPackType,
      newCoinsTotal: user.coins,
      transactionId: "tx_album_complete_" + Date.now(),
    };

    this.processedRequests.set(idempotencyKey, response);
    return { fromCache: false, response };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Recompensa de Álbum Completo (Fase 7.2)");
  console.log("==========================================================================\n");

  const engine = new MockAlbumRewardEngine();
  const userId = "player_album_winner_01";
  engine.users.set(userId, { coins: 50, completedAlbums: {}, availablePacks: {} });

  // ----------------------------------------------------
  // TEST 1: Intento de reclamo con álbum incompleto (9/10 cartas)
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Jugador con 9/10 cartas intenta reclamar premio del álbum...");
  const incompleteCards = new Set(["LD", "VJ", "EH", "KM", "PE", "LY", "JB", "RO", "MS"]); // Falta KDB
  engine.userCollections.set(userId, incompleteCards);

  try {
    await engine.claimAlbumReward(userId, "album_piloto_01", "KEY_REWARD_001");
    console.error("  ❌ FALLÓ: El servidor permitió reclamar premio con álbum incompleto.");
    process.exit(1);
  } catch (err) {
    console.log(`  🛡️ Servidor bloqueó la petición con éxito: "${err.message}"`);
    console.log("  ✅ PASÓ: Validación autoritativa de 100% de cartas verificada.\n");
  }

  // ----------------------------------------------------
  // TEST 2: Jugador consigue la última carta (10/10) y reclama premio mayor
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Jugador obtiene la carta 10 ('KDB') y reclama recompensa...");
  incompleteCards.add("KDB"); // 10/10 completado
  engine.userCollections.set(userId, incompleteCards);

  const claimRes = await engine.claimAlbumReward(userId, "album_piloto_01", "KEY_REWARD_001");
  console.log(`  🎉 Recompensa entregada: +${claimRes.response.rewardCoins} monedas y 1 Sobre Mítico ('${claimRes.response.rewardPackType}')`);
  console.log(`  🪙 Nuevo saldo del jugador: ${claimRes.response.newCoinsTotal} monedas (Esperado: 300)`);
  console.log(`  📦 Sobres Míticos en inventario: ${engine.users.get(userId).availablePacks["pack_mitico_garantizado"]}`);

  if (!claimRes.fromCache && claimRes.response.newCoinsTotal === 300 && engine.users.get(userId).availablePacks["pack_mitico_garantizado"] === 1) {
    console.log("  ✅ PASÓ: Gran premio de finalización de álbum entregado con éxito.\n");
  } else {
    console.error("  ❌ FALLÓ en la entrega de recompensa.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Intento de reclamar el mismo álbum por segunda vez
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Jugador intenta reclamar el mismo álbum una segunda vez con otra clave...");
  try {
    await engine.claimAlbumReward(userId, "album_piloto_01", "KEY_REWARD_FRAUD_002");
    console.error("  ❌ FALLÓ: Se permitió reclamar dos veces el premio del mismo álbum.");
    process.exit(1);
  } catch (err) {
    console.log(`  🛡️ Servidor bloqueó el segundo reclamo: "${err.message}"`);
    console.log("  ✅ PASÓ: Prevención de doble reclamo de álbum verificada.\n");
  }

  // ----------------------------------------------------
  // TEST 4: Idempotencia ante caída de red
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Reintento por caída de red con la MISMA clave: KEY_REWARD_001");
  const retryRes = await engine.claimAlbumReward(userId, "album_piloto_01", "KEY_REWARD_001");
  console.log(`  ⚡ ¿Servido desde caché?: ${retryRes.fromCache}`);
  console.log(`  🪙 Saldo protegido: ${engine.users.get(userId).coins} monedas (Esperado: 300, NO 550)`);

  if (retryRes.fromCache && engine.users.get(userId).coins === 300) {
    console.log("  ✅ PASÓ: Idempotencia validada, saldo y recompensas protegidas.");
  } else {
    console.error("  ❌ FALLÓ en la idempotencia.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE RECOMPENSA DE ÁLBUM FUERON EXITOSAS! (4/4)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
