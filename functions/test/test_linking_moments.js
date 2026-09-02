/**
 * Test Automatizado de Momentos de Vinculación de Cuenta (Fase 6 - Puntos 3 y 4)
 * Valida:
 * 1. Momento 1 (GDD 10.1): Compra de monedas con dinero real interceptada para usuario anónimo, requiriendo vinculación.
 * 2. Compra exitosa tras vincular con Google.
 * 3. Momento 2 (GDD 10.1): Recordatorio suave al alcanzar el 100% de cartas en el álbum piloto.
 * 4. Ajustes (TDD 2.12): Bloqueo de cierre de sesión en cuentas anónimas para evitar pérdida irrecuperable de datos.
 */

class MockGameLinkingFlow {
  constructor() {
    this.user = {
      uid: "anon_test_987",
      displayName: "JUGADOR_1024",
      isAnonymous: true,
      isLinked: false,
      provider: "anonymous",
      coins: 200,
      albumProgress: { current: 0, total: 10 },
    };
  }

  // Momento 1: Intento de compra con dinero real en la Tienda
  attemptRealMoneyPurchase(productId, priceTag) {
    if (this.user.isAnonymous && !this.user.isLinked) {
      return {
        allowed: false,
        reason: "CUENTA_NO_VINCULADA",
        action: "REDIRECCION_A_LOGIN_VINCULAR",
        message: `Para proteger tu compra real (${priceTag}), es necesario vincular tu cuenta con Google o Email.`,
      };
    }

    // Si ya está vinculado, se procesa
    this.user.coins += 500;
    return {
      allowed: true,
      coinsAdded: 500,
      newCoinsTotal: this.user.coins,
    };
  }

  // Vinculación con Google (linkWithCredential)
  linkWithGoogle(googleName) {
    this.user.isLinked = true;
    this.user.isAnonymous = false;
    this.user.provider = "google";
    this.user.displayName = googleName;
    return {
      success: true,
      uid: this.user.uid,
      displayName: this.user.displayName,
      isLinked: true,
    };
  }

  // Momento 2: Evaluación de progreso del álbum
  updateAlbumProgress(current, total) {
    this.user.albumProgress = { current, total };
    const isCompleted = current >= total && total > 0;

    let shouldShowReminder = false;
    if (isCompleted && this.user.isAnonymous && !this.user.isLinked) {
      shouldShowReminder = true;
    }

    return {
      current,
      total,
      isCompleted,
      shouldShowReminder,
      reminderMessage: shouldShowReminder
        ? "¡Álbum completado! Recordatorio: Vincula tu cuenta con Google para asegurar tu colección permanente."
        : null,
    };
  }

  // Ajustes: Intento de cerrar sesión
  attemptLogout() {
    if (this.user.isAnonymous && !this.user.isLinked) {
      return {
        canLogout: false,
        action: "MOSTRAR_ADVERTENCIA_Y_VINCULAR",
        message: "Cuenta anónima: no se puede cerrar sesión sin vincular antes para evitar pérdida de progreso.",
      };
    }

    this.user = null;
    return {
      canLogout: true,
      action: "CERRAR_SESION_LIMPIA",
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Momentos de Vinculación de Cuenta (GDD 10.1 / TDD 2.12)");
  console.log("==========================================================================\n");

  const game = new MockGameLinkingFlow();

  // ----------------------------------------------------
  // TEST 1: Momento 1 - Compra real con cuenta anónima
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Usuario anónimo intenta comprar paquete de monedas de $0.99 en la Tienda...");
  const purchaseAttempt1 = game.attemptRealMoneyPurchase("coins_tier_1", "$0.99");

  console.log(`  🛡️ ¿Compra autorizada directamente?: ${purchaseAttempt1.allowed}`);
  console.log(`  🛑 Razón del bloqueo: ${purchaseAttempt1.reason}`);
  console.log(`  📲 Acción ejecutada: ${purchaseAttempt1.action}`);
  console.log(`  💬 Mensaje al jugador: "${purchaseAttempt1.message}"`);

  if (!purchaseAttempt1.allowed && purchaseAttempt1.action === "REDIRECCION_A_LOGIN_VINCULAR") {
    console.log("  ✅ PASÓ: Momento 1 interceptó correctamente la compra para proteger el dinero del usuario.\n");
  } else {
    console.error("  ❌ FALLÓ en el Momento 1.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Vinculación con Google y reintento de compra
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Usuario completa la vinculación con Google y reintenta la compra...");
  const linkRes = game.linkWithGoogle("Juan Pérez");
  console.log(`  🔗 Estado de cuenta: Vinculada (${linkRes.provider || "google"})`);

  const purchaseAttempt2 = game.attemptRealMoneyPurchase("coins_tier_1", "$0.99");
  console.log(`  💰 ¿Compra autorizada?: ${purchaseAttempt2.allowed}`);
  console.log(`  🪙 Saldo total tras la compra: ${purchaseAttempt2.newCoinsTotal} monedas (Esperado: 700)`);

  if (purchaseAttempt2.allowed && purchaseAttempt2.newCoinsTotal === 700) {
    console.log("  ✅ PASÓ: Compra procesada con éxito una vez vinculada la cuenta.\n");
  } else {
    console.error("  ❌ FALLÓ en la compra post-vinculación.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Momento 2 - Álbum completado (Simulación con usuario anónimo nuevo)
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Usuario anónimo nuevo avanza en su álbum piloto...");
  const gameAnon = new MockGameLinkingFlow();

  // Caso 3a: 8/10 cartas (aún incompleto)
  const p1 = gameAnon.updateAlbumProgress(8, 10);
  console.log(`  📖 Progreso 8/10: ¿Recordatorio disparado?: ${p1.shouldShowReminder}`);

  // Caso 3b: 10/10 cartas (álbum completado al 100%)
  const p2 = gameAnon.updateAlbumProgress(10, 10);
  console.log(`  🏆 Progreso 10/10 (100%): ¿Recordatorio disparado?: ${p2.shouldShowReminder}`);
  console.log(`  💬 Mensaje de aviso: "${p2.reminderMessage}"`);

  if (!p1.shouldShowReminder && p2.shouldShowReminder && p2.isCompleted) {
    console.log("  ✅ PASÓ: Momento 2 activó el recordatorio suave exactamente al completar el álbum.\n");
  } else {
    console.error("  ❌ FALLÓ en el Momento 2.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: Ajustes - Seguridad al cerrar sesión (TDD 2.12)
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Usuario anónimo intenta cerrar sesión desde Ajustes...");
  const logoutAnon = gameAnon.attemptLogout();
  console.log(`  🚪 ¿Cierre de sesión permitido a ciegas?: ${logoutAnon.canLogout}`);
  console.log(`  🛡️ Acción preventiva: ${logoutAnon.action}`);

  if (!logoutAnon.canLogout && logoutAnon.action === "MOSTRAR_ADVERTENCIA_Y_VINCULAR") {
    console.log("  ✅ PASÓ: Cierre de sesión protegido para evitar pérdida de cuentas anónimas.\n");
  } else {
    console.error("  ❌ FALLÓ en la protección de Ajustes.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODOS LOS TESTS DE MOMENTOS DE VINCULACIÓN PASARON CON ÉXITO! (4/4)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
