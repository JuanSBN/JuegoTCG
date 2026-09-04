import * as functions from "firebase-functions/v1";
import { getCachedIdempotentResult } from "./utils/idempotency";

// Export Pack Operations (Fase 5.2)
export { openPack } from "./packs/openPack";

// Export Economy Operations (Fase 5.3, 5.4, 5.5)
export { claimFreePack } from "./economy/claimFreePack";
export { watchAdReward } from "./economy/watchAdReward";
export { purchaseCoins } from "./economy/purchaseCoins";

// Export Missions Operations (Fase 4.5)
export { claimMissionReward } from "./missions/claimMissionReward";

// Export Albums Operations (Fase 7.2)
export { claimAlbumCompletionReward } from "./albums/claimAlbumCompletionReward";

// Export Events Operations (Fase 5.7)
export { getActiveEvents } from "./events/getActiveEvents";

// Export Maintenance Scheduled Operations (Fase 5.6)
export { cleanupProcessedRequests, triggerManualCleanup } from "./maintenance/cleanupProcessedRequests";

// Export Social & Friends Operations (Fase 8)
export { sendFriendRequest } from "./social/sendFriendRequest";
export { acceptFriendRequest, rejectFriendRequest } from "./social/manageFriendRequest";
export { getSocialData } from "./social/getSocialData";
export { compareAlbums } from "./social/compareAlbums";
export { recalculateCollectionPower, recalculateCollectionPowerTrigger } from "./social/recalculateCollectionPower";
export { getCollectionRanking } from "./social/getCollectionRanking";
export { proposeTrade, acceptTrade, cancelTrade } from "./social/tradeOperations";

// Export Marketplace Types & Operations (Fase 8.5)
export * from "./market/marketTypes";
export { listCardForSale } from "./market/listCardForSale";
export { buyListedCard } from "./market/buyListedCard";
export { cancelListing, updateListingPrice } from "./market/manageListing";

/**
 * Health check / ping function to verify Firebase Functions connectivity.
 */
export const healthCheck = functions.https.onCall(async (data: any, context: functions.https.CallableContext) => {
  return {
    status: "ok",
    serverTime: new Date().toISOString(),
    authUid: context.auth?.uid || null,
  };
});

/**
 * Example / Test endpoint for Idempotency verification.
 */
export const checkIdempotencyTest = functions.https.onCall(async (data: any, context: functions.https.CallableContext) => {
  const { idempotencyKey } = data;
  if (!idempotencyKey) {
    throw new functions.https.HttpsError(
      "invalid-argument",
      "El parámetro 'idempotencyKey' es obligatorio."
    );
  }

  const cached = await getCachedIdempotentResult(idempotencyKey);
  return {
    idempotencyKey,
    alreadyProcessed: cached.cached,
    cachedResult: cached.result || null,
  };
});
