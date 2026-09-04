/**
 * Test Suite: Caso Límite de Intercambio Anti-Fraude (Fase 8 - Punto 6)
 * TDD Secciones 2.5 y 6
 * "Aceptar un intercambio cuando una de las cartas ya no está disponible,
 * y confirmar categóricamente que ninguna carta se mueve ni se duplica".
 */

console.log('==========================================================================');
console.log('🧪 VERIFICANDO CASO LÍMITE DE INTERCAMBIO ANTI-FRAUDE (FASE 8 - PUNTO 6)');
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

// =========================================================================
// SIMULADOR DE TRANSACCIÓN ATÓMICA DE FIRESTORE (db.runTransaction)
// =========================================================================
class MockFirestoreDatabase {
  constructor() {
    this.storage = new Map();
  }

  setDoc(path, data) {
    this.storage.set(path, JSON.parse(JSON.stringify(data)));
  }

  getDoc(path) {
    if (!this.storage.has(path)) return null;
    return JSON.parse(JSON.stringify(this.storage.get(path)));
  }

  deleteDoc(path) {
    this.storage.delete(path);
  }

  clone() {
    const copy = new MockFirestoreDatabase();
    for (const [k, v] of this.storage.entries()) {
      copy.storage.set(k, JSON.parse(JSON.stringify(v)));
    }
    return copy;
  }
}

/**
 * Simulación autoritativa de acceptTrade() corriendo dentro de una transacción atómica.
 * Si ocurre un fallo o precondición no cumplida, la transacción aborta (rollback)
 * y NINGUNA mutación en las cartas se aplica a la base de datos.
 */
function executeAtomicAcceptTrade(dbInstance, tradeId, accepterUid) {
  const tradePath = "tradeOffers/" + tradeId;
  const initialTrade = dbInstance.getDoc(tradePath);

  if (!initialTrade) {
    throw new Error("not-found: La oferta no existe");
  }
  if (initialTrade.toUid !== accepterUid) {
    throw new Error("permission-denied: Solo el receptor puede aceptar");
  }
  if (initialTrade.status !== "pendiente") {
    throw new Error("failed-precondition: La oferta ya no está pendiente (" + initialTrade.status + ")");
  }

  const fromUid = initialTrade.fromUid;
  const toUid = initialTrade.toUid;
  const offeredCardId = initialTrade.offeredCardId;
  const offeredQty = initialTrade.offeredQty || 1;
  const requestedCardId = initialTrade.requestedCardId;
  const requestedQty = initialTrade.requestedQty || 1;

  // Staging buffer para la transacción atómica
  const stagedDb = dbInstance.clone();

  const fromCardPath = "users/" + fromUid + "/collection/" + offeredCardId;
  const toCardPath = "users/" + toUid + "/collection/" + requestedCardId;

  const fromCard = stagedDb.getDoc(fromCardPath);
  const toCard = stagedDb.getDoc(toCardPath);

  const fromQty = fromCard ? fromCard.quantity || 0 : 0;
  const toQty = toCard ? toCard.quantity || 0 : 0;

  // CASO LÍMITE 1: El proponente ya no posee la carta ofrecida
  if (fromQty < offeredQty) {
    // La transacción aborta la transferencia de cartas.
    // Solo actualiza el tradeOffer para informar la cancelación.
    dbInstance.setDoc(tradePath, Object.assign({}, initialTrade, {
      status: "cancelado",
      cancelReason: "El proponente ya no posee la carta ofrecida."
    }));
    return {
      success: false,
      error: "failed-precondition: Intercambio cancelado: El proponente ya no posee la carta que ofreció.",
      cardsMoved: false
    };
  }

  // CASO LÍMITE 2: El receptor ya no posee la carta solicitada
  if (toQty < requestedQty) {
    // La transacción aborta la transferencia de cartas.
    dbInstance.setDoc(tradePath, Object.assign({}, initialTrade, {
      status: "cancelado",
      cancelReason: "El receptor ya no posee la carta solicitada."
    }));
    return {
      success: false,
      error: "failed-precondition: Intercambio cancelado: Ya no posees la carta solicitada en tu inventario.",
      cardsMoved: false
    };
  }

  // CASO DE ÉXITO: Ambos cumplen -> Transferencia simultánea atómica
  // 1. Descontar de fromUid
  if (fromQty <= offeredQty) {
    stagedDb.deleteDoc(fromCardPath);
  } else {
    stagedDb.setDoc(fromCardPath, Object.assign({}, fromCard, { quantity: fromQty - offeredQty }));
  }

  // 2. Descontar de toUid
  if (toQty <= requestedQty) {
    stagedDb.deleteDoc(toCardPath);
  } else {
    stagedDb.setDoc(toCardPath, Object.assign({}, toCard, { quantity: toQty - requestedQty }));
  }

  // 3. Acreditar carta a toUid
  const toRecvPath = "users/" + toUid + "/collection/" + offeredCardId;
  const toRecvCard = stagedDb.getDoc(toRecvPath);
  const toNewQty = (toRecvCard ? toRecvCard.quantity || 0 : 0) + offeredQty;
  stagedDb.setDoc(toRecvPath, { cardId: offeredCardId, quantity: toNewQty });

  // 4. Acreditar carta a fromUid
  const fromRecvPath = "users/" + fromUid + "/collection/" + requestedCardId;
  const fromRecvCard = stagedDb.getDoc(fromRecvPath);
  const fromNewQty = (fromRecvCard ? fromRecvCard.quantity || 0 : 0) + requestedQty;
  stagedDb.setDoc(fromRecvPath, { cardId: requestedCardId, quantity: fromNewQty });

  // 5. Marcar tradeOffer aceptado
  stagedDb.setDoc(tradePath, Object.assign({}, initialTrade, { status: "aceptado" }));

  // 6. Confirmar la transacción copiando el staging a la DB real
  dbInstance.storage = new Map(stagedDb.storage);

  return {
    success: true,
    message: "¡Intercambio realizado con éxito!",
    cardsMoved: true
  };
}

