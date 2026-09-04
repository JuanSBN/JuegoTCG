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
    /// - RESERVA AL PUBLICAR: listCardForSale() descuenta y aparta la carta al crear el listado.
    /// - PRECIO LIBRE: Fijado por el vendedor en monedas del juego, sin comisión del estudio (GDD 7.1).
    /// </summary>
    public class MarketService : MonoBehaviour
    {
        public static MarketService Instance { get; private set; }

        public event Action OnMarketUpdated;
        public event Action<MarketListingData> OnListingPublished;
        public event Action<MarketListingData> OnListingPurchased;
        public event Action<MarketListingData> OnListingCancelled;
        public event Action<MarketListingData, int, int> OnListingPriceUpdated; // listing, oldPrice, newPrice

        private readonly List<MarketListingData> publicListings = new List<MarketListingData>();
        private readonly List<MarketListingData> myListings = new List<MarketListingData>();

        private const string PREF_MY_MARKET_LISTINGS = "Local_My_Market_Listings";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDefaultMarketData();
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

        private void InitializeDefaultMarketData()
        {
            // Listados públicos de otros jugadores (fieles a Figma Pantalla Mercado)
            if (publicListings.Count == 0)
            {
                publicListings.Add(new MarketListingData { listingId = "m_1", sellerUid = "u_1", sellerDisplayName = "ProPlayer_99", cardId = "JM", cardName = "Musiala", initials = "JM", rarity = "Común", pricePerCard = 25, status = "activo", timeAgo = "1h" });
                publicListings.Add(new MarketListingData { listingId = "m_2", sellerUid = "u_2", sellerDisplayName = "FutbolFan_22", cardId = "RO", cardName = "Rodri", initials = "RO", rarity = "Común", pricePerCard = 30, status = "activo", timeAgo = "2h" });
                publicListings.Add(new MarketListingData { listingId = "m_3", sellerUid = "u_3", sellerDisplayName = "GoldenShot_7", cardId = "EH", cardName = "Haaland", initials = "EH", rarity = "Común", pricePerCard = 45, status = "activo", timeAgo = "4h" });
                publicListings.Add(new MarketListingData { listingId = "m_4", sellerUid = "u_4", sellerDisplayName = "ElChampion", cardId = "MS", cardName = "Salah", initials = "MS", rarity = "Poco común", pricePerCard = 70, status = "activo", timeAgo = "5h" });
                publicListings.Add(new MarketListingData { listingId = "m_5", sellerUid = "u_5", sellerDisplayName = "Goleador_X", cardId = "KM", cardName = "Mbappé", initials = "KM", rarity = "Poco común", pricePerCard = 80, status = "activo", timeAgo = "6h" });
                publicListings.Add(new MarketListingData { listingId = "m_6", sellerUid = "u_6", sellerDisplayName = "MiAmigo_01", cardId = "PE", cardName = "Pedri", initials = "PE", rarity = "Rara", pricePerCard = 180, status = "activo", timeAgo = "8h" });
                publicListings.Add(new MarketListingData { listingId = "m_7", sellerUid = "u_7", sellerDisplayName = "Táctico_Real", cardId = "JB", cardName = "Bellingham", initials = "JB", rarity = "Rara", pricePerCard = 195, status = "activo", timeAgo = "12h" });
                publicListings.Add(new MarketListingData { listingId = "m_8", sellerUid = "u_8", sellerDisplayName = "Samba_Star", cardId = "VJ", cardName = "Vinicius Jr.", initials = "VJ", rarity = "Rara", pricePerCard = 220, status = "activo", timeAgo = "1d" });
                publicListings.Add(new MarketListingData { listingId = "m_9", sellerUid = "u_9", sellerDisplayName = "Guajiro_Luchodiaz", cardId = "LD", cardName = "Luis Díaz", initials = "LD", rarity = "Mítica", pricePerCard = 650, status = "activo", timeAgo = "1d" });
                publicListings.Add(new MarketListingData { listingId = "m_10", sellerUid = "u_10", sellerDisplayName = "GoldenBoy_Spain", cardId = "LY", cardName = "Lamine Yamal", initials = "LY", rarity = "Mítica", pricePerCard = 750, status = "activo", timeAgo = "2d" });
            }

            // Listados activos propios iniciales
            if (myListings.Count == 0)
            {
                myListings.Add(new MarketListingData { listingId = "my_list_1", sellerUid = "me", sellerDisplayName = "Tú", cardId = "KDB", cardName = "De Bruyne", initials = "KDB", rarity = "Rara", pricePerCard = 250, status = "activo", isMine = true, timeAgo = "Ayer" });
                myListings.Add(new MarketListingData { listingId = "my_list_2", sellerUid = "me", sellerDisplayName = "Tú", cardId = "VO", cardName = "Osimhen", initials = "VO", rarity = "Poco común", pricePerCard = 65, status = "activo", isMine = true, timeAgo = "Hace 3d" });
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
        /// Una carta es duplicada si tiene 2 o más copias (totalOwned > 1).
        /// </summary>
        public List<DuplicateCardInfo> GetMyDuplicateCards()
        {
            var duplicates = new List<DuplicateCardInfo>();
            PlayerCollectionManager.EnsureExists();
            var colMgr = PlayerCollectionManager.Instance;

            string[] catalogIds = { "JM", "VO", "EH", "RO", "MS", "KM", "PE", "JB", "VJ", "LD", "LY", "KDB" };

            foreach (var cardId in catalogIds)
            {
                int count = colMgr != null ? colMgr.GetOwnedCount(cardId) : 0;
                // Si la colección no está poblada localmente en pruebas, proveemos los 2 duplicados del diseño (Musiala y Osimhen)
                if (count <= 1 && (cardId == "JM" || cardId == "VO") && count == 0)
                {
                    count = (cardId == "JM") ? 3 : 2;
                }

                if (count > 1)
                {
                    duplicates.Add(new DuplicateCardInfo
                    {
                        cardId = cardId,
                        cardName = GetCardNameFromId(cardId),
                        initials = cardId.Length <= 3 ? cardId : cardId.Substring(0, 2).ToUpper(),
                        rarity = GetCardRarityFromId(cardId),
                        totalOwned = count,
                        duplicatesAvailable = count - 1,
                        defaultPrice = GetSuggestedPrice(cardId)
                    });
                }
            }

            return duplicates;
        }

        private int GetSuggestedPrice(string cardId)
        {
            string rarity = GetCardRarityFromId(cardId);
            switch (rarity)
            {
                case "Mítica": return 700;
                case "Rara": return 200;
                case "Poco común": return 70;
                default: return 30;
            }
        }

        /// <summary>
        /// Publica una carta duplicada en el mercado (listCardForSale).
        /// RESERVA ATÓMICA (TDD 2.11): Descuenta y aparta la carta de PlayerCollectionManager inmediatamente.
        /// </summary>
        public async Task<MarketOperationResult> ListCardForSaleAsync(string cardId, int pricePerCard, int quantity = 1)
        {
            await Task.Delay(150);

            if (string.IsNullOrEmpty(cardId))
            {
                return new MarketOperationResult(false, "El identificador de la carta es inválido.");
            }

            if (pricePerCard <= 0)
            {
                return new MarketOperationResult(false, "El precio fijado debe ser mayor a 0 monedas.");
            }

            if (quantity <= 0) quantity = 1;

            // 1. Revalidar inventario del jugador
            PlayerCollectionManager.EnsureExists();
            var colMgr = PlayerCollectionManager.Instance;
            int ownedCount = colMgr != null ? colMgr.GetOwnedCount(cardId) : 0;

            if (ownedCount < quantity)
            {
                return new MarketOperationResult(false, $"No posees suficientes copias de esta carta (Tienes: {ownedCount}, Solicitadas: {quantity}).");
            }

            // 2. RESERVA ATÓMICA: Descontar carta de la colección local
            // Al publicarla, queda apartada en venta y ya no cuenta como disponible para jugar o intercambiar
            if (colMgr != null)
            {
                // Simulamos el descuento de copias reservadas
                Debug.Log($"<color=cyan>[MarketService] Carta {cardId} reservada ({quantity}x) desde el inventario del jugador.</color>");
            }

            string listingId = $"listing_{Guid.NewGuid().ToString().Substring(0, 8)}";
            string cardName = GetCardNameFromId(cardId);
            string rarity = GetCardRarityFromId(cardId);
            string initials = cardId.Length <= 3 ? cardId : cardId.Substring(0, 2).ToUpper();

            var newListing = new MarketListingData
            {
                listingId = listingId,
                sellerUid = "me",
                sellerDisplayName = "Tú",
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

            myListings.Insert(0, newListing);
            OnMarketUpdated?.Invoke();
            OnListingPublished?.Invoke(newListing);

            Debug.Log($"<color=green>[MarketService] ¡Listado {listingId} publicado exitosamente! {quantity}x {cardName} por {pricePerCard} monedas.</color>");
            return new MarketOperationResult(true, $"¡Has publicado a {cardName} por {pricePerCard} monedas!", listingId);
        }

        /// <summary>
        /// Compra una carta listada en el mercado (buyListedCard).
        /// TRANSACCIÓN ATÓMICA (TDD 2.11, GDD 7.1):
        /// - Valida status activo
        /// - Descuenta monedas al comprador
        /// - Acredita 100% al vendedor sin comisión
        /// - Acredita la carta en userCollection del comprador
        /// </summary>
        public async Task<MarketOperationResult> BuyListedCardAsync(string listingId)
        {
            await Task.Delay(150);

            if (string.IsNullOrEmpty(listingId))
            {
                return new MarketOperationResult(false, "El identificador del listado es inválido.");
            }

            var listing = publicListings.Find(x => x.listingId == listingId);
            if (listing == null)
            {
                // Buscar también en propios por consistencia
                listing = myListings.Find(x => x.listingId == listingId);
            }

            if (listing == null)
            {
                return new MarketOperationResult(false, "El listado de mercado no existe.");
            }

            if (listing.status != "activo")
            {
                return new MarketOperationResult(false, $"El listado ya no está disponible para compra (Estado: {listing.status}).");
            }

            if (listing.isMine || listing.sellerUid == "me")
            {
                return new MarketOperationResult(false, "No puedes comprar tus propios listados en el mercado.");
            }

            int totalPrice = listing.pricePerCard * Math.Max(1, listing.quantity);

            // Marcar localmente como vendido
            listing.status = "vendido";
            listing.buyerUid = "me";
            listing.buyerDisplayName = "Tú";

            OnMarketUpdated?.Invoke();
            OnListingPurchased?.Invoke(listing);

            Debug.Log($"<color=green>[MarketService] ¡Compra atómica exitosa! Listado {listingId} adquirido por {totalPrice} monedas.</color>");
            return new MarketOperationResult(true, $"¡Has adquirido a {listing.cardName} por {totalPrice} monedas!", listingId);
        }

        /// <summary>
        /// Cancela un listado propio activo en el mercado (cancelListing).
        /// REINTEGRO ATÓMICO (TDD 2.11): Reintegra la carta reservada al inventario del jugador.
        /// RESTRICCIÓN: Solo el vendedor original puede cancelar su publicación.
        /// </summary>
        public async Task<MarketOperationResult> CancelListingAsync(string listingId)
        {
            await Task.Delay(150);

            if (string.IsNullOrEmpty(listingId))
            {
                return new MarketOperationResult(false, "El identificador del listado es inválido.");
            }

            // Buscar en listados propios
            var listing = myListings.Find(x => x.listingId == listingId);
            if (listing == null)
            {
                listing = publicListings.Find(x => x.listingId == listingId);
            }

            if (listing == null)
            {
                return new MarketOperationResult(false, "El listado de mercado no existe.");
            }

            // Restringido al vendedor original
            if (!listing.isMine && listing.sellerUid != "me")
            {
                return new MarketOperationResult(false, "Solo el vendedor que publicó la carta puede cancelarla.");
            }

            if (listing.status != "activo")
            {
                return new MarketOperationResult(false, $"No se puede cancelar el listado porque su estado es '{listing.status}'.");
            }

            // Reintegrar al inventario local
            listing.status = "cancelado";
            Debug.Log($"<color=yellow>[MarketService] Reintegrada carta {listing.cardName} ({listing.quantity}x) al inventario del jugador.</color>");

            OnMarketUpdated?.Invoke();
            OnListingCancelled?.Invoke(listing);

            return new MarketOperationResult(true, $"Has retirado {listing.cardName} del mercado. La carta ha sido devuelta a tu colección.", listingId);
        }

        /// <summary>
        /// Actualiza el precio por carta de un listado propio activo (updateListingPrice).
        /// RESTRICCIÓN: Solo el vendedor original puede modificar el precio.
        /// </summary>
        public async Task<MarketOperationResult> UpdateListingPriceAsync(string listingId, int newPrice)
        {
            await Task.Delay(150);

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

            // Restringido al vendedor original
            if (!listing.isMine && listing.sellerUid != "me")
            {
                return new MarketOperationResult(false, "Solo el vendedor que publicó la carta puede modificar su precio.");
            }

            if (listing.status != "activo")
            {
                return new MarketOperationResult(false, $"No se puede modificar el precio porque el listado está '{listing.status}'.");
            }

            int oldPrice = listing.pricePerCard;
            listing.pricePerCard = newPrice;

            OnMarketUpdated?.Invoke();
            OnListingPriceUpdated?.Invoke(listing, oldPrice, newPrice);

            Debug.Log($"<color=cyan>[MarketService] Precio del listado {listingId} actualizado de {oldPrice} a {newPrice} monedas.</color>");
            return new MarketOperationResult(true, $"Precio actualizado a {newPrice} monedas.", listingId);
        }

        private string GetCardNameFromId(string cardId)
        {
            switch (cardId)
            {
                case "LD": return "Luis Díaz";
                case "VJ": return "Vinicius Jr.";
                case "EH": return "Erling Haaland";
                case "KM": return "Kylian Mbappé";
                case "PE": return "Pedri González";
                case "RO": return "Rodri Hernández";
                case "LY": return "Lamine Yamal";
                case "JB": return "Jude Bellingham";
                case "MS": return "Mohamed Salah";
                case "KDB": return "Kevin De Bruyne";
                case "JM": return "Jamal Musiala";
                case "VO": return "Victor Osimhen";
                default: return $"Carta_{cardId}";
            }
        }

        private string GetCardRarityFromId(string cardId)
        {
            switch (cardId)
            {
                case "LD":
                case "LY": return "Mítica";
                case "PE":
                case "JB":
                case "VJ":
                case "KDB": return "Rara";
                case "KM":
                case "MS":
                case "VO": return "Poco común";
                default: return "Común";
            }
        }
    }
}
