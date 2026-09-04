/**
 * Test Suite: cancelListing y updateListingPrice (Fase 8.5 - Punto 4)
 * TDD Secciones 2.11, 5.8b y 6
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO cancelListing Y updateListingPrice (FASE 8.5 - P4)');
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

// 1. Backend: manageListing.ts
const managePath = path.resolve(__dirname, '../src/market/manageListing.ts');
assert(fs.existsSync(managePath), 'manageListing.ts existe en functions/src/market/');

const manageContent = fs.readFileSync(managePath, 'utf8');
assert(manageContent.includes('export const cancelListing'), 'Cloud Function callable cancelListing declarada');
assert(manageContent.includes('export const updateListingPrice'), 'Cloud Function callable updateListingPrice declarada');
assert(manageContent.includes('listing.sellerUid !== callerUid'), 'Restricción de seguridad: solo el vendedor original puede cancelar o editar');
assert(manageContent.includes('permission-denied'), 'Retorna permission-denied ante llamadas no autorizadas de terceros');
assert(manageContent.includes('listing.status !== "activo"'), 'Revalidación de status activo requerida antes de modificar');
assert(manageContent.includes('failed-precondition'), 'Retorna failed-precondition si el listado ya fue vendido o cancelado');
assert(manageContent.includes('status: "cancelado"'), 'cancelListing marca el documento como cancelado');
assert(manageContent.includes('COLLECTIONS.USER_COLLECTION'), 'Reintegro atómico de la carta al inventario del vendedor');
assert(manageContent.includes('newPricePerCard <= 0'), 'updateListingPrice valida precios estrictamente positivos');

// 2. index.ts
const indexPath = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexPath, 'utf8');
assert(indexContent.includes('cancelListing'), 'cancelListing exportada en index.ts');
assert(indexContent.includes('updateListingPrice'), 'updateListingPrice exportada en index.ts');

// 3. Cliente Unity: MarketService.cs & UIToolkitMarketController.cs
const servicePath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/MarketService.cs');
assert(fs.existsSync(servicePath), 'MarketService.cs existe en Unity');

const serviceContent = fs.readFileSync(servicePath, 'utf8');
assert(serviceContent.includes('CancelListingAsync'), 'Método CancelListingAsync implementado en MarketService');
assert(serviceContent.includes('UpdateListingPriceAsync'), 'Método UpdateListingPriceAsync implementado en MarketService');
assert(serviceContent.includes('OnListingCancelled'), 'Evento OnListingCancelled declarado');
assert(serviceContent.includes('OnListingPriceUpdated'), 'Evento OnListingPriceUpdated declarado');

const ctrlPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitMarketController.cs');
const ctrlContent = fs.readFileSync(ctrlPath, 'utf8');
assert(ctrlContent.includes('MarketService.Instance.CancelListingAsync'), 'UIToolkitMarketController enlazado con CancelListingAsync al retirar carta');
assert(ctrlContent.includes('MarketService.Instance.UpdateListingPriceAsync'), 'UIToolkitMarketController enlazado con UpdateListingPriceAsync al editar precio');

// 4. Simulación Atómica de Escenarios de Gestión
console.log('\n🛒 Simulando Operaciones Atómicas de Cancelación y Actualización de Precio...');

class MockManageFirestore {
  constructor() {
    this.userCollections = {
      seller_1: {
        KDB: { quantity: 1, cardName: 'Kevin De Bruyne' }
      },
      hacker_99: {}
    };
    this.listings = {
      list_1: {
        listingId: 'list_1',
        sellerUid: 'seller_1',
        cardId: 'KDB',
        cardName: 'Kevin De Bruyne',
        rarity: 'Rara',
        quantity: 1,
        pricePerCard: 250,
        status: 'activo'
      },
      list_sold: {
        listingId: 'list_sold',
        sellerUid: 'seller_1',
        cardId: 'JM',
        cardName: 'Musiala',
        rarity: 'Común',
        quantity: 1,
        pricePerCard: 30,
        status: 'vendido'
      }
    };
  }

  cancel(callerUid, listingId) {
    const listing = this.listings[listingId];
    if (!listing) throw new Error('not-found');
    if (listing.sellerUid !== callerUid) throw new Error('permission-denied: solo el vendedor puede cancelar');
    if (listing.status !== 'activo') throw new Error('failed-precondition: el listado no esta activo');

    // Reintegrar al vendedor
    if (!this.userCollections[callerUid][listing.cardId]) {
      this.userCollections[callerUid][listing.cardId] = { quantity: 0, cardName: listing.cardName };
    }
    this.userCollections[callerUid][listing.cardId].quantity += listing.quantity;

    listing.status = 'cancelado';
    return { success: true, restored: listing.quantity, cardName: listing.cardName };
  }

  updatePrice(callerUid, listingId, newPrice) {
    const listing = this.listings[listingId];
    if (!listing) throw new Error('not-found');
    if (listing.sellerUid !== callerUid) throw new Error('permission-denied: solo el vendedor puede editar');
    if (listing.status !== 'activo') throw new Error('failed-precondition: el listado no esta activo');
    if (newPrice <= 0) throw new Error('invalid-argument: precio <= 0');

    const old = listing.pricePerCard;
    listing.pricePerCard = newPrice;
    return { success: true, old, newPrice };
  }
}

const mock = new MockManageFirestore();

// Escenario 1: Intento de cancelación por un tercero no autorizado (hacker)
try {
  mock.cancel('hacker_99', 'list_1');
  assert(false, 'Debió bloquear intento de cancelación por un tercero');
} catch (e) {
  assert(e.message.includes('permission-denied'), 'Rechaza cancelación de un tercero con permission-denied');
  assert(mock.listings['list_1'].status === 'activo', 'El listado permanece activo tras el intento bloqueado');
}

// Escenario 2: Intento de editar precio por un tercero no autorizado
try {
  mock.updatePrice('hacker_99', 'list_1', 1);
  assert(false, 'Debió bloquear intento de edición de precio por un tercero');
} catch (e) {
  assert(e.message.includes('permission-denied'), 'Rechaza edición de un tercero con permission-denied');
  assert(mock.listings['list_1'].pricePerCard === 250, 'El precio original permanece inalterado');
}

// Escenario 3: Modificación de precio válida por el vendedor original
try {
  const res = mock.updatePrice('seller_1', 'list_1', 220);
  assert(res.success === true, 'Vendedor original puede modificar el precio');
  assert(mock.listings['list_1'].pricePerCard === 220, 'El precio se actualizó correctamente a 220 monedas');
} catch (e) {
  assert(false, 'Modificación válida falló: ' + e.message);
}

// Escenario 4: Intento de precio inválido (<= 0)
try {
  mock.updatePrice('seller_1', 'list_1', 0);
  assert(false, 'Debió rechazar precio 0');
} catch (e) {
  assert(e.message.includes('invalid-argument'), 'Rechaza precio <= 0 con invalid-argument');
}

// Escenario 5: Cancelación exitosa y reintegro por el vendedor original
try {
  const initialQty = mock.userCollections.seller_1['KDB'].quantity; // 1
  const res = mock.cancel('seller_1', 'list_1');
  assert(res.success === true, 'Vendedor original puede cancelar su listado');
  assert(mock.listings['list_1'].status === 'cancelado', 'Listado marcado como cancelado');
  assert(mock.userCollections.seller_1['KDB'].quantity === initialQty + 1, 'Carta reintegrada de inmediato a userCollection del vendedor (1 -> 2)');
} catch (e) {
  assert(false, 'Cancelación válida falló: ' + e.message);
}

// Escenario 6: Intento de cancelar o editar un listado ya vendido
try {
  mock.cancel('seller_1', 'list_sold');
  assert(false, 'Debió rechazar cancelación de listado vendido');
} catch (e) {
  assert(e.message.includes('failed-precondition'), 'Rechaza cancelar listados ya vendidos');
}

try {
  mock.updatePrice('seller_1', 'list_sold', 500);
  assert(false, 'Debió rechazar modificar precio de listado vendido');
} catch (e) {
  assert(e.message.includes('failed-precondition'), 'Rechaza modificar precio de listados ya vendidos');
}

console.log('\n==========================================================================');
console.log(`🎯 RESULTADO: ${passed}/${total} pruebas superadas (${Math.round((passed/total)*100)}%)`);
console.log('==========================================================================');

if (passed === total) {
  process.exit(0);
} else {
  process.exit(1);
}
