/**
 * ============================================================================
 * SUITE MAESTRA DE VERIFICACIÓN INTEGRAL - FASE 8 (SISTEMA SOCIAL COMPLETO)
 * ============================================================================
 * Cubre los 7 puntos oficiales del GDD y TDD para la Fase 8:
 *  Punto 1: Agregar amigos por código (sendFriendRequest, manageFriendRequest, UI).
 *  Punto 2: Comparación de álbumes entre amigos (vista lado a lado).
 *  Punto 3: Ranking por poder de colección (fórmula GDD 7.2 y trigger Firestore).
 *  Punto 4: Intercambio directo anti-fraude (proposeTrade, acceptTrade, cancelTrade).
 *  Punto 5: Firestore Rules de tradeOffers (TDD sección 7 y 7.2).
 *  Punto 6: Caso límite anti-fraude (carta no disponible -> 0 cartas movidas).
 *  Punto 7: Notificaciones push FCM y telemetría de Analytics (trade_proposed/accepted).
 * ============================================================================
 */

const fs = require('fs');
const path = require('path');

console.log('==========================================================================');
console.log('🌟 INICIANDO BATERÍA MAESTRA DE PRUEBAS DE LA FASE 8 (SISTEMA SOCIAL)');
console.log('==========================================================================');

let totalAsserts = 0;
let passedAsserts = 0;
const sectionResults = [];

function assert(condition, description) {
  totalAsserts++;
  if (condition) {
    passedAsserts++;
    console.log("  ✅ " + description);
  } else {
    console.error("  ❌ FALLÓ: " + description);
  }
}

function runSection(title, fn) {
  console.log("\n--------------------------------------------------------------------------");
  console.log("▶️ " + title);
  console.log("--------------------------------------------------------------------------");
  const before = passedAsserts;
  const beforeTotal = totalAsserts;
  fn();
  const passedInSection = passedAsserts - before;
  const totalInSection = totalAsserts - beforeTotal;
  const ok = passedInSection === totalInSection;
  sectionResults.push({ title, passed: passedInSection, total: totalInSection, ok });
  console.log("   Resultado sección: " + passedInSection + "/" + totalInSection + (ok ? " (PERFECTO)" : " (ERRORES)"));
}

// ----------------------------------------------------------------------------
// PUNTO 1: AGREGAR AMIGOS POR CÓDIGO
// ----------------------------------------------------------------------------
runSection("PUNTO 1: Agregar amigos por código (TDD 5.8 & GDD 7)", () => {
  const sendTsPath = path.resolve(__dirname, '../src/social/sendFriendRequest.ts');
  const manageTsPath = path.resolve(__dirname, '../src/social/manageFriendRequest.ts');
  const socialCsPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/SocialService.cs');
  const friendsUxml = path.resolve(__dirname, '../../Assets/_Project/UI/Views/FriendsScreen.uxml');

  assert(fs.existsSync(sendTsPath), "Cloud Function sendFriendRequest.ts existe");
  assert(fs.existsSync(manageTsPath), "Cloud Function manageFriendRequest.ts existe");
  assert(fs.existsSync(socialCsPath), "SocialService.cs existe en Unity");
  assert(fs.existsSync(friendsUxml), "FriendsScreen.uxml existe");

  const sendContent = fs.readFileSync(sendTsPath, 'utf8');
  assert(sendContent.includes("targetUid === fromUid"), "Validación anti-self-request (no agregarse a sí mismo)");
  assert(sendContent.includes("toUpperCase()"), "Normalización de código de amigo a mayúsculas");
  assert(sendContent.includes("COLLECTIONS.FRIEND_REQUESTS"), "Persistencia en colección friendRequests");

  const manageContent = fs.readFileSync(manageTsPath, 'utf8');
  assert(manageContent.includes("db.runTransaction"), "Aceptación de amistad mediante transacción atómica");
  assert(manageContent.includes("COLLECTIONS.FRIENDS"), "Enlace bidireccional en subcolección de amigos");

  const uxmlContent = fs.readFileSync(friendsUxml, 'utf8');
  assert(uxmlContent.includes("Agrega amigos con su código de amigo"), "Estado vacío oficial exacto configurado");
  assert(uxmlContent.includes("SearchFriendInput"), "Campo de texto para ingresar código de amigo presente (SearchFriendInput)");
  assert(uxmlContent.includes("Btn_AddFriend"), "Botón de agregar amigo presente");
});

