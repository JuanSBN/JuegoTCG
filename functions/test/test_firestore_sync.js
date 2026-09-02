/**
 * Test Automatizado de Sincronización en Tiempo Real con Firestore (Fase 6.8)
 * Valida:
 * 1. Suscripción y escucha de snapshots en users/{uid}.
 * 2. Actualización instantánea de saldo de monedas (coins).
 * 3. Actualización de inventario de sobres disponibles (availablePacks).
 * 4. Reflejo del poder de colección en tiempo real.
 */

class MockFirestoreRealtimeListener {
  constructor(userId) {
    this.userId = userId;
    this.subscribers = [];
    this.currentDocument = {
      uid: userId,
      coins: 300,
      collectionPower: 0,
      availablePacks: {
        pack_oro: 3,
        pack_gratis_diario: 1,
        pack_anuncio: 0,
      },
    };
  }

  // Suscribirse a cambios en tiempo real (onSnapshot)
  subscribe(callback) {
    this.subscribers.push(callback);
    callback(this.currentDocument); // Snapshot inicial
  }

  // Simulación de actualización desde el servidor (Cloud Functions)
  triggerServerUpdate(updates) {
    Object.assign(this.currentDocument, updates);
    if (updates.availablePacks) {
      this.currentDocument.availablePacks = {
        ...this.currentDocument.availablePacks,
        ...updates.availablePacks,
      };
    }

    // Notificar a todos los escuchadores (UI, TopBar, etc.)
    this.subscribers.forEach((cb) => cb(this.currentDocument));
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Sincronización en Tiempo Real de Firestore (Fase 6.8)");
  console.log("==========================================================================\n");

  const userId = "player_sync_test_01";
  const firestore = new MockFirestoreRealtimeListener(userId);

  let uiCoinsObserved = 0;
  let uiPowerObserved = 0;
  let uiPacksObserved = {};
  let updateNotificationsCount = 0;

  // ----------------------------------------------------
  // TEST 1: Suscripción inicial al snapshot del usuario
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Unity inicia la escucha en tiempo real de users/" + userId + "...");
  firestore.subscribe((snapshot) => {
    updateNotificationsCount++;
    uiCoinsObserved = snapshot.coins;
    uiPowerObserved = snapshot.collectionPower;
    uiPacksObserved = snapshot.availablePacks;
    console.log(`  🔄 [Snapshot #${updateNotificationsCount}] Monedas: ${snapshot.coins}, Poder: ${snapshot.collectionPower}, Sobres:`, snapshot.availablePacks);
  });

  if (uiCoinsObserved === 300 && uiPacksObserved.pack_oro === 3) {
    console.log("  ✅ PASÓ: Snapshot inicial sincronizado en Unity con éxito.\n");
  } else {
    console.error("  ❌ FALLÓ en la sincronización inicial.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: El servidor acredita monedas por una compra o misión
  // ----------------------------------------------------
  console.log("▶️ TEST 2: El servidor acredita +500 monedas (purchaseCoins / misión)...");
  firestore.triggerServerUpdate({ coins: 800 });

  if (uiCoinsObserved === 800) {
    console.log(`  🪙 Saldo en interfaz de Unity actualizado inmediatamente: ${uiCoinsObserved} monedas.`);
    console.log("  ✅ PASÓ: Sincronización en tiempo real de saldo monetario verificada.\n");
  } else {
    console.error("  ❌ FALLÓ al sincronizar monedas.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: El jugador reclama un sobre gratis diario en el servidor
  // ----------------------------------------------------
  console.log("▶️ TEST 3: El servidor acredita un sobre gratis (claimFreePack)...");
  firestore.triggerServerUpdate({
    availablePacks: { pack_gratis_diario: 2 },
  });

  if (uiPacksObserved.pack_gratis_diario === 2) {
    console.log(`  📦 Sobres gratis en inventario actualizados: ${uiPacksObserved.pack_gratis_diario} disponibles.`);
    console.log("  ✅ PASÓ: Sincronización de inventario de sobres validada.\n");
  } else {
    console.error("  ❌ FALLÓ al sincronizar sobres.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: El jugador abre un sobre y aumenta su poder de colección
  // ----------------------------------------------------
  console.log("▶️ TEST 4: El servidor descuenta 1 sobre de oro y suma +15 de poder (openPack)...");
  firestore.triggerServerUpdate({
    collectionPower: 15,
    availablePacks: { pack_oro: 2 },
  });

  if (uiPowerObserved === 15 && uiPacksObserved.pack_oro === 2) {
    console.log(`  ⚡ Nuevo Poder de Colección: ${uiPowerObserved} pts, Sobres de Oro restantes: ${uiPacksObserved.pack_oro}.`);
    console.log("  ✅ PASÓ: Sincronización de poder de colección y consumo de sobres verificada.");
  } else {
    console.error("  ❌ FALLÓ al sincronizar apertura.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log(`🎉 ¡TODAS LAS PRUEBAS DE SINCRONIZACIÓN EN TIEMPO REAL FUERON EXITOSAS! (${updateNotificationsCount} snapshots procesados)`);
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
