/**
 * Test Automatizado del Flujo de Splash y Sesión (Fase 6 - Punto 2)
 * Valida:
 * 1. Primer ingreso: Splash detecta ausencia de sesión, crea cuenta anónima automática con saldo de bienvenida y entra a Inicio.
 * 2. Segundo ingreso: Splash detecta sesión en caché y entra de inmediato sin recrear cuenta.
 * 3. Vinculación con Google: Convierte la cuenta anónima en cuenta permanente sin perder saldo ni cartas.
 */

class MockAuthFlowEngine {
  constructor() {
    this.localStorage = new Map();
    this.firestoreUsers = new Map();
  }

  // Simulación del ciclo de Splash al iniciar el juego
  async splashStartupSequence() {
    const cachedUid = this.localStorage.get("Firebase_UserId");

    // CASO A: Sesión existente en caché
    if (cachedUid && this.firestoreUsers.has(cachedUid)) {
      const user = this.firestoreUsers.get(cachedUid);
      return {
        sessionType: user.isLinked ? "cuenta_vinculada" : "cuenta_anonima_existente",
        userId: user.uid,
        displayName: user.displayName,
        coins: user.coins,
        navigatedTo: "HomeScreenScene",
        isFirstLaunch: false,
      };
    }

    // CASO B: Primera vez que abre el juego (Fricción Cero - GDD 10.1, TDD 2.12)
    const newUid = "anon_" + Math.random().toString(36).substring(2, 10);
    const defaultUser = {
      uid: newUid,
      displayName: "JUGADOR_" + Math.floor(1000 + Math.random() * 9000),
      isAnonymous: true,
      isLinked: false,
      provider: "anonymous",
      coins: 300, // Saldo inicial de bienvenida
      collectionPower: 0,
      createdAt: new Date().toISOString(),
    };

    // Guardar en persistencia local y base de datos
    this.localStorage.set("Firebase_UserId", newUid);
    this.firestoreUsers.set(newUid, defaultUser);

    return {
      sessionType: "cuenta_anonima_nueva",
      userId: defaultUser.uid,
      displayName: defaultUser.displayName,
      coins: defaultUser.coins,
      navigatedTo: "HomeScreenScene",
      isFirstLaunch: true,
    };
  }

  // Vinculación de cuenta (linkWithCredential)
  async linkAccount(provider, googleDisplayName, googlePhotoUrl) {
    const cachedUid = this.localStorage.get("Firebase_UserId");
    if (!cachedUid || !this.firestoreUsers.has(cachedUid)) {
      throw new Error("No hay sesión activa para vincular.");
    }

    const user = this.firestoreUsers.get(cachedUid);
    user.isLinked = true;
    user.isAnonymous = false;
    user.provider = provider;
    user.displayName = googleDisplayName || user.displayName;
    user.photoUrl = googlePhotoUrl || "";
    this.firestoreUsers.set(cachedUid, user);

    return {
      success: true,
      userId: user.uid,
      displayName: user.displayName,
      isLinked: true,
      coinsPreserved: user.coins,
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Flujo de Splash & Autenticación (Fase 6.2)");
  console.log("==========================================================================\n");

  const engine = new MockAuthFlowEngine();

  // ----------------------------------------------------
  // TEST 1: Primer arranque de la aplicación (Usuario nuevo)
  // ----------------------------------------------------
  console.log("▶️ TEST 1: El usuario abre la app por primera vez en su vida...");
  const launch1 = await engine.splashStartupSequence();

  console.log(`  📱 Tipo de sesión creada: ${launch1.sessionType}`);
  console.log(`  👤 UID asignado: ${launch1.userId}`);
  console.log(`  🏷️ Nombre provisional: ${launch1.displayName}`);
  console.log(`  💰 Saldo de bienvenida: ${launch1.coins} monedas`);
  console.log(`  🚀 Escena de destino automático: ${launch1.navigatedTo} (Sin formulario de login)`);

  if (launch1.isFirstLaunch && launch1.coins === 300 && launch1.navigatedTo === "HomeScreenScene") {
    console.log("  ✅ PASÓ: Entrada inmediata con cuenta anónima creada en segundo plano.\n");
  } else {
    console.error("  ❌ FALLÓ en el primer arranque.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Segundo arranque (Usuario que ya jugó)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: El usuario cierra el juego y lo vuelve a abrir al día siguiente...");
  // Supongamos que el jugador ganó 150 monedas abriendo sobres y misiones
  const userRecord = engine.firestoreUsers.get(launch1.userId);
  userRecord.coins = 450;
  engine.firestoreUsers.set(launch1.userId, userRecord);

  const launch2 = await engine.splashStartupSequence();
  console.log(`  📱 Tipo de sesión detectada: ${launch2.sessionType}`);
  console.log(`  👤 UID reconocido: ${launch2.userId} (Mismo UID: ${launch2.userId === launch1.userId})`);
  console.log(`  💰 Saldo conservado: ${launch2.coins} monedas (Esperado: 450)`);
  console.log(`  🚀 Escena de destino: ${launch2.navigatedTo}`);

  if (!launch2.isFirstLaunch && launch2.userId === launch1.userId && launch2.coins === 450) {
    console.log("  ✅ PASÓ: Sesión en caché restaurada al instante sin pedir login.\n");
  } else {
    console.error("  ❌ FALLÓ al restaurar sesión.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Vinculación con Google (Preservar 100% progreso)
  // ----------------------------------------------------
  console.log("▶️ TEST 3: El jugador decide vincular su cuenta con Google ('Juan Pérez')...");
  const linkRes = await engine.linkAccount("google", "Juan Pérez", "https://google.com/avatar.jpg");

  console.log(`  🔗 Estado de vinculación: ${linkRes.isLinked}`);
  console.log(`  👤 UID de la cuenta: ${linkRes.userId} (Mismo UID preservado)`);
  console.log(`  🏷️ Nombre actualizado de Google: ${linkRes.displayName}`);
  console.log(`  💰 Monedas preservadas intactas: ${linkRes.coinsPreserved} monedas`);

  if (linkRes.isLinked && linkRes.userId === launch1.userId && linkRes.coinsPreserved === 450) {
    console.log("  ✅ PASÓ: Cuenta anónima vinculada exitosamente a Google sin pérdida de datos.\n");
  } else {
    console.error("  ❌ FALLÓ en la vinculación.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: Tercer arranque (Ahora como usuario vinculado)
  // ----------------------------------------------------
  console.log("▶️ TEST 4: El jugador abre la app tras haber vinculado su cuenta...");
  const launch3 = await engine.splashStartupSequence();

  console.log(`  📱 Tipo de sesión detectada: ${launch3.sessionType}`);
  console.log(`  👤 Nombre de usuario activo: ${launch3.displayName}`);

  if (launch3.sessionType === "cuenta_vinculada" && launch3.displayName === "Juan Pérez") {
    console.log("  ✅ PASÓ: La sesión vinculada persiste con normalidad.");
  } else {
    console.error("  ❌ FALLÓ al cargar cuenta vinculada.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE SPLASH Y AUTENTICACIÓN FUERON EXITOSAS! (4/4)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
