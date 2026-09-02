"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.validateAppCheck = validateAppCheck;
const functions = __importStar(require("firebase-functions/v1"));
/**
 * Middleware de verificación de Firebase App Check (TDD Sección 2.7)
 *
 * En producción con Play Integrity, verifica que la llamada provenga exclusivamente
 * de una instancia legítima de la aplicación de Android.
 *
 * En entorno de desarrollo o pruebas locales (Unity Editor / Emuladores),
 * permite llamadas si no está explícitamente forzado en modo estricto.
 */
function validateAppCheck(context, functionName) {
    const isAppCheckEnforced = process.env.ENFORCE_APP_CHECK === "true" || process.env.NODE_ENV === "production";
    if (isAppCheckEnforced) {
        if (!context.app) {
            console.warn(`[AppCheck:BLOCKED] Función '${functionName}' rechazada por ausencia de token de App Check válido.`);
            throw new functions.https.HttpsError("unauthenticated", "Petición rechazada por seguridad: Esta llamada requiere una instancia genuina y verificada de la aplicación (App Check).");
        }
        console.log(`[AppCheck:VALID] Petición verificada con éxito para appId: ${context.app.appId} en '${functionName}'.`);
    }
}
//# sourceMappingURL=appCheck.js.map