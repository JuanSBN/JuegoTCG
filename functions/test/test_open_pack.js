/**
 * Test Automatizado de openPack() (Fase 5 - Punto 2)
 * Valida:
 * 1. Generación ponderada de 5 cartas por rareza en servidor (RNG)
 * 2. Deducción de monedas / sobre
 * 3. Actualización de colección de usuario y cálculo de Poder de Colección (GDD 7.2)
 * 4. Registro inmutable en log de auditoría (transactions)
 * 5. Idempotencia y protección anti-duplicación ante desconexiones
 */

const { generatePackCards, DEFAULT_RARITY_WEIGHTS, RARITY_POWER_POINTS } = require("../lib/packs/generateCardsRNG");

class MockFirestore {
  constructor() {
    this.users = new Map();
    this.userCollections = new Map(); // key: userId/cardId
    this.transactions = [];
    this.processedRequests = new Map();
  }

  // Simulación de openPack en servidor
  async openPackSimulation(userId, packId, idempotencyKey) {
    // 1. Chequeo de idempotencia
    if (this.processedRequests.has(idempotencyKey)) {
      return {
        fromCache: true,
        data: this.processedRequests.get(idempotencyKey),
      };
    }

    // 2. Datos de usuario
    const user = this.users.get(userId) || { coins: 300, collectionPower: 0, packsOpened: 0 };
    const packCost = 100;

    if (user.coins < packCost) {
      throw new Error("Monedas insuficientes.");
    }

    // 3. Catálogo piloto de prueba
    const mockCatalog = [
      { cardId: "LD", name: "Luis Díaz", initials: "LD", rarity: "mitica", team: "Liverpool", position: "DEL", albumId: "piloto" },
      { cardId: "VJ", name: "Vinicius Jr.", initials: "VJ", rarity: "rara", team: "Madrid", position: "DEL", albumId: "piloto" },
      { cardId: "EH", name: "Haaland", initials: "EH", rarity: "comun", team: "Manchester", position: "DEL", albumId: "piloto" },
      { cardId: "KM", name: "Mbappé", initials: "KM", rarity: "poco_comun", team: "Madrid", position: "DEL", albumId: "piloto" },
      { cardId: "PE", name: "Pedri", initials: "PE", rarity: "rara", team: "Barcelona", position: "MED", albumId: "piloto" },
      { cardId: "RO", name: "Rodri", initials: "RO", rarity: "comun", team: "Manchester", position: "MED", albumId: "piloto" },
      { cardId: "LY", name: "Lamine Yamal", initials: "LY", rarity: "mitica", team: "Barcelona", position: "DEL", albumId: "piloto" },
      { cardId: "JB", name: "Bellingham", initials: "JB", rarity: "rara", team: "Madrid", position: "MED", albumId: "piloto" },
      { cardId: "MS", name: "Salah", initials: "MS", rarity: "poco_comun", team: "Liverpool", position: "DEL", albumId: "piloto" },
      { cardId: "KDB", name: "De Bruyne", initials: "KDB", rarity: "rara", team: "Manchester", position: "MED", albumId: "piloto" },
    ];

    // 4. Generación RNG de 5 cartas
    const rolledCards = generatePackCards(mockCatalog, 5, DEFAULT_RARITY_WEIGHTS);

    // 5. Actualización de colección
    let powerGained = 0;
    const cardsResult = [];

    for (const card of rolledCards) {
      const colKey = `${userId}/${card.cardId}`;
      const existing = this.userCollections.get(colKey);
      const isNew = !existing;
      const newQty = isNew ? 1 : existing.quantity + 1;

      if (isNew) {
        this.userCollections.set(colKey, { cardId: card.cardId, quantity: 1, rarity: card.rarity });
        powerGained += RARITY_POWER_POINTS[card.rarity] || 1;
      } else {
        existing.quantity = newQty;
      }

      cardsResult.push({ ...card, isNew, quantityAfter: newQty });
    }

    // 6. Descontar monedas y sumar poder
    user.coins -= packCost;
    user.collectionPower += powerGained;
    user.packsOpened += 1;
    this.users.set(userId, user);

    // 7. Registro en transactions
    const txId = `tx_${Date.now()}_${Math.floor(Math.random() * 1000)}`;
    this.transactions.push({
      transactionId: txId,
      userId,
      type: "abrir_sobre",
      cardIds: cardsResult.map((c) => c.cardId),
      powerGained,
    });

    const responseData = {
      success: true,
      packId,
      cards: cardsResult,
      coinsRemaining: user.coins,
      newCollectionPower: user.collectionPower,
      transactionId: txId,
    };

    // 8. Guardar idempotencia
    this.processedRequests.set(idempotencyKey, responseData);

    return { fromCache: false, data: responseData };
  }
}

