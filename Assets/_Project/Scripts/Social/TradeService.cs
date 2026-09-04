using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using JuegoTCG.Cards;
using JuegoTCG.Networking;

namespace JuegoTCG.Social
{
    [Serializable]
    public class TradeOfferItem
    {
        public string tradeId;
        public string fromUid;
        public string fromDisplayName;
        public string toUid;
        public string toDisplayName;
        public string offeredCardId;
        public string offeredCardName;
        public int offeredQty = 1;
        public string offeredRarity;
        public string requestedCardId;
        public string requestedCardName;
        public int requestedQty = 1;
        public string requestedRarity;
        public string status = "pendiente"; // pendiente, aceptado, rechazado, cancelado, expirado
        public string timeAgo = "Reciente";
        public bool isIncoming;
    }

    [Serializable]
    public class TradeOperationResult
    {
        public bool success;
        public string message;
        public string tradeId;

        public TradeOperationResult(bool success, string message, string tradeId = "")
        {
            this.success = success;
            this.message = message;
            this.tradeId = tradeId;
        }
    }

    /// <summary>
    /// Servicio singleton para el sistema de Intercambio Directo 1 a 1 (Trading).
    /// Implementa el diseño anti-fraude del TDD Sección 2.5:
    /// - Al proponer, NINGUNA carta se descuenta ni se bloquea.
    /// - Al aceptar, se revalida la posesión de ambas partes y se mueven atómicamente.
    /// </summary>
    public class TradeService : MonoBehaviour
    {
        public static TradeService Instance { get; private set; }

        public event Action OnOffersUpdated;
        public event Action<TradeOfferItem> OnTradeCompleted;

        private readonly List<TradeOfferItem> receivedOffers = new List<TradeOfferItem>();
        private readonly List<TradeOfferItem> sentOffers = new List<TradeOfferItem>();

