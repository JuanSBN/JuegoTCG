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
exports.getActiveEvents = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const firebase_1 = require("../firebase");
/**
 * Cloud Function: getActiveEvents
 * Devuelve la lista de eventos especiales activos con la hora del servidor y los segundos restantes calculados,
 * garantizando que el temporizador (countdown) de la pantalla de inicio no dependa del reloj del dispositivo móvil.
 */
exports.getActiveEvents = functions.https.onCall(async (data, context) => {
    const serverNowMs = Date.now();
    const serverNowDate = new Date(serverNowMs);
    try {
        const eventsQuery = await firebase_1.db
            .collection(firebase_1.COLLECTIONS.ALBUMS)
            .where("active", "==", true)
            .where("type", "==", "evento")
            .get();
        const activeEvents = [];
        if (!eventsQuery.empty) {
            eventsQuery.docs.forEach((doc) => {
                const d = doc.data();
                const endsAtMs = d.endsAt?.toMillis?.() || (serverNowMs + 3 * 24 * 60 * 60 * 1000);
                const startsAtMs = d.startsAt?.toMillis?.() || serverNowMs;
                // Solo incluir eventos que ya iniciaron y no hayan vencido
                if (serverNowMs >= startsAtMs && serverNowMs < endsAtMs) {
                    const remainingSeconds = Math.max(0, Math.floor((endsAtMs - serverNowMs) / 1000));
                    activeEvents.push({
                        eventId: doc.id,
                        title: d.name || "Evento Especial de Colección",
                        subtitle: d.subtitle || "Completa el álbum del evento para ganar recompensas exclusivas",
                        bannerAssetPath: d.bannerAssetPath || "",
                        albumId: d.albumId || doc.id,
                        startsAt: new Date(startsAtMs).toISOString(),
                        endsAt: new Date(endsAtMs).toISOString(),
                        remainingSeconds,
                        featuredReward: d.rewardOnComplete?.description || "Sobre Mítico + 500 Monedas",
                    });
                }
            });
        }
        // Si aún no hay eventos creados en la base de datos, proveer evento piloto activo por defecto
        if (activeEvents.length === 0) {
            const defaultEndMs = serverNowMs + (2 * 24 * 60 * 60 * 1000) + (14 * 60 * 60 * 1000); // 2 días y 14 horas
            activeEvents.push({
                eventId: "event_torneo_leyendas_2026",
                title: "Torneo de Leyendas 2026",
                subtitle: "Consigue las cartas históricas exclusivas antes de que termine el evento",
                albumId: "album_evento_leyendas",
                startsAt: serverNowDate.toISOString(),
                endsAt: new Date(defaultEndMs).toISOString(),
                remainingSeconds: Math.floor((defaultEndMs - serverNowMs) / 1000),
                featuredReward: "Sobre Mítico Garantizado + Insignia de Perfil",
            });
        }
        return {
            serverTime: serverNowDate.toISOString(),
            events: activeEvents,
        };
    }
    catch (err) {
        console.error("[getActiveEvents] Error consultando eventos:", err.message);
        // Fallback seguro en caso de consulta inicial
        const fallbackEndMs = serverNowMs + (2 * 24 * 60 * 60 * 1000) + (14 * 60 * 60 * 1000);
        return {
            serverTime: serverNowDate.toISOString(),
            events: [
                {
                    eventId: "event_torneo_leyendas_2026",
                    title: "Torneo de Leyendas 2026",
                    subtitle: "Consigue las cartas históricas exclusivas antes de que termine el evento",
                    albumId: "album_evento_leyendas",
                    startsAt: serverNowDate.toISOString(),
                    endsAt: new Date(fallbackEndMs).toISOString(),
                    remainingSeconds: Math.floor((fallbackEndMs - serverNowMs) / 1000),
                    featuredReward: "Sobre Mítico Garantizado + Insignia de Perfil",
                },
            ],
        };
    }
});
//# sourceMappingURL=getActiveEvents.js.map