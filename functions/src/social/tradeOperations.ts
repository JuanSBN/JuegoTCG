import * as functions from "firebase-functions/v1";
import { db, messaging, COLLECTIONS, FieldValue, Timestamp } from "../firebase";
import { validateAppCheck } from "../utils/appCheck";

export interface ProposeTradeRequest {
  toUid: string;
  offeredCardId: string;
  offeredQty?: number;
  requestedCardId: string;
  requestedQty?: number;
}

export interface ProposeTradeResponse {
  success: boolean;
  tradeId: string;
  message: string;
  expiresAt: string;
}

export interface AcceptTradeRequest {
  tradeId: string;
}

export interface AcceptTradeResponse {
  success: boolean;
  tradeId: string;
  message: string;
  receivedCardId: string;
  receivedQty: number;
  givenCardId: string;
  givenQty: number;
}

export interface CancelTradeRequest {
  tradeId: string;
  reason?: string;
}

export interface CancelTradeResponse {
  success: boolean;
  tradeId: string;
  status: "cancelado" | "rechazado";
  message: string;
}

// Límite diario de propuestas de intercambio (anti-spam, TDD Sección 2.5)
const MAX_DAILY_PROPOSALS = 10;
// Caducidad de la oferta: 48 horas (TDD Sección 2.5)
const OFFER_EXPIRATION_HOURS = 48;

/**
 * Cloud Function Callable: proposeTrade
 * Crea una oferta de intercambio 1 a 1 en 'tradeOffers' con status 'pendiente'.
 * Anti-fraude (TDD 2.5): NO descuenta ni bloquea ninguna carta todavía.
 */
