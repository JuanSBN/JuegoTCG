/**
 * Test Suite: Comparación de Álbumes entre Amigos (Vista Lado a Lado) - Fase 8 Punto 2
 * Valida:
 *  1. Backend Cloud Function (compareAlbums.ts) e index.ts
 *  2. Estructuras C# y métodos en SocialService.cs
 *  3. Integración en UIToolkitFriendsController.cs (Head-to-head, filtros, tarjetas)
 *  4. Definición de UI Toolkit en FriendsScreen.uxml
 *  5. Estilos en FriendsScreen.uss
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO COMPARACIÓN DE ÁLBUMES ENTRE AMIGOS (FASE 8 - PUNTO 2)');
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

// 1. Backend Cloud Function
const compareAlbumsTs = path.resolve(__dirname, '../src/social/compareAlbums.ts');
assert(fs.existsSync(compareAlbumsTs), 'compareAlbums.ts existe en functions/src/social/');

const compareTsContent = fs.existsSync(compareAlbumsTs) ? fs.readFileSync(compareAlbumsTs, 'utf8') : '';
assert(compareTsContent.includes('export const compareAlbums'), 'Cloud Function compareAlbums declarada como export');
assert(compareTsContent.includes('PILOT_ALBUM_CARDS'), 'Catálogo base del álbum piloto contemplado');
assert(compareTsContent.includes('missing_for_me') && compareTsContent.includes('missing_for_friend'), 'Estados de faltantes analizados lado a lado');
assert(compareTsContent.includes('both_owned'), 'Detección de cartas en común implementada');

const indexTs = path.resolve(__dirname, '../src/index.ts');
const indexContent = fs.readFileSync(indexTs, 'utf8');
assert(indexContent.includes('compareAlbums'), 'compareAlbums exportada en index.ts');

// 2. C# SocialService
const socialServiceCs = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/SocialService.cs');
assert(fs.existsSync(socialServiceCs), 'SocialService.cs existe');

const socialCsContent = fs.readFileSync(socialServiceCs, 'utf8');
assert(socialCsContent.includes('enum CardComparisonStatus'), 'Enum CardComparisonStatus declarado');
assert(socialCsContent.includes('class CardComparisonItem'), 'Clase CardComparisonItem declarada');
assert(socialCsContent.includes('class AlbumComparisonData'), 'Clase AlbumComparisonData declarada');
assert(socialCsContent.includes('GetFriendAlbumComparison'), 'Método GetFriendAlbumComparison implementado');
assert(socialCsContent.includes('MissingForMe') && socialCsContent.includes('MissingForFriend'), 'Lógica de clasificación de faltantes y duplicados en C#');

// 3. C# UIToolkitFriendsController
const controllerCs = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitFriendsController.cs');
assert(fs.existsSync(controllerCs), 'UIToolkitFriendsController.cs existe');

const ctrlContent = fs.readFileSync(controllerCs, 'utf8');
assert(ctrlContent.includes('OpenCompareModal'), 'Método OpenCompareModal implementado en controlador');
assert(ctrlContent.includes('SetFilter'), 'Filtrado interactivo por tabs implementado');
assert(ctrlContent.includes('RenderCardsList'), 'Generación dinámica de filas de cartas comparadas implementada');
assert(ctrlContent.includes('Btn_TradeWithFriend'), 'Acción directa Proponer Intercambio enlazada');
assert(ctrlContent.includes('CompareDualProgressMe'), 'Soporte de barra de progreso dual implementado');

// 4. UI Toolkit UXML (FriendsScreen.uxml)
const uxmlPath = path.resolve(__dirname, '../../Assets/_Project/UI/Views/FriendsScreen.uxml');
assert(fs.existsSync(uxmlPath), 'FriendsScreen.uxml existe');

const uxmlContent = fs.readFileSync(uxmlPath, 'utf8');
assert(uxmlContent.includes('name="CompareModal"'), 'Elemento CompareModal presente');
assert(uxmlContent.includes('class="modal-box modal-box-compare"'), 'Clase modal-box-compare configurada');
assert(uxmlContent.includes('name="CompareHeadToHead"'), 'Sección CompareHeadToHead presente');
assert(uxmlContent.includes('name="CompareMeCol"') && uxmlContent.includes('name="CompareFriendCol"'), 'Columnas Tú y Amigo presentes lado a lado');
assert(uxmlContent.includes('name="CompareDualProgressMe"') && uxmlContent.includes('name="CompareDualProgressFriend"'), 'Barra de progreso dual presente');
assert(uxmlContent.includes('name="Btn_Filter_All"') && uxmlContent.includes('name="Btn_Filter_Need"') && uxmlContent.includes('name="Btn_Filter_Offers"'), '3 Tabs de filtros rápidos presentes');
assert(uxmlContent.includes('name="CompareScrollView"'), 'ScrollView de cartas comparadas presente');
assert(uxmlContent.includes('name="CompareCardsList"'), 'Contenedor dinámico CompareCardsList presente');
assert(uxmlContent.includes('name="Btn_TradeWithFriend"'), 'Botón Btn_TradeWithFriend presente en modal');

// 5. UI Toolkit USS (FriendsScreen.uss)
const ussPath = path.resolve(__dirname, '../../Assets/_Project/UI/Styles/FriendsScreen.uss');
assert(fs.existsSync(ussPath), 'FriendsScreen.uss existe');

const ussContent = fs.readFileSync(ussPath, 'utf8');
assert(ussContent.includes('.modal-box-compare'), 'Estilo .modal-box-compare declarado');
assert(ussContent.includes('.compare-h2h-container'), 'Estilo .compare-h2h-container declarado');
assert(ussContent.includes('.compare-dual-progress-track'), 'Estilo .compare-dual-progress-track declarado');
assert(ussContent.includes('.compare-tab-active'), 'Estilo .compare-tab-active declarado');
assert(ussContent.includes('.compare-tag-missing-me'), 'Estilo .compare-tag-missing-me declarado');
assert(ussContent.includes('.compare-tag-missing-friend'), 'Estilo .compare-tag-missing-friend declarado');
assert(ussContent.includes('.compare-tag-both'), 'Estilo .compare-tag-both declarado');
assert(ussContent.includes('.compare-btn-primary'), 'Estilo .compare-btn-primary declarado');

console.log('==========================================================================');
console.log(`🎉 RESULTADO: ${passed}/${total} PRUEBAS COMPLETADAS CON ÉXITO.`);
console.log('==========================================================================');

if (passed !== total) {
  process.exit(1);
}
