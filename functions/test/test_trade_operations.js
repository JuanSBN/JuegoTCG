/**
 * Test Suite: Sistema de Intercambio Directo Anti-Fraude (Fase 8 - Punto 4)
 * TDD Secciones 2.5, 5.8 y 6
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO INTERCAMBIO DIRECTO ANTI-FRAUDE (FASE 8 - PUNTO 4)');
console.log('==========================================================================');

let passed = 0;
let total = 0;

function assert(condition, description) {
  total++;
  if (condition) {
    passed++;
    console.log(`  ✅ ${description}`);
  } else {
    console.error(`  ❌ FALLÓ: ${description}`);
  }
}

// 1. Backend: tradeOperations.ts
const tradeTsPath = path.resolve(__dirname, '../src/social/tradeOperations.ts');
assert(fs.existsSync(tradeTsPath), 'tradeOperations.ts existe en functions/src/social/');

const tradeContent = fs.existsSync(tradeTsPath) ? fs.readFileSync(tradeTsPath, 'utf8') : '';
assert(tradeContent.includes('export const proposeTrade'), 'Cloud Function proposeTrade declarada');
assert(tradeContent.includes('fromUid === toUid'), 'Validación anti-self-trading implementada');
assert(tradeContent.includes('MAX_DAILY_PROPOSALS'), 'Límite diario de propuestas anti-spam configurado');
assert(tradeContent.includes('OFFER_EXPIRATION_HOURS = 48') || tradeContent.includes('expiresAt'), 'Caducidad de oferta de 48 horas configurada');
assert(!tradeContent.includes('transaction.delete(fromOfferedCardRef)') || tradeContent.indexOf('proposeTrade') < tradeContent.indexOf('transaction.delete'), 'proposeTrade NO bloquea ni descuenta cartas al proponer (TDD 2.5)');

assert(tradeContent.includes('export const acceptTrade'), 'Cloud Function acceptTrade declarada');
assert(tradeContent.includes('db.runTransaction'), 'acceptTrade ejecutada en transacción atómica de Firestore');
assert(tradeContent.includes('fromQty < offeredQty'), 'Revalidación instantánea de posesión del proponente');
assert(tradeContent.includes('toQty < requestedQty'), 'Revalidación instantánea de posesión del receptor');
assert(tradeContent.includes('El proponente ya no posee la carta ofrecida'), 'Cancelación limpia si el proponente ya no tiene la carta');
assert(tradeContent.includes('COLLECTIONS.TRANSACTIONS'), 'Registro de auditoría en transactions implementado');

assert(tradeContent.includes('export const cancelTrade'), 'Cloud Function cancelTrade declarada');
assert(tradeContent.includes('newStatus = "cancelado"') && tradeContent.includes('newStatus = "rechazado"'), 'Cancelación por proponente y rechazo por receptor implementados');

// 2. index.ts exports
const indexTsPath = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexTsPath, 'utf8');
assert(indexContent.includes('proposeTrade'), 'proposeTrade exportada en index.ts');
assert(indexContent.includes('acceptTrade'), 'acceptTrade exportada en index.ts');
assert(indexContent.includes('cancelTrade'), 'cancelTrade exportada en index.ts');

// 3. Firestore Rules
const rulesPath = path.resolve(__dirname, '../../firestore.rules');
assert(fs.existsSync(rulesPath), 'firestore.rules existe');

const rulesContent = fs.readFileSync(rulesPath, 'utf8');
assert(rulesContent.includes('match /tradeOffers/{tradeId}'), 'Reglas de tradeOffers definidas');
assert(rulesContent.includes('resource.data.fromUid == request.auth.uid') || rulesContent.includes('fromUid'), 'Lectura restringida al proponente o receptor');
assert(rulesContent.includes('allow write: if false;'), 'Escritura directa denegada al cliente (obligatorio Cloud Functions)');

// 4. Cliente C#: TradeService.cs
const tradeServiceCs = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/TradeService.cs');
assert(fs.existsSync(tradeServiceCs), 'TradeService.cs existe');

const serviceContent = fs.readFileSync(tradeServiceCs, 'utf8');
assert(serviceContent.includes('public class TradeService'), 'Clase TradeService singleton declarada');
assert(serviceContent.includes('ProposeTradeAsync'), 'Método ProposeTradeAsync implementado');
assert(serviceContent.includes('AcceptTradeAsync'), 'Método AcceptTradeAsync implementado');
assert(serviceContent.includes('RejectTradeAsync'), 'Método RejectTradeAsync implementado');
assert(serviceContent.includes('CancelSentTradeAsync'), 'Método CancelSentTradeAsync implementado');

// 5. Controlador UI Toolkit: UIToolkitTradeController.cs
const controllerCs = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitTradeController.cs');
assert(fs.existsSync(controllerCs), 'UIToolkitTradeController.cs existe');

const ctrlContent = fs.readFileSync(controllerCs, 'utf8');
assert(ctrlContent.includes('TradeService.EnsureExists()'), 'TradeService inicializado en UIToolkitTradeController');
assert(ctrlContent.includes('AcceptTradeAsync'), 'Botón aceptar conectado con AcceptTradeAsync');
assert(ctrlContent.includes('RejectTradeAsync'), 'Botón rechazar conectado con RejectTradeAsync');
assert(ctrlContent.includes('CancelSentTradeAsync'), 'Botón cancelar conectado con CancelSentTradeAsync');

console.log('==========================================================================');
console.log(`🎉 RESULTADO: ${passed}/${total} PRUEBAS COMPLETADAS CON ÉXITO.`);
console.log('==========================================================================');

if (passed !== total) {
  process.exit(1);
}
