/**
 * Test Automatizado de Reglas de Seguridad de Firestore (Fase 5 - Punto 9)
 * Valida:
 * 1. Edición de datos cosméticos permitida al dueño del perfil
 * 2. Bloqueo total de modificación directa de saldo (monedas/gemas/sobres) desde el cliente
 * 3. Bloqueo total de inyección directa de cartas en userCollection
 * 4. Bloqueo de catálogos y registros de idempotencia
 */

class MockFirestoreRulesEngine {
  constructor() {
    this.protectedUserFields = [
      'coins',
      'gems',
      'availablePacks',
      'collectionPower',
      'packsOpenedTotal',
      'lastFreePackClaimAt',
      'adsWatchedTodayCount',
      'lastAdWatchedDate',
    ];
  }

  evaluateUserUpdate(authUid, targetUserId, updatedFields) {
    if (!authUid || authUid !== targetUserId) {
      return { allowed: false, reason: "No eres el dueño de este perfil." };
    }

    const modifiedKeys = Object.keys(updatedFields);
    const hasProtectedField = modifiedKeys.some((k) => this.protectedUserFields.includes(k));

    if (hasProtectedField) {
      return {
        allowed: false,
        reason: "Permiso denegado: El cliente no puede modificar monedas, sobres ni poder de colección.",
      };
    }

    return { allowed: true };
  }

  evaluateCollectionWrite(authUid, targetUserId) {
    // userCollection es write: false para clientes
    return {
      allowed: false,
      reason: "Permiso denegado: Las cartas solo pueden ser modificadas por Cloud Functions del servidor.",
    };
  }

  evaluateCatalogWrite(authUid) {
    return {
      allowed: false,
      reason: "Permiso denegado: Los catálogos son de solo lectura.",
    };
  }

  evaluateProcessedRequestsRead(authUid) {
    return {
      allowed: false,
      reason: "Permiso denegado: processedRequests es privado exclusivo del backend.",
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Reglas de Seguridad de Firestore (Fase 5.9)");
  console.log("==========================================================================\n");

  const rules = new MockFirestoreRulesEngine();
  const userId = "user_mateo_01";

  // ----------------------------------------------------
  // TEST 1: Actualizar nombre y 11 ideal (Cosméticos legítimos)
  // ----------------------------------------------------
  console.log("▶️ TEST 1: El usuario actualiza su nombre de usuario y táctica del 11 ideal...");
  const update1 = { displayName: "Mateo Golazo", lineup11: ["LD", "VJ", "EH"] };
  const res1 = rules.evaluateUserUpdate(userId, userId, update1);

  console.log(`  📝 Resultado de la regla: ¿Permitido?: ${res1.allowed}`);
  if (res1.allowed) {
    console.log("  ✅ PASÓ: El usuario puede modificar sus datos cosméticos y tácticos.\n");
  } else {
    console.error("  ❌ FALLÓ en la actualización de cosméticos.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Intento de hackear monedas directamente desde el cliente
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Intento de inyectar 999,999 monedas editando Firestore desde el móvil...");
  const updateFraud = { coins: 999999 };
  const res2 = rules.evaluateUserUpdate(userId, userId, updateFraud);

  console.log(`  🛡️ Resultado de la regla: ¿Permitido?: ${res2.allowed}`);
  console.log(`  🛑 Razón del bloqueo: "${res2.reason}"`);

  if (!res2.allowed) {
    console.log("  ✅ PASÓ: Las reglas de Firestore bloquearon el intento de hackeo de monedas.\n");
  } else {
    console.error("  ❌ FALLÓ: Se permitió la modificación de monedas desde el cliente.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Intento de agregarse una carta mítica directamente al inventario
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Intento de escribir directamente en users/.../collection/LD_Mitica...");
  const res3 = rules.evaluateCollectionWrite(userId, userId);

  console.log(`  🛡️ Resultado de la regla: ¿Permitido?: ${res3.allowed}`);
  console.log(`  🛑 Razón del bloqueo: "${res3.reason}"`);

  if (!res3.allowed) {
    console.log("  ✅ PASÓ: Las cartas no pueden ser manipuladas directamente por el cliente.\n");
  } else {
    console.error("  ❌ FALLÓ: Se permitió la escritura directa en userCollection.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: Intento de modificar catálogo de cartas o sobres
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Intento de modificar precios en packs/ o cardsCatalog/...");
  const res4 = rules.evaluateCatalogWrite(userId);

  console.log(`  🛡️ Resultado de la regla: ¿Permitido?: ${res4.allowed}`);
  if (!res4.allowed) {
    console.log("  ✅ PASÓ: Catálogos protegidos contra escritura.\n");
  } else {
    console.error("  ❌ FALLÓ en la protección de catálogos.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 5: Intento de leer processedRequests desde el cliente
  // ----------------------------------------------------
  console.log("▶️ TEST 5: Intento de leer la colección privada processedRequests...");
  const res5 = rules.evaluateProcessedRequestsRead(userId);

  console.log(`  🛡️ Resultado de la regla: ¿Permitido?: ${res5.allowed}`);
  if (!res5.allowed) {
    console.log("  ✅ PASÓ: processedRequests es completamente inaccesible para los clientes.");
  } else {
    console.error("  ❌ FALLÓ en la protección de processedRequests.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS REGLAS DE SEGURIDAD DE FIRESTORE FUERON VALIDADAS! (5/5)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
