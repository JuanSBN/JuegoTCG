using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using JuegoTCG.Cards;
using JuegoTCG.Networking;

namespace JuegoTCG.Social
{
    [Serializable]
    public class MarketListingData
    {
        public string listingId;
        public string sellerUid;
        public string sellerDisplayName;
        public string cardId;
        public string cardName;
        public string initials;
        public string rarity;
        public int quantity = 1;
        public int pricePerCard;
        public string status = "activo"; // activo, vendido, cancelado
        public string buyerUid;
        public string buyerDisplayName;
        public string timeAgo = "Reciente";
        public bool isMine;
    }

    [Serializable]
    public class DuplicateCardInfo
    {
        public string cardId;
        public string cardName;
        public string initials;
        public string rarity;
        public int totalOwned;
        public int duplicatesAvailable; // copias sobrantes después de la primera
        public int defaultPrice;
    }

    [Serializable]
    public class MarketOperationResult
    {
        public bool success;
        public string message;
        public string listingId;

        public MarketOperationResult(bool success, string message, string listingId = "")
        {
            this.success = success;
            this.message = message;
            this.listingId = listingId;
        }
    }

    /// <summary>
    /// Servicio singleton para el Mercado de Cartas entre Jugadores (Fase 8.5).
    /// Implementa el diseño del TDD Sección 2.11 y 5.8b:
    /// - RESERVA AL PUBLICAR: Descuenta y aparta la carta al crear el listado.
    /// - PRECIO LIBRE: Fijado por el vendedor en monedas del juego, sin comisión del estudio (GDD 7.1).
    /// - REINTEGRO AL CANCELAR: Devuelve la carta al inventario del vendedor.
    /// - TRANSACCIÓN ATÓMICA AL COMPRAR: Descuenta monedas, entrega la carta y liquida al vendedor.
    /// </summary>
    public class MarketService : MonoBehaviour
    {
        public static MarketService Instance { get; private set; }

        public event Action OnMarketUpdated;
        public event Action<MarketListingData> OnListingPublished;
        public event Action<MarketListingData> OnListingPurchased;
        public event Action<MarketListingData> OnListingCancelled;
        public event Action<MarketListingData, int, int> OnListingPriceUpdated;

        private readonly List<MarketListingData> publicListings = new List<MarketListingData>();
        private readonly List<MarketListingData> myListings = new List<MarketListingData>();
        private readonly HashSet<string> paidSellerListings = new HashSet<string>();

