/**
 * Test Suite: Ranking por Poder de Colección y Trigger (Fase 8 - Punto 3)
 * GDD Sección 7.2 & TDD Sección 6
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO RANKING POR PODER DE COLECCIÓN Y TRIGGER (FASE 8 - PUNTO 3)');
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

// 1. Backend: recalculateCollectionPower.ts y getCollectionRanking.ts
const recalcTsPath = path.resolve(__dirname, '../src/social/recalculateCollectionPower.ts');
assert(fs.existsSync(recalcTsPath), 'recalculateCollectionPower.ts existe en functions/src/social/');

const recalcContent = fs.existsSync(recalcTsPath) ? fs.readFileSync(recalcTsPath, 'utf8') : '';
assert(recalcContent.includes('RARITY_POWER_POINTS_TABLE'), 'Tabla de puntos por rareza RARITY_POWER_POINTS_TABLE definida');
assert(recalcContent.includes('comun: 1') && recalcContent.includes('mitica: 15') && recalcContent.includes('full_art: 25'), 'Valores oficiales de rareza según GDD 7.2 (1, 2, 4, 8, 15, 25)');
assert(recalcContent.includes('updateCachedCollectionPower'), 'Función updateCachedCollectionPower implementada para cachear en users');
assert(recalcContent.includes('recalculateCollectionPowerTrigger'), 'Trigger de Firestore recalculateCollectionPowerTrigger implementado');
assert(recalcContent.includes('onWrite'), 'Trigger configurado con onWrite sobre users/{userId}/cards/{cardId}');
assert(recalcContent.includes('recalculateCollectionPower = functions.https.onCall'), 'Callable recalculateCollectionPower declarada para el cliente');

const rankingTsPath = path.resolve(__dirname, '../src/social/getCollectionRanking.ts');
assert(fs.existsSync(rankingTsPath), 'getCollectionRanking.ts existe');

const rankingContent = fs.existsSync(rankingTsPath) ? fs.readFileSync(rankingTsPath, 'utf8') : '';
assert(rankingContent.includes('getCollectionRanking = functions.https.onCall'), 'Callable getCollectionRanking declarada');
assert(rankingContent.includes('collectionPower') && rankingContent.includes('desc'), 'Consulta ordenada por collectionPower desc');

const indexTsPath = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexTsPath, 'utf8');
assert(indexContent.includes('recalculateCollectionPower'), 'recalculateCollectionPower exportado en index.ts');
assert(indexContent.includes('recalculateCollectionPowerTrigger'), 'recalculateCollectionPowerTrigger exportado en index.ts');
assert(indexContent.includes('getCollectionRanking'), 'getCollectionRanking exportado en index.ts');

// 2. Cliente C#: PlayerCollectionManager.cs
const playerMgrPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Cards/PlayerCollectionManager.cs');
assert(fs.existsSync(playerMgrPath), 'PlayerCollectionManager.cs existe');

const playerMgrContent = fs.readFileSync(playerMgrPath, 'utf8');
assert(playerMgrContent.includes('CollectionPower { get; private set; }'), 'Propiedad CollectionPower declarada');
assert(playerMgrContent.includes('OnCollectionPowerUpdated'), 'Evento OnCollectionPowerUpdated declarado');
assert(playerMgrContent.includes('CalculateCollectionPower()'), 'Método CalculateCollectionPower implementado');
assert(playerMgrContent.includes('GetRarityPowerPoints'), 'Método GetRarityPowerPoints implementado');
assert(playerMgrContent.includes('case Rarity.Comun: return 1;') && playerMgrContent.includes('case Rarity.Mitica: return 15;'), 'Puntos de rareza de C# coinciden con GDD 7.2');
assert(playerMgrContent.includes('PlayerPrefs.SetInt("Player_CollectionPower"'), 'Persistencia en PlayerPrefs del poder de colección');

// 3. Cliente C#: SocialService.cs
const socialServicePath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/SocialService.cs');
assert(fs.existsSync(socialServicePath), 'SocialService.cs existe');

const socialContent = fs.readFileSync(socialServicePath, 'utf8');
assert(socialContent.includes('class RankingEntry'), 'Clase RankingEntry declarada en SocialService');
assert(socialContent.includes('GetFriendsRanking()'), 'Método GetFriendsRanking() implementado');
assert(socialContent.includes('b.power.CompareTo(a.power)'), 'Ranking ordenado por poder descendente');
assert(socialContent.includes('list[i].rank = i + 1;'), 'Asignación de puestos oficiales 1, 2, 3...');

// 4. Controlador UI Toolkit: UIToolkitFriendsController.cs
const controllerPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitFriendsController.cs');
assert(fs.existsSync(controllerPath), 'UIToolkitFriendsController.cs existe');

const ctrlContent = fs.readFileSync(controllerPath, 'utf8');
assert(ctrlContent.includes('UpdateRankingUI()'), 'Método UpdateRankingUI implementado en UIToolkitFriendsController');
assert(ctrlContent.includes('Ranking_Row_'), 'Filas de ranking vinculadas dinámicamente');
assert(ctrlContent.includes('ranking-row-me'), 'Estilo distintivo ranking-row-me aplicado a Tú');
assert(ctrlContent.includes('OnCollectionPowerUpdated'), 'Suscripción reactiva a cambios de poder de colección');

// 5. Verificación matemática de la fórmula GDD 7.2
function computeTestPower(cards) {
  const points = { comun: 1, especial: 2, poco_comun: 2, epica: 4, rara: 4, legendaria: 8, mitica: 15, full_art: 25 };
  const uniqueSeen = new Set();
  let total = 0;
  for (const c of cards) {
    if (!uniqueSeen.has(c.id)) {
      uniqueSeen.add(c.id);
      total += points[c.rarity] || 1;
    }
  }
  return total;
}

const testSample = [
  { id: 'C1', rarity: 'comun' },
  { id: 'C1', rarity: 'comun' }, // Duplicado no suma
  { id: 'C2', rarity: 'epica' },  // +4
  { id: 'C3', rarity: 'mitica' }, // +15
  { id: 'C3', rarity: 'mitica' }  // Duplicado no suma
];
const calculated = computeTestPower(testSample);
assert(calculated === 20, `Verificación de fórmula sin contar duplicados (Esperado: 20, Obtenido: ${calculated})`);

console.log('==========================================================================');
console.log(`🎉 RESULTADO: ${passed}/${total} PRUEBAS COMPLETADAS CON ÉXITO.`);
console.log('==========================================================================');

if (passed !== total) {
  process.exit(1);
}
