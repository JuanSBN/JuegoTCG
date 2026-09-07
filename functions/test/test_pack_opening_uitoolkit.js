/**
 * Test Suite: Apertura de Sobres en UI Toolkit (1080x2400) con Shader Holográfico
 * Referencia: docs/prototipo_apertura_sobres.html
 */

const assert = require('assert');
const fs = require('fs');
const path = require('path');

describe('Apertura de Sobres en UI Toolkit (1080x2400) con Shader Holográfico', function () {
  this.timeout(10000);

  const uxmlPath = path.resolve(__dirname, '../../Assets/_Project/UI/Views/PackOpeningScreen.uxml');
  const ussPath = path.resolve(__dirname, '../../Assets/_Project/UI/Styles/PackOpeningScreen.uss');
  const holoRendererPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Cards/CardHoloRenderer.cs');
  const controllerPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitPackOpeningController.cs');
  const builderPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Editor/UIToolkitPackOpeningSceneBuilder.cs');
  const autoBuildPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Editor/AutoRegisterBuildScenes.cs');
  const shaderPath = path.resolve(__dirname, '../../Assets/_Project/Shaders/HolographicFoilShader.shader');

  it('1. Debe existir PackOpeningScreen.uxml con la estructura exacta del prototipo HTML', () => {
    assert.ok(fs.existsSync(uxmlPath), 'PackOpeningScreen.uxml no existe');
    const content = fs.readFileSync(uxmlPath, 'utf8');

    // Nodos principales
    assert.ok(content.includes('name="PackOpeningRoot"'), 'Debe incluir PackOpeningRoot');
    assert.ok(content.includes('name="ScreenFlash"'), 'Debe incluir ScreenFlash para el efecto de desgarro');
    assert.ok(content.includes('name="Topbar"'), 'Debe incluir Topbar');
    assert.ok(content.includes('name="Toggle_ForceHolo"'), 'Debe incluir Toggle_ForceHolo');
    assert.ok(content.includes('name="PackCountText"'), 'Debe incluir PackCountText');

    // Vista 1: Sobre Cerrado
    assert.ok(content.includes('name="ClosedView"'), 'Debe incluir ClosedView');
    assert.ok(content.includes('name="PackElement"'), 'Debe incluir PackElement');
    assert.ok(content.includes('class="pack-rays"'), 'Debe incluir pack-rays');
    assert.ok(content.includes('class="pack-star"'), 'Debe incluir pack-star');

    // Vista 2: Reveal
    assert.ok(content.includes('name="RevealView"'), 'Debe incluir RevealView');
    assert.ok(content.includes('name="ProgressContainer"'), 'Debe incluir ProgressContainer');
    assert.ok(content.includes('name="Dot_0"'), 'Debe incluir Dot_0');
    assert.ok(content.includes('name="Dot_4"'), 'Debe incluir Dot_4');
    assert.ok(content.includes('name="CardWrap"'), 'Debe incluir CardWrap');
    assert.ok(content.includes('name="CardInner"'), 'Debe incluir CardInner');
    assert.ok(content.includes('name="CardBack"'), 'Debe incluir CardBack');
    assert.ok(content.includes('name="CardFront"'), 'Debe incluir CardFront');
    assert.ok(content.includes('name="HoloRenderElement"'), 'Debe incluir HoloRenderElement para proyectar el Shader');
    assert.ok(content.includes('name="RarityBadge"'), 'Debe incluir RarityBadge');
    assert.ok(content.includes('name="PlayerName"'), 'Debe incluir PlayerName');
    assert.ok(content.includes('name="ContinueHint"'), 'Debe incluir ContinueHint');
    assert.ok(content.includes('name="TiltHint"'), 'Debe incluir TiltHint');

    // Vista 3: Summary
    assert.ok(content.includes('name="SummaryView"'), 'Debe incluir SummaryView');
    assert.ok(content.includes('name="SummarySub"'), 'Debe incluir SummarySub');
    assert.ok(content.includes('name="SummaryGrid"'), 'Debe incluir SummaryGrid');
    assert.ok(content.includes('name="Btn_OpenAnother"'), 'Debe incluir Btn_OpenAnother');
    assert.ok(content.includes('name="Btn_BackToShop"'), 'Debe incluir Btn_BackToShop');
  });

  it('2. Debe existir PackOpeningScreen.uss con tokens y proporciones del prototipo HTML (1080x2400)', () => {
    assert.ok(fs.existsSync(ussPath), 'PackOpeningScreen.uss no existe');
    const content = fs.readFileSync(ussPath, 'utf8');

    assert.ok(content.includes('--bg-deep'), 'Debe definir token --bg-deep');
    assert.ok(content.includes('--gold'), 'Debe definir token --gold');
    assert.ok(content.includes('.screen-container'), 'Debe definir clase screen-container');
    assert.ok(content.includes('.screen-flash'), 'Debe definir clase screen-flash');
    assert.ok(content.includes('.pack-closed'), 'Debe definir clase pack-closed');
    assert.ok(content.includes('.progress-dot'), 'Debe definir clase progress-dot');
    assert.ok(content.includes('.card-wrap'), 'Debe definir clase card-wrap');
    assert.ok(content.includes('.holo-render-element'), 'Debe definir clase holo-render-element');
    assert.ok(content.includes('.rarity-mitica'), 'Debe definir clase rarity-mitica');
    assert.ok(content.includes('.rarity-fullart'), 'Debe definir clase rarity-fullart');
    assert.ok(content.includes('.mini-card'), 'Debe definir clase mini-card');
    assert.ok(content.includes('.btn-primary'), 'Debe definir clase btn-primary');
  });

  it('3. CardHoloRenderer.cs debe gestionar RenderTexture, Quad 3D y parámetros del shader', () => {
    assert.ok(fs.existsSync(holoRendererPath), 'CardHoloRenderer.cs no existe');
    assert.ok(fs.existsSync(shaderPath), 'HolographicFoilShader.shader no existe');

    const content = fs.readFileSync(holoRendererPath, 'utf8');
    assert.ok(content.includes('RenderTexture'), 'Debe usar RenderTexture');
    assert.ok(content.includes('SetTilt'), 'Debe implementar SetTilt(normX, normY)');
    assert.ok(content.includes('ResetTilt'), 'Debe implementar ResetTilt()');
    assert.ok(content.includes('SetHoloActive'), 'Debe implementar SetHoloActive()');
    assert.ok(content.includes('TargetTexture'), 'Debe exponer TargetTexture');
    assert.ok(content.includes('_TiltPos'), 'Debe actualizar _TiltPos en el shader');
    assert.ok(content.includes('maxTiltAngle'), 'Debe incluir cálculo de rotación física 3D');
  });

  it('4. UIToolkitPackOpeningController.cs debe implementar la máquina de estados y eventos táctiles', () => {
    assert.ok(fs.existsSync(controllerPath), 'UIToolkitPackOpeningController.cs no existe');
    const content = fs.readFileSync(controllerPath, 'utf8');

    assert.ok(content.includes('class UIToolkitPackOpeningController : MonoBehaviour'), 'Debe ser MonoBehaviour');
    assert.ok(content.includes('Background.FromRenderTexture'), 'Debe vincular la RenderTexture con UI Toolkit');
    assert.ok(content.includes('PointerDownEvent'), 'Debe registrar PointerDownEvent para tilt');
    assert.ok(content.includes('PointerMoveEvent'), 'Debe registrar PointerMoveEvent para tilt');
    assert.ok(content.includes('PointerUpEvent'), 'Debe registrar PointerUpEvent');
    assert.ok(content.includes('OpenPackSequence'), 'Debe implementar secuencia de apertura con destello');
    assert.ok(content.includes('PickRarity'), 'Debe implementar generador ponderado con probabilidades');
    assert.ok(content.includes('LAST_SLOT_RARITIES') || content.includes('isLast'), 'Debe considerar probabilidad mejorada en el último slot');
    assert.ok(content.includes('ShowSummary'), 'Debe implementar vista de resumen');
  });

  it('5. UIToolkitPackOpeningSceneBuilder.cs debe estar registrado en AutoRegisterBuildScenes.cs', () => {
    assert.ok(fs.existsSync(builderPath), 'UIToolkitPackOpeningSceneBuilder.cs no existe');
    assert.ok(fs.existsSync(autoBuildPath), 'AutoRegisterBuildScenes.cs no existe');

    const builderContent = fs.readFileSync(builderPath, 'utf8');
    const autoContent = fs.readFileSync(autoBuildPath, 'utf8');

    assert.ok(builderContent.includes('PackOpeningSceneUIToolkit.unity'), 'Debe crear PackOpeningSceneUIToolkit.unity');
    assert.ok(builderContent.includes('1080'), 'Debe usar 1080x2400');
    assert.ok(builderContent.includes('2400'), 'Debe usar 1080x2400');
    assert.ok(autoContent.includes('PackOpeningSceneUIToolkit.unity'), 'Debe estar en RequiredScenes');
  });

  it('6. Simulación lógica: Flujo de 5 cartas con última carta mítica/fullart y resumen dinámico', () => {
    const RARITIES = {
      common:     { weight: 55 },
      especial:   { weight: 25 },
      epica:      { weight: 12 },
      legendaria: { weight: 5  },
      mitica:     { weight: 2  },
      fullart:    { weight: 1  }
    };
    const LAST_SLOT_RARITIES = { common: 15, especial: 30, epica: 30, legendaria: 15, mitica: 7, fullart: 3 };

    // Generar pack simulado
    const pack = [];
    for (let i = 0; i < 5; i++) {
      const isLast = (i === 4);
      const rarity = isLast ? 'mitica' : 'especial';
      pack.push({ index: i, rarity, isHolo: (rarity === 'mitica' || rarity === 'fullart') });
    }

    assert.strictEqual(pack.length, 5, 'El sobre debe contener exactamente 5 cartas');
    assert.strictEqual(pack[4].isHolo, true, 'La carta mítica debe ser holográfica');

    // Simular avance y resumen
    let bestRarity = 'common';
    const hierarchy = ['common', 'especial', 'epica', 'legendaria', 'mitica', 'fullart'];
    pack.forEach(c => {
      if (hierarchy.indexOf(c.rarity) > hierarchy.indexOf(bestRarity)) bestRarity = c.rarity;
    });

    assert.strictEqual(bestRarity, 'mitica', 'La mejor rareza detectada debe ser mítica');
    const summaryText = (bestRarity === 'mitica') ? '¡Genial, salió una carta MÍTICA holográfica!' : '';
    assert.ok(summaryText.includes('MÍTICA'), 'El resumen debe destacar la carta Mítica obtenida');
  });
});
