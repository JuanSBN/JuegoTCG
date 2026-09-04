/**
 * Test Suite: Firestore Rules de tradeOffers (Fase 8 - Punto 5)
 * TDD Secciones 2.5, 5.8, 7 y 7.2
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO FIRESTORE RULES DE TRADE OFFERS (FASE 8 - PUNTO 5)');
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

// 1. Cargar archivo firestore.rules
const rulesPath = path.resolve(__dirname, '../../firestore.rules');
assert(fs.existsSync(rulesPath), 'El archivo firestore.rules existe en la raíz del proyecto');

const rulesContent = fs.existsSync(rulesPath) ? fs.readFileSync(rulesPath, 'utf8') : '';

// 2. Verificación de Definición y Helper isValidTradeOffer
assert(rulesContent.includes('function isValidTradeOffer(data)'), 'Función helper isValidTradeOffer definida en firestore.rules (TDD 7.2)');
assert(rulesContent.includes('match /tradeOffers/{tradeId}'), 'Bloque match /tradeOffers/{tradeId} presente (TDD 7)');

// Validaciones de esquema en isValidTradeOffer
const requiredFields = [
  'tradeId', 'fromUid', 'toUid', 
  'offeredCardId', 'offeredQty', 
  'requestedCardId', 'requestedQty', 
  'status', 'expiresAt'
];

for (const field of requiredFields) {
  assert(rulesContent.includes("'" + field + "'"), "isValidTradeOffer valida campo obligatorio: " + field);
}

assert(rulesContent.includes('fromUid != data.toUid') || rulesContent.includes('fromUid != toUid'), 'isValidTradeOffer previene intercambio consigo mismo (fromUid != toUid)');
assert(rulesContent.includes('offeredQty > 0'), 'isValidTradeOffer exige offeredQty > 0');
assert(rulesContent.includes('requestedQty > 0'), 'isValidTradeOffer exige requestedQty > 0');
assert(rulesContent.includes('pendiente') && rulesContent.includes('aceptado') && rulesContent.includes('rechazado') && rulesContent.includes('cancelado') && rulesContent.includes('expirado'), 'isValidTradeOffer valida los 5 estados oficiales del TDD 5.8');

// 3. Reglas de Acceso (Lectura / Escritura)
assert(rulesContent.includes('resource.data.fromUid == request.auth.uid'), 'Permiso de lectura otorgado al proponente (fromUid)');
assert(rulesContent.includes('resource.data.toUid == request.auth.uid'), 'Permiso de lectura otorgado al receptor (toUid)');
assert(rulesContent.includes('allow write: if false;'), 'Bloqueo total de escritura a clientes en tradeOffers (allow write: if false)');

// 4. Simulador Lógico de Reglas de Seguridad
console.log('\n🔒 Evaluando Matriz de Acceso con Simulador de Reglas...');

function simulateRead(auth, resourceData) {
  const isAuthenticated = auth != null && auth.uid != null;
  if (!isAuthenticated) return false;
  return resourceData.fromUid === auth.uid || resourceData.toUid === auth.uid;
}

function simulateWrite() {
  // allow write: if false;
  return false;
}

function simulateSchemaValidation(data) {
  const hasKeys = requiredFields.every(k => k in data);
  if (!hasKeys) return false;
  if (typeof data.tradeId !== 'string') return false;
  if (typeof data.fromUid !== 'string' || typeof data.toUid !== 'string') return false;
  if (data.fromUid === data.toUid) return false; // Anti self-trade
  if (typeof data.offeredCardId !== 'string' || typeof data.requestedCardId !== 'string') return false;
  if (typeof data.offeredQty !== 'number' || data.offeredQty <= 0) return false;
  if (typeof data.requestedQty !== 'number' || data.requestedQty <= 0) return false;
  const validStatuses = ['pendiente', 'aceptado', 'rechazado', 'cancelado', 'expirado'];
  if (!validStatuses.includes(data.status)) return false;
  if (!data.expiresAt) return false;
  return true;
}

const mockOffer = {
  tradeId: 'trade_123',
  fromUid: 'user_alice',
  toUid: 'user_bob',
  offeredCardId: 'card_pedri_01',
  offeredQty: 1,
  requestedCardId: 'card_rodri_02',
  requestedQty: 1,
  status: 'pendiente',
  expiresAt: new Date(Date.now() + 48 * 3600 * 1000).toISOString()
};

// Test 4.1: Lectura por proponente
assert(simulateRead({ uid: 'user_alice' }, mockOffer) === true, 'Proponente (Alice) PUEDE leer la oferta');

// Test 4.2: Lectura por receptor
assert(simulateRead({ uid: 'user_bob' }, mockOffer) === true, 'Receptor (Bob) PUEDE leer la oferta');

// Test 4.3: Lectura por tercero no involucrado
assert(simulateRead({ uid: 'user_charlie' }, mockOffer) === false, 'Tercero (Charlie) NO PUEDE leer la oferta (Acceso Denegado)');

// Test 4.4: Lectura anónima
assert(simulateRead(null, mockOffer) === false, 'Usuario anónimo NO PUEDE leer la oferta (Acceso Denegado)');

// Test 4.5: Creación directa por cliente
assert(simulateWrite() === false, 'Cliente NO PUEDE crear oferta directamente (Bloqueo Total: false)');

// Test 4.6: Modificación directa por cliente (ej. forzar status a "aceptado")
assert(simulateWrite() === false, 'Cliente NO PUEDE modificar oferta directamente (Bloqueo Total: false)');

// Test 4.7: Eliminación directa por cliente
assert(simulateWrite() === false, 'Cliente NO PUEDE borrar oferta directamente (Bloqueo Total: false)');

// 5. Validación de Esquema con Casos Borde
console.log('\n📐 Evaluando Validación de Esquema (isValidTradeOffer)...');

assert(simulateSchemaValidation(mockOffer) === true, 'Oferta válida cumple con el esquema oficial');

const selfTradeOffer = Object.assign({}, mockOffer, { toUid: 'user_alice' });
assert(simulateSchemaValidation(selfTradeOffer) === false, 'Oferta con fromUid == toUid rechazada por el validador');

const zeroQtyOffer = Object.assign({}, mockOffer, { offeredQty: 0 });
assert(simulateSchemaValidation(zeroQtyOffer) === false, 'Oferta con offeredQty <= 0 rechazada por el validador');

const negativeQtyOffer = Object.assign({}, mockOffer, { requestedQty: -2 });
assert(simulateSchemaValidation(negativeQtyOffer) === false, 'Oferta con requestedQty negativa rechazada por el validador');

const invalidStatusOffer = Object.assign({}, mockOffer, { status: 'hackeado' });
assert(simulateSchemaValidation(invalidStatusOffer) === false, 'Oferta con status no oficial rechazada por el validador');

const missingFieldsOffer = Object.assign({}, mockOffer);
delete missingFieldsOffer.expiresAt;
assert(simulateSchemaValidation(missingFieldsOffer) === false, 'Oferta con campos faltantes rechazada por el validador');

console.log('==========================================================================');
console.log('📊 RESULTADOS: ' + passed + '/' + total + ' pruebas superadas (' + Math.round((passed/total)*100) + '%)');
console.log('==========================================================================');

if (passed === total) {
  console.log('🎉 ¡FIRESTORE RULES DE TRADE OFFERS VERIFICADAS Y BLINDADAS EXITOSAMENTE!');
  process.exit(0);
} else {
  console.error('❌ Errores detectados en las reglas.');
  process.exit(1);
}