        public IReadOnlyList<MarketListingData> PublicListings => publicListings;
        public IReadOnlyList<MarketListingData> MyListings => myListings;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var go = new GameObject("[MarketService]");
                Instance = go.AddComponent<MarketService>();
                DontDestroyOnLoad(go);
            }
        }

        /// <summary>
        /// Sincroniza en tiempo real los listados públicos y propios desde Firestore.
        /// </summary>
        public async Task RefreshCloudMarketAsync()
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsAuthenticated) return;
            string token = FirebaseAuthManager.Instance.IdToken;
            string myUid = FirebaseAuthManager.Instance.UserId;
            if (string.IsNullOrEmpty(token)) return;

            try
            {
                // 1. Cargar listados públicos activos
                var activeCloud = await FirebaseRestClient.GetActiveMarketListingsAsync(token, myUid);
                if (activeCloud != null)
                {
                    publicListings.Clear();
                    publicListings.AddRange(activeCloud);
                }

                // 2. Cargar mis publicaciones para ver si se vendió alguna
                if (!string.IsNullOrEmpty(myUid))
                {
                    var myCloud = await FirebaseRestClient.GetMyMarketListingsAsync(token, myUid);
                    if (myCloud != null)
                    {
                        myListings.Clear();
                        foreach (var item in myCloud)
                        {
                            myListings.Add(item);

                            // Si se vendió una publicación propia y aún no liquidamos las monedas al vendedor:
                            if (item.status == "vendido" && !paidSellerListings.Contains(item.listingId))
                            {
                                paidSellerListings.Add(item.listingId);
                                int revenue = item.pricePerCard * Mathf.Max(1, item.quantity);
                                FirebaseAuthManager.Instance.AddCoins(revenue);
                                Debug.Log($"<color=green>[MarketService] ¡Tu carta {item.cardName} fue comprada por {item.buyerDisplayName}! Recibiste +{revenue} monedas.</color>");
                                OnListingPurchased?.Invoke(item);
                            }
                        }
                    }
                }

                OnMarketUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MarketService] Error sincronizando mercado en la nube: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los listados públicos activos con filtro opcional de rareza.
        /// </summary>
        public List<MarketListingData> GetActiveListings(string rarityFilter = "Todas")
        {
            if (string.IsNullOrEmpty(rarityFilter) || rarityFilter == "Todas")
            {
                return new List<MarketListingData>(publicListings.FindAll(x => x.status == "activo"));
            }

            string filterNorm = rarityFilter.Trim().ToLowerInvariant();
            return new List<MarketListingData>(publicListings.FindAll(x =>
                x.status == "activo" && x.rarity.ToLowerInvariant() == filterNorm));
        }

        /// <summary>
        /// Obtiene los listados propios del jugador actual que siguen activos.
        /// </summary>
        public List<MarketListingData> GetMyActiveListings()
        {
            return new List<MarketListingData>(myListings.FindAll(x => x.status == "activo"));
        }

        /// <summary>
        /// Obtiene las cartas duplicadas que posee el jugador listas para vender o publicar (TDD 2.11).
        /// Una carta es duplicada si tiene 2 o más copias (totalOwned > 1) según el catálogo oficial del Álbum Piloto.
        /// </summary>
        public List<DuplicateCardInfo> GetMyDuplicateCards()
        {
            var duplicates = new List<DuplicateCardInfo>();
            PlayerCollectionManager.EnsureExists();
            var colMgr = PlayerCollectionManager.Instance;
            var catalog = colMgr != null ? colMgr.GetCatalog() : null;

            if (catalog == null || catalog.Count == 0) return duplicates;

            foreach (var card in catalog)
            {
                int count = colMgr.GetOwnedCount(card.cardId);
                if (count <= 1) continue;

                duplicates.Add(new DuplicateCardInfo
                {
                    cardId = card.cardId,
                    cardName = card.playerName,
                    initials = card.initials,
                    rarity = card.rarity.ToString(),
                    totalOwned = count,
                    duplicatesAvailable = count - 1,
                    defaultPrice = GetSuggestedPrice(card.rarity)
                });
            }

            return duplicates;
        }

        public static int GetSuggestedPrice(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica: return 700;
                case Rarity.Legendaria: return 350;
                case Rarity.Epica: return 150;
                case Rarity.Especial: return 70;
                case Rarity.Comun:
                default: return 30;
            }
        }

        /// <summary>
        /// Publica una carta duplicada en el mercado (listCardForSale).
        /// RESERVA ATÓMICA (TDD 2.11): Descuenta y aparta la carta de PlayerCollectionManager inmediatamente.
        /// </summary>
        public async Task<MarketOperationResult> ListCardForSaleAsync(string cardId, int pricePerCard, int quantity = 1)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return new MarketOperationResult(false, "El identificador de la carta es inválido.");
            }

            if (pricePerCard <= 0)
            {
                return new MarketOperationResult(false, "El precio fijado debe ser mayor a 0 monedas.");
            }

            if (quantity <= 0) quantity = 1;

            PlayerCollectionManager.EnsureExists();
            var colMgr = PlayerCollectionManager.Instance;
            int ownedCount = colMgr != null ? colMgr.GetOwnedCount(cardId) : 0;

            if (ownedCount < quantity)
            {
                return new MarketOperationResult(false, $"No posees suficientes copias de esta carta (Tienes: {ownedCount}, Solicitadas: {quantity}).");
            }

            // 1. RESERVA ATÓMICA: Descontar carta de la colección local
            colMgr.RemoveCard(cardId, quantity);

            string listingId = "list_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            string sellerUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "me";
            string sellerName = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.DisplayName : "Tú";

            var cardInfo = colMgr.GetCatalog().Find(c => c.cardId == cardId);
            string cardName = cardInfo != null ? cardInfo.playerName : $"Carta_{cardId}";
            string initials = cardInfo != null ? cardInfo.initials : cardId.Substring(0, 2).ToUpper();
            string rarity = cardInfo != null ? cardInfo.rarity.ToString() : "Comun";

            var newListing = new MarketListingData
            {
                listingId = listingId,
                sellerUid = sellerUid,
                sellerDisplayName = sellerName,
                cardId = cardId,
                cardName = cardName,
                initials = initials,
                rarity = rarity,
                quantity = quantity,
                pricePerCard = pricePerCard,
                status = "activo",
                isMine = true,
                timeAgo = "Ahora"
            };

            // 2. Guardar en Firestore si hay sesión autenticada
            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            if (!string.IsNullOrEmpty(token))
            {
                bool cloudOk = await FirebaseRestClient.CreateMarketListingAsync(token, newListing);
                if (!cloudOk)
                {
                    // Si falla la nube, revertir la deducción
                    colMgr.AddCard(cardId, quantity);
                    return new MarketOperationResult(false, "Error al publicar la carta en el mercado de la nube.");
                }
            }

            myListings.Insert(0, newListing);
            publicListings.Insert(0, newListing);
            OnMarketUpdated?.Invoke();
            OnListingPublished?.Invoke(newListing);

            Debug.Log($"<color=green>[MarketService] ¡Listado {listingId} publicado! {quantity}x {cardName} por {pricePerCard} monedas.</color>");
            return new MarketOperationResult(true, $"¡Has publicado a {cardName} por {pricePerCard} monedas!", listingId);
        }

        /// <summary>
        /// Compra una carta listada en el mercado (buyListedCard).
        /// TRANSACCIÓN ATÓMICA (TDD 2.11, GDD 7.1):
        /// - Valida status activo y monedas del comprador
        /// - Descuenta monedas al comprador
        /// - Acredita la carta en la colección del comprador
        /// - Actualiza en Firestore a 'vendido'
        /// </summary>
        public async Task<MarketOperationResult> BuyListedCardAsync(string listingId)
        {
            if (string.IsNullOrEmpty(listingId))
            {
                return new MarketOperationResult(false, "El identificador del listado es inválido.");
            }

            var listing = publicListings.Find(x => x.listingId == listingId);
            if (listing == null)
            {
                listing = myListings.Find(x => x.listingId == listingId);
            }

            if (listing == null)
            {
                return new MarketOperationResult(false, "El listado de mercado no existe.");
            }

            if (listing.status != "activo")
            {
                return new MarketOperationResult(false, $"El listado ya no está disponible (Estado: {listing.status}).");
            }

            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "me";
            string myDisplayName = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.DisplayName : "Comprador";

            if (!string.IsNullOrEmpty(myUid) && listing.sellerUid == myUid)
            {
                return new MarketOperationResult(false, "No puedes comprar tus propias cartas publicadas.");
            }

            int totalPrice = listing.pricePerCard * Math.Max(1, listing.quantity);
            int currentCoins = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.Coins : 0;

            if (currentCoins < totalPrice)
            {
                return new MarketOperationResult(false, $"Monedas insuficientes. Requiere {totalPrice} monedas (Tienes: {currentCoins}).");
            }

            // 1. Descontar monedas
            FirebaseAuthManager.Instance.AddCoins(-totalPrice);

            // 2. Actualizar en la nube a 'vendido'
            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            if (!string.IsNullOrEmpty(token))
            {
                bool cloudOk = await FirebaseRestClient.BuyMarketListingAsync(token, listingId, myUid, myDisplayName);
                if (!cloudOk)
                {
                    // Revertir descuento de monedas si falla la nube
                    FirebaseAuthManager.Instance.AddCoins(totalPrice);
                    return new MarketOperationResult(false, "Error al procesar la compra en la nube.");
                }
            }

            // 3. Acreditar carta al comprador
            PlayerCollectionManager.EnsureExists();
            PlayerCollectionManager.Instance.AddCard(listing.cardId, listing.quantity);

            // 4. Actualizar estado local
            listing.status = "vendido";
            listing.buyerUid = myUid;
            listing.buyerDisplayName = myDisplayName;
            publicListings.Remove(listing);

            OnMarketUpdated?.Invoke();
            OnListingPurchased?.Invoke(listing);

            Debug.Log($"<color=green>[MarketService] ¡Compra exitosa! Adquiriste {listing.quantity}x {listing.cardName} por {totalPrice} monedas.</color>");
            return new MarketOperationResult(true, $"¡Has adquirido a {listing.cardName} por {totalPrice} monedas!", listingId);
        }

        /// <summary>
        /// Cancela un listado propio activo en el mercado (cancelListing).
        /// REINTEGRO ATÓMICO (TDD 2.11): Devuelve la carta reservada al inventario del jugador.
        /// </summary>
        public async Task<MarketOperationResult> CancelListingAsync(string listingId)
        {
            if (string.IsNullOrEmpty(listingId))
            {
                return new MarketOperationResult(false, "El identificador del listado es inválido.");
            }

            var listing = myListings.Find(x => x.listingId == listingId);
            if (listing == null)
            {
                listing = publicListings.Find(x => x.listingId == listingId);
            }

            if (listing == null)
            {
                return new MarketOperationResult(false, "El listado de mercado no existe.");
            }

            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "me";
            if (!listing.isMine && listing.sellerUid != myUid)
            {
                return new MarketOperationResult(false, "Solo el vendedor que publicó la carta puede retirarla.");
            }

            if (listing.status != "activo")
            {
                return new MarketOperationResult(false, $"No se puede cancelar el listado porque su estado es '{listing.status}'.");
            }

            // 1. Actualizar en Firestore
            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            if (!string.IsNullOrEmpty(token))
            {
                await FirebaseRestClient.CancelMarketListingAsync(token, listingId);
            }

            // 2. Reintegrar carta a la colección
            PlayerCollectionManager.EnsureExists();
            PlayerCollectionManager.Instance.AddCard(listing.cardId, listing.quantity);

            listing.status = "cancelado";
            publicListings.Remove(listing);

            OnMarketUpdated?.Invoke();
            OnListingCancelled?.Invoke(listing);

            Debug.Log($"<color=yellow>[MarketService] Reintegrada carta {listing.cardName} ({listing.quantity}x) al inventario del jugador.</color>");
            return new MarketOperationResult(true, $"Has retirado {listing.cardName} del mercado. La carta ha sido devuelta a tu colección.", listingId);
        }

        /// <summary>
        /// Actualiza el precio por carta de un listado propio activo (updateListingPrice).
        /// </summary>
        public async Task<MarketOperationResult> UpdateListingPriceAsync(string listingId, int newPrice)
        {
            if (string.IsNullOrEmpty(listingId))
            {
                return new MarketOperationResult(false, "El identificador del listado es inválido.");
            }

            if (newPrice <= 0)
            {
                return new MarketOperationResult(false, "El precio fijado debe ser mayor a 0 monedas.");
            }

            var listing = myListings.Find(x => x.listingId == listingId);
            if (listing == null)
            {
                listing = publicListings.Find(x => x.listingId == listingId);
            }

            if (listing == null)
            {
                return new MarketOperationResult(false, "El listado de mercado no existe.");
            }

            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "me";
            if (!listing.isMine && listing.sellerUid != myUid)
            {
                return new MarketOperationResult(false, "Solo el vendedor que publicó la carta puede modificar su precio.");
            }

            if (listing.status != "activo")
            {
                return new MarketOperationResult(false, $"No se puede modificar el precio porque el listado está '{listing.status}'.");
            }

            int oldPrice = listing.pricePerCard;

            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            if (!string.IsNullOrEmpty(token))
            {
                await FirebaseRestClient.UpdateMarketListingPriceAsync(token, listingId, newPrice);
            }

            listing.pricePerCard = newPrice;
            OnMarketUpdated?.Invoke();
            OnListingPriceUpdated?.Invoke(listing, oldPrice, newPrice);

            Debug.Log($"<color=cyan>[MarketService] Precio de {listing.cardName} actualizado a {newPrice} monedas.</color>");
            return new MarketOperationResult(true, $"Precio actualizado a {newPrice} monedas.", listingId);
        }
    }
}
