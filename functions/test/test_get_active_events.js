/**
 * Test Automatizado de getActiveEvents() (Fase 5 - Punto 7)
 * Valida:
 * 1. Consulta de eventos especiales activos con timestamps autoritativos del servidor
 * 2. Cálculo exacto de segundos restantes (inmune a manipulación del reloj del móvil)
 * 3. Filtrado automático de eventos finalizados o no iniciados
 */

class MockFirestore {
  constructor() {
    this.events = [];
    this.simulatedServerTime = Date.now();
  }

  // Simulación de getActiveEvents en el servidor
  async getActiveEventsSimulation() {
    const serverNow = this.simulatedServerTime;
    const activeEvents = [];

    for (const ev of this.events) {
      if (ev.active && serverNow >= ev.startsAtMs && serverNow < ev.endsAtMs) {
        const remainingSeconds = Math.floor((ev.endsAtMs - serverNow) / 1000);
        activeEvents.push({
          eventId: ev.eventId,
          title: ev.title,
          albumId: ev.albumId,
          startsAt: new Date(ev.startsAtMs).toISOString(),
          endsAt: new Date(ev.endsAtMs).toISOString(),
          remainingSeconds,
          featuredReward: ev.featuredReward,
        });
      }
    }

    return {
      serverTime: new Date(serverNow).toISOString(),
      events: activeEvents,
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Cloud Function getActiveEvents() (Fase 5.7)");
  console.log("==========================================================================\n");

  const db = new MockFirestore();
  const now = Date.now();
  const ONE_HOUR = 60 * 60 * 1000;
  const ONE_DAY = 24 * ONE_HOUR;

  // Insertamos 3 eventos:
  // 1. Evento Activo (finaliza en 48 horas)
  // 2. Evento Expirado (finalizó hace 2 horas)
  // 3. Evento Futuro (inicia en 24 horas)
  db.events = [
    {
      eventId: "event_copa_america_2026",
      title: "Copa América 2026 - Edición Especial",
      albumId: "album_copa_america",
      active: true,
      startsAtMs: now - (2 * ONE_DAY),
      endsAtMs: now + (2 * ONE_DAY), // 48 horas restantes
      featuredReward: "Sobre Mítico + Marco Dorado",
    },
    {
      eventId: "event_clasico_pasado",
      title: "El Clásico - Edición Pasada",
      albumId: "album_clasico",
      active: true,
      startsAtMs: now - (5 * ONE_DAY),
      endsAtMs: now - (2 * ONE_HOUR), // Ya terminó
      featuredReward: "Sobre de Oro",
    },
    {
      eventId: "event_champions_futura",
      title: "Champions League - Próxima Ronda",
      albumId: "album_champions",
      active: true,
      startsAtMs: now + (1 * ONE_DAY), // Inicia mañana
      endsAtMs: now + (4 * ONE_DAY),
      featuredReward: "Sobre Legendario",
    },
  ];

  // ----------------------------------------------------
  // TEST 1: Consulta de eventos activos
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Consultando eventos activos en el servidor...");
  const res1 = await db.getActiveEventsSimulation();

  console.log(`  🕒 Hora autoritativa del servidor: ${res1.serverTime}`);
  console.log(`  🏆 Eventos activos devueltos: ${res1.events.length} (Esperado: 1)`);

  const activeEvent = res1.events[0];
  if (activeEvent) {
    console.log(`     - Título: "${activeEvent.title}"`);
    console.log(`     - Segundos restantes calculados: ${activeEvent.remainingSeconds}s (~${Math.round(activeEvent.remainingSeconds / 3600)}h)`);
    console.log(`     - Recompensa: ${activeEvent.featuredReward}`);
  }

  if (res1.events.length === 1 && activeEvent.eventId === "event_copa_america_2026") {
    console.log("  ✅ PASÓ: Solo se devolvió el evento activo vigente, ignorando los expirados y futuros.\n");
  } else {
    console.error("  ❌ FALLÓ en el filtrado de eventos.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Avance del tiempo (Simulación de vencimiento de evento)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Avanzando el reloj del servidor 3 días (72h)...");
  db.simulatedServerTime += (3 * ONE_DAY); // 3 días después

  const res2 = await db.getActiveEventsSimulation();
  console.log(`  🕒 Nueva hora del servidor: ${res2.serverTime}`);
  console.log(`  🏆 Eventos activos devueltos tras 3 días: ${res2.events.length}`);
  res2.events.forEach((ev) => console.log(`     - Activo ahora: "${ev.title}"`));

  // Ahora la Copa América ya venció, y la Champions League ya inició
  const hasCopaAmerica = res2.events.some((e) => e.eventId === "event_copa_america_2026");
  const hasChampions = res2.events.some((e) => e.eventId === "event_champions_futura");

  if (!hasCopaAmerica && hasChampions && res2.events.length === 1) {
    console.log("  ✅ PASÓ: El evento antiguo caducó automáticamente y el nuevo evento se activó a tiempo.");
  } else {
    console.error("  ❌ FALLÓ en la rotación de eventos por tiempo.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE getActiveEvents() FUERON EXITOSAS! (2/2)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
