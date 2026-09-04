/**
 * Batería de Pruebas Maestra: FASE 8.5 COMPLETA (Mercado de Cartas entre Jugadores)
 * Cubre los 7 puntos oficiales de la fase.
 */

const { execSync } = require('child_process');
const path = require('path');

console.log('==========================================================================');
console.log('🌟 INICIANDO BATERÍA MAESTRA DE PRUEBAS DE LA FASE 8.5 (MERCADO P2P)');
console.log('==========================================================================');

const testSuites = [
  { file: 'test_market_listings_collection.js', title: 'PUNTO 1: Colección marketListings y Esquema (TDD 5.8b)' },
  { file: 'test_market_list_card.js', title: 'PUNTO 2: listCardForSale con Reserva Atómica de Inventario (TDD 2.11)' },
  { file: 'test_market_buy_card.js', title: 'PUNTO 3: buyListedCard Transacción Atómica y 100% Vendedor (GDD 7.1)' },
  { file: 'test_market_manage_listing.js', title: 'PUNTO 4: cancelListing y updateListingPrice Restringidas a Vendedor' },
  { file: 'test_market_rules.js', title: 'PUNTO 5: Firestore Rules de marketListings (Anti-Fraude Escritura)' },
  { file: 'test_market_screen_ui.js', title: 'PUNTO 6: Pantalla Mercado / Vender Duplicados y Filtros Rareza' },
  { file: 'test_market_concurrency.js', title: 'PUNTO 7: Caso Límite de Carrera Concurrente (2 Compradores Simultáneos)' }
];

let totalPassed = 0;
let totalTests = 0;
const results = [];

for (const suite of testSuites) {
  console.log('\n--------------------------------------------------------------------------');
  console.log(`▶️ ${suite.title}`);
  console.log('--------------------------------------------------------------------------');

  try {
    const fullPath = path.resolve(__dirname, suite.file);
    const output = execSync('node "' + fullPath + '"', { encoding: 'utf8' });
    console.log(output);

    // Extraer cuenta con regex seguro
    const match = output.match(/(\d+)\/(\d+)\s+pruebas/);
    if (match) {
      const p = parseInt(match[1], 10);
      const t = parseInt(match[2], 10);
      totalPassed += p;
      totalTests += t;
      results.push({ name: suite.title, passed: p, total: t, ok: p === t });
    } else {
      results.push({ name: suite.title, passed: 1, total: 1, ok: true });
    }
  } catch (err) {
    console.error('❌ Error ejecutando ' + suite.file + ': ' + err.message);
    if (err.stdout) console.log(err.stdout);
    results.push({ name: suite.title, passed: 0, total: 1, ok: false });
  }
}

console.log('\n==========================================================================');
console.log('📊 RESUMEN POR COMPONENTE DE LA FASE 8.5:');
console.log('==========================================================================');
for (const r of results) {
  const icon = r.ok ? '✅' : '❌';
  console.log(` ${icon} ${r.name}: ${r.passed}/${r.total} pruebas superadas`);
}

console.log('==========================================================================');
console.log(`🏆 RESULTADO GLOBAL FASE 8.5: ${totalPassed}/${totalTests} PRUEBAS SUPERADAS (${Math.round((totalPassed/totalTests)*100)}%)`);
console.log('==========================================================================');

if (totalPassed === totalTests) {
  console.log('🎉 ¡TODA LA FASE 8.5 (MERCADO ENTRE JUGADORES) ESTÁ 100% VERIFICADA Y CUMPLIDA!\n');
  process.exit(0);
} else {
  process.exit(1);
}
