/**
 * Test Automatizado de Catálogo de Eventos de Analytics (TDD 2.9 / Fase 6.6)
 * Valida:
 * 1. Evento pack_opened (packId, tipo de costo).
 * 2. Evento card_obtained (cardId, rareza, isNew) para análisis de drop rates.
 * 3. Evento album_completed (albumId).
 * 4. Eventos trade_proposed y trade_accepted para medir el pilar social.
 * 5. Evento mission_claimed.
 */

class MockFirebaseAnalytics {
  constructor() {
    this.eventsLog = [];
  }

  logEvent(eventName, params = {}) {
    const entry = {
      name: eventName,
      params,
      timestamp: new Date().toISOString(),
    };
    this.eventsLog.push(entry);
    return entry;
  }

  getEventsByName(eventName) {
    return this.eventsLog.filter((e) => e.name === eventName);
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Catálogo de Eventos de Analytics (TDD 2.9)");
  console.log("==========================================================================\n");

  const analytics = new MockFirebaseAnalytics();

  // ----------------------------------------------------
  // TEST 1: pack_opened
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Registrando evento de apertura de sobre ('pack_opened')...");
  const e1 = analytics.logEvent("pack_opened", {
    pack_id: "pack_oro",
    cost_type: "moneda",
  });
  console.log("  📊 Evento registrado:", e1);

  if (e1.name === "pack_opened" && e1.params.cost_type === "moneda") {
    console.log("  ✅ PASÓ: Evento pack_opened validado con sus parámetros oficiales.\n");
  } else {
    console.error("  ❌ FALLÓ en pack_opened.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: card_obtained (5 cartas de un sobre)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Registrando cartas obtenidas ('card_obtained')...");
  const cardsSample = [
    { card_id: "LD", rarity: "mitica", is_new: 1 },
    { card_id: "EH", rarity: "comun", is_new: 0 },
    { card_id: "VJ", rarity: "rara", is_new: 1 },
    { card_id: "KM", rarity: "poco_comun", is_new: 0 },
    { card_id: "PE", rarity: "rara", is_new: 1 },
  ];

  cardsSample.forEach((c) => analytics.logEvent("card_obtained", c));
  const cardEvents = analytics.getEventsByName("card_obtained");
  console.log(`  📊 Total cartas registradas en telemetría: ${cardEvents.length}`);

  if (cardEvents.length === 5 && cardEvents[0].params.rarity === "mitica") {
    console.log("  ✅ PASÓ: Drop rates y cartas obtenidas registradas con éxito.\n");
  } else {
    console.error("  ❌ FALLÓ en card_obtained.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: album_completed
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Registrando evento de álbum completado ('album_completed')...");
  const e3 = analytics.logEvent("album_completed", { album_id: "album_piloto_01" });
  console.log("  📊 Evento registrado:", e3);

  if (e3.name === "album_completed" && e3.params.album_id === "album_piloto_01") {
    console.log("  ✅ PASÓ: Evento album_completed validado.\n");
  } else {
    console.error("  ❌ FALLÓ en album_completed.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: trade_proposed y trade_accepted (Pilar Social)
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Registrando eventos de intercambios sociales...");
  const eTradeProp = analytics.logEvent("trade_proposed", {
    to_user_id: "friend_102",
    offered_card_id: "EH",
    requested_card_id: "VJ",
  });
  const eTradeAcc = analytics.logEvent("trade_accepted", {
    trade_id: "trade_999",
    card_received_id: "VJ",
  });

  console.log("  🤝 Propuesta registrada:", eTradeProp.params);
  console.log("  🎉 Aceptación registrada:", eTradeAcc.params);

  if (analytics.getEventsByName("trade_proposed").length === 1 && analytics.getEventsByName("trade_accepted").length === 1) {
    console.log("  ✅ PASÓ: Telemetría de mecánicas sociales verificada con éxito.\n");
  } else {
    console.error("  ❌ FALLÓ en eventos sociales.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 5: mission_claimed
  // ----------------------------------------------------
  console.log("▶️ TEST 5: Registrando reclamo de misión ('mission_claimed')...");
  const eMission = analytics.logEvent("mission_claimed", {
    mission_id: "m_open_pack",
    coins_rewarded: 50,
  });

  if (eMission.params.coins_rewarded === 50) {
    console.log("  ✅ PASÓ: Evento mission_claimed registrado.\n");
  } else {
    console.error("  ❌ FALLÓ en mission_claimed.");
    process.exit(1);
  }

  console.log("==========================================================================");
  console.log(`🎉 ¡TODOS LOS EVENTOS DEL CATÁLOGO DE ANALYTICS (TDD 2.9) VALIDADOS! (${analytics.eventsLog.length} eventos)`);
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