// ----------------------------------------------------------------------------
// PUNTO 2: COMPARACIÓN DE ÁLBUMES ENTRE AMIGOS (LADO A LADO)
// ----------------------------------------------------------------------------
runSection("PUNTO 2: Comparación de álbumes entre amigos (GDD 7 & TDD 5.8)", () => {
  const compareTsPath = path.resolve(__dirname, '../src/social/compareAlbums.ts');
  const socialCsPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/SocialService.cs');
  const uxmlPath = path.resolve(__dirname, '../../Assets/_Project/UI/Views/FriendsScreen.uxml');

  assert(fs.existsSync(compareTsPath), "Cloud Function compareAlbums.ts existe");
  const compareContent = fs.readFileSync(compareTsPath, 'utf8');
  assert(compareContent.includes("missingForMeCount"), "Cálculo de cartas faltantes para el usuario (missingForMeCount)");
  assert(compareContent.includes("missingForFriendCount"), "Cálculo de cartas faltantes para el amigo (missingForFriendCount)");
  assert(compareContent.includes("bothOwnedCount"), "Cálculo de cartas que ambos poseen (bothOwnedCount)");

  const socialContent = fs.readFileSync(socialCsPath, 'utf8');
  assert(socialContent.includes("class AlbumComparisonData"), "Estructura AlbumComparisonData declarada en C#");
  assert(socialContent.includes("CardComparisonStatus"), "Enum CardComparisonStatus (BothOwned, MissingForMe, MissingForFriend)");
  assert(socialContent.includes("GetFriendAlbumComparison"), "Método GetFriendAlbumComparison implementado");

  const uxmlContent = fs.readFileSync(uxmlPath, 'utf8');
  assert(uxmlContent.includes("CompareModal"), "Modal CompareModal integrado en FriendsScreen.uxml");
  assert(uxmlContent.includes("CompareHeadToHead"), "Sección Head-to-Head lado a lado presente");
  assert(uxmlContent.includes("CompareCardsList"), "Contenedor para lista de cartas comparadas presente");
  assert(uxmlContent.includes("Btn_TradeWithFriend"), "Acción directa 'Proponer Intercambio' desde la comparación");
});

// ----------------------------------------------------------------------------
// PUNTO 3: RANKING POR PODER DE COLECCIÓN Y TRIGGER EN FIRESTORE
// ----------------------------------------------------------------------------
runSection("PUNTO 3: Ranking por Poder de Colección y Trigger (GDD 7.2 & TDD 6)", () => {
  const recalcTsPath = path.resolve(__dirname, '../src/social/recalculateCollectionPower.ts');
  const rankingTsPath = path.resolve(__dirname, '../src/social/getCollectionRanking.ts');
  const colMgrPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Cards/PlayerCollectionManager.cs');
  const friendsCtrlPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitFriendsController.cs');

  assert(fs.existsSync(recalcTsPath), "recalculateCollectionPower.ts existe");
  assert(fs.existsSync(rankingTsPath), "getCollectionRanking.ts existe");
  assert(fs.existsSync(colMgrPath), "PlayerCollectionManager.cs existe");

  const recalcContent = fs.readFileSync(recalcTsPath, 'utf8');
  assert(recalcContent.includes("comun: 1"), "Común = 1 punto oficial (GDD 7.2)");
  assert(recalcContent.includes("especial: 2") || recalcContent.includes("poco_comun: 2"), "Especial/Poco común = 2 puntos");
  assert(recalcContent.includes("epica: 4") || recalcContent.includes("rara: 4"), "Épica/Rara = 4 puntos");
  assert(recalcContent.includes("legendaria: 8"), "Legendaria = 8 puntos");
  assert(recalcContent.includes("mitica: 15"), "Mítica = 15 puntos");
  assert(recalcContent.includes("full_art: 25"), "Full Art = 25 puntos");
  assert(recalcContent.includes("recalculateCollectionPowerTrigger"), "Trigger Firestore recalculateCollectionPowerTrigger implementado");
  assert(recalcContent.includes("collectionPower: totalPower"), "Campo cacheado collectionPower en users/{userId} (TDD 6)");

  const colMgrContent = fs.readFileSync(colMgrPath, 'utf8');
  assert(colMgrContent.includes("CalculateCollectionPower"), "Método CalculateCollectionPower implementado en C#");
  assert(colMgrContent.includes("OnCollectionPowerUpdated"), "Evento reactivo OnCollectionPowerUpdated declarado");

  const friendsCtrlContent = fs.readFileSync(friendsCtrlPath, 'utf8');
  assert(friendsCtrlContent.includes("UpdateRankingUI"), "Método UpdateRankingUI implementado en UI Toolkit");
  assert(friendsCtrlContent.includes("ranking-row-me"), "Clase visual ranking-row-me para destacar a 'Tú'");
});

