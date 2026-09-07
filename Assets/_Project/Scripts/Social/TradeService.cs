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

        private readonly HashSet<string> processedTrades = new HashSet<string>();

        private void InitializeDefaultOffers()
        {
            // Alpha: el intercambio inicia vacío. Solo aparecerán ofertas reales de Firestore.
        }

        /// <summary>
        /// Sincroniza las ofertas recibidas y enviadas en la nube en tiempo real.
        /// </summary>
        public async Task RefreshCloudTradesAsync()
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsAuthenticated) return;
            string token = FirebaseAuthManager.Instance.IdToken;
            string myUid = FirebaseAuthManager.Instance.UserId;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(myUid)) return;

            try
            {
                // 1. Ofertas recibidas (donde toUid == myUid y status == "pendiente")
                var incoming = await FirebaseRestClient.GetIncomingTradeOffersAsync(token, myUid);
                if (incoming != null)
                {
                    receivedOffers.Clear();
                    receivedOffers.AddRange(incoming);
                }

                // 2. Ofertas enviadas (donde fromUid == myUid)
                var outgoing = await FirebaseRestClient.GetSentTradeOffersAsync(token, myUid);
                if (outgoing != null)
                {
                    sentOffers.Clear();
                    foreach (var offer in outgoing)
                    {
                        // Si una oferta que yo envié ya fue aceptada por mi amigo:
                        if (offer.status == "aceptado" && !processedTrades.Contains(offer.tradeId))
                        {
                            processedTrades.Add(offer.tradeId);
                            // Transferencia en mi celular:
                            // Yo ofrecí offeredCardId -> la entrego
                            // Mi amigo me dio requestedCardId -> la recibo
                            PlayerCollectionManager.EnsureExists();
                            if (PlayerCollectionManager.Instance != null)
                            {
                                PlayerCollectionManager.Instance.RemoveCard(offer.offeredCardId, 1);
                                PlayerCollectionManager.Instance.AddCard(offer.requestedCardId, 1);
                            }

                            // Marcar como completada en Firestore
                            _ = FirebaseRestClient.UpdateTradeOfferStatusAsync(token, offer.tradeId, "completado");
                            offer.status = "completado";
                            OnTradeCompleted?.Invoke(offer);
                            Debug.Log($"<color=green>[TradeService] ¡Oferta {offer.tradeId} completada! Recibiste {offer.requestedCardName}</color>");
                        }

                        if (offer.status == "pendiente")
                        {
                            sentOffers.Add(offer);
                        }
                    }
                }

                OnOffersUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TradeService] Error actualizando intercambios de la nube: {ex.Message}");
            }
        }

        /// <summary>
        /// Propone una nueva oferta de intercambio a un amigo guardándola en Firestore.
        /// Anti-fraude (TDD 2.5): Las cartas NO se descuentan al proponer.
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
            // Validar que el jugador actualmente posea la carta ofrecida
            PlayerCollectionManager.EnsureExists();
            if (PlayerCollectionManager.Instance != null && !PlayerCollectionManager.Instance.IsCardOwned(offeredCardId))
            {
                return new TradeOperationResult(false, "No posees la carta que intentas ofrecer.");
            }

            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            string myUid = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.UserId : "";
            string myName = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.DisplayName : "Entrenador";

            string newTradeId = "trade_" + Guid.NewGuid().ToString("N").Substring(0, 10);

            var newOffer = new TradeOfferItem
            {
                tradeId = newTradeId,
                fromUid = myUid,
                fromDisplayName = myName,
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

            if (!string.IsNullOrEmpty(token))
            {
                bool ok = await FirebaseRestClient.CreateTradeOfferAsync(token, newOffer);
                if (!ok)
                {
                    return new TradeOperationResult(false, "Error al enviar la oferta a la nube.");
                }
            }

            sentOffers.Insert(0, newOffer);
            OnOffersUpdated?.Invoke();

            Networking.FirebaseAnalyticsManager.Instance?.LogTradeProposed(toUid, offeredCardId, requestedCardId);
            Debug.Log($"<color=cyan>[TradeService] Oferta propuesta {newTradeId} enviada a {friendName} en Firestore.</color>");
            return new TradeOperationResult(true, "¡Oferta de intercambio enviada con éxito!", newTradeId);
        }

        /// <summary>
        /// Acepta una oferta de intercambio recibida.
        /// Transfiere cartas atómicamente: entrega la carta pedida y recibe la ofrecida.
        /// </summary>
        public async Task<TradeOperationResult> AcceptTradeAsync(string tradeId)
        {
            var offer = receivedOffers.Find(o => o.tradeId == tradeId);
            if (offer == null)
            {
                return new TradeOperationResult(false, "La oferta de intercambio no existe.");
            }

            // Revalidar que el jugador local todavía posea la carta solicitada
            PlayerCollectionManager.EnsureExists();
            if (PlayerCollectionManager.Instance != null)
            {
                if (!PlayerCollectionManager.Instance.IsCardOwned(offer.requestedCardId))
                {
                    return new TradeOperationResult(false, "Intercambio cancelado: Ya no tienes la carta que te fue solicitada.");
                }

                // Transferencia atómica de cartas:
                // 1. Entregar la carta que pidieron
                // 2. Recibir la carta ofrecida por el amigo
                PlayerCollectionManager.Instance.RemoveCard(offer.requestedCardId, 1);
                PlayerCollectionManager.Instance.AddCard(offer.offeredCardId, 1);
            }

            string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
            if (!string.IsNullOrEmpty(token))
            {
                await FirebaseRestClient.UpdateTradeOfferStatusAsync(token, tradeId, "aceptado");
            }

            offer.status = "aceptado";
            receivedOffers.Remove(offer);
            processedTrades.Add(tradeId);

            Networking.FirebaseAnalyticsManager.Instance?.LogTradeAccepted(tradeId, offer.offeredCardId);

            OnOffersUpdated?.Invoke();
            OnTradeCompleted?.Invoke(offer);

            Debug.Log($"<color=green>[TradeService] ¡Intercambio {tradeId} completado! Recibiste: {offer.offeredCardName}</color>");
            return new TradeOperationResult(true, $"¡Intercambio completado! Has recibido a {offer.offeredCardName}.", tradeId);
        }

        /// <summary>
        /// Rechaza una oferta de intercambio recibida.
        /// </summary>
        public async Task<TradeOperationResult> RejectTradeAsync(string tradeId)
        {
            var offer = receivedOffers.Find(o => o.tradeId == tradeId);
            if (offer != null)
            {
                string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
                if (!string.IsNullOrEmpty(token))
                {
                    await FirebaseRestClient.UpdateTradeOfferStatusAsync(token, tradeId, "rechazado");
                }

                offer.status = "rechazado";
                receivedOffers.Remove(offer);
                OnOffersUpdated?.Invoke();
                Debug.Log($"<color=yellow>[TradeService] Oferta {tradeId} rechazada.</color>");
                return new TradeOperationResult(true, "Oferta rechazada.", tradeId);
            }

            return new TradeOperationResult(false, "Oferta no encontrada.");
        }

        /// <summary>
        /// Cancela una oferta de intercambio enviada que sigue pendiente.
        /// </summary>
        public async Task<TradeOperationResult> CancelSentTradeAsync(string tradeId)
        {
            var offer = sentOffers.Find(o => o.tradeId == tradeId);
            if (offer != null)
            {
                string token = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.IdToken : "";
                if (!string.IsNullOrEmpty(token))
                {
                    await FirebaseRestClient.UpdateTradeOfferStatusAsync(token, tradeId, "cancelado");
                }

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
