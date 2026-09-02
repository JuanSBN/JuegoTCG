import * as functions from "firebase-functions/v1";

/**
 * Middleware de verificación de Firebase App Check (TDD Sección 2.7)
 * 
 * En producción con Play Integrity, verifica que la llamada provenga exclusivamente
 * de una instancia legítima de la aplicación de Android.
 * 
 * En entorno de desarrollo o pruebas locales (Unity Editor / Emuladores),
 * permite llamadas si no está explícitamente forzado en modo estricto.
 */
export function validateAppCheck(
  context: functions.https.CallableContext,
  functionName: string
): void {
  const isAppCheckEnforced = process.env.ENFORCE_APP_CHECK === "true" || process.env.NODE_ENV === "production";

  if (isAppCheckEnforced) {
    if (!context.app) {
      console.warn(`[AppCheck:BLOCKED] Función '${functionName}' rechazada por ausencia de token de App Check válido.`);
      throw new functions.https.HttpsError(
        "unauthenticated",
        "Petición rechazada por seguridad: Esta llamada requiere una instancia genuina y verificada de la aplicación (App Check)."
      );
    }
    console.log(`[AppCheck:VALID] Petición verificada con éxito para appId: ${context.app.appId} en '${functionName}'.`);
  }
}