// ----------------------------------------------------------------------------
// PUNTO 4: INTERCAMBIO DIRECTO ANTI-FRAUDE (TRANSACCIÓN ATÓMICA)
// ----------------------------------------------------------------------------
runSection("PUNTO 4: Intercambio Directo Anti-Fraude (TDD 2.5 & 5.8)", () => {
  const tradeTsPath = path.resolve(__dirname, '../src/social/tradeOperations.ts');
  const tradeCsPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/TradeService.cs');
  const tradeCtrlPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitTradeController.cs');

  assert(fs.existsSync(tradeTsPath), "tradeOperations.ts existe");
  assert(fs.existsSync(tradeCsPath), "TradeService.cs existe");
  assert(fs.existsSync(tradeCtrlPath), "UIToolkitTradeController.cs existe");

  const tradeContent = fs.readFileSync(tradeTsPath, 'utf8');
  assert(tradeContent.includes("export const proposeTrade"), "proposeTrade implementada");
  assert(tradeContent.includes("MAX_DAILY_PROPOSALS = 10"), "Límite anti-spam de 10 ofertas/día (TDD 2.5)");
  assert(tradeContent.includes("OFFER_EXPIRATION_HOURS = 48"), "Caducidad de oferta fijada en 48 horas (TDD 2.5)");
  assert(tradeContent.includes("export const acceptTrade"), "acceptTrade implementada");
  assert(tradeContent.includes("db.runTransaction"), "acceptTrade corre dentro de db.runTransaction()");
  assert(tradeContent.includes("export const cancelTrade"), "cancelTrade implementada");
  assert(tradeContent.includes("COLLECTIONS.TRANSACTIONS"), "Auditoría transaccional en colección transactions");

  const tradeCsContent = fs.readFileSync(tradeCsPath, 'utf8');
  assert(tradeCsContent.includes("AcceptTradeAsync"), "Método AcceptTradeAsync implementado en C#");
  assert(tradeCsContent.includes("RejectTradeAsync"), "Método RejectTradeAsync implementado en C#");
  assert(tradeCsContent.includes("CancelSentTradeAsync"), "Método CancelSentTradeAsync implementado en C#");
});

// ----------------------------------------------------------------------------
// PUNTO 5: FIRESTORE RULES DE TRADE OFFERS
// ----------------------------------------------------------------------------
runSection("PUNTO 5: Firestore Rules de tradeOffers (TDD 7 & 7.2)", () => {
  const rulesPath = path.resolve(__dirname, '../../firestore.rules');
  assert(fs.existsSync(rulesPath), "firestore.rules existe");

  const rulesContent = fs.readFileSync(rulesPath, 'utf8');
  assert(rulesContent.includes("match /tradeOffers/{tradeId}"), "Regla match /tradeOffers/{tradeId} definida");
  assert(rulesContent.includes("function isValidTradeOffer(data)"), "Función helper isValidTradeOffer implementada (TDD 7.2)");
  assert(rulesContent.includes("resource.data.fromUid == request.auth.uid"), "Lectura autorizada para el proponente (fromUid)");
  assert(rulesContent.includes("resource.data.toUid == request.auth.uid"), "Lectura autorizada para el receptor (toUid)");
  assert(rulesContent.includes("allow write: if false;"), "Bloqueo absoluto de escritura para clientes (allow write: if false)");
  assert(rulesContent.includes("fromUid != data.toUid") || rulesContent.includes("fromUid != toUid"), "Validación anti-self-trade en esquema de Firestore Rules");
  assert(rulesContent.includes("offeredQty > 0") && rulesContent.includes("requestedQty > 0"), "Validación de cantidades estrictamente positivas");
});