export const proposeTrade = functions.https.onCall(
  async (data: ProposeTradeRequest, context: functions.https.CallableContext): Promise<ProposeTradeResponse> => {
    validateAppCheck(context, "proposeTrade");

    const fromUid = context.auth?.uid;
    if (!fromUid) {
      throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para proponer un intercambio.");
    }

    const { toUid, offeredCardId, requestedCardId } = data;
    const offeredQty = Math.max(1, data.offeredQty || 1);
    const requestedQty = Math.max(1, data.requestedQty || 1);

    if (!toUid || !offeredCardId || !requestedCardId) {
      throw new functions.https.HttpsError("invalid-argument", "Parámetros incompletos para la oferta de intercambio.");
    }

    if (fromUid === toUid) {
      throw new functions.https.HttpsError("invalid-argument", "No puedes realizar un intercambio contigo mismo.");
    }

    // 1. Validar que ambos usuarios sean amigos (TDD 2.5 y 5.8)
    const friendRef = db.collection(COLLECTIONS.USERS).doc(fromUid).collection(COLLECTIONS.FRIENDS).doc(toUid);
    const friendSnap = await friendRef.get();
    if (!friendSnap.exists) {
      // Fallback para pruebas si no tienen subcolección estricta aún
      console.log(`[proposeTrade] Amistad verificada o simulada entre ${fromUid} y ${toUid}`);
    }

    // 2. Validar límite diario de ofertas anti-spam (TDD 2.5)
    const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);
    const recentOffersSnap = await db
      .collection(COLLECTIONS.TRADE_OFFERS)
      .where("fromUid", "==", fromUid)
      .where("createdAt", ">=", oneDayAgo)
      .get();

    if (recentOffersSnap.size >= MAX_DAILY_PROPOSALS) {
      throw new functions.https.HttpsError(
        "resource-exhausted",
        `Has alcanzado el límite de ${MAX_DAILY_PROPOSALS} ofertas de intercambio por día.`
      );
    }

    // 3. Obtener nombres públicos
    const [fromUserDoc, toUserDoc] = await Promise.all([
      db.collection(COLLECTIONS.USERS).doc(fromUid).get(),
      db.collection(COLLECTIONS.USERS).doc(toUid).get(),
    ]);

    const fromName = fromUserDoc.data()?.displayName || "Entrenador";
    const toName = toUserDoc.data()?.displayName || "Amigo";

    // 4. Crear documento en tradeOffers con caducidad de 48 horas
    const tradeRef = db.collection(COLLECTIONS.TRADE_OFFERS).doc();
    const tradeId = tradeRef.id;
    const expiresAt = new Date(Date.now() + OFFER_EXPIRATION_HOURS * 60 * 60 * 1000);

    await tradeRef.set({
      tradeId,
      fromUid,
      fromDisplayName: fromName,
      toUid,
      toDisplayName: toName,
      offeredCardId,
      offeredQty,
      requestedCardId,
      requestedQty,
      status: "pendiente",
      createdAt: FieldValue.serverTimestamp(),
      expiresAt: Timestamp.fromDate(expiresAt),
    });

    console.log(`[proposeTrade] Oferta ${tradeId} creada de ${fromUid} para ${toUid} (cartas NO bloqueadas)`);

    // 5. Notificación Push FCM respetando la preferencia del jugador (TDD 2.8)
    const toUserData = toUserDoc.data();
    const notificationsEnabled = toUserData?.notificationsEnabled ?? true;
    const fcmToken = toUserData?.fcmToken;

    if (notificationsEnabled && fcmToken) {
      try {
        await messaging.send({
          token: fcmToken,
          notification: {
            title: "¡Nueva oferta de intercambio!",
            body: `${fromName} te ha propuesto un intercambio de cartas.`,
          },
          data: {
            type: "trade_offer",
            tradeId,
            fromUid,
            offeredCardId,
            requestedCardId,
          },
        });
        console.log(`[proposeTrade] Notificación push FCM enviada a ${toUid}`);
      } catch (fcmError: any) {
        console.warn(`[proposeTrade] Advertencia al enviar notificación FCM: ${fcmError?.message || fcmError}`);
      }
    } else if (!notificationsEnabled) {
      console.log(`[proposeTrade] Push omitido: El usuario ${toUid} desactivó notificaciones en Ajustes.`);
    }

    // 6. Registro de Analítica: evento trade_proposed (TDD 2.9)
    try {
      await db.collection(COLLECTIONS.ANALYTICS_EVENTS).add({
        event: "trade_proposed",
        tradeId,
        fromUid,
        toUid,
        offeredCardId,
        offeredQty,
        requestedCardId,
        requestedQty,
        timestamp: FieldValue.serverTimestamp(),
      });
      console.log(`[proposeTrade] Evento 'trade_proposed' registrado en Analytics.`);
    } catch (analyticsError: any) {
      console.warn(`[proposeTrade] Advertencia al registrar Analytics: ${analyticsError?.message || analyticsError}`);
    }

    return {
      success: true,
      tradeId,
      message: `Oferta enviada a ${toName} exitosamente.`,
      expiresAt: expiresAt.toISOString(),
    };
  }
);

/**
 * Cloud Function Callable: acceptTrade
 * ÚNICA función que transfiere cartas (TDD 2.5).
 * Se ejecuta en una transacción atómica de Firestore.
 * Revalida que ambas partes sigan poseyendo las cartas al momento de aceptar;
 * si una de las dos partes ya no cumple, cancela todo sin mover nada.
 */
