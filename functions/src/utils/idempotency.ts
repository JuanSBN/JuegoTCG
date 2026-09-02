import { db, COLLECTIONS, FieldValue, Transaction } from "../firebase";

export interface ProcessedRequestRecord<T = any> {
  idempotencyKey: string;
  userId: string;
  functionName: string;
  result: T;
  createdAt: FieldValue;
}

/**
 * Checks if a request with the given idempotencyKey has already been processed.
 * If processed, returns the previous cached result.
 */
export async function getCachedIdempotentResult<T = any>(
  idempotencyKey: string
): Promise<{ cached: boolean; result?: T }> {
  if (!idempotencyKey) {
    return { cached: false };
  }

  const reqDocRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
  const doc = await reqDocRef.get();

  if (doc.exists) {
    const data = doc.data();
    return {
      cached: true,
      result: data?.result as T,
    };
  }

  return { cached: false };
}

/**
 * Saves the idempotency record inside an active Firestore Transaction to guarantee
 * atomicity (the request cannot be recorded without executing, nor executed without recording).
 */
export function recordIdempotencyInTransaction<T = any>(
  transaction: Transaction,
  idempotencyKey: string,
  userId: string,
  functionName: string,
  result: T
): void {
  if (!idempotencyKey) return;

  const reqDocRef = db.collection(COLLECTIONS.PROCESSED_REQUESTS).doc(idempotencyKey);
  transaction.set(reqDocRef, {
    idempotencyKey,
    userId,
    functionName,
    result,
    createdAt: FieldValue.serverTimestamp(),
  });
}