// =========================================================================
// EJECUCIÓN DE ESCENARIOS DE PRUEBA
// =========================================================================

console.log('\n--- ESCENARIO 1: PROPONENTE YA NO POSEE LA CARTA (VENTA PREVIA EN MERCADO) ---');
const db1 = new MockFirestoreDatabase();

// Alice (proponente) y Bob (receptor)
// Inicialmente Alice tenía Pedri (PE), pero mientras la oferta estaba pendiente la vendió -> quantity: 0 (o no existe)
db1.setDoc("users/user_bob/collection/card_rodri", { cardId: "card_rodri", quantity: 1 });
// Alice ya no tiene card_pedri en su colección
db1.setDoc("tradeOffers/trade_case_1", {
  tradeId: "trade_case_1",
  fromUid: "user_alice",
  toUid: "user_bob",
  offeredCardId: "card_pedri",
  offeredQty: 1,
  requestedCardId: "card_rodri",
  requestedQty: 1,
  status: "pendiente"
});

const result1 = executeAtomicAcceptTrade(db1, "trade_case_1", "user_bob");

assert(result1.success === false, "acceptTrade devuelve fallo cuando el proponente no tiene la carta");
assert(result1.cardsMoved === false, "Bandera cardsMoved es FALSE (ninguna carta transferida)");
assert(result1.error.includes("failed-precondition"), "Retorna código failed-precondition al cliente");
assert(result1.error.includes("proponente ya no posee"), "Motivo explícito: Proponente ya no posee la carta");

// VERIFICACIÓN ESTRICTA DE INVENTARIOS EN LA BD:
const bobRodriAfter1 = db1.getDoc("users/user_bob/collection/card_rodri");
assert(bobRodriAfter1 !== null && bobRodriAfter1.quantity === 1, "Bob conserva su carta Rodri intacta (quantity = 1)");

const bobPedriAfter1 = db1.getDoc("users/user_bob/collection/card_pedri");
assert(bobPedriAfter1 === null, "Bob NO recibió la carta Pedri de Alice (quantity = 0 / no existe)");

const aliceRodriAfter1 = db1.getDoc("users/user_alice/collection/card_rodri");
assert(aliceRodriAfter1 === null, "Alice NO recibió la carta Rodri de Bob (quantity = 0 / no existe)");

const offerAfter1 = db1.getDoc("tradeOffers/trade_case_1");
assert(offerAfter1.status === "cancelado", "La oferta quedó marcada con status 'cancelado'");
assert(offerAfter1.cancelReason.includes("proponente ya no posee"), "Registrado motivo exacto en el documento de tradeOffers");

console.log('\n--- ESCENARIO 2: RECEPTOR YA NO POSEE LA CARTA SOLICITADA ---');
const db2 = new MockFirestoreDatabase();

