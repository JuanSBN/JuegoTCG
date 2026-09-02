/**
 * Test Automatizado de Idempotencia y Resiliencia de Red
 * Simula el ciclo de vida de transacciones en Firestore y reintentos ante fallos de conexión.
 */

class MockFirestoreTransaction {
  constructor(db) {
    this.db = db;
  }

  set(docRef, data) {
    this.db.storage.set(docRef.path, data);
  }

  get(docRef) {
    const data = this.db.storage.get(docRef.path);
    return {
      exists: !!data,
      data: () => data,
    };
  }
}

class MockDocRef {
  constructor(path) {
    this.path = path;
  }
}

class MockFirestore {
  constructor() {
    this.storage = new Map();
  }

  collection(name) {
    return {
      doc: (id) => new MockDocRef(`${name}/${id}`),
    };
  }

  async runTransaction(updateFunction) {
    const tx = new MockFirestoreTransaction(this);
    return await updateFunction(tx);
  }
}

// Lógica de Idempotencia del Servidor (TDD Sección 2.6 y 5.9)
async function executeEconomicOperationWithIdempotency(
  db,
  idempotencyKey,
  userId,
  functionName,
  operationLogic
) {
  // 1. Verificar si ya fue procesada anteriormente
  const reqDoc = db.storage.get(`processedRequests/${idempotencyKey}`);
  if (reqDoc) {
    console.log(`  🔍 [CACHE HIT] La clave "${idempotencyKey}" ya fue procesada previamente.`);
    return { result: reqDoc.result, fromCache: true };
  }

  // 2. Ejecutar la operación económica dentro de una transacción atómica
  return await db.runTransaction(async (tx) => {
    // Re-chequeo dentro de la transacción
    const checkDoc = tx.get(new MockDocRef(`processedRequests/${idempotencyKey}`));
    if (checkDoc.exists) {
      return { result: checkDoc.data().result, fromCache: true };
    }

    // Ejecutar lógica autoritativa del servidor
    const opResult = await operationLogic();

    // Guardar el registro de idempotencia de forma atómica
    tx.set(new MockDocRef(`processedRequests/${idempotencyKey}`), {
      idempotencyKey,
      userId,
      functionName,
      result: opResult,
      createdAt: new Date().toISOString(),
    });

    return { result: opResult, fromCache: false };
  });
}

// ==========================================
// EJECUCIÓN DEL TEST AUTOMATIZADO
// ==========================================
async function runTests() {
  console.log("\n========================================================");
  console.log("🧪 INICIANDO TEST AUTOMATIZADO: MECANISMO DE IDEMPOTENCIA");
  console.log("========================================================\n");

  const db = new MockFirestore();
  const userId = "user_jugador_01";
  let timesLogicExecuted = 0;

  const mockOpenPackLogic = async () => {
    timesLogicExecuted++;
    return {
      packId: "pack_oro_01",
      cardsObtained: ["LD_Luis_Diaz_Mitica", "VJ_Vinicius_Rara", "EH_Haaland_Comun"],
      coinsDeducted: 100,
    };
  };

  const key1 = "UUID-RETRY-TEST-001";

  // ----------------------------------------------------
  // CASO 1: Primer intento legítimo (Conexión normal)
  // ----------------------------------------------------
  console.log("▶️ CASO 1: Primer intento de abrir sobre con key:", key1);
  const attempt1 = await executeEconomicOperationWithIdempotency(
    db,
    key1,
    userId,
    "openPack",
    mockOpenPackLogic
  );

  console.log("  📦 Resultado recibido:", JSON.stringify(attempt1.result));
  console.log("  ⚡ ¿Vino de caché?:", attempt1.fromCache);
  console.log("  🔄 Veces que el servidor ejecutó el cobro/RNG:", timesLogicExecuted);

  if (!attempt1.fromCache && timesLogicExecuted === 1) {
    console.log("  ✅ PASÓ: El primer intento ejecutó el cobro y generó las cartas correctamente.\n");
  } else {
    console.error("  ❌ FALLÓ: El primer intento no se comportó como esperado.\n");
    process.exit(1);
  }

  // ----------------------------------------------------
  // CASO 2: Segundo intento por caída de red (Misma key)
  // ----------------------------------------------------
  console.log("▶️ CASO 2: Simulación de caída de red / doble clic con la MISMA key:", key1);
  const attempt2 = await executeEconomicOperationWithIdempotency(
    db,
    key1,
    userId,
    "openPack",
    mockOpenPackLogic
  );

  console.log("  📦 Resultado recibido:", JSON.stringify(attempt2.result));
  console.log("  ⚡ ¿Vino de caché?:", attempt2.fromCache);
  console.log("  🔄 Veces que el servidor ejecutó el cobro/RNG:", timesLogicExecuted);

  if (attempt2.fromCache && timesLogicExecuted === 1) {
    console.log("  ✅ PASÓ: El servidor protegió al usuario y devolvió las mismas cartas sin cobrar doble.\n");
  } else {
    console.error("  ❌ FALLÓ: El servidor ejecutó el cobro dos veces (Error crítico de duplicación).\n");
    process.exit(1);
  }

  // ----------------------------------------------------
  // CASO 3: Nueva solicitud legítima (Nueva key)
  // ----------------------------------------------------
  const key2 = "UUID-NEW-PURCHASE-002";
  console.log("▶️ CASO 3: Nueva apertura de sobre con NUEVA key:", key2);
  const attempt3 = await executeEconomicOperationWithIdempotency(
    db,
    key2,
    userId,
    "openPack",
    mockOpenPackLogic
  );

  console.log("  📦 Resultado recibido:", JSON.stringify(attempt3.result));
  console.log("  ⚡ ¿Vino de caché?:", attempt3.fromCache);
  console.log("  🔄 Veces que el servidor ejecutó el cobro/RNG:", timesLogicExecuted);

  if (!attempt3.fromCache && timesLogicExecuted === 2) {
    console.log("  ✅ PASÓ: La nueva solicitud se procesó como una transacción independiente.\n");
  } else {
    console.error("  ❌ FALLÓ: La nueva solicitud no incrementó el contador de operaciones.\n");
    process.exit(1);
  }

  console.log("========================================================");
  console.log("🎉 ¡TODOS LOS TESTS DE IDEMPOTENCIA PASARON CON ÉXITO! (3/3)");
  console.log("========================================================\n");
}

runTests().catch(console.error);