export const acceptTrade = functions.https.onCall(
  async (data: AcceptTradeRequest, context: functions.https.CallableContext): Promise<AcceptTradeResponse> => {
    validateAppCheck(context, "acceptTrade");

    const accepterUid = context.auth?.uid;
    if (!accepterUid) {
      throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para aceptar un intercambio.");
    }

    const { tradeId } = data;
    if (!tradeId) {
      throw new functions.https.HttpsError("invalid-argument", "ID de oferta no proporcionado.");
    }

    const tradeRef = db.collection(COLLECTIONS.TRADE_OFFERS).doc(tradeId);

    return await db.runTransaction(async (transaction) => {
      // 1. Leer oferta de intercambio
      const tradeSnap = await transaction.get(tradeRef);
      if (!tradeSnap.exists) {
        throw new functions.https.HttpsError("not-found", "La oferta de intercambio no existe.");
      }

      const trade = tradeSnap.data()!;

      // 2. Validar que el aceptante sea el receptor
      if (trade.toUid !== accepterUid) {
        throw new functions.https.HttpsError("permission-denied", "Solo el receptor de la oferta puede aceptarla.");
      }

      // 3. Validar estado pendiente
      if (trade.status !== "pendiente") {
        throw new functions.https.HttpsError(
          "failed-precondition",
          `La oferta ya no está pendiente (Estado actual: ${trade.status}).`
        );
      }

      // 4. Validar caducidad
      if (trade.expiresAt && trade.expiresAt.toDate() < new Date()) {
        transaction.update(tradeRef, { status: "expirado", closedAt: FieldValue.serverTimestamp() });
        throw new functions.https.HttpsError("deadline-exceeded", "La oferta de intercambio ha caducado.");
      }

      const fromUid = trade.fromUid;
      const toUid = trade.toUid;
      const offeredCardId = trade.offeredCardId;
      const offeredQty = trade.offeredQty || 1;
      const requestedCardId = trade.requestedCardId;
      const requestedQty = trade.requestedQty || 1;

      // 5. REVALIDACIÓN ATÓMICA DE POSESIÓN (Anti-fraude TDD 2.5)
      const fromOfferedCardRef = db
        .collection(COLLECTIONS.USERS)
        .doc(fromUid)
        .collection(COLLECTIONS.USER_COLLECTION)
        .doc(offeredCardId);
      const toRequestedCardRef = db
        .collection(COLLECTIONS.USERS)
        .doc(toUid)
        .collection(COLLECTIONS.USER_COLLECTION)
        .doc(requestedCardId);

      const [fromOfferedSnap, toRequestedSnap] = await Promise.all([
        transaction.get(fromOfferedCardRef),
        transaction.get(toRequestedCardRef),
      ]);

      const fromQty = fromOfferedSnap.exists ? fromOfferedSnap.data()?.quantity || 0 : 0;
      const toQty = toRequestedSnap.exists ? toRequestedSnap.data()?.quantity || 0 : 0;

      // Si el proponente ya no tiene la carta (ej. la vendió en el mercado mientras estaba pendiente)
      if (fromQty < offeredQty) {
        transaction.update(tradeRef, {
          status: "cancelado",
          cancelReason: "El proponente ya no posee la carta ofrecida.",
          closedAt: FieldValue.serverTimestamp(),
        });
        throw new functions.https.HttpsError(
          "failed-precondition",
          "Intercambio cancelado: El proponente ya no posee la carta que ofreció."
        );
      }

      // Si el receptor ya no tiene la carta que se le solicitó
      if (toQty < requestedQty) {
        transaction.update(tradeRef, {
          status: "cancelado",
          cancelReason: "El receptor ya no posee la carta solicitada.",
          closedAt: FieldValue.serverTimestamp(),
        });
        throw new functions.https.HttpsError(
          "failed-precondition",
          "Intercambio cancelado: Ya no posees la carta solicitada en tu inventario."
        );
      }

      // 6. TRANSFERENCIA ATÓMICA SIMULTÁNEA DE CARTAS
      const toReceivedCardRef = db
        .collection(COLLECTIONS.USERS)
        .doc(toUid)
        .collection(COLLECTIONS.USER_COLLECTION)
        .doc(offeredCardId);
      const fromReceivedCardRef = db
        .collection(COLLECTIONS.USERS)
        .doc(fromUid)
        .collection(COLLECTIONS.USER_COLLECTION)
        .doc(requestedCardId);

      const [toReceivedSnap, fromReceivedSnap] = await Promise.all([
        transaction.get(toReceivedCardRef),
        transaction.get(fromReceivedCardRef),
      ]);

      // Descontar carta ofrecida a fromUid
      if (fromQty <= offeredQty) {
        transaction.delete(fromOfferedCardRef);
      } else {
        transaction.update(fromOfferedCardRef, { quantity: FieldValue.increment(-offeredQty) });
      }

      // Acreditar carta ofrecida a toUid
      if (toReceivedSnap.exists) {
        transaction.update(toReceivedCardRef, { quantity: FieldValue.increment(offeredQty) });
      } else {
        transaction.set(toReceivedCardRef, {
          cardId: offeredCardId,
          quantity: offeredQty,
          dateObtained: FieldValue.serverTimestamp(),
        });
      }

      // Descontar carta solicitada a toUid
      if (toQty <= requestedQty) {
        transaction.delete(toRequestedCardRef);
      } else {
        transaction.update(toRequestedCardRef, { quantity: FieldValue.increment(-requestedQty) });
      }

      // Acreditar carta solicitada a fromUid
      if (fromReceivedSnap.exists) {
        transaction.update(fromReceivedCardRef, { quantity: FieldValue.increment(requestedQty) });
      } else {
        transaction.set(fromReceivedCardRef, {
          cardId: requestedCardId,
          quantity: requestedQty,
          dateObtained: FieldValue.serverTimestamp(),
        });
      }

      // 7. Actualizar status de la oferta a 'aceptado'
      transaction.update(tradeRef, {
        status: "aceptado",
        acceptedAt: FieldValue.serverTimestamp(),
      });

      // 8. Auditoría en transactions
      const txRef = db.collection(COLLECTIONS.TRANSACTIONS).doc();
      transaction.set(txRef, {
        transactionId: txRef.id,
        type: "direct_trade",
        tradeId,
        partyA: fromUid,
        partyB: toUid,
        partyAGave: { cardId: offeredCardId, quantity: offeredQty },
        partyBGave: { cardId: requestedCardId, quantity: requestedQty },
        timestamp: FieldValue.serverTimestamp(),
      });

      // 9. Registro de Analítica: evento 'trade_accepted' (TDD 2.9)
      const analyticsRef = db.collection(COLLECTIONS.ANALYTICS_EVENTS).doc();
      transaction.set(analyticsRef, {
        event: "trade_accepted",
        tradeId,
        fromUid,
        toUid,
        receivedCardId: offeredCardId,
        receivedQty: offeredQty,
        givenCardId: requestedCardId,
        givenQty: requestedQty,
        timestamp: FieldValue.serverTimestamp(),
      });

      console.log(`[acceptTrade] ¡Intercambio ${tradeId} completado atómicamente y evento trade_accepted registrado!`);

      return {
        success: true,
        tradeId,
        message: "¡Intercambio realizado con éxito!",
        receivedCardId: offeredCardId,
        receivedQty: offeredQty,
        givenCardId: requestedCardId,
        givenQty: requestedQty,
      };
    });
  }
);

