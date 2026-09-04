"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.isValidMarketListingData = isValidMarketListingData;
/**
 * Validador de esquema documental para marketListings (TDD 5.8b y 7.2).
 */
function isValidMarketListingData(data) {
    if (!data || typeof data !== "object")
        return false;
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
        if (!(field in data))
            return false;
    }
    if (typeof data.listingId !== "string" || !data.listingId.trim())
        return false;
    if (typeof data.sellerUid !== "string" || !data.sellerUid.trim())
        return false;
    if (typeof data.cardId !== "string" || !data.cardId.trim())
        return false;
    if (typeof data.quantity !== "number" || data.quantity <= 0 || !Number.isInteger(data.quantity))
        return false;
    if (typeof data.pricePerCard !== "number" || data.pricePerCard <= 0 || !Number.isInteger(data.pricePerCard))
        return false;
    const validStatuses = ["activo", "vendido", "cancelado"];
    if (!validStatuses.includes(data.status))
        return false;
    return true;
}
//# sourceMappingURL=marketTypes.js.map