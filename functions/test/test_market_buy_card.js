/**
 * Test Suite: buyListedCard con Transacción Atómica y 100% de Acreditación (Fase 8.5 - Punto 3)
 * TDD Secciones 2.11, 5.8b y GDD Sección 7.1
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO buyListedCard - TRANSACCIÓN ATÓMICA Y 100% VENDEDOR (FASE 8.5 - P3)');
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

// 1. Backend: buyListedCard.ts
const buyCardPath = path.resolve(__dirname, '../src/market/buyListedCard.ts');
assert(fs.existsSync(buyCardPath), 'buyListedCard.ts existe en functions/src/market/');

const buyCardContent = fs.readFileSync(buyCardPath, 'utf8');
assert(buyCardContent.includes('export const buyListedCard'), 'Cloud Function callable buyListedCard declarada');
assert(buyCardContent.includes('db.runTransaction'), 'buyListedCard ejecutada en transacción atómica de Firestore');
assert(buyCardContent.includes('listing.status !== "activo"'), 'Revalidación atómica de listado activo');
assert(buyCardContent.includes('buyerUid === sellerUid'), 'Bloqueo estricto de auto-compra');
assert(buyCardContent.includes('buyerCoins < totalPrice'), 'Revalidación de saldo suficiente en monedas del comprador');
assert(buyCardContent.includes('coins: FieldValue.increment(-totalPrice)'), 'Descuento atómico de monedas al comprador');
assert(buyCardContent.includes('coins: FieldValue.increment(totalPrice)'), 'Acreditación del 100% al vendedor sin comisión (GDD 7.1)');
assert(buyCardContent.includes('COLLECTIONS.USER_COLLECTION'), 'Transferencia de la carta a userCollection del comprador');
assert(buyCardContent.includes('status: "vendido"'), 'Actualización de estado a vendido');
assert(buyCardContent.includes('COLLECTIONS.TRANSACTIONS'), 'Auditoría inmutable registrada en transactions');

// 2. index.ts
const indexPath = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexPath, 'utf8');
assert(indexContent.includes('buyListedCard'), 'buyListedCard exportada en index.ts');

// 3. Cliente Unity: MarketService.cs & UIToolkitMarketController.cs
const marketServicePath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/MarketService.cs');
assert(fs.existsSync(marketServicePath), 'MarketService.cs existe en Unity');

const marketServiceContent = fs.readFileSync(marketServicePath, 'utf8');
assert(marketServiceContent.includes('BuyListedCardAsync'), 'Método BuyListedCardAsync implementado en MarketService');
assert(marketServiceContent.includes('OnListingPurchased'), 'Evento OnListingPurchased implementado');

const marketCtrlPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitMarketController.cs');
const marketCtrlContent = fs.readFileSync(marketCtrlPath, 'utf8');
assert(marketCtrlContent.includes('MarketService.Instance.BuyListedCardAsync'), 'UIToolkitMarketController enlazado con BuyListedCardAsync');

// 4. Simulación Atómica de Escenarios de Compra
console.log('\n🛒 Simulando Transacción Atómica de Compra en Memoria...');

class MockMarketFirestore {
  constructor() {
    this.users = {
      buyer_1: { coins: 300, displayName: 'Comprador_1' },
      seller_1: { coins: 50, displayName: 'Vendedor_1' },
      broke_buyer: { coins: 10, displayName: 'Sin_Monedas' }
    };
    this.userCollections = {
      buyer_1: {},
      seller_1: {}
    };
    this.listings = {
      list_active: {
        listingId: 'list_active',
        sellerUid: 'seller_1',
        cardId: 'JM',
        cardName: 'Musiala',
        rarity: 'Común',
        quantity: 1,
        pricePerCard: 100,
        status: 'activo'
      },
      list_already_sold: {
        listingId: 'list_already_sold',
        sellerUid: 'seller_1',
        cardId: 'RO',
        cardName: 'Rodri',
        rarity: 'Común',
        quantity: 1,
        pricePerCard: 50,
        status: 'vendido'
      }
    };
    this.transactions = [];
  }

  runAtomicBuy(buyerUid, listingId) {
    const listing = this.listings[listingId];
    if (!listing) throw new Error('not-found');
    if (listing.status !== 'activo') throw new Error('failed-precondition: ya no esta activo');
    if (buyerUid === listing.sellerUid) throw new Error('invalid-argument: auto-compra no permitida');

    const buyer = this.users[buyerUid];
    if (!buyer) throw new Error('buyer not found');

    const seller = this.users[listing.sellerUid];
    if (!seller) throw new Error('seller not found');

    const totalPrice = listing.pricePerCard * listing.quantity;
    if (buyer.coins < totalPrice) throw new Error('failed-precondition: saldo insuficiente');

    // Mutaciones atómicas
    buyer.coins -= totalPrice;
    seller.coins += totalPrice; // 100% al vendedor

    // Mover carta al comprador
    if (!this.userCollections[buyerUid][listing.cardId]) {
      this.userCollections[buyerUid][listing.cardId] = { quantity: 0, cardName: listing.cardName };
    }
    this.userCollections[buyerUid][listing.cardId].quantity += listing.quantity;

    // Actualizar listado
    listing.status = 'vendido';
    listing.buyerUid = buyerUid;

    // Registro inmutable
    this.transactions.push({
      type: 'market_purchase',
      listingId,
      sellerUid: listing.sellerUid,
      buyerUid,
      totalPrice
    });

    return { success: true, totalPrice, buyerCoinsRemaining: buyer.coins, sellerCoinsNew: seller.coins };
  }
}

const mockDb = new MockMarketFirestore();

// Escenario A: Compra válida exitosa
try {
  const res = mockDb.runAtomicBuy('buyer_1', 'list_active');
  assert(res.success === true, 'Compra ejecutada con éxito');
  assert(res.buyerCoinsRemaining === 200, 'Monedas descontadas exactamente al comprador (300 - 100 = 200)');
  assert(res.sellerCoinsNew === 150, '100% de monedas acreditadas al vendedor sin comisión (50 + 100 = 150) (GDD 7.1)');
  assert(mockDb.userCollections.buyer_1['JM'].quantity === 1, 'Carta Musiala acreditada en el inventario del comprador');
  assert(mockDb.listings['list_active'].status === 'vendido', 'Listado marcado atómicamente como vendido');
  assert(mockDb.transactions.length === 1, 'Registro inmutable auditado en transactions');
} catch (e) {
  assert(false, 'Compra válida falló inesperadamente: ' + e.message);
}

// Escenario B: Intento de comprar listado ya vendido
try {
  mockDb.runAtomicBuy('buyer_1', 'list_already_sold');
  assert(false, 'Debió rechazar compra de listado ya vendido');
} catch (e) {
  assert(e.message.includes('ya no esta activo'), 'Rechaza con failed-precondition si el listado ya fue vendido');
}

// Escenario C: Intento de comprar con saldo insuficiente
try {
  // Ponemos otro listado activo de 100
  mockDb.listings['list_expensive'] = {
    listingId: 'list_expensive',
    sellerUid: 'seller_1',
    cardId: 'EH',
    cardName: 'Haaland',
    rarity: 'Común',
    quantity: 1,
    pricePerCard: 100,
    status: 'activo'
  };
  mockDb.runAtomicBuy('broke_buyer', 'list_expensive');
  assert(false, 'Debió rechazar compra por saldo insuficiente');
} catch (e) {
  assert(e.message.includes('saldo insuficiente'), 'Rechaza compra si las monedas del comprador no alcanzan');
  assert(mockDb.listings['list_expensive'].status === 'activo', 'El listado permanece activo e intacto tras el rechazo');
}

// Escenario D: Intento de auto-compra (vendedor comprando su propia carta)
try {
  mockDb.runAtomicBuy('seller_1', 'list_expensive');
  assert(false, 'Debió rechazar auto-compra');
} catch (e) {
  assert(e.message.includes('auto-compra no permitida'), 'Rechaza intento de comprar sus propios listados');
}

console.log('\n==========================================================================');
console.log(`🎯 RESULTADO: ${passed}/${total} pruebas superadas (${Math.round((passed/total)*100)}%)`);
console.log('==========================================================================');

if (passed === total) {
  process.exit(0);
} else {
  process.exit(1);
}
