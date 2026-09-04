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
var __exportStar = (this && this.__exportStar) || function(m, exports) {
    for (var p in m) if (p !== "default" && !Object.prototype.hasOwnProperty.call(exports, p)) __createBinding(exports, m, p);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.checkIdempotencyTest = exports.healthCheck = exports.updateListingPrice = exports.cancelListing = exports.buyListedCard = exports.listCardForSale = exports.cancelTrade = exports.acceptTrade = exports.proposeTrade = exports.getCollectionRanking = exports.recalculateCollectionPowerTrigger = exports.recalculateCollectionPower = exports.compareAlbums = exports.getSocialData = exports.rejectFriendRequest = exports.acceptFriendRequest = exports.sendFriendRequest = exports.triggerManualCleanup = exports.cleanupProcessedRequests = exports.getActiveEvents = exports.claimAlbumCompletionReward = exports.claimMissionReward = exports.purchaseCoins = exports.watchAdReward = exports.claimFreePack = exports.openPack = void 0;
const functions = __importStar(require("firebase-functions/v1"));
const idempotency_1 = require("./utils/idempotency");
// Export Pack Operations (Fase 5.2)
var openPack_1 = require("./packs/openPack");
Object.defineProperty(exports, "openPack", { enumerable: true, get: function () { return openPack_1.openPack; } });
// Export Economy Operations (Fase 5.3, 5.4, 5.5)
var claimFreePack_1 = require("./economy/claimFreePack");
Object.defineProperty(exports, "claimFreePack", { enumerable: true, get: function () { return claimFreePack_1.claimFreePack; } });
var watchAdReward_1 = require("./economy/watchAdReward");
Object.defineProperty(exports, "watchAdReward", { enumerable: true, get: function () { return watchAdReward_1.watchAdReward; } });
var purchaseCoins_1 = require("./economy/purchaseCoins");
Object.defineProperty(exports, "purchaseCoins", { enumerable: true, get: function () { return purchaseCoins_1.purchaseCoins; } });
// Export Missions Operations (Fase 4.5)
var claimMissionReward_1 = require("./missions/claimMissionReward");
Object.defineProperty(exports, "claimMissionReward", { enumerable: true, get: function () { return claimMissionReward_1.claimMissionReward; } });
// Export Albums Operations (Fase 7.2)
var claimAlbumCompletionReward_1 = require("./albums/claimAlbumCompletionReward");
Object.defineProperty(exports, "claimAlbumCompletionReward", { enumerable: true, get: function () { return claimAlbumCompletionReward_1.claimAlbumCompletionReward; } });
// Export Events Operations (Fase 5.7)
var getActiveEvents_1 = require("./events/getActiveEvents");
Object.defineProperty(exports, "getActiveEvents", { enumerable: true, get: function () { return getActiveEvents_1.getActiveEvents; } });
// Export Maintenance Scheduled Operations (Fase 5.6)
var cleanupProcessedRequests_1 = require("./maintenance/cleanupProcessedRequests");
Object.defineProperty(exports, "cleanupProcessedRequests", { enumerable: true, get: function () { return cleanupProcessedRequests_1.cleanupProcessedRequests; } });
Object.defineProperty(exports, "triggerManualCleanup", { enumerable: true, get: function () { return cleanupProcessedRequests_1.triggerManualCleanup; } });
// Export Social & Friends Operations (Fase 8)
var sendFriendRequest_1 = require("./social/sendFriendRequest");
Object.defineProperty(exports, "sendFriendRequest", { enumerable: true, get: function () { return sendFriendRequest_1.sendFriendRequest; } });
var manageFriendRequest_1 = require("./social/manageFriendRequest");
Object.defineProperty(exports, "acceptFriendRequest", { enumerable: true, get: function () { return manageFriendRequest_1.acceptFriendRequest; } });
Object.defineProperty(exports, "rejectFriendRequest", { enumerable: true, get: function () { return manageFriendRequest_1.rejectFriendRequest; } });
var getSocialData_1 = require("./social/getSocialData");
Object.defineProperty(exports, "getSocialData", { enumerable: true, get: function () { return getSocialData_1.getSocialData; } });
var compareAlbums_1 = require("./social/compareAlbums");
Object.defineProperty(exports, "compareAlbums", { enumerable: true, get: function () { return compareAlbums_1.compareAlbums; } });
var recalculateCollectionPower_1 = require("./social/recalculateCollectionPower");
Object.defineProperty(exports, "recalculateCollectionPower", { enumerable: true, get: function () { return recalculateCollectionPower_1.recalculateCollectionPower; } });
Object.defineProperty(exports, "recalculateCollectionPowerTrigger", { enumerable: true, get: function () { return recalculateCollectionPower_1.recalculateCollectionPowerTrigger; } });
var getCollectionRanking_1 = require("./social/getCollectionRanking");
Object.defineProperty(exports, "getCollectionRanking", { enumerable: true, get: function () { return getCollectionRanking_1.getCollectionRanking; } });
var tradeOperations_1 = require("./social/tradeOperations");
Object.defineProperty(exports, "proposeTrade", { enumerable: true, get: function () { return tradeOperations_1.proposeTrade; } });
Object.defineProperty(exports, "acceptTrade", { enumerable: true, get: function () { return tradeOperations_1.acceptTrade; } });
Object.defineProperty(exports, "cancelTrade", { enumerable: true, get: function () { return tradeOperations_1.cancelTrade; } });
// Export Marketplace Types & Operations (Fase 8.5)
__exportStar(require("./market/marketTypes"), exports);
var listCardForSale_1 = require("./market/listCardForSale");
Object.defineProperty(exports, "listCardForSale", { enumerable: true, get: function () { return listCardForSale_1.listCardForSale; } });
var buyListedCard_1 = require("./market/buyListedCard");
Object.defineProperty(exports, "buyListedCard", { enumerable: true, get: function () { return buyListedCard_1.buyListedCard; } });
var manageListing_1 = require("./market/manageListing");
Object.defineProperty(exports, "cancelListing", { enumerable: true, get: function () { return manageListing_1.cancelListing; } });
Object.defineProperty(exports, "updateListingPrice", { enumerable: true, get: function () { return manageListing_1.updateListingPrice; } });
/**
 * Health check / ping function to verify Firebase Functions connectivity.
 */
exports.healthCheck = functions.https.onCall(async (data, context) => {
    return {
        status: "ok",
        serverTime: new Date().toISOString(),
        authUid: context.auth?.uid || null,
    };
});
/**
 * Example / Test endpoint for Idempotency verification.
 */
exports.checkIdempotencyTest = functions.https.onCall(async (data, context) => {
    const { idempotencyKey } = data;
    if (!idempotencyKey) {
        throw new functions.https.HttpsError("invalid-argument", "El parámetro 'idempotencyKey' es obligatorio.");
    }
    const cached = await (0, idempotency_1.getCachedIdempotentResult)(idempotencyKey);
    return {
        idempotencyKey,
        alreadyProcessed: cached.cached,
        cachedResult: cached.result || null,
    };
});
//# sourceMappingURL=index.js.map