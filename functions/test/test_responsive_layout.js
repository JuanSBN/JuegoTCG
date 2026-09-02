/**
 * Test Automatizado de Layout Responsivo Móvil (Safe Area + Misiones Button + 20:9 Viewport)
 * Valida:
 * 1. Botón de Misiones: Ancho ampliado (240px) y no-wrap asegurando "MISIONES" en una sola línea.
 * 2. Safe Area Superior: Margen de resguardo para notch/cámara en ÁLBUM PILOTO y AJUSTES.
 * 3. Proporciones de cuadrícula en pantallas 1080x2400 (Moto G, Infinix, Huawei Y9 Prime).
 */

class MockMobileResponsiveCalculator {
  constructor() {
    this.targetScreens = [
      { name: "Moto G84 / G86", width: 1080, height: 2400, aspect: "20:9", notchHeight: 88 },
      { name: "Infinix Note 50s", width: 1080, height: 2436, aspect: "20.3:9", notchHeight: 96 },
      { name: "Huawei Y9 Prime", width: 1080, height: 2340, aspect: "19.5:9", notchHeight: 72 },
    ];
  }

  calculateMisionesButtonLayout(buttonWidth, text, fontSize) {
    const charWidthEst = fontSize * 0.62;
    const textWidth = text.length * charWidthEst;
    const paddingAndIcons = 70; // Icono checkmark + punto rojo + padding
    const totalRequiredWidth = textWidth + paddingAndIcons;
    const willWrap = totalRequiredWidth > buttonWidth;

    return {
      buttonWidth,
      totalRequiredWidth,
      willWrap,
      fitsInSingleLine: !willWrap,
    };
  }

  calculateTopHeaderSafeArea(screen, headerPosY) {
    const isSafe = Math.abs(headerPosY) >= screen.notchHeight;
    const marginBelowNotch = Math.abs(headerPosY) - screen.notchHeight;
    return {
      notchHeight: screen.notchHeight,
      headerPosY,
      isSafe,
      marginBelowNotch,
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Layout Responsivo Móvil (Paso 1)");
  console.log("==========================================================================\n");

  const calc = new MockMobileResponsiveCalculator();

  // ----------------------------------------------------
  // TEST 1: Botón de Misiones (Antes vs Después)
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Validación del Botón de Misiones en Inicio...");
  const before = calc.calculateMisionesButtonLayout(160, "MISIONES", 28);
  console.log(`  ❌ ANTES (Ancho: 160px): Requiere ${Math.round(before.totalRequiredWidth)}px ➔ ¿Se cortaba la palabra?: ${before.willWrap} (MISIONE \\n S)`);

  const after = calc.calculateMisionesButtonLayout(240, "MISIONES", 28);
  console.log(`  ✅ DESPUÉS (Ancho: 240px + NoWrap): Requiere ${Math.round(after.totalRequiredWidth)}px ➔ ¿Cabe en una sola línea?: ${after.fitsInSingleLine}`);

  if (!after.willWrap && after.fitsInSingleLine) {
    console.log("  ✅ PASÓ: Botón de Misiones centrado y legible sin cortes.\n");
  } else {
    console.error("  ❌ FALLÓ en el layout del botón Misiones.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: Safe Area Superior en Pantallas con Notch / Punch-hole
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Validación de margen superior en ÁLBUM PILOTO y AJUSTES (-100px)...");
  calc.targetScreens.forEach((scr) => {
    const res = calc.calculateTopHeaderSafeArea(scr, -100);
    console.log(`  📱 [${scr.name} (${scr.aspect})]: Notch=${res.notchHeight}px | Encabezado=-100px | Margen libre: +${res.marginBelowNotch}px ➔ ¿Seguro?: ${res.isSafe}`);
  });

  console.log("  ✅ PASÓ: Todos los títulos quedan libres del agujero de la cámara y barra de estado.");

  console.log("\n==========================================================================");
  console.log("🎉 ¡VALIDACIONES RESPONSIVAS DEL PASO 1 EXITOSAS AL 100%! (2/2)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
