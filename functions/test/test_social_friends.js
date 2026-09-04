const fs = require('fs');
const path = require('path');

const rootDir = path.resolve(__dirname, '../..');

console.log('==========================================================================');
console.log('🧪 VERIFICANDO SISTEMA SOCIAL Y GESTIÓN DE AMIGOS (FASE 8 - PUNTO 1)');
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

// 1. firebase.ts - Colecciones de amigos
const firebasePath = path.join(rootDir, 'functions/src/firebase.ts');
assert(fs.existsSync(firebasePath), 'firebase.ts existe');
if (fs.existsSync(firebasePath)) {
    const fb = fs.readFileSync(firebasePath, 'utf8');
    assert(fb.includes('FRIENDS: "friends"'), 'Constante FRIENDS declarada en COLLECTIONS');
    assert(fb.includes('FRIEND_REQUESTS: "friendRequests"'), 'Constante FRIEND_REQUESTS declarada en COLLECTIONS');
}

// 2. sendFriendRequest.ts
const sendReqPath = path.join(rootDir, 'functions/src/social/sendFriendRequest.ts');
assert(fs.existsSync(sendReqPath), 'sendFriendRequest.ts existe');
if (fs.existsSync(sendReqPath)) {
    const code = fs.readFileSync(sendReqPath, 'utf8');
    assert(code.includes('sendFriendRequest = functions.https.onCall'), 'Cloud Function sendFriendRequest declarada');
    assert(code.includes('targetUid === fromUid'), 'Bloqueo de auto-solicitud (no agregarse a sí mismo)');
    assert(code.includes('normalizedCode = rawCode.trim().toUpperCase()'), 'Normalización de código de amigo a mayúsculas');
    assert(code.includes('existingFriendDoc.exists'), 'Validación de que no sean amigos previamente');
    assert(code.includes('reverseReqDoc.exists') && code.includes('autoAccepted'), 'Soporte para aceptación mutua automática si ambos se solicitaron');
    assert(code.includes('COLLECTIONS.FRIEND_REQUESTS'), 'Persistencia en colección friendRequests con estado pending');
}

// 3. manageFriendRequest.ts
const manageReqPath = path.join(rootDir, 'functions/src/social/manageFriendRequest.ts');
assert(fs.existsSync(manageReqPath), 'manageFriendRequest.ts existe');
if (fs.existsSync(manageReqPath)) {
    const code = fs.readFileSync(manageReqPath, 'utf8');
    assert(code.includes('acceptFriendRequest = functions.https.onCall'), 'Cloud Function acceptFriendRequest declarada');
    assert(code.includes('rejectFriendRequest = functions.https.onCall'), 'Cloud Function rejectFriendRequest declarada');
    assert(code.includes('db.runTransaction'), 'Aceptación de amistad mediante transacción atómica');
    assert(code.includes('toFriendRef') && code.includes('fromFriendRef'), 'Enlace bidireccional en subcolecciones de amigos de ambos usuarios');
    assert(code.includes('friendCount: FieldValue.increment(1)'), 'Incremento de contador de amigos');
}

// 4. getSocialData.ts
const getSocialPath = path.join(rootDir, 'functions/src/social/getSocialData.ts');
assert(fs.existsSync(getSocialPath), 'getSocialData.ts existe');
if (fs.existsSync(getSocialPath)) {
    const code = fs.readFileSync(getSocialPath, 'utf8');
    assert(code.includes('getSocialData = functions.https.onCall'), 'Cloud Function getSocialData declarada');
    assert(code.includes('generateRandomFriendCode'), 'Generador de códigos de amigo disponible');
    assert(code.includes('code = "FC-"'), 'Formato estándar de código FC-XXXX');
}

// 5. Exportaciones en index.ts
const indexPath = path.join(rootDir, 'functions/src/index.ts');
assert(fs.existsSync(indexPath), 'index.ts existe');
if (fs.existsSync(indexPath)) {
    const idx = fs.readFileSync(indexPath, 'utf8');
    assert(idx.includes('sendFriendRequest') && idx.includes('acceptFriendRequest'), 'Funciones exportadas en index.ts');
    assert(idx.includes('rejectFriendRequest') && idx.includes('getSocialData'), 'getSocialData y rejectFriendRequest exportados');
}

