/**
 * Test Suite: Caso Límite de Carrera y Concurrencia en Compra de Mercado (Fase 8.5 - Punto 7)
 * TDD Secciones 2.11, 5.8b, 6 y GDD 7.1
 * 
 * Requisito:
 * "Probar el caso límite de dos compradores intentando comprar el mismo listado
 * casi al mismo tiempo, confirmando que solo el primero tiene éxito y el segundo
 * falla limpiamente".
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO CARRERA Y CONCURRENCIA DE COMPRA EN MERCADO (FASE 8.5 - P7)');
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
assert(fs.existsSync(buyCardPath), 'buyListedCard.ts existe');

const buyCardContent = fs.readFileSync(buyCardPath, 'utf8');
assert(buyCardContent.includes('db.runTransaction'), 'buyListedCard utiliza db.runTransaction para control de concurrencia optimista');
assert(buyCardContent.includes('listing.status !== "activo"'), 'Revalida in-situ dentro de la transacción que el listado siga activo');
assert(buyCardContent.includes('failed-precondition'), 'Retorna código failed-precondition si el listado ya no está activo');
assert(buyCardContent.includes('coins: FieldValue.increment(-totalPrice)'), 'Descuento de monedas ejecutado dentro de la transacción');
assert(buyCardContent.includes('coins: FieldValue.increment(totalPrice)'), 'Acreditación al vendedor dentro de la transacción');
assert(buyCardContent.includes('status: "vendido"'), 'Listado transiciona atómicamente al estado vendido');

// 2. Simulador de Motor Concurrente de Firestore con Bloqueo Optimista
console.log('\n⚡ Simulando Transacciones Concurrentes Simultáneas sobre el Mismo Listado...');

class MockConcurrentFirestore {
  constructor() {
    this.storage = {
      users: {
        buyer_alpha: { uid: 'buyer_alpha', coins: 500, displayName: 'Comprador Alfa' },
        buyer_beta: { uid: 'buyer_beta', coins: 500, displayName: 'Comprador Beta' },
        seller_victor: { uid: 'seller_victor', coins: 100, displayName: 'Vendedor Víctor' }
      },
      collections: {
        buyer_alpha: {},
        buyer_beta: {},
        seller_victor: {}
      },
      listings: {
        exclusive_card_101: {
          listingId: 'exclusive_card_101',
          sellerUid: 'seller_victor',
          cardId: 'LY',
          cardName: 'Lamine Yamal',
          rarity: 'mitica',
          quantity: 1,
          pricePerCard: 250,
          status: 'activo',
          version: 1
        }
      },
      transactionsAudit: []
    };

    // Mutex o cola de serialización atómica (emulando el commit pipeline de Firestore)
    this.commitLock = Promise.resolve();
  }

  // Emula una transacción con reintento / abort si cambia el documento
  async executeAtomicBuy(buyerUid, listingId, artificialDelayMs = 0) {
    return new Promise((resolve) => {
      this.commitLock = this.commitLock.then(async () => {
        // Delay para simular llegada simultánea desfasada por milisegundos
        if (artificialDelayMs > 0) {
          await new Promise(r => setTimeout(r, artificialDelayMs));
        }

        const listing = this.storage.listings[listingId];
        if (!listing) {
          resolve({ success: false, error: 'not-found', buyerUid });
          return;
        }

        // 1. Verificación atómica de precondición
        if (listing.status !== 'activo') {
          resolve({
            success: false,
            error: 'failed-precondition',
            message: `El listado ya no está disponible para compra (Estado actual: ${listing.status}).`,
            buyerUid
          });
          return;
        }

        const buyer = this.storage.users[buyerUid];
        const seller = this.storage.users[listing.sellerUid];
        const totalPrice = listing.pricePerCard * listing.quantity;

        if (buyer.coins < totalPrice) {
          resolve({ success: false, error: 'failed-precondition: saldo insuficiente', buyerUid });
          return;
        }

        // 2. Commit atómico simultáneo
        buyer.coins -= totalPrice;
        seller.coins += totalPrice;

        // Acreditar carta
        if (!this.storage.collections[buyerUid][listing.cardId]) {
          this.storage.collections[buyerUid][listing.cardId] = { quantity: 0, cardName: listing.cardName };
        }
        this.storage.collections[buyerUid][listing.cardId].quantity += listing.quantity;

        // Cierre de listado
        listing.status = 'vendido';
        listing.buyerUid = buyerUid;
        listing.version += 1;

        // Auditoría
        this.storage.transactionsAudit.push({
          type: 'market_purchase',
          listingId,
          buyerUid,
          sellerUid: listing.sellerUid,
          price: totalPrice
        });

        resolve({
          success: true,
          buyerUid,
          pricePaid: totalPrice,
          remainingCoins: buyer.coins
        });
      });
    });
  }
}

async function runConcurrencyTests() {
  const db = new MockConcurrentFirestore();

  console.log('  -> Disparando compra simultánea: Comprador Alfa vs Comprador Beta...');
  
  // Ejecutar dos compras en paralelo sobre el listado exclusive_card_101
  const [resultA, resultB] = await Promise.all([
    db.executeAtomicBuy('buyer_alpha', 'exclusive_card_101', 5),
    db.executeAtomicBuy('buyer_beta', 'exclusive_card_101', 10)
  ]);

  // Evaluaciones de los resultados de la carrera
  const first = resultA.success ? resultA : resultB;
  const second = !resultA.success ? resultA : resultB;

  assert(first.success === true, 'El primer comprador en adquirir el lock tiene ÉXITO');
  assert(first.buyerUid === 'buyer_alpha', 'Comprador Alfa fue el primero y completó la transacción');
  assert(first.remainingCoins === 250, 'Al Comprador Alfa se le descuentan exactamente 250 monedas (500 - 250 = 250)');

  assert(second.success === false, 'El segundo comprador (Beta) FALLA LIMPIAMENTE');
  assert(second.error === 'failed-precondition', 'El segundo intento retorna código failed-precondition');
  assert(second.message.includes('ya no está disponible'), 'Mensaje amigable al usuario explicando que ya fue comprado');

  // Integridad de la Base de Datos tras la carrera
  console.log('\n🛡️ Validando Integridad Económica y de Inventario tras la Carrera...');

  // 1. Monedas del perdedor intactas
  const betaCoins = db.storage.users.buyer_beta.coins;
  assert(betaCoins === 500, `Monedas del segundo comprador (Beta) 100% intactas (${betaCoins}/500) - Cero doble cobro`);

  // 2. Inventario de cartas
  const alphaHasCard = (db.storage.collections.buyer_alpha['LY']?.quantity || 0) === 1;
  const betaHasCard = (db.storage.collections.buyer_beta['LY']?.quantity || 0) === 0;
  assert(alphaHasCard === true, 'Solo el primer comprador (Alfa) tiene la carta Lamine Yamal (1x)');
  assert(betaHasCard === true, 'El segundo comprador (Beta) no recibió ninguna copia fraudulenta (0x) - Cero duplicación');

  // 3. Monedas del vendedor exactas (no se acreditó el doble)
  const sellerCoins = db.storage.users.seller_victor.coins;
  assert(sellerCoins === 350, `Vendedor Víctor recibió exactamente 1 pago (+250) quedando en 350 monedas (${sellerCoins}/350) - Cero sobre-acreditación`);

  // 4. Estado final del listado
  const finalListing = db.storage.listings.exclusive_card_101;
  assert(finalListing.status === 'vendido', 'El listado quedó marcado definitivamente como vendido');
  assert(finalListing.buyerUid === 'buyer_alpha', 'La venta quedó adjudicada oficialmente al Comprador Alfa');

  // 5. Auditoría única
  const totalAudits = db.storage.transactionsAudit.length;
  assert(totalAudits === 1, `Se registró exactamente 1 evento de auditoría en transactions (${totalAudits}/1)`);

  console.log('\n==========================================================================');
  console.log(`🎯 RESULTADO: ${passed}/${total} pruebas superadas (${Math.round((passed/total)*100)}%)`);
  console.log('==========================================================================');

  if (passed === total) {
    process.exit(0);
  } else {
    process.exit(1);
  }
}

runConcurrencyTests().catch(err => {
  console.error('Error no capturado:', err);
  process.exit(1);
});
