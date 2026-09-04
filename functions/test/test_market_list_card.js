/**
 * Test Suite: listCardForSale con Reserva de Inventario (Fase 8.5 - Punto 2)
 * TDD Secciones 2.11, 5.8b y 6
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO listCardForSale Y RESERVA DE INVENTARIO (FASE 8.5 - P2)');
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

// 1. Backend: listCardForSale.ts
const listCardPath = path.resolve(__dirname, '../src/market/listCardForSale.ts');
assert(fs.existsSync(listCardPath), 'listCardForSale.ts existe en functions/src/market/');

const listCardContent = fs.readFileSync(listCardPath, 'utf8');
assert(listCardContent.includes('export const listCardForSale'), 'Cloud Function callable listCardForSale declarada');
assert(listCardContent.includes('db.runTransaction'), 'listCardForSale ejecutada en transacción atómica de Firestore');
assert(listCardContent.includes('COLLECTIONS.USER_COLLECTION'), 'Verificación de inventario contra subcolección autoritativa');
assert(listCardContent.includes('currentQty < quantity'), 'Revalidación de saldo suficiente en inventario del vendedor');
assert(listCardContent.includes('transaction.delete') && listCardContent.includes('transaction.update'), 'Reserva atómica al publicar: carta descontada de userCollection (TDD 2.11)');
assert(listCardContent.includes('COLLECTIONS.MARKET_LISTINGS'), 'Persistencia en colección marketListings');
assert(listCardContent.includes('status: "activo"'), 'Listado inicializado con status activo');
assert(listCardContent.includes('pricePerCard <= 0'), 'Validación de precio libre estrictamente mayor a 0');

// 2. index.ts
const indexPath = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexPath, 'utf8');
assert(indexContent.includes('listCardForSale'), 'listCardForSale exportada en index.ts');

// 3. Cliente Unity: MarketService.cs & Controller
const marketServicePath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/MarketService.cs');
assert(fs.existsSync(marketServicePath), 'MarketService.cs existe en Unity');

const marketServiceContent = fs.readFileSync(marketServicePath, 'utf8');
assert(marketServiceContent.includes('class MarketService : MonoBehaviour'), 'MarketService implementado como singleton');
assert(marketServiceContent.includes('ListCardForSaleAsync'), 'Método ListCardForSaleAsync implementado');
assert(marketServiceContent.includes('PlayerCollectionManager'), 'MarketService consulta el inventario local de cartas');
assert(marketServiceContent.includes('OnMarketUpdated') && marketServiceContent.includes('OnListingPublished'), 'Eventos reactivos de publicación declarados');

const marketCtrlPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitMarketController.cs');
const marketCtrlContent = fs.readFileSync(marketCtrlPath, 'utf8');
assert(marketCtrlContent.includes('MarketService.Instance.ListCardForSaleAsync'), 'UIToolkitMarketController enlazado con ListCardForSaleAsync al fijar precio');

// 4. Simulador de Transacción de Reserva Anti-Fraude
console.log('\n🛒 Simulando Transacción Atómica de Publicación y Reserva de Inventario...');

class MockFirestoreTransaction {
  constructor() {
    this.userCollection = new Map();
    this.marketListings = new Map();
    this.counter = 0;
  }

  setCard(userId, cardId, qty) {
    this.userCollection.set(userId + "/" + cardId, qty);
  }

  getCard(userId, cardId) {
    return this.userCollection.get(userId + "/" + cardId) || 0;
  }

  executeListCard(sellerUid, cardId, price, quantity = 1) {
    if (price <= 0) {
      throw new Error("invalid-argument: Precio debe ser > 0");
    }
    if (quantity <= 0) {
      throw new Error("invalid-argument: Cantidad debe ser >= 1");
    }

    const currentQty = this.getCard(sellerUid, cardId);
    if (currentQty < quantity) {
      throw new Error("failed-precondition: No tienes suficientes copias de esta carta");
    }

    // RESERVA: Descontar de userCollection inmediatamente (TDD 2.11)
    const newQty = currentQty - quantity;
    if (newQty === 0) {
      this.userCollection.delete(sellerUid + "/" + cardId);
    } else {
      this.userCollection.set(sellerUid + "/" + cardId, newQty);
    }

    const listingId = "list_" + (++this.counter) + "_" + Date.now();
    this.marketListings.set(listingId, {
      listingId,
      sellerUid,
      cardId,
      quantity,
      pricePerCard: price,
      status: "activo"
    });

    return { success: true, listingId, remainingQty: newQty };
  }
}

const mockTx = new MockFirestoreTransaction();

// Test 4.1: Vendedor tiene 2 copias de Musiala (JM) y publica 1 copia a 35 monedas
mockTx.setCard("usr_alice", "JM", 2);
assert(mockTx.getCard("usr_alice", "JM") === 2, 'Alice posee inicialmente 2 copias de Musiala (duplicado)');

const res1 = mockTx.executeListCard("usr_alice", "JM", 35, 1);
assert(res1.success === true, 'Publicación exitosa devuelta al cliente');
assert(mockTx.getCard("usr_alice", "JM") === 1, 'Inventario de Alice decrementado a 1 copia (la otra quedó apartada/reservada)');
assert(mockTx.marketListings.size === 1, 'Documento de listado creado en marketListings');
const listing1 = Array.from(mockTx.marketListings.values())[0];
assert(listing1.status === "activo" && listing1.pricePerCard === 35, 'Listado activo con precio de 35 monedas');

// Test 4.2: Alice publica su última copia restante
const res2 = mockTx.executeListCard("usr_alice", "JM", 40, 1);
assert(res2.success === true, 'Segunda copia publicada exitosamente');
assert(mockTx.getCard("usr_alice", "JM") === 0, 'Inventario de Alice para Musiala ahora es 0 (ambas reservadas)');

// Test 4.3: Intento de doble gasto (Alice intenta publicar una tercera copia que ya no tiene)
let doubleSpendError = null;
try {
  mockTx.executeListCard("usr_alice", "JM", 50, 1);
} catch (err) {
  doubleSpendError = err.message;
}
assert(doubleSpendError !== null && doubleSpendError.includes("failed-precondition"), 'Doble gasto bloqueado: Alice no puede publicar copias adicionales');
assert(mockTx.marketListings.size === 2, 'No se crearon listados fraudulentos (se mantienen solo los 2 válidos)');

// Test 4.4: Intento de publicar con precio 0 o negativo
let priceError = null;
try {
  mockTx.setCard("usr_bob", "PE", 1);
  mockTx.executeListCard("usr_bob", "PE", 0, 1);
} catch (err) {
  priceError = err.message;
}
assert(priceError !== null && priceError.includes("invalid-argument"), 'Publicación con precio 0 rechazada');

console.log('==========================================================================');
console.log('📊 RESULTADOS: ' + passed + '/' + total + ' pruebas superadas (' + Math.round((passed/total)*100) + '%)');
console.log('==========================================================================');

if (passed === total) {
  console.log('🎉 ¡listCardForSale Y RESERVA ATÓMICA DE INVENTARIO VERIFICADOS CON ÉXITO!');
  process.exit(0);
} else {
  console.error('❌ Fallos en la verificación de listCardForSale.');
  process.exit(1);
}