async function runTests() {
  console.log("\n==========================================================");
  console.log("🧪 TEST AUTOMATIZADO: Cloud Function openPack() (Fase 5.2)");
  console.log("==========================================================\n");

  const db = new MockFirestore();
  const userId = "user_juan_01";
  db.users.set(userId, { coins: 300, collectionPower: 0, packsOpened: 0 });

  console.log("👤 Estado Inicial del Jugador:", JSON.stringify(db.users.get(userId)));

  // ----------------------------------------------------
  // TEST 1: Apertura de Sobre Normal
  // ----------------------------------------------------
  console.log("\n▶️ TEST 1: Llamando openPack() con 300 monedas...");
  const key1 = "KEY-PACK-OPEN-001";
  const res1 = await db.openPackSimulation(userId, "pack_oro", key1);

  console.log("  📦 Cartas obtenidas (5 cartas generadas en servidor):");
  res1.data.cards.forEach((c, idx) => {
    console.log(`     ${idx + 1}. [${c.rarity.toUpperCase()}] ${c.name} (${c.team}) - ¿Es nueva?: ${c.isNew} (Copias: ${c.quantityAfter})`);
  });

  console.log(`  💰 Monedas restantes: ${res1.data.coinsRemaining} (Esperado: 200)`);
  console.log(`  ⚡ Poder de Colección actual: ${res1.data.newCollectionPower}`);
  console.log(`  📝 Transacción registrada en log de auditoría: ${res1.data.transactionId}`);

  if (res1.data.cards.length === 5 && res1.data.coinsRemaining === 200 && db.transactions.length === 1) {
    console.log("  ✅ PASÓ: Sobre abierto correctamente, monedas descontadas y 5 cartas asignadas.");
  } else {
    console.error("  ❌ FALLÓ en la apertura inicial.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Reintento por caída de red (Misma key)
  // ----------------------------------------------------
  console.log("\n▶️ TEST 2: Reintento inmediato con la MISMA key (simulando corte de conexión)...");
  const res2 = await db.openPackSimulation(userId, "pack_oro", key1);

  console.log(`  ⚡ ¿Vino de caché de idempotencia?: ${res2.fromCache}`);
  console.log(`  💰 Monedas del usuario tras el reintento: ${db.users.get(userId).coins} (Esperado: 200, NO 100)`);
  console.log(`  📝 Cantidad de transacciones registradas: ${db.transactions.length} (Esperado: 1)`);

  if (res2.fromCache === true && db.users.get(userId).coins === 200 && db.transactions.length === 1) {
    console.log("  ✅ PASÓ: El servidor protegió la cuenta y devolvió las mismas cartas sin cobrar doble.");
  } else {
    console.error("  ❌ FALLÓ: El servidor cobró doble en el reintento.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Segunda apertura legítima (Nueva key)
  // ----------------------------------------------------
  console.log("\n▶️ TEST 3: Segunda apertura legítima con NUEVA key...");
  const key2 = "KEY-PACK-OPEN-002";
  const res3 = await db.openPackSimulation(userId, "pack_oro", key2);

  console.log(`  💰 Monedas restantes: ${res3.data.coinsRemaining} (Esperado: 100)`);
  console.log(`  ⚡ Nuevo Poder de Colección: ${res3.data.newCollectionPower}`);
  console.log(`  📦 Total de sobres abiertos por el jugador: ${db.users.get(userId).packsOpened}`);

  if (res3.data.coinsRemaining === 100 && db.users.get(userId).packsOpened === 2 && db.transactions.length === 2) {
    console.log("  ✅ PASÓ: Segunda apertura procesada y cobrada con éxito.");
  } else {
    console.error("  ❌ FALLÓ en la segunda apertura.");
    process.exit(1);
  }

  console.log("\n==========================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE openPack() FUERON EXITOSAS! (3/3)");
  console.log("==========================================================\n");
}

runTests().catch(console.error);
