const fs = require('fs');
const path = require('path');

const rootDir = path.resolve(__dirname, '../..');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO INTEGRACIÓN NATIVA DE GOOGLE SIGN-IN PARA ANDROID EN UNITY');
console.log('==========================================================================');

let passed = 0;
let total = 0;

function assert(condition, desc) {
    total++;
    if (condition) {
        passed++;
        console.log(`  ✅ ${desc}`);
    } else {
        console.error(`  ❌ FALLÓ: ${desc}`);
        process.exitCode = 1;
    }
}

// 1. mainTemplate.gradle
const gradlePath = path.join(rootDir, 'Assets/Plugins/Android/mainTemplate.gradle');
assert(fs.existsSync(gradlePath), 'mainTemplate.gradle existe');
if (fs.existsSync(gradlePath)) {
    const gradle = fs.readFileSync(gradlePath, 'utf8');
    assert(gradle.includes('play-services-auth:21.2.0'), 'Dependencia play-services-auth:21.2.0 presente en Gradle');
}

// 2. AndroidManifest.xml
const manifestPath = path.join(rootDir, 'Assets/Plugins/Android/AndroidManifest.xml');
assert(fs.existsSync(manifestPath), 'AndroidManifest.xml existe');
if (fs.existsSync(manifestPath)) {
    const manifest = fs.readFileSync(manifestPath, 'utf8');
    assert(manifest.includes('com.juansbn.juegotcg.GoogleSignInActivity'), 'Actividad GoogleSignInActivity declarada');
    assert(manifest.includes('Theme.Translucent.NoTitleBar'), 'Tema translúcido configurado');
}

// 3. GoogleSignInActivity.java
const javaPath = path.join(rootDir, 'Assets/Plugins/Android/GoogleSignInActivity.java');
assert(fs.existsSync(javaPath), 'GoogleSignInActivity.java existe');
if (fs.existsSync(javaPath)) {
    const java = fs.readFileSync(javaPath, 'utf8');
    assert(java.includes('GoogleSignIn.getClient'), 'GoogleSignInClient inicializado');
    assert(java.includes('getSignInIntent'), 'Intent de selección de cuentas solicitado');
    assert(java.includes('UnitySendMessage("GoogleSignInManager"'), 'Puente bidireccional UnitySendMessage implementado');
}

// 4. GoogleSignInUser.cs
const userPath = path.join(rootDir, 'Assets/_Project/Scripts/Networking/GoogleSignInUser.cs');
assert(fs.existsSync(userPath), 'GoogleSignInUser.cs existe');
if (fs.existsSync(userPath)) {
    const user = fs.readFileSync(userPath, 'utf8');
    assert(user.includes('DisplayName') && user.includes('IdToken'), 'Campos de perfil e IdToken en GoogleSignInUser');
}

// 5. GoogleSignInManager.cs
const managerPath = path.join(rootDir, 'Assets/_Project/Scripts/Networking/GoogleSignInManager.cs');
assert(fs.existsSync(managerPath), 'GoogleSignInManager.cs existe');
if (fs.existsSync(managerPath)) {
    const manager = fs.readFileSync(managerPath, 'utf8');
    assert(manager.includes('EnsureExists()'), 'Método EnsureExists() disponible');
    assert(manager.includes('LaunchNativeAndroidSignIn'), 'Lanzador nativo de Android presente');
    assert(manager.includes('SimulateEditorSignIn'), 'Simulador para Unity Editor presente');
    assert(manager.includes('OnGoogleSignInSuccess'), 'Manejador de éxito desde Android presente');
}

// 6. LoginScreenController.cs
const loginPath = path.join(rootDir, 'Assets/_Project/Scripts/UI/LoginScreenController.cs');
assert(fs.existsSync(loginPath), 'LoginScreenController.cs existe');
if (fs.existsSync(loginPath)) {
    const login = fs.readFileSync(loginPath, 'utf8');
    assert(login.includes('GoogleSignInManager.EnsureExists()'), 'EnsureExists en LoginScreenController');
    assert(login.includes('GoogleSignInManager.Instance.SignIn'), 'SignIn en LoginScreenController');
    assert(login.includes('LinkGoogleAccountAsync'), 'Enlace con FirebaseAuthManager en Login');
}

// 7. UIToolkitSettingsController.cs
const settingsPath = path.join(rootDir, 'Assets/_Project/Scripts/UI/UIToolkitSettingsController.cs');
assert(fs.existsSync(settingsPath), 'UIToolkitSettingsController.cs existe');
if (fs.existsSync(settingsPath)) {
    const settings = fs.readFileSync(settingsPath, 'utf8');
    assert(settings.includes('GoogleSignInManager.EnsureExists()'), 'EnsureExists en SettingsController');
    assert(settings.includes('GoogleSignInManager.Instance.SignIn'), 'SignIn en SettingsController');
    assert(settings.includes('LinkGoogleAccountAsync'), 'Enlace con FirebaseAuthManager en Settings');
}

// 8. FirebaseAuthManager.cs
const authPath = path.join(rootDir, 'Assets/_Project/Scripts/Networking/FirebaseAuthManager.cs');
assert(fs.existsSync(authPath), 'FirebaseAuthManager.cs existe');
if (fs.existsSync(authPath)) {
    const auth = fs.readFileSync(authPath, 'utf8');
    assert(auth.includes('LinkGoogleAccountAsync(GoogleSignInUser googleUser)'), 'Método LinkGoogleAccountAsync presente');
}

console.log('==========================================================================');
console.log(`🎉 RESULTADO: ${passed}/${total} PRUEBAS COMPLETADAS CON ÉXITO.`);
console.log('==========================================================================');
