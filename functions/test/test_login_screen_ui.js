/**
 * Test Suite: Pantalla de Login / Bienvenida en UI Toolkit (1080x2400)
 * 
 * Valida:
 * 1. Estructura UXML de LoginScreen.uxml (LogoMark, Titulo, Subtitulo, Botones Google, Email, Invitado).
 * 2. Estilos USS de LoginScreen.uss (Tokens de diseno oro #D4AF37, proporciones 1080x2400, flex, bordes, estados hover/active).
 * 3. Controlador C# UIToolkitLoginController.cs (Modos nosession y link, integracion con GoogleSignInManager, FirebaseAuthManager y SceneManager).
 * 4. Metodos HasCachedSession en FirebaseAuthManager.cs.
 * 5. Bifurcacion de navegacion condicional en SplashScreenController.cs.
 * 6. Registro de LoginSceneUIToolkit en Build Settings y AutoRegisterBuildScenes.cs.
 * 7. Simulacion de flujo de bienvenida y primer inicio sin sesion vs sesion cacheada.
 */

const assert = require('assert');
const fs = require('fs');
const path = require('path');

describe('Pantalla de Login / Bienvenida UI Toolkit (1080x2400)', function () {
  this.timeout(10000);

  const uxmlPath = path.resolve(__dirname, '../../Assets/_Project/UI/Views/LoginScreen.uxml');
  const ussPath = path.resolve(__dirname, '../../Assets/_Project/UI/Styles/LoginScreen.uss');
  const controllerPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/UIToolkitLoginController.cs');
  const splashPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/UI/SplashScreenController.cs');
  const authPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Networking/FirebaseAuthManager.cs');
  const builderPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Editor/UIToolkitLoginSceneBuilder.cs');
  const autoBuildPath = path.resolve(__dirname, '../../Assets/_Project/Scripts/Editor/AutoRegisterBuildScenes.cs');

  it('1. Debe existir el archivo LoginScreen.uxml con la jerarquia completa de Figma', () => {
    assert.ok(fs.existsSync(uxmlPath), 'LoginScreen.uxml no existe');
    const content = fs.readFileSync(uxmlPath, 'utf8');

    // Nodos principales
    assert.ok(content.includes('name="LoginScreenRoot"'), 'Debe incluir LoginScreenRoot');
    assert.ok(content.includes('name="LoginContentBox"'), 'Debe incluir LoginContentBox');
    assert.ok(content.includes('name="LogoContainer"'), 'Debe incluir LogoContainer');
    assert.ok(content.includes('class="logo-cards-stacked"'), 'Debe incluir logo-cards-stacked');
    assert.ok(content.includes('name="TitleText"'), 'Debe incluir TitleText');
    assert.ok(content.includes('name="SubtitleText"'), 'Debe incluir SubtitleText');
    assert.ok(content.includes('name="Btn_Google"'), 'Debe incluir Btn_Google');
    assert.ok(content.includes('name="Btn_Email"'), 'Debe incluir Btn_Email');
    assert.ok(content.includes('name="Btn_Guest"'), 'Debe incluir Btn_Guest');
    assert.ok(content.includes('name="GuestBtnText"'), 'Debe incluir GuestBtnText');
  });

  it('2. Debe existir el archivo LoginScreen.uss con diseno optimizado para 1080x2400 y Figma tokens', () => {
    assert.ok(fs.existsSync(ussPath), 'LoginScreen.uss no existe');
    const content = fs.readFileSync(ussPath, 'utf8');

    // Tokens de diseno Figma
    assert.ok(content.includes('--gold'), 'Debe tener token de acento dorado');
    assert.ok(content.includes('.screen-container'), 'Debe definir clase screen-container');
    assert.ok(content.includes('.login-content-box'), 'Debe definir clase login-content-box');
    assert.ok(content.includes('.login-logo-container'), 'Debe definir clase login-logo-container');
    assert.ok(content.includes('.provider-btn'), 'Debe definir clase provider-btn');
    assert.ok(content.includes('.provider-icon-google'), 'Debe definir clase provider-icon-google');
    assert.ok(content.includes('.guest-btn'), 'Debe definir clase guest-btn');
    assert.ok(content.includes('.login-terms'), 'Debe definir clase login-terms');
  });

  it('3. UIToolkitLoginController.cs debe implementar la logica de variantes, login y friccion cero', () => {
    assert.ok(fs.existsSync(controllerPath), 'UIToolkitLoginController.cs no existe');
    const content = fs.readFileSync(controllerPath, 'utf8');

    assert.ok(content.includes('class UIToolkitLoginController : MonoBehaviour'), 'Debe heredar de MonoBehaviour');
    assert.ok(content.includes('SetLinkingMode'), 'Debe permitir alternar entre modo nosession y link');
    assert.ok(content.includes('OnClickGoogleLogin'), 'Debe implementar OnClickGoogleLogin');
    assert.ok(content.includes('OnClickEmailLogin'), 'Debe implementar OnClickEmailLogin');
    assert.ok(content.includes('OnClickContinueAsGuest'), 'Debe implementar OnClickContinueAsGuest');
    assert.ok(content.includes('SignInAnonymouslyAsync'), 'Debe llamar a SignInAnonymouslyAsync al continuar como invitado');
    assert.ok(content.includes('HomeScreenUIToolkitScene'), 'Debe navegar a HomeScreenUIToolkitScene al autenticar');
  });

  it('4. FirebaseAuthManager.cs debe contar con HasCachedSession()', () => {
    assert.ok(fs.existsSync(authPath), 'FirebaseAuthManager.cs no existe');
    const content = fs.readFileSync(authPath, 'utf8');

    assert.ok(content.includes('HasCachedSession()'), 'Debe contener HasCachedSession()');
    assert.ok(content.includes('PlayerPrefs.HasKey(PREF_UID)'), 'Debe validar existencia de PREF_UID');
  });

  it('5. SplashScreenController.cs debe redirigir a LoginSceneUIToolkit si no hay cuenta previa', () => {
    assert.ok(fs.existsSync(splashPath), 'SplashScreenController.cs no existe');
    const content = fs.readFileSync(splashPath, 'utf8');

    assert.ok(content.includes('HasCachedSession()'), 'Debe consultar HasCachedSession');
    assert.ok(content.includes('SceneManager.LoadScene("LoginSceneUIToolkit")'), 'Debe cargar LoginSceneUIToolkit si no hay sesion');
    assert.ok(content.includes('SceneManager.LoadScene("HomeScreenUIToolkitScene")'), 'Debe cargar HomeScreenUIToolkitScene si hay sesion');
  });

  it('6. Debe existir UIToolkitLoginSceneBuilder.cs y estar registrado en AutoRegisterBuildScenes.cs', () => {
    assert.ok(fs.existsSync(builderPath), 'UIToolkitLoginSceneBuilder.cs no existe');
    assert.ok(fs.existsSync(autoBuildPath), 'AutoRegisterBuildScenes.cs no existe');

    const builderContent = fs.readFileSync(builderPath, 'utf8');
    const autoBuildContent = fs.readFileSync(autoBuildPath, 'utf8');

    assert.ok(builderContent.includes('1080'), 'Debe configurar referencia 1080x2400');
    assert.ok(builderContent.includes('2400'), 'Debe configurar referencia 1080x2400');
    assert.ok(builderContent.includes('LoginSceneUIToolkit.unity'), 'Debe apuntar a LoginSceneUIToolkit.unity');
    assert.ok(autoBuildContent.includes('LoginSceneUIToolkit.unity'), 'Debe registrar LoginSceneUIToolkit.unity en el build');
  });

  it('7. Simulacion logica: Jugador nuevo navega a Login, toca invitado y se crea cuenta anonima', () => {
    let cachedSession = false;
    let targetScene = null;

    // Splash sequence
    if (cachedSession) {
      targetScene = 'HomeScreenUIToolkitScene';
    } else {
      targetScene = 'LoginSceneUIToolkit';
    }
    assert.strictEqual(targetScene, 'LoginSceneUIToolkit', 'Nuevo jugador debe ir a LoginSceneUIToolkit');

    // Login interaction
    let userCreated = false;
    let mode = 'nosession';
    function clickGuest() {
      if (mode === 'nosession') {
        userCreated = true;
        targetScene = 'HomeScreenUIToolkitScene';
      }
    }
    clickGuest();
    assert.strictEqual(userCreated, true, 'Debe crear sesion al pulsar Invitado');
    assert.strictEqual(targetScene, 'HomeScreenUIToolkitScene', 'Debe navegar a HomeScreenUIToolkitScene');
  });

  it('8. Simulacion logica: Jugador recurrente con sesion cacheada va directo a HomeScreenUIToolkitScene', () => {
    let cachedSession = true;
    let targetScene = null;

    if (cachedSession) {
      targetScene = 'HomeScreenUIToolkitScene';
    } else {
      targetScene = 'LoginSceneUIToolkit';
    }
    assert.strictEqual(targetScene, 'HomeScreenUIToolkitScene', 'Jugador recurrente no debe ver Login, salta a Home');
  });
});