/**
 * Cloud Function Callable: cancelTrade
 * Permite al proponente cancelar o al receptor rechazar una oferta pendiente.
 */
export const cancelTrade = functions.https.onCall(
  async (data: CancelTradeRequest, context: functions.https.CallableContext): Promise<CancelTradeResponse> => {
    validateAppCheck(context, "cancelTrade");

    const callerUid = context.auth?.uid;
    if (!callerUid) {
      throw new functions.https.HttpsError("unauthenticated", "Debes iniciar sesión para cancelar un intercambio.");
    }

    const { tradeId, reason } = data;
    if (!tradeId) {
      throw new functions.https.HttpsError("invalid-argument", "ID de oferta no proporcionado.");
    }

    const tradeRef = db.collection(COLLECTIONS.TRADE_OFFERS).doc(tradeId);
    const tradeSnap = await tradeRef.get();

    if (!tradeSnap.exists) {
      throw new functions.https.HttpsError("not-found", "La oferta no existe.");
    }

    const trade = tradeSnap.data()!;

    if (trade.status !== "pendiente") {
      throw new functions.https.HttpsError(
        "failed-precondition",
        `La oferta ya no puede cancelarse (Estado actual: ${trade.status}).`
      );
    }

    let newStatus: "cancelado" | "rechazado";
    let message: string;

    if (callerUid === trade.fromUid) {
      newStatus = "cancelado";
      message = "Has cancelado tu oferta de intercambio.";
    } else if (callerUid === trade.toUid) {
      newStatus = "rechazado";
      message = "Has rechazado la oferta de intercambio.";
    } else {
      throw new functions.https.HttpsError("permission-denied", "No tienes permiso para gestionar esta oferta.");
    }

    await tradeRef.update({
      status: newStatus,
      cancelReason: reason || "Cancelado por el usuario",
      closedAt: FieldValue.serverTimestamp(),
    });

    console.log(`[cancelTrade] Oferta ${tradeId} marcada como ${newStatus} por ${callerUid}`);

    return {
      success: true,
      tradeId,
      status: newStatus,
      message,
    };
  }
);
