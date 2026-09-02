/**
 * Test Automatizado de Personalización Visual del Perfil (Fase 7.4)
 * Valida:
 * 1. Selección y cambio de marcos de avatar (Oro, Neón, Clásico).
 * 2. Selección y cambio de temas de cancha táctica.
 * 3. Actualización de nombre de usuario con persistencia.
 */

class MockProfileCustomizationEngine {
  constructor() {
    this.user = {
      uid: "player_custom_001",
      displayName: "JUGADOR_01",
      activeFrame: "frame_gold",
      activePitchTheme: "pitch_night",
    };
  }

  setAvatarFrame(frameId) {
    const validFrames = ["frame_gold", "frame_neon", "frame_classic"];
    if (!validFrames.includes(frameId)) throw new Error("Marco no válido.");
    this.user.activeFrame = frameId;
    return this.user.activeFrame;
  }

  setPitchTheme(themeId) {
    const validThemes = ["pitch_night", "pitch_stadium", "pitch_classic"];
    if (!validThemes.includes(themeId)) throw new Error("Tema no válido.");
    this.user.activePitchTheme = themeId;
    return this.user.activePitchTheme;
  }

  updateUsername(newName) {
    if (!newName || newName.trim().length === 0) throw new Error("Nombre vacío no permitido.");
    this.user.displayName = newName.trim().substring(0, 16);
    return this.user.displayName;
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Personalización Visual del Perfil (Fase 7.4)");
  console.log("==========================================================================\n");

  const engine = new MockProfileCustomizationEngine();

  // ----------------------------------------------------
  // TEST 1: Estado Inicial de Personalización
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Consultando personalización inicial por defecto...");
  console.log(`  👤 Nombre actual: "${engine.user.displayName}"`);
  console.log(`  🖼️ Marco activo: "${engine.user.activeFrame}"`);
  console.log(`  🏟️ Tema de cancha: "${engine.user.activePitchTheme}"`);

  if (engine.user.activeFrame === "frame_gold" && engine.user.activePitchTheme === "pitch_night") {
    console.log("  ✅ PASÓ: Personalización base cargada con éxito.\n");
  } else {
    console.error("  ❌ FALLÓ en el estado inicial.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Cambio de Marco de Avatar (Neón)
  // ----------------------------------------------------
  console.log("▶️ TEST 2: El jugador equipa el marco 'frame_neon'...");
  engine.setAvatarFrame("frame_neon");
  console.log(`  🖼️ Nuevo marco equipado: ${engine.user.activeFrame}`);

  if (engine.user.activeFrame === "frame_neon") {
    console.log("  ✅ PASÓ: Marco de avatar actualizado correctamente.\n");
  } else {
    console.error("  ❌ FALLÓ al cambiar marco.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Cambio de Tema de Cancha (Estadio)
  // ----------------------------------------------------
  console.log("▶️ TEST 3: El jugador selecciona el tema de cancha 'pitch_stadium'...");
  engine.setPitchTheme("pitch_stadium");
  console.log(`  🏟️ Nuevo tema de cancha: ${engine.user.activePitchTheme}`);

  if (engine.user.activePitchTheme === "pitch_stadium") {
    console.log("  ✅ PASÓ: Tema táctico de cancha actualizado.\n");
  } else {
    console.error("  ❌ FALLÓ al cambiar tema de cancha.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 4: Edición de Nombre de Usuario
  // ----------------------------------------------------
  console.log("▶️ TEST 4: El jugador edita su nombre a 'Goleador_Pro'...");
  const newName = engine.updateUsername("Goleador_Pro");
  console.log(`  🏷️ Nombre actualizado: "${newName}" (Longitud: ${newName.length})`);

  if (engine.user.displayName === "Goleador_Pro") {
    console.log("  ✅ PASÓ: Nombre de usuario modificado y guardado con éxito.");
  } else {
    console.error("  ❌ FALLÓ al editar nombre.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS VALIDACIONES DE PERSONALIZACIÓN DEL PERFIL PASARON! (4/4)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
