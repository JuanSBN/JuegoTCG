import { Timestamp } from "../firebase";

/**
 * Estados permitidos para un listado de mercado (TDD Sección 5.8b).
 */
export type MarketListingStatus = "activo" | "vendido" | "cancelado";

/**
 * Representación documental estricta de un documento en /marketListings/{listingId} (TDD Sección 5.8b).
 */
export interface MarketListing {
  listingId: string;
  sellerUid: string;
  sellerDisplayName: string;
  cardId: string;
  cardName: string;
  rarity: string;
  quantity: number;
  pricePerCard: number;
  status: MarketListingStatus;
  buyerUid?: string | null;
  buyerDisplayName?: string | null;
  createdAt: Timestamp | FirebaseFirestore.FieldValue;
  closedAt?: Timestamp | FirebaseFirestore.FieldValue | null;
}

/**
 * Validador de esquema documental para marketListings (TDD 5.8b y 7.2).
 */
export function isValidMarketListingData(data: any): boolean {
  if (!data || typeof data !== "object") return false;

  const requiredFields = [
    "listingId",
    "sellerUid",
    "sellerDisplayName",
    "cardId",
    "cardName",
    "rarity",
    "quantity",
    "pricePerCard",
    "status",
  ];

  for (const field of requiredFields) {
    if (!(field in data)) return false;
  }

  if (typeof data.listingId !== "string" || !data.listingId.trim()) return false;
  if (typeof data.sellerUid !== "string" || !data.sellerUid.trim()) return false;
  if (typeof data.cardId !== "string" || !data.cardId.trim()) return false;
  if (typeof data.quantity !== "number" || data.quantity <= 0 || !Number.isInteger(data.quantity)) return false;
  if (typeof data.pricePerCard !== "number" || data.pricePerCard <= 0 || !Number.isInteger(data.pricePerCard)) return false;

  const validStatuses: MarketListingStatus[] = ["activo", "vendido", "cancelado"];
  if (!validStatuses.includes(data.status)) return false;

  return true;
}
