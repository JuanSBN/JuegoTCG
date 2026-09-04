package com.juansbn.juegotcg;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

import com.google.android.gms.auth.api.signin.GoogleSignIn;
import com.google.android.gms.auth.api.signin.GoogleSignInAccount;
import com.google.android.gms.auth.api.signin.GoogleSignInClient;
import com.google.android.gms.auth.api.signin.GoogleSignInOptions;
import com.google.android.gms.common.api.ApiException;
import com.google.android.gms.tasks.Task;
import com.unity3d.player.UnityPlayer;

/**
 * Actividad nativa transparente que gestiona el flujo de Google Sign-In en Android
 * lanzando el selector de cuentas del sistema y devolviendo el resultado a Unity.
 */
public class GoogleSignInActivity extends Activity {
    private static final String TAG = "GoogleSignInActivity";
    private static final int RC_SIGN_IN = 9001;
    public static final String EXTRA_WEB_CLIENT_ID = "extra_web_client_id";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        String webClientId = getIntent().getStringExtra(EXTRA_WEB_CLIENT_ID);
        Log.d(TAG, "Iniciando selector de cuentas de Google. WebClientId configurado: " + (webClientId != null && !webClientId.isEmpty()));

        GoogleSignInOptions.Builder gsoBuilder = new GoogleSignInOptions.Builder(GoogleSignInOptions.DEFAULT_SIGN_IN)
                .requestEmail()
                .requestProfile();

        if (webClientId != null && !webClientId.trim().isEmpty()) {
            gsoBuilder.requestIdToken(webClientId.trim());
        }

        GoogleSignInOptions gso = gsoBuilder.build();
        GoogleSignInClient client = GoogleSignIn.getClient(this, gso);

        // Cerrar sesión previa para forzar que el selector de cuentas aparezca siempre al pulsar el botón
        client.signOut().addOnCompleteListener(this, task -> {
            Intent signInIntent = client.getSignInIntent();
            startActivityForResult(signInIntent, RC_SIGN_IN);
        });
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == RC_SIGN_IN) {
            Task<GoogleSignInAccount> task = GoogleSignIn.getSignedInAccountFromIntent(data);
            handleSignInResult(task);
        }
    }

    private void handleSignInResult(Task<GoogleSignInAccount> completedTask) {
        try {
            GoogleSignInAccount account = completedTask.getResult(ApiException.class);
            if (account != null) {
                String displayName = escapeJson(account.getDisplayName() != null ? account.getDisplayName() : "Usuario Google");
                String email = escapeJson(account.getEmail() != null ? account.getEmail() : "");
                String idToken = escapeJson(account.getIdToken() != null ? account.getIdToken() : "");
                String id = escapeJson(account.getId() != null ? account.getId() : "");
                String photoUrl = escapeJson(account.getPhotoUrl() != null ? account.getPhotoUrl().toString() : "");

                String json = "{\"success\":true,"
                        + "\"displayName\":\"" + displayName + "\","
                        + "\"email\":\"" + email + "\","
                        + "\"idToken\":\"" + idToken + "\","
                        + "\"id\":\"" + id + "\","
                        + "\"photoUrl\":\"" + photoUrl + "\"}";

                Log.d(TAG, "Cuenta seleccionada exitosamente: " + email);
                UnityPlayer.UnitySendMessage("GoogleSignInManager", "OnGoogleSignInSuccess", json);
            } else {
                sendFailure("No se obtuvo información de la cuenta seleccionada.");
            }
        } catch (ApiException e) {
            int statusCode = e.getStatusCode();
            Log.w(TAG, "Fallo en selección de cuenta Google. Código de error: " + statusCode);
            String message = "Error de autenticación Google (" + statusCode + ")";
            if (statusCode == 12501) {
                message = "Selección de cuenta cancelada por el usuario.";
            } else if (statusCode == 10) {
                message = "Error de configuración (Developer Error 10). Verifica la huella SHA-1 en Firebase Console.";
            }
            sendFailure(message);
        } finally {
            finish();
        }
    }

    private void sendFailure(String error) {
        String json = "{\"success\":false,\"error\":\"" + escapeJson(error) + "\"}";
        UnityPlayer.UnitySendMessage("GoogleSignInManager", "OnGoogleSignInFailed", json);
    }

    private String escapeJson(String str) {
        if (str == null) return "";
        return str.replace("\\", "\\\\")
                  .replace("\"", "\\\"")
                  .replace("\n", "\\n")
                  .replace("\r", "\\r")
                  .replace("\t", "\\t");
    }
}
