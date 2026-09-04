/**
 * Test Suite: Pantalla de Mercado / Vender Duplicados (Fase 8.5 - Punto 6)
 * GDD Secciones 7.1, 8.3 y TDD 2.11, 5.8b
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO PANTALLA MERCADO Y VENTA DE DUPLICADOS (FASE 8.5 - P6)');
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

// 1. Archivos UI Toolkit
const uxmlPath = path.resolve(__dirname, '../../Assets/_Project/UI/Views/MarketScreen.uxml');
assert(fs.existsSync(uxmlPath), 'MarketScreen.uxml existe en Views/UI');

const ussPath = path.resolve(__dirname, '../../Assets/_Project/UI/Styles/MarketScreen.uss');
assert(fs.existsSync(ussPath), 'MarketScreen.uss existe en Styles/UI');

const uxmlContent = fs.readFileSync(uxmlPath, 'utf8');
const ussContent = fs.readFileSync(ussPath, 'utf8');

// 2. Modos de Navegación (COMPRAR vs MIS VENTAS)
assert(uxmlContent.includes('name="Tab_Buy"'), 'Pestaña COMPRAR presente en UXML');
assert(uxmlContent.includes('name="Tab_Sell"'), 'Pestaña MIS VENTAS (Vender Duplicados) presente en UXML');
assert(uxmlContent.includes('name="MarketCardsGrid"'), 'Cuadrícula de listados de otros jugadores presente');
assert(uxmlContent.includes('name="MyListingsContainer"'), 'Contenedor de Mis Ventas presente');

// 3. Filtros de Rareza en Modo COMPRAR
const rarityFilters = ['Filter_Todas', 'Filter_Comun', 'Filter_PocoComun', 'Filter_Rara', 'Filter_Mitica'];
for (const f of rarityFilters) {
  assert(uxmlContent.includes('name="' + f + '"'), 'Filtro de rareza disponible: ' + f);
}

// 4. Sección 'Vender Duplicados' (Cuadrícula de duplicados propios)
assert(uxmlContent.includes('class="my-duplicates-section"'), 'Sección de duplicados propios presente');
assert(uxmlContent.includes('class="duplicates-grid"'), 'Cuadrícula de duplicados para publicar presente');
assert(uxmlContent.includes('name="Btn_Publish_1"'), 'Botón PUBLICAR en duplicado 1');
assert(uxmlContent.includes('name="Btn_Publish_2"'), 'Botón PUBLICAR en duplicado 2');
assert(uxmlContent.includes('name="PriceModal"'), 'Modal para fijar precio libre al publicar');
assert(uxmlContent.includes('name="PriceInput"'), 'Campo de entrada de precio en monedas');

// 5. Sección 'Listados Activos' (Retirar y Editar Precio)
assert(uxmlContent.includes('class="active-listings-section"'), 'Sección de listados activos propios presente');
assert(uxmlContent.includes('name="Btn_EditPrice_1"'), 'Botón EDITAR PRECIO presente');
assert(uxmlContent.includes('name="Btn_Withdraw_1"'), 'Botón RETIRAR del mercado presente');

// 6. Controlador C#: UIToolkitMarketController.cs
const ctrlPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitMarketController.cs');
assert(fs.existsSync(ctrlPath), 'UIToolkitMarketController.cs existe');

const ctrlContent = fs.readFileSync(ctrlPath, 'utf8');
assert(ctrlContent.includes('SwitchMode(true)'), 'Soporta alternar al modo COMPRAR');
assert(ctrlContent.includes('SwitchMode(false)'), 'Soporta alternar al modo MIS VENTAS');
assert(ctrlContent.includes('FilterByRarity'), 'Función de filtrado por rareza implementada');
assert(ctrlContent.includes('BuyCard'), 'Acción de compra de cartas implementada');
assert(ctrlContent.includes('OpenPublishModal'), 'Apertura de modal de fijar precio implementada');
assert(ctrlContent.includes('ConfirmPriceModal'), 'Confirmación de precio conectada a MarketService');
assert(ctrlContent.includes('WithdrawListing'), 'Retiro de listado conectado a MarketService');

// 7. Servicio C#: MarketService.cs
const servicePath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/MarketService.cs');
const serviceContent = fs.readFileSync(servicePath, 'utf8');
assert(serviceContent.includes('GetActiveListings'), 'Método GetActiveListings con filtro de rareza');
assert(serviceContent.includes('GetMyActiveListings'), 'Método GetMyActiveListings implementado');
assert(serviceContent.includes('GetMyDuplicateCards'), 'Método GetMyDuplicateCards implementado para duplicados propios');
assert(serviceContent.includes('ListCardForSaleAsync'), 'ListCardForSaleAsync implementado');
assert(serviceContent.includes('BuyListedCardAsync'), 'BuyListedCardAsync implementado');
assert(serviceContent.includes('CancelListingAsync'), 'CancelListingAsync implementado');
assert(serviceContent.includes('UpdateListingPriceAsync'), 'UpdateListingPriceAsync implementado');

console.log('\n==========================================================================');
console.log(`🎯 RESULTADO: ${passed}/${total} pruebas superadas (${Math.round((passed/total)*100)}%)`);
console.log('==========================================================================');

if (passed === total) {
  process.exit(0);
} else {
  process.exit(1);
}
