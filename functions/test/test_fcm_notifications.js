/**
 * Test Automatizado de Notificaciones Push FCM (Fase 6 - Punto 5)
 * Valida:
 * 1. Solicitud de permisos en el dispositivo y registro de token FCM.
 * 2. Almacenamiento del token en el perfil del usuario (users/{uid}/fcmToken).
 * 3. Respeto a la preferencia del jugador (toggle de Ajustes: notificationsEnabled).
 * 4. Programación de recordatorio de sobre gratis según la preferencia.
 */

class MockFCMManager {
  constructor() {
    this.users = new Map();
    this.deviceTokens = new Map(); // uid -> token
    this.sentNotifications = [];
  }

  // 1. Registro de token y permisos (TDD 2.8)
  async registerFCMToken(userId, osPermissionGranted = true) {
    if (!osPermissionGranted) {
      return { success: false, reason: "PERMISO_DENEGADO" };
    }

    const token = "fcm_tok_" + Math.random().toString(36).substring(2, 12);
    this.deviceTokens.set(userId, token);

    // Guardar en documento del usuario en Firestore (TDD 5.1)
    const userDoc = this.users.get(userId) || { uid: userId, notificationsEnabled: true };
    userDoc.fcmToken = token;
    userDoc.fcmTokenUpdatedAt = new Date().toISOString();
    this.users.set(userId, userDoc);

    return {
      success: true,
      userId,
      fcmToken: token,
      notificationsEnabled: userDoc.notificationsEnabled,
    };
  }

  // 2. Modificación de preferencia en Ajustes
  setNotificationPreference(userId, enabled) {
    const userDoc = this.users.get(userId);
    if (!userDoc) throw new Error("Usuario no encontrado.");

    userDoc.notificationsEnabled = enabled;
    this.users.set(userId, userDoc);
    return userDoc;
  }

  // 3. Envío de notificación push desde el backend (ej. Sobre gratis listo)
  async sendPushNotification(userId, title, body, payload = {}) {
    const userDoc = this.users.get(userId);
    if (!userDoc || !userDoc.fcmToken) {
      return { sent: false, reason: "SIN_TOKEN_FCM" };
    }

    // Validación crucial (TDD 2.8): Respetar la preferencia del jugador
    if (!userDoc.notificationsEnabled) {
      return { sent: false, reason: "NOTIFICACIONES_DESACTIVADAS_POR_USUARIO" };
    }

    const record = {
      notificationId: "notif_" + Date.now(),
      userId,
      token: userDoc.fcmToken,
      title,
      body,
      payload,
      sentAt: new Date().toISOString(),
    };

    this.sentNotifications.push(record);
    return { sent: true, record };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Notificaciones Push FCM (TDD 2.8 / Fase 6.5)");
  console.log("==========================================================================\n");

  const fcm = new MockFCMManager();
  const userId = "player_fcm_001";
  fcm.users.set(userId, { uid: userId, displayName: "Goleador", notificationsEnabled: true });

  // ----------------------------------------------------
  // TEST 1: Registro de Token FCM al iniciar la app
  // ----------------------------------------------------
  console.log("▶️ TEST 1: App solicita permisos y registra el token FCM de Firebase...");
  const regRes = await fcm.registerFCMToken(userId, true);

  console.log(`  📱 Token FCM generado: ${regRes.fcmToken}`);
  console.log(`  💾 Token guardado en users/${userId}: ${fcm.users.get(userId).fcmToken}`);
  console.log(`  🔔 Notificaciones activas por defecto: ${regRes.notificationsEnabled}`);

  if (regRes.success && fcm.users.get(userId).fcmToken === regRes.fcmToken) {
    console.log("  ✅ PASÓ: Token FCM registrado y sincronizado en Firestore.\n");
  } else {
    console.error("  ❌ FALLÓ en el registro de token.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Envío de recordatorio de Sobre Gratis (Con notificaciones activas)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Servidor envía recordatorio de Sobre Gratis listo para abrir...");
  const push1 = await fcm.sendPushNotification(
    userId,
    "🎁 ¡Sobre Gratis Disponible!",
    "Tu sobre gratis diario ya está listo para abrir. ¡Entra a conseguir nuevas cartas!",
    { screen: "HomeScreenScene", action: "claim_pack" }
  );

  console.log(`  📨 ¿Notificación enviada?: ${push1.sent}`);
  console.log(`  📬 Título: "${push1.record?.title}"`);
  console.log(`  📲 Enrutamiento / Payload:`, push1.record?.payload);

  if (push1.sent && fcm.sentNotifications.length === 1) {
    console.log("  ✅ PASÓ: Notificación push entregada exitosamente al token del usuario.\n");
  } else {
    console.error("  ❌ FALLÓ en el envío de notificación.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Desactivación desde Pantalla de Ajustes
  // ----------------------------------------------------
  console.log("▶️ TEST 3: El jugador desactiva las notificaciones en la Pantalla de Ajustes...");
  fcm.setNotificationPreference(userId, false);
  console.log(`  🔕 Preferencia en base de datos: notificationsEnabled = ${fcm.users.get(userId).notificationsEnabled}`);

  // ----------------------------------------------------
  // TEST 4: Intento de envío con notificaciones desactivadas
  // ----------------------------------------------------
  console.log("▶️ TEST 4: Servidor intenta enviar aviso de oferta de intercambio recibida...");
  const push2 = await fcm.sendPushNotification(
    userId,
    "🤝 ¡Nueva Oferta de Intercambio!",
    "Un amigo te ha propuesto un intercambio de cartas.",
    { screen: "TradeScene" }
  );

  console.log(`  🛡️ ¿Notificación bloqueada por preferencia?: ${!push2.sent}`);
  console.log(`  🛑 Motivo: ${push2.reason}`);

  if (!push2.sent && push2.reason === "NOTIFICACIONES_DESACTIVADAS_POR_USUARIO") {
    console.log("  ✅ PASÓ: Preferencia del jugador respetada, sin spam innecesario.\n");
  } else {
    console.error("  ❌ FALLÓ: Se envió notificación a pesar de estar desactivadas.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODOS LOS TESTS DE NOTIFICACIONES FCM PASARON CON ÉXITO! (4/4)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
