import * as functions from "firebase-functions/v1";
import { db, COLLECTIONS } from "../firebase";

export interface ActiveEventItem {
  eventId: string;
  title: string;
  subtitle: string;
  bannerAssetPath?: string;
  albumId: string;
  startsAt: string;
  endsAt: string;
  remainingSeconds: number;
  featuredReward: string;
}

export interface GetActiveEventsResponse {
  serverTime: string;
  events: ActiveEventItem[];
}

/**
 * Cloud Function: getActiveEvents
 * Devuelve la lista de eventos especiales activos con la hora del servidor y los segundos restantes calculados,
 * garantizando que el temporizador (countdown) de la pantalla de inicio no dependa del reloj del dispositivo móvil.
 */
export const getActiveEvents = functions.https.onCall(
  async (data: any, context: functions.https.CallableContext): Promise<GetActiveEventsResponse> => {
    const serverNowMs = Date.now();
    const serverNowDate = new Date(serverNowMs);

    try {
      const eventsQuery = await db
        .collection(COLLECTIONS.ALBUMS)
        .where("active", "==", true)
        .where("type", "==", "evento")
        .get();

      const activeEvents: ActiveEventItem[] = [];

      if (!eventsQuery.empty) {
        eventsQuery.docs.forEach((doc: any) => {
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
    } catch (err: any) {
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
  }
);