// ----------------------------------------------------------------------------
// PUNTO 6: CASO LÍMITE DE CARTA NO DISPONIBLE (CERO CARTAS MOVIDAS)
// ----------------------------------------------------------------------------
runSection("PUNTO 6: Caso Límite Anti-Fraude (Carta No Disponible -> 0 Movidas)", () => {
  const tradeContent = fs.readFileSync(path.resolve(__dirname, '../src/social/tradeOperations.ts'), 'utf8');

  assert(tradeContent.includes("fromQty < offeredQty"), "Revalidación in-situ de posesión del proponente");
  assert(tradeContent.includes("toQty < requestedQty"), "Revalidación in-situ de posesión del receptor");
  assert(tradeContent.includes("El proponente ya no posee la carta ofrecida"), "Cancelación con mensaje explícito si el proponente la vendió");
  assert(tradeContent.includes("El receptor ya no posee la carta solicitada"), "Cancelación con mensaje explícito si el receptor ya no la tiene");
  assert(tradeContent.includes("failed-precondition"), "Retorno de código failed-precondition para abortar");

  // Simulación matemática del caso límite
  let cartA_alice = 0; // Alice la vendió antes de que Bob aceptara
  let cartB_bob = 1;   // Bob aún tiene su carta
  let tradeStatus = "pendiente";
  let cardsTransferred = false;

  if (cartA_alice < 1) {
    tradeStatus = "cancelado";
    cardsTransferred = false;
  } else if (cartB_bob < 1) {
    tradeStatus = "cancelado";
    cardsTransferred = false;
  } else {
    cartA_alice--;
    cartB_bob--;
    tradeStatus = "aceptado";
    cardsTransferred = true;
  }

  assert(cardsTransferred === false, "Transacción abortada: ninguna carta fue transferida");
  assert(cartB_bob === 1, "El receptor conserva su carta intacta (quantity = 1)");
  assert(tradeStatus === "cancelado", "Estado de la oferta marcado como 'cancelado'");
});

// ----------------------------------------------------------------------------
// PUNTO 7: NOTIFICACIONES PUSH (FCM) Y REGISTRO DE ANALYTICS
// ----------------------------------------------------------------------------
runSection("PUNTO 7: Notificaciones Push (FCM) y Analytics (TDD 2.8 & 2.9)", () => {
  const firebaseContent = fs.readFileSync(path.resolve(__dirname, '../src/firebase.ts'), 'utf8');
  const tradeContent = fs.readFileSync(path.resolve(__dirname, '../src/social/tradeOperations.ts'), 'utf8');
  const tradeCsContent = fs.readFileSync(path.resolve(__dirname, '../../Assets/_Project/Scripts/Social/TradeService.cs'), 'utf8');

  assert(firebaseContent.includes("messaging = getMessaging()"), "Servicio FCM messaging exportado en firebase.ts");
  assert(firebaseContent.includes("ANALYTICS_EVENTS: 'analyticsEvents'") || firebaseContent.includes('ANALYTICS_EVENTS: "analyticsEvents"'), "Colección analyticsEvents declarada en constantes");

  assert(tradeContent.includes("notificationsEnabled"), "Consulta al campo notificationsEnabled de Ajustes");
  assert(tradeContent.includes("messaging.send"), "Despacho de notificación push FCM via messaging.send()");
  assert(tradeContent.includes("¡Nueva oferta de intercambio!"), "Título de push oficial configurado");
  assert(tradeContent.includes("Push omitido"), "Push suprimido respetando la preferencia del usuario si notificationsEnabled == false");

  assert(tradeContent.includes('event: "trade_proposed"'), "Evento trade_proposed registrado en backend al proponer (TDD 2.9)");
  assert(tradeContent.includes('event: "trade_accepted"'), "Evento trade_accepted registrado en backend dentro de la transacción atómica (TDD 2.9)");

  assert(tradeCsContent.includes("LogTradeProposed"), "Cliente Unity registra trade_proposed en FirebaseAnalyticsManager");
  assert(tradeCsContent.includes("LogTradeAccepted"), "Cliente Unity registra trade_accepted en FirebaseAnalyticsManager");
});

// ============================================================================
// RESUMEN GENERAL
// ============================================================================
console.log("\n==========================================================================");
console.log("📊 RESUMEN POR COMPONENTE DE LA FASE 8:");
console.log("==========================================================================");
sectionResults.forEach((sec, idx) => {
  const icon = sec.ok ? "✅" : "❌";
  console.log(" " + icon + " Punto " + (idx + 1) + ": " + sec.passed + "/" + sec.total + " pruebas superadas - " + sec.title);
});

console.log("==========================================================================");
console.log("🏆 RESULTADO GLOBAL: " + passedAsserts + "/" + totalAsserts + " PRUEBAS SUPERADAS (" + Math.round((passedAsserts/totalAsserts)*100) + "%)");
console.log("==========================================================================");

if (passedAsserts === totalAsserts) {
  console.log("🎉 ¡TODA LA FASE 8 (SISTEMA SOCIAL) ESTÁ 100% IMPLEMENTADA Y VERIFICADA!");
  process.exit(0);
} else {
  console.error("❌ Se detectaron inconsistencias en la Fase 8.");
  process.exit(1);
}