// 6. FirebaseAuthManager.cs
const authPath = path.join(rootDir, 'Assets/_Project/Scripts/Networking/FirebaseAuthManager.cs');
assert(fs.existsSync(authPath), 'FirebaseAuthManager.cs existe');
if (fs.existsSync(authPath)) {
    const auth = fs.readFileSync(authPath, 'utf8');
    assert(auth.includes('FriendCode') && auth.includes('PREF_FRIEND_CODE'), 'Propiedad FriendCode y persistencia en PlayerPrefs implementadas');
    assert(auth.includes('SetFriendCode'), 'Método SetFriendCode implementado');
    assert(auth.includes('OnFriendCodeChanged'), 'Evento OnFriendCodeChanged implementado');
}

// 7. SocialService.cs
const socialPath = path.join(rootDir, 'Assets/_Project/Scripts/Social/SocialService.cs');
assert(fs.existsSync(socialPath), 'SocialService.cs existe');
if (fs.existsSync(socialPath)) {
    const soc = fs.readFileSync(socialPath, 'utf8');
    assert(soc.includes('SocialService : MonoBehaviour'), 'SocialService implementado como singleton');
    assert(soc.includes('SendFriendRequestByCodeAsync'), 'Método SendFriendRequestByCodeAsync disponible');
    assert(soc.includes('AcceptRequestAsync') && soc.includes('RejectRequestAsync'), 'Métodos AcceptRequestAsync y RejectRequestAsync disponibles');
    assert(soc.includes('MyFriendCode'), 'Acceso a código de amigo propio');
}

// 8. FriendsScreen.uxml & FriendsScreen.uss
const uxmlPath = path.join(rootDir, 'Assets/_Project/UI/Views/FriendsScreen.uxml');
const ussPath = path.join(rootDir, 'Assets/_Project/UI/Styles/FriendsScreen.uss');
assert(fs.existsSync(uxmlPath), 'FriendsScreen.uxml existe');
assert(fs.existsSync(ussPath), 'FriendsScreen.uss existe');
if (fs.existsSync(uxmlPath) && fs.existsSync(ussPath)) {
    const uxml = fs.readFileSync(uxmlPath, 'utf8');
    const uss = fs.readFileSync(ussPath, 'utf8');
    assert(uxml.includes('name="FriendsEmptyState"'), 'Elemento FriendsEmptyState presente en UXML');
    assert(uxml.includes('Agrega amigos con su código de amigo'), 'Texto de estado vacío exacto configurado');
    assert(uss.includes('.empty-state-box') && uss.includes('.empty-state-text'), 'Estilos de estado vacío presentes en USS');
}

// 9. Controladores UI Toolkit
const friendsCtrlPath = path.join(rootDir, 'Assets/_Project/Scripts/UI/UIToolkitFriendsController.cs');
const profileCtrlPath = path.join(rootDir, 'Assets/_Project/Scripts/UI/UIToolkitProfileController.cs');
if (fs.existsSync(friendsCtrlPath) && fs.existsSync(profileCtrlPath)) {
    const friendsCtrl = fs.readFileSync(friendsCtrlPath, 'utf8');
    const profileCtrl = fs.readFileSync(profileCtrlPath, 'utf8');
    assert(friendsCtrl.includes('SocialService.EnsureExists()'), 'SocialService inicializado en UIToolkitFriendsController');
    assert(friendsCtrl.includes('SendFriendRequestByCodeAsync'), 'Envío de solicitud por código enlazado al botón');
    assert(profileCtrl.includes('FriendCode'), 'Código de amigo sincronizado en UIToolkitProfileController');
}

console.log('==========================================================================');
console.log(`🎉 RESULTADO: ${passed}/${total} PRUEBAS COMPLETADAS CON ÉXITO.`);
console.log('==========================================================================');
