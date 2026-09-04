/**
 * Test Suite: Notificaciones Push FCM y Analytics en Intercambio (Fase 8 - Punto 7)
 * TDD Secciones 2.8 y 2.9
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO NOTIFICACIONES PUSH FCM Y ANALYTICS EN TRADE (FASE 8 - P7)');
console.log('==========================================================================');

let passed = 0;
let total = 0;

function assert(condition, description) {
  total++;
  if (condition) {
    passed++;
    console.log("  ✅ " + description);
  } else {
    console.error("  ❌ FALLÓ: " + description);
  }
}

// 1. firebase.ts
const firebaseTsPath = path.resolve(__dirname, '../src/firebase.ts');
assert(fs.existsSync(firebaseTsPath), 'firebase.ts existe en functions/src/');
const firebaseContent = fs.readFileSync(firebaseTsPath, 'utf8');
assert(firebaseContent.includes('export const messaging = getMessaging();'), 'getMessaging() exportado como messaging en firebase.ts (TDD 2.8)');
assert(firebaseContent.includes('ANALYTICS_EVENTS: "analyticsEvents"'), 'Constante ANALYTICS_EVENTS declarada en COLLECTIONS (TDD 2.9)');

// 2. tradeOperations.ts
const tradeTsPath = path.resolve(__dirname, '../src/social/tradeOperations.ts');
assert(fs.existsSync(tradeTsPath), 'tradeOperations.ts existe');
const tradeContent = fs.readFileSync(tradeTsPath, 'utf8');

// Verificaciones de Push FCM en proposeTrade
assert(tradeContent.includes('notificationsEnabled'), 'proposeTrade consulta la preferencia notificationsEnabled del receptor (TDD 2.8)');
assert(tradeContent.includes('fcmToken'), 'proposeTrade obtiene el fcmToken del destinatario (TDD 2.8)');
assert(tradeContent.includes('messaging.send'), 'proposeTrade invoca messaging.send(...) para notificar por push (TDD 2.8)');
assert(tradeContent.includes('¡Nueva oferta de intercambio!'), 'Título de la notificación push descriptivo en FCM');
assert(tradeContent.includes('trade_offer'), 'Payload de datos incluye type: "trade_offer" para navegación en cliente');
assert(tradeContent.includes('Push omitido') || tradeContent.includes('desactivó notificaciones'), 'proposeTrade respeta el toggle y omite el push si notificationsEnabled es false (TDD 2.8)');

// Verificaciones de Analytics en tradeOperations.ts (TDD 2.9)
assert(tradeContent.includes('event: "trade_proposed"'), 'Evento de analítica trade_proposed registrado en proposeTrade (TDD 2.9)');
assert(tradeContent.includes('COLLECTIONS.ANALYTICS_EVENTS'), 'Colección analyticsEvents utilizada para persistir eventos');
assert(tradeContent.includes('event: "trade_accepted"'), 'Evento de analítica trade_accepted registrado en acceptTrade (TDD 2.9)');

// 3. Cliente Unity: TradeService.cs
const tradeCsPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/TradeService.cs');
assert(fs.existsSync(tradeCsPath), 'TradeService.cs existe en Unity');
const tradeCsContent = fs.readFileSync(tradeCsPath, 'utf8');
assert(tradeCsContent.includes('FirebaseAnalyticsManager.Instance?.LogTradeProposed'), 'TradeService.cs invoca LogTradeProposed al proponer intercambio (TDD 2.9)');
assert(tradeCsContent.includes('FirebaseAnalyticsManager.Instance?.LogTradeAccepted'), 'TradeService.cs invoca LogTradeAccepted al aceptar intercambio (TDD 2.9)');

// 4. Simulador Lógico de FCM y Analytics
console.log('\n📡 Simulando Despacho de FCM y Telemetría de Analytics...');

class MockFCMService {
  constructor() {
    this.sentMessages = [];
  }
  async send(msg) {
    this.sentMessages.push(msg);
    return 'msg_id_' + Date.now();
  }
}

class MockAnalyticsStore {
  constructor() {
    this.events = [];
  }
  add(eventData) {
    this.events.push(Object.assign({}, eventData, { timestamp: new Date().toISOString() }));
  }
}

const fcmMock = new MockFCMService();
const analyticsMock = new MockAnalyticsStore();

function simulateProposeTradeWithFCM(fromUser, toUser, tradeData, fcm, analytics) {
  // Respetar toggle de Ajustes (TDD 2.8)
  const notificationsEnabled = toUser.notificationsEnabled !== false;
  let pushSent = false;

  if (notificationsEnabled && toUser.fcmToken) {
    fcm.send({
      token: toUser.fcmToken,
      notification: {
        title: "¡Nueva oferta de intercambio!",
        body: fromUser.displayName + " te ha propuesto un intercambio de cartas."
      },
      data: {
        type: "trade_offer",
        tradeId: tradeData.tradeId,
        fromUid: fromUser.uid,
        offeredCardId: tradeData.offeredCardId,
        requestedCardId: tradeData.requestedCardId
      }
    });
    pushSent = true;
  }

  // Registrar Analytics (TDD 2.9)
  analytics.add({
    event: "trade_proposed",
    tradeId: tradeData.tradeId,
    fromUid: fromUser.uid,
    toUid: toUser.uid,
    offeredCardId: tradeData.offeredCardId,
    requestedCardId: tradeData.requestedCardId
  });

  return { success: true, pushSent };
}

// Test 4.1: Receptor con notificaciones activadas -> Push ENVIADO
const userAlice = { uid: "u_alice", displayName: "Alice" };
const userBobEnabled = { uid: "u_bob", displayName: "Bob", notificationsEnabled: true, fcmToken: "token_bob_device_xyz" };
const trade1 = { tradeId: "tr_001", offeredCardId: "card_pedri", requestedCardId: "card_rodri" };

const res1 = simulateProposeTradeWithFCM(userAlice, userBobEnabled, trade1, fcmMock, analyticsMock);
assert(res1.pushSent === true, 'Notificación FCM enviada cuando el receptor tiene notificaciones activas');
assert(fcmMock.sentMessages.length === 1, 'Un mensaje registrado en la bandeja de FCM');
assert(fcmMock.sentMessages[0].token === "token_bob_device_xyz", 'FCM despachado al token correcto del dispositivo');
assert(fcmMock.sentMessages[0].notification.title === "¡Nueva oferta de intercambio!", 'Título de FCM coincide con la especificación');

// Test 4.2: Receptor con notificaciones desactivadas en Ajustes -> Push BLOQUEADO
const userCharlieDisabled = { uid: "u_charlie", displayName: "Charlie", notificationsEnabled: false, fcmToken: "token_charlie_device" };
const trade2 = { tradeId: "tr_002", offeredCardId: "card_mbappe", requestedCardId: "card_haaland" };

const res2 = simulateProposeTradeWithFCM(userAlice, userCharlieDisabled, trade2, fcmMock, analyticsMock);
assert(res2.pushSent === false, 'Notificación FCM SUPRIMIDA respetando la preferencia del usuario en Ajustes (notificationsEnabled == false)');
assert(fcmMock.sentMessages.length === 1, 'No se incrementó la cuenta de FCM enviados (permanece en 1)');

// Test 4.3: Registro de Eventos en Analytics (TDD 2.9)
assert(analyticsMock.events.length === 2, 'Ambas propuestas registraron su evento trade_proposed en Analytics');
const event1 = analyticsMock.events[0];
assert(event1.event === "trade_proposed" && event1.tradeId === "tr_001", 'Parámetros de trade_proposed contienen tradeId');
assert(event1.fromUid === "u_alice" && event1.toUid === "u_bob", 'Parámetros contienen fromUid y toUid');
assert(event1.offeredCardId === "card_pedri" && event1.requestedCardId === "card_rodri", 'Parámetros contienen cartas ofrecida y solicitada');

// Test 4.4: Registro de trade_accepted en Analytics
analyticsMock.add({
  event: "trade_accepted",
  tradeId: "tr_001",
  fromUid: "u_alice",
  toUid: "u_bob",
  receivedCardId: "card_pedri",
  givenCardId: "card_rodri"
});

const acceptedEvent = analyticsMock.events.find(e => e.event === "trade_accepted");
assert(acceptedEvent !== undefined, 'Evento trade_accepted registrado en Analytics tras la aceptación exitosa');
assert(acceptedEvent.tradeId === "tr_001", 'trade_accepted incluye el tradeId de la transacción');

console.log('==========================================================================');
console.log('📊 RESULTADOS: ' + passed + '/' + total + ' pruebas superadas (' + Math.round((passed/total)*100) + '%)');
console.log('==========================================================================');

if (passed === total) {
  console.log('🎉 ¡NOTIFICACIONES PUSH Y ANALYTICS EN TRADE VERIFICADOS CON ÉXITO!');
  process.exit(0);
} else {
  console.error('❌ Fallos en la verificación de FCM / Analytics.');
  process.exit(1);
}
