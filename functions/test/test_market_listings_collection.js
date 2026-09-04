/**
 * Test Suite: Colección marketListings en Firestore (Fase 8.5 - Punto 1)
 * TDD Sección 5.8b y 7.2
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO COLECCIÓN marketListings EN FIRESTORE (FASE 8.5 - PUNTO 1)');
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

// 1. Constante de Colección en firebase.ts
const firebaseTsPath = path.resolve(__dirname, '../src/firebase.ts');
assert(fs.existsSync(firebaseTsPath), 'firebase.ts existe en functions/src/');
const firebaseContent = fs.readFileSync(firebaseTsPath, 'utf8');
assert(firebaseContent.includes('MARKET_LISTINGS: "marketListings"'), 'Constante MARKET_LISTINGS declarada en COLLECTIONS (TDD 5.8b)');

// 2. Módulo marketTypes.ts
const marketTypesPath = path.resolve(__dirname, '../src/market/marketTypes.ts');
assert(fs.existsSync(marketTypesPath), 'marketTypes.ts existe en functions/src/market/');
const marketTypesContent = fs.readFileSync(marketTypesPath, 'utf8');

assert(marketTypesContent.includes('export type MarketListingStatus = "activo" | "vendido" | "cancelado"'), 'MarketListingStatus define los 3 estados oficiales (activo, vendido, cancelado)');
assert(marketTypesContent.includes('export interface MarketListing'), 'Interface MarketListing declarada en TypeScript');
assert(marketTypesContent.includes('listingId: string;'), 'Campo listingId definido en MarketListing');
assert(marketTypesContent.includes('sellerUid: string;'), 'Campo sellerUid definido en MarketListing');
assert(marketTypesContent.includes('sellerDisplayName: string;'), 'Campo sellerDisplayName definido en MarketListing');
assert(marketTypesContent.includes('cardId: string;'), 'Campo cardId definido en MarketListing');
assert(marketTypesContent.includes('cardName: string;'), 'Campo cardName definido en MarketListing');
assert(marketTypesContent.includes('rarity: string;'), 'Campo rarity definido en MarketListing');
assert(marketTypesContent.includes('quantity: number;'), 'Campo quantity definido en MarketListing');
assert(marketTypesContent.includes('pricePerCard: number;'), 'Campo pricePerCard definido en MarketListing');
assert(marketTypesContent.includes('status: MarketListingStatus;'), 'Campo status tipado con MarketListingStatus');
assert(marketTypesContent.includes('buyerUid?: string | null;'), 'Campo opcional buyerUid contemplado para ventas');
assert(marketTypesContent.includes('createdAt:'), 'Campo createdAt definido en MarketListing');

// 3. Exportaciones en index.ts
const indexTsPath = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexTsPath, 'utf8');
assert(indexContent.includes('from "./market/marketTypes"'), 'marketTypes exportado en index.ts para el ecosistema backend');

// 4. Pruebas Funcionales del Validador de Esquema (isValidMarketListingData)
const { isValidMarketListingData } = require('../lib/market/marketTypes');

console.log('\n📐 Evaluando Validador de Esquema de marketListings...');

const validListingActive = {
  listingId: "listing_001",
  sellerUid: "usr_alice",
  sellerDisplayName: "Alice_TCG",
  cardId: "PE",
  cardName: "Pedri González",
  rarity: "epica",
  quantity: 1,
  pricePerCard: 180,
  status: "activo",
  createdAt: new Date().toISOString()
};

assert(isValidMarketListingData(validListingActive) === true, 'Listado activo válido es aceptado');

const validListingSold = Object.assign({}, validListingActive, {
  status: "vendido",
  buyerUid: "usr_bob",
  buyerDisplayName: "Bob_Master",
  closedAt: new Date().toISOString()
});
assert(isValidMarketListingData(validListingSold) === true, 'Listado vendido con comprador es aceptado');

const validListingCancelled = Object.assign({}, validListingActive, {
  status: "cancelado",
  closedAt: new Date().toISOString()
});
assert(isValidMarketListingData(validListingCancelled) === true, 'Listado cancelado es aceptado');

// Casos Borde y Rechazos
const zeroPriceListing = Object.assign({}, validListingActive, { pricePerCard: 0 });
assert(isValidMarketListingData(zeroPriceListing) === false, 'Listado con precio 0 es rechazado (precio debe ser > 0)');

const negativePriceListing = Object.assign({}, validListingActive, { pricePerCard: -50 });
assert(isValidMarketListingData(negativePriceListing) === false, 'Listado con precio negativo es rechazado');

const zeroQtyListing = Object.assign({}, validListingActive, { quantity: 0 });
assert(isValidMarketListingData(zeroQtyListing) === false, 'Listado con cantidad 0 es rechazado');

const invalidStatusListing = Object.assign({}, validListingActive, { status: "robado" });
assert(isValidMarketListingData(invalidStatusListing) === false, 'Listado con status no oficial es rechazado');

const missingSellerListing = Object.assign({}, validListingActive, { sellerUid: "" });
assert(isValidMarketListingData(missingSellerListing) === false, 'Listado sin sellerUid es rechazado');

const missingCardListing = Object.assign({}, validListingActive, { cardId: "" });
assert(isValidMarketListingData(missingCardListing) === false, 'Listado sin cardId es rechazado');

console.log('==========================================================================');
console.log('📊 RESULTADOS: ' + passed + '/' + total + ' pruebas superadas (' + Math.round((passed/total)*100) + '%)');
console.log('==========================================================================');

if (passed === total) {
  console.log('🎉 ¡COLECCIÓN marketListings Y ESQUEMA VERIFICADOS CON ÉXITO!');
  process.exit(0);
} else {
  console.error('❌ Fallos en la verificación de marketListings.');
  process.exit(1);
}
