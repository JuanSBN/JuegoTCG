import { initializeApp, getApps } from "firebase-admin/app";
import { getFirestore, FieldValue, Timestamp, Transaction } from "firebase-admin/firestore";
import { getAuth } from "firebase-admin/auth";

if (!getApps().length) {
  initializeApp();
}

export const db = getFirestore();
export const auth = getAuth();
export { FieldValue, Timestamp, Transaction };

// Collection Names (TDD Section 5)
export const COLLECTIONS = {
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
} as const;
