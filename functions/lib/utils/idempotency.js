"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.getCachedIdempotentResult = getCachedIdempotentResult;
exports.recordIdempotencyInTransaction = recordIdempotencyInTransaction;
const firebase_1 = require("../firebase");
/**
 * Checks if a request with the given idempotencyKey has already been processed.
 * If processed, returns the previous cached result.
 */
async function getCachedIdempotentResult(idempotencyKey) {
    if (!idempotencyKey) {
        return { cached: false };
    }
    const reqDocRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
    const doc = await reqDocRef.get();
    if (doc.exists) {
        const data = doc.data();
        return {
            cached: true,
            result: data?.result,
        };
    }
    return { cached: false };
}
/**
 * Saves the idempotency record inside an active Firestore Transaction to guarantee
 * atomicity (the request cannot be recorded without executing, nor executed without recording).
 */
function recordIdempotencyInTransaction(transaction, idempotencyKey, userId, functionName, result) {
    if (!idempotencyKey)
        return;
    const reqDocRef = firebase_1.db.collection(firebase_1.COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
    transaction.set(reqDocRef, {
        idempotencyKey,
        userId,
        functionName,
        result,
        createdAt: firebase_1.FieldValue.serverTimestamp(),
    });
}
//# sourceMappingURL=idempotency.js.map