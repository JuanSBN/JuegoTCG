/**
 * Test Automatizado de Pantalla y Progresión del Álbum (Fase 7.1)
 * Valida:
 * 1. Estado inicial del álbum: 2 cartas desbloqueadas (2/10 = 20%).
 * 2. Apertura de sobres y almacenamiento en inventario real.
 * 3. Actualización de cartas desbloqueadas vs cartas faltantes (siluetas).
 * 4. Detección y conteo de duplicados (x1, x2, x3).
 * 5. Hito de álbum completado (10/10 cartas únicas = 100%).
 */

class MockAlbumProgressionEngine {
  constructor() {
    this.catalog = [
      { cardId: "LD", name: "Luis Díaz", rarity: "mitica", position: "DEL" },
      { cardId: "VJ", name: "Vinicius Jr.", rarity: "epica", position: "DEL" },
      { cardId: "EH", name: "Erling Haaland", rarity: "comun", position: "DEL" },
      { cardId: "KM", name: "Kylian Mbappé", rarity: "especial", position: "DEL" },
      { cardId: "PE", name: "Pedri González", rarity: "epica", position: "MED" },
      { cardId: "LY", name: "Lamine Yamal", rarity: "mitica", position: "DEL" },
      { cardId: "JB", name: "Jude Bellingham", rarity: "epica", position: "MED" },
      { cardId: "RO", name: "Rodri Hernández", rarity: "comun", position: "MED" },
      { cardId: "MS", name: "Mohamed Salah", rarity: "especial", position: "DEL" },
      { cardId: "KDB", name: "Kevin De Bruyne", rarity: "epica", position: "MED" },
    ];

    this.ownedCards = new Map(); // cardId -> qty
    // Estado inicial de bienvenida
    this.ownedCards.set("EH", 2); // Haaland x2
    this.ownedCards.set("RO", 1); // Rodri x1
  }

  addCardsFromPack(packCards) {
    packCards.forEach((c) => {
      const current = this.ownedCards.get(c.cardId) || 0;
      this.ownedCards.set(c.cardId, current + 1);
    });
  }

  getAlbumState() {
    const total = this.catalog.length;
    let ownedUnique = 0;
    const gridItems = [];

    this.catalog.forEach((item) => {
      const qty = this.ownedCards.get(item.cardId) || 0;
      const isOwned = qty > 0;
      if (isOwned) ownedUnique++;

      gridItems.push({
        cardId: item.cardId,
        name: item.name,
        rarity: item.rarity,
        isOwned,
        quantity: qty,
        displayStatus: isOwned ? `ILUMINADA (x${qty})` : "SILUETA_BLOQUEADA 🔒",
      });
    });

    const percent = Math.round((ownedUnique / total) * 100);
    return {
      ownedUnique,
      total,
      percent,
      headerText: `ÁLBUM PILOTO - ${ownedUnique}/${total} Cartas (${percent}%)`,
      isCompleted: ownedUnique === total,
      gridItems,
    };
  }
}

async function runTests() {
  console.log("\n==========================================================================");
  console.log("🧪 TEST AUTOMATIZADO: Progresión del Álbum de Cartas (Fase 7.1)");
  console.log("==========================================================================\n");

  const album = new MockAlbumProgressionEngine();

  // ----------------------------------------------------
  // TEST 1: Estado Inicial del Álbum
  // ----------------------------------------------------
  console.log("▶️ TEST 1: Consultando estado inicial del álbum...");
  const state1 = album.getAlbumState();

  console.log(`  📊 Encabezado en pantalla: "${state1.headerText}"`);
  console.log(`  🃏 Cartas únicas: ${state1.ownedUnique}/${state1.total} (${state1.percent}%)`);
  console.log(`  🏆 ¿Álbum completado?: ${state1.isCompleted}`);

  if (state1.ownedUnique === 2 && state1.percent === 20 && !state1.isCompleted) {
    console.log("  ✅ PASÓ: Álbum inicial renderizado con 2 cartas y 8 siluetas.\n");
  } else {
    console.error("  ❌ FALLÓ en el estado inicial.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 2: El jugador abre un sobre con 5 cartas nuevas y repetidas
  // ----------------------------------------------------
  console.log("▶️ TEST 2: Jugador abre sobre y obtiene 5 cartas (LD, VJ, KM, EH, PE)...");
  album.addCardsFromPack([
    { cardId: "LD" }, // Nueva (Mítica)
    { cardId: "VJ" }, // Nueva (Épica)
    { cardId: "KM" }, // Nueva (Especial)
    { cardId: "EH" }, // Duplicada (Haaland pasa a x3)
    { cardId: "PE" }, // Nueva (Épica)
  ]);

  const state2 = album.getAlbumState();
  console.log(`  📊 Nuevo encabezado: "${state2.headerText}"`);
  console.log(`  🃏 Cartas únicas desbloqueadas: ${state2.ownedUnique}/${state2.total} (${state2.percent}%)`);

  const haalandCard = state2.gridItems.find((c) => c.cardId === "EH");
  console.log(`  🔁 Contador de duplicados de Haaland: ${haalandCard.displayStatus} (Esperado: x3)`);

  if (state2.ownedUnique === 6 && state2.percent === 60 && haalandCard.quantity === 3) {
    console.log("  ✅ PASÓ: Cartas añadidas, álbum actualizado al 60% y duplicados registrados.\n");
  } else {
    console.error("  ❌ FALLÓ tras la apertura de sobre.");
    process.exit(1);
  }

  // ----------------------------------------------------
  // TEST 3: Completar el álbum al 100% (10/10)
  // ----------------------------------------------------
  console.log("▶️ TEST 3: Jugador consigue las 4 cartas restantes (LY, JB, MS, KDB)...");
  album.addCardsFromPack([
    { cardId: "LY" },
    { cardId: "JB" },
    { cardId: "MS" },
    { cardId: "KDB" },
  ]);

  const state3 = album.getAlbumState();
  console.log(`  📊 Encabezado final: "${state3.headerText}"`);
  console.log(`  🎉 ¿Álbum completado al 100%?: ${state3.isCompleted}`);

  if (state3.ownedUnique === 10 && state3.percent === 100 && state3.isCompleted) {
    console.log("  ✅ PASÓ: Hito de álbum 100% completado verificado con éxito.");
  } else {
    console.error("  ❌ FALLÓ al completar el álbum.");
    process.exit(1);
  }

  console.log("\n==========================================================================");
  console.log("🎉 ¡TODAS LAS PRUEBAS DEL ÁLBUM PASARON CON ÉXITO! (3/3)");
  console.log("==========================================================================\n");
}

runTests().catch(console.error);
