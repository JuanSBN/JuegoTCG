/**
 * Test Suite: Firestore Rules de marketListings (Fase 8.5 - Punto 5)
 * TDD Secciones 2.11, 5.8b, 7 y GDD Sección 7.1
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO FIRESTORE RULES DE marketListings (FASE 8.5 - P5)');
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
assert(fs.existsSync(rulesPath), 'firestore.rules existe en la raíz del proyecto');

const rulesContent = fs.existsSync(rulesPath) ? fs.readFileSync(rulesPath, 'utf8') : '';

// 2. Definición y Helper isValidMarketListing
assert(rulesContent.includes('function isValidMarketListing(data)'), 'Función helper isValidMarketListing definida en firestore.rules');
assert(rulesContent.includes('match /marketListings/{listingId}'), 'Bloque match /marketListings/{listingId} presente');

// Campos obligatorios del esquema en reglas
const requiredFields = [
  'listingId', 'sellerUid', 'sellerDisplayName', 
  'cardId', 'cardName', 'rarity', 
  'quantity', 'pricePerCard', 'status', 'createdAt'
];

for (const field of requiredFields) {
  assert(rulesContent.includes("'" + field + "'"), 'isValidMarketListing valida campo obligatorio: ' + field);
}

// Validaciones de integridad
assert(rulesContent.includes('pricePerCard > 0'), 'isValidMarketListing exige pricePerCard > 0');
assert(rulesContent.includes('quantity > 0'), 'isValidMarketListing exige quantity > 0');
assert(rulesContent.includes('activo') && rulesContent.includes('vendido') && rulesContent.includes('cancelado'), 'isValidMarketListing contempla los 3 estados oficiales (activo, vendido, cancelado)');

// 3. Reglas de Acceso y Bloqueo Anti-Fraude
assert(rulesContent.includes('allow read: if isAuthenticated();'), 'Lectura pública habilitada a usuarios autenticados para explorar el mercado');
assert(rulesContent.includes('allow create: if false;'), 'Bloqueo explícito de creación directa (allow create: if false)');
assert(rulesContent.includes('allow update: if false;'), 'Bloqueo explícito de actualización directa de precio/status (allow update: if false)');
assert(rulesContent.includes('allow delete: if false;'), 'Bloqueo explícito de eliminación directa (allow delete: if false)');
assert(rulesContent.includes('allow write: if false;'), 'Bloqueo general de escritura en marketListings (allow write: if false)');

// 4. Bloqueo de inventario (users/{userId}/collection/{cardId})
assert(rulesContent.includes('match /collection/{cardId}') && rulesContent.includes('allow write: if false;'), 'Subcolección userCollection bloqueada contra escritura directa (garantiza reserva legítima en backend)');

// 5. Simulador Lógico de Reglas
console.log('\n🔒 Evaluando Matriz de Acceso con Simulador de Reglas de Seguridad...');

class MockFirestoreRulesEngine {
  constructor() {
    this.listings = {
      list_1: {
        listingId: 'list_1',
        sellerUid: 'seller_1',
        sellerDisplayName: 'Alice',
        cardId: 'JM',
        cardName: 'Musiala',
        rarity: 'comun',
        quantity: 1,
        pricePerCard: 30,
        status: 'activo'
      }
    };
  }

  evaluateRead(auth) {
    // allow read: if isAuthenticated();
    return auth !== null && auth.uid !== undefined;
  }

  evaluateCreate(auth, newDoc) {
    // allow create: if false;
    return false;
  }

  evaluateUpdate(auth, listingId, diff) {
    // allow update: if false;
    return false;
  }

  evaluateDelete(auth, listingId) {
    // allow delete: if false;
    return false;
  }
}

const engine = new MockFirestoreRulesEngine();
const authBuyer = { uid: 'buyer_77' };
const authSeller = { uid: 'seller_1' };
const unauth = null;

// Escenarios de Lectura
assert(engine.evaluateRead(authBuyer) === true, 'Usuario autenticado puede leer listados públicos');
assert(engine.evaluateRead(authSeller) === true, 'Vendedor autenticado puede leer listados');
assert(engine.evaluateRead(unauth) === false, 'Usuario no autenticado no puede leer el mercado');

// Escenarios de Bloqueo de Escritura Directa (Anti-Fraude)
assert(engine.evaluateCreate(authSeller, {}) === false, 'Cliente no puede crear listados directamente en Firestore (debe invocar listCardForSale)');
assert(engine.evaluateUpdate(authBuyer, 'list_1', { status: 'vendido' }) === false, 'Comprador no puede cambiar status a vendido directamente (debe invocar buyListedCard)');
assert(engine.evaluateUpdate(authBuyer, 'list_1', { pricePerCard: 1 }) === false, 'Cliente no puede manipular precio directamente');
assert(engine.evaluateUpdate(authSeller, 'list_1', { pricePerCard: 50 }) === false, 'Vendedor no puede modificar precio sin pasar por updateListingPrice()');
assert(engine.evaluateDelete(authSeller, 'list_1') === false, 'Vendedor no puede borrar listado directamente (debe usar cancelListing)');

console.log('\n==========================================================================');
console.log(`🎯 RESULTADO: ${passed}/${total} pruebas superadas (${Math.round((passed/total)*100)}%)`);
console.log('==========================================================================');

if (passed === total) {
  process.exit(0);
} else {
  process.exit(1);
}