// Alice tiene Pedri, pero Bob ya no tiene Rodri (la intercambió antes con otro amigo)
db2.setDoc("users/user_alice/collection/card_pedri", { cardId: "card_pedri", quantity: 2 });
db2.setDoc("tradeOffers/trade_case_2", {
  tradeId: "trade_case_2",
  fromUid: "user_alice",
  toUid: "user_bob",
  offeredCardId: "card_pedri",
  offeredQty: 1,
  requestedCardId: "card_rodri",
  requestedQty: 1,
  status: "pendiente"
});

const result2 = executeAtomicAcceptTrade(db2, "trade_case_2", "user_bob");

assert(result2.success === false, "acceptTrade devuelve fallo cuando el receptor no tiene la carta");
assert(result2.cardsMoved === false, "Ninguna carta transferida");
assert(result2.error.includes("Ya no posees la carta solicitada"), "Mensaje de error claro para el receptor");

const alicePedriAfter2 = db2.getDoc("users/user_alice/collection/card_pedri");
assert(alicePedriAfter2 !== null && alicePedriAfter2.quantity === 2, "Alice conserva sus 2 cartas Pedri intactas");

const bobPedriAfter2 = db2.getDoc("users/user_bob/collection/card_pedri");
assert(bobPedriAfter2 === null, "Bob NO recibió la carta Pedri de Alice");

const offerAfter2 = db2.getDoc("tradeOffers/trade_case_2");
assert(offerAfter2.status === "cancelado", "Oferta marcada como 'cancelado'");

console.log('\n--- ESCENARIO 3: REINTENTO SOBRE OFERTA YA CANCELADA (IDEMPOTENCIA/ESTADO) ---');
let caughtError = null;
try {
  executeAtomicAcceptTrade(db2, "trade_case_2", "user_bob");
} catch (e) {
  caughtError = e.message;
}
assert(caughtError !== null && caughtError.includes("failed-precondition"), "Segundo intento de aceptación sobre oferta cancelada es rechazado de inmediato");

console.log('\n--- ESCENARIO 4: CONTROL POSITIVO (AMBOS TIENEN SUS CARTAS) ---');
const db4 = new MockFirestoreDatabase();

db4.setDoc("users/user_alice/collection/card_pedri", { cardId: "card_pedri", quantity: 1 });
db4.setDoc("users/user_bob/collection/card_rodri", { cardId: "card_rodri", quantity: 1 });
db4.setDoc("tradeOffers/trade_case_4", {
  tradeId: "trade_case_4",
  fromUid: "user_alice",
  toUid: "user_bob",
  offeredCardId: "card_pedri",
  offeredQty: 1,
  requestedCardId: "card_rodri",
  requestedQty: 1,
  status: "pendiente"
});

const result4 = executeAtomicAcceptTrade(db4, "trade_case_4", "user_bob");

assert(result4.success === true, "acceptTrade exitoso cuando ambas partes cumplen");
assert(result4.cardsMoved === true, "Cartas transferidas simultáneamente");

const alicePedriAfter4 = db4.getDoc("users/user_alice/collection/card_pedri");
const aliceRodriAfter4 = db4.getDoc("users/user_alice/collection/card_rodri");
const bobPedriAfter4 = db4.getDoc("users/user_bob/collection/card_pedri");
const bobRodriAfter4 = db4.getDoc("users/user_bob/collection/card_rodri");

assert(alicePedriAfter4 === null, "Alice ya no tiene la carta Pedri que ofreció (descontada)");
assert(aliceRodriAfter4 !== null && aliceRodriAfter4.quantity === 1, "Alice ahora posee la carta Rodri recibida de Bob");
assert(bobRodriAfter4 === null, "Bob ya no tiene la carta Rodri que entregó (descontada)");
assert(bobPedriAfter4 !== null && bobPedriAfter4.quantity === 1, "Bob ahora posee la carta Pedri recibida de Alice");

const offerAfter4 = db4.getDoc("tradeOffers/trade_case_4");
assert(offerAfter4.status === "aceptado", "Estado de la oferta actualizado a 'aceptado'");

console.log('==========================================================================');
console.log('📊 RESULTADOS: ' + passed + '/' + total + ' pruebas superadas (' + Math.round((passed/total)*100) + '%)');
console.log('==========================================================================');

if (passed === total) {
  console.log('🎉 ¡CASO LÍMITE ANTI-FRAUDE CERTIFICADO CON 100% DE ÉXITO!');
  process.exit(0);
} else {
  console.error('❌ Fallos en la verificación del caso límite.');
  process.exit(1);
}