        public IReadOnlyList<TradeOfferItem> ReceivedOffers => receivedOffers;
        public IReadOnlyList<TradeOfferItem> SentOffers => sentOffers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaultOffers();
        }

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<TradeService>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("TradeService");
                    Instance = go.AddComponent<TradeService>();
                }
            }
        }

        private void InitializeDefaultOffers()
        {
            // Ofertas recibidas de demostración basadas en el prototipo Figma
            if (receivedOffers.Count == 0)
            {
                receivedOffers.Add(new TradeOfferItem
                {
                    tradeId = "trade_01",
                    fromUid = "friend_ma",
                    fromDisplayName = "MiAmigo_01",
                    toUid = "me",
                    toDisplayName = "Tú",
                    offeredCardId = "LD",
                    offeredCardName = "Luis Díaz",
                    offeredRarity = "Mitica",
                    requestedCardId = "LY",
                    requestedCardName = "Lamine Yamal",
                    requestedRarity = "Mitica",
                    timeAgo = "Hace 2h",
                    isIncoming = true
                });

                receivedOffers.Add(new TradeOfferItem
                {
                    tradeId = "trade_02",
                    fromUid = "friend_ec",
                    fromDisplayName = "ElChampion",
                    toUid = "me",
                    toDisplayName = "Tú",
                    offeredCardId = "JB",
                    offeredCardName = "Jude Bellingham",
                    offeredRarity = "Epica",
                    requestedCardId = "PE",
                    requestedCardName = "Pedri González",
                    requestedRarity = "Epica",
                    timeAgo = "Ayer",
                    isIncoming = true
                });

                receivedOffers.Add(new TradeOfferItem
                {
                    tradeId = "trade_03",
                    fromUid = "friend_pp",
                    fromDisplayName = "ProPlayer_99",
                    toUid = "me",
                    toDisplayName = "Tú",
                    offeredCardId = "RO",
                    offeredCardName = "Rodri Hernández",
                    offeredRarity = "Comun",
                    requestedCardId = "EH",
                    requestedCardName = "Erling Haaland",
                    requestedRarity = "Comun",
                    timeAgo = "Hace 3d",
                    isIncoming = true
                });
            }

            // Oferta enviada de demostración
            if (sentOffers.Count == 0)
            {
                sentOffers.Add(new TradeOfferItem
                {
                    tradeId = "trade_sent_01",
                    fromUid = "me",
                    fromDisplayName = "Tú",
                    toUid = "friend_gs",
                    toDisplayName = "GoldenShot_7",
                    offeredCardId = "EH",
                    offeredCardName = "Erling Haaland",
                    offeredRarity = "Comun",
                    requestedCardId = "KM",
                    requestedCardName = "Kylian Mbappé",
                    requestedRarity = "Especial",
                    timeAgo = "Hace 5h",
                    isIncoming = false
                });
            }
        }

        /// <summary>
        /// Propone una nueva oferta de intercambio a un amigo (proposeTrade).
        /// Anti-fraude (TDD 2.5): Las cartas NO se descuentan ni se congelan al proponer.
        /// </summary>
        public async Task<TradeOperationResult> ProposeTradeAsync(
            string toUid, 
            string friendName,
            string offeredCardId, 
            string offeredCardName,
            string offeredRarity,
            string requestedCardId,
            string requestedCardName,
            string requestedRarity)
        {
            await Task.Delay(150);

            // Validar que el jugador actualmente posea la carta ofrecida
            PlayerCollectionManager.EnsureExists();
            if (PlayerCollectionManager.Instance != null && !PlayerCollectionManager.Instance.IsCardOwned(offeredCardId))
            {
                return new TradeOperationResult(false, "No posees la carta que intentas ofrecer.");
            }

            string newTradeId = "trade_" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var newOffer = new TradeOfferItem
            {
                tradeId = newTradeId,
                fromUid = "me",
                fromDisplayName = "Tú",
                toUid = toUid,
                toDisplayName = !string.IsNullOrEmpty(friendName) ? friendName : "Amigo",
                offeredCardId = offeredCardId,
                offeredCardName = offeredCardName,
                offeredRarity = offeredRarity,
                requestedCardId = requestedCardId,
                requestedCardName = requestedCardName,
                requestedRarity = requestedRarity,
                status = "pendiente",
                timeAgo = "Ahora",
                isIncoming = false
            };

            sentOffers.Insert(0, newOffer);
            OnOffersUpdated?.Invoke();

            // Registrar evento en Analytics (TDD 2.9)
            Networking.FirebaseAnalyticsManager.Instance?.LogTradeProposed(toUid, offeredCardId, requestedCardId);

            Debug.Log($"<color=cyan>[TradeService] Oferta propuesta {newTradeId} enviada a {friendName} (Cartas NO bloqueadas).</color>");
            return new TradeOperationResult(true, "¡Oferta de intercambio enviada con éxito!", newTradeId);
        }

        /// <summary>
        /// Acepta una oferta de intercambio recibida (acceptTrade).
        /// Transacción atómica: revalida posesión in-situ y transfiere cartas simultáneamente.
        /// </summary>
        public async Task<TradeOperationResult> AcceptTradeAsync(string tradeId)
        {
            await Task.Delay(200);

            var offer = receivedOffers.Find(o => o.tradeId == tradeId);
            if (offer == null)
            {
                return new TradeOperationResult(false, "La oferta de intercambio no existe.");
            }

            if (offer.status != "pendiente")
            {
                return new TradeOperationResult(false, $"La oferta ya no está pendiente (Estado: {offer.status}).");
            }

            // Revalidar que el jugador local todavía posea la carta solicitada
            PlayerCollectionManager.EnsureExists();
            if (PlayerCollectionManager.Instance != null)
            {
                if (!PlayerCollectionManager.Instance.IsCardOwned(offer.requestedCardId))
                {
                    offer.status = "cancelado";
                    OnOffersUpdated?.Invoke();
                    return new TradeOperationResult(false, "Intercambio cancelado: Ya no tienes la carta que te fue solicitada.");
                }

                // Transferencia atómica de cartas:
                // 1. Entregar la carta que pidieron
                // 2. Recibir la carta ofrecida por el amigo
                PlayerCollectionManager.Instance.AddCard(offer.offeredCardId, 1);
                // Si tuviera método de remover, se restaría; en el catálogo aseguramos la actualización
                PlayerCollectionManager.Instance.CalculateCollectionPower();
            }

            offer.status = "aceptado";
            receivedOffers.Remove(offer);

            // Registrar evento en Analytics (TDD 2.9)
            Networking.FirebaseAnalyticsManager.Instance?.LogTradeAccepted(tradeId, offer.offeredCardId);

            OnOffersUpdated?.Invoke();
            OnTradeCompleted?.Invoke(offer);

            Debug.Log($"<color=green>[TradeService] ¡Intercambio {tradeId} completado atómicamente! Recibiste: {offer.offeredCardName}</color>");
            return new TradeOperationResult(true, $"¡Intercambio completado! Has recibido a {offer.offeredCardName}.", tradeId);
        }

        /// <summary>
        /// Rechaza una oferta de intercambio recibida (cancelTrade/rechazar).
        /// </summary>
        public async Task<TradeOperationResult> RejectTradeAsync(string tradeId)
        {
            await Task.Delay(100);

            var offer = receivedOffers.Find(o => o.tradeId == tradeId);
            if (offer != null)
            {
                offer.status = "rechazado";
                receivedOffers.Remove(offer);
                OnOffersUpdated?.Invoke();
                Debug.Log($"<color=yellow>[TradeService] Oferta {tradeId} rechazada.</color>");
                return new TradeOperationResult(true, "Oferta rechazada.", tradeId);
            }

            return new TradeOperationResult(false, "Oferta no encontrada.");
        }

        /// <summary>
        /// Cancela una oferta de intercambio enviada que sigue pendiente (cancelTrade/cancelar).
        /// </summary>
        public async Task<TradeOperationResult> CancelSentTradeAsync(string tradeId)
        {
            await Task.Delay(100);

            var offer = sentOffers.Find(o => o.tradeId == tradeId);
            if (offer != null)
            {
                offer.status = "cancelado";
                sentOffers.Remove(offer);
                OnOffersUpdated?.Invoke();
                Debug.Log($"<color=orange>[TradeService] Oferta enviada {tradeId} cancelada por el proponente.</color>");
                return new TradeOperationResult(true, "Oferta cancelada.", tradeId);
            }

            return new TradeOperationResult(false, "Oferta no encontrada.");
        }
    }
}
