"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.COLLECTIONS = exports.Transaction = exports.Timestamp = exports.FieldValue = exports.auth = exports.db = void 0;
const app_1 = require("firebase-admin/app");
const firestore_1 = require("firebase-admin/firestore");
Object.defineProperty(exports, "FieldValue", { enumerable: true, get: function () { return firestore_1.FieldValue; } });
Object.defineProperty(exports, "Timestamp", { enumerable: true, get: function () { return firestore_1.Timestamp; } });
Object.defineProperty(exports, "Transaction", { enumerable: true, get: function () { return firestore_1.Transaction; } });
const auth_1 = require("firebase-admin/auth");
if (!(0, app_1.getApps)().length) {
    (0, app_1.initializeApp)();
}
exports.db = (0, firestore_1.getFirestore)();
exports.auth = (0, auth_1.getAuth)();
// Collection Names (TDD Section 5)
exports.COLLECTIONS = {
    USERS: "users",
    CARDS_CATALOG: "cardsCatalog",
    ALBUMS: "albums",
    PACKS: "packs",
    USER_COLLECTION: "collection", // Subcollection of users/{uid}/collection
    TRANSACTIONS: "transactions",
    MARKET_LISTINGS: "marketListings",
    TRADE_OFFERS: "tradeOffers",
    PROCESSED_REQUESTS: "processedRequests",
    DATA_PACKS: "dataPacks",
    USER_MISSIONS: "userMissions",
};
//# sourceMappingURL=firebase.js.map