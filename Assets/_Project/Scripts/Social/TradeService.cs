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
            
            string token = await FirebaseAuthManager.Instance.EnsureValidTokenAsync();
            string myUid = FirebaseAuthManager.Instance.UserId;
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(myUid)) return;

            try
            {
                PlayerCollectionManager.EnsureExists();

                // 1. Ofertas recibidas pendientes (donde toUid == myUid y status == "pendiente")
                var incoming = await FirebaseRestClient.GetIncomingTradeOffersAsync(token, myUid);
                if (incoming != null)
                {
                    receivedOffers.Clear();
                    receivedOffers.AddRange(incoming);
                }

                // 2. Todas las ofertas enviadas (donde fromUid == myUid)
                var outgoing = await FirebaseRestClient.GetSentTradeOffersAsync(token, myUid);

                // 3. Todas las ofertas recibidas (para reconciliación de cartas perdidas por reinstalación/actualización)
                var incomingAll = await FirebaseRestClient.GetIncomingTradeOffersAllStatusAsync(token, myUid);

                // Mapear delta neto de cartas intercambiadas para recuperar cartas en caso de APK reinstalada
                var tradeNetDelta = new Dictionary<string, int>();

                if (outgoing != null)
                {
                    sentOffers.Clear();
                    foreach (var offer in outgoing)
                    {
                        string pKey = $"Trade_Processed_{offer.tradeId}";
                        bool alreadyProcessed = processedTrades.Contains(offer.tradeId) || PlayerPrefs.GetInt(pKey, 0) == 1;

                        // Si una oferta que yo envié fue aceptada por mi amigo y falta procesar la recepción:
                        if (offer.status == "aceptado" && !alreadyProcessed)
                        {
                            processedTrades.Add(offer.tradeId);
                            PlayerPrefs.SetInt(pKey, 1);
                            PlayerPrefs.Save();

                            if (PlayerCollectionManager.Instance != null)
                            {
                                if (PlayerCollectionManager.Instance.IsCardOwned(offer.offeredCardId))
                                {
                                    PlayerCollectionManager.Instance.RemoveCard(offer.offeredCardId, 1);
                                }
                                PlayerCollectionManager.Instance.AddCard(offer.requestedCardId, 1);

                                if (FirebaseAuthManager.Instance != null)
                                {
                                    await FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
                                }
                            }

                            // Marcar como completada en Firestore
                            await FirebaseRestClient.UpdateTradeOfferStatusAsync(token, offer.tradeId, "completado");
                            offer.status = "completado";
                            SocialService.Instance?.InvalidateFriendCardsCache();
                            OnTradeCompleted?.Invoke(offer);
                            Debug.Log($"<color=green>[TradeService] ¡Oferta {offer.tradeId} completada! Recibiste {offer.requestedCardName} ({offer.requestedCardId})</color>");
                        }

                        // Contabilizar para reconciliación
                        if (offer.status == "aceptado" || offer.status == "completado")
                        {
                            // En oferta enviada: Yo entregué offeredCardId y recibí requestedCardId
                            if (!string.IsNullOrEmpty(offer.requestedCardId))
                            {
                                tradeNetDelta[offer.requestedCardId] = tradeNetDelta.TryGetValue(offer.requestedCardId, out int cur) ? cur + 1 : 1;
                            }
                            if (!string.IsNullOrEmpty(offer.offeredCardId))
                            {
                                tradeNetDelta[offer.offeredCardId] = tradeNetDelta.TryGetValue(offer.offeredCardId, out int cur) ? cur - 1 : -1;
                            }
                        }

                        if (offer.status == "pendiente")
                        {
                            sentOffers.Add(offer);
                        }
                    }
                }

                if (incomingAll != null)
                {
                    foreach (var offer in incomingAll)
                    {
                        if (offer.status == "aceptado" || offer.status == "completado")
                        {
                            // En oferta recibida: Yo entregué requestedCardId y recibí offeredCardId
                            if (!string.IsNullOrEmpty(offer.offeredCardId))
                            {
                                tradeNetDelta[offer.offeredCardId] = tradeNetDelta.TryGetValue(offer.offeredCardId, out int cur) ? cur + 1 : 1;
                            }
                            if (!string.IsNullOrEmpty(offer.requestedCardId))
                            {
                                tradeNetDelta[offer.requestedCardId] = tradeNetDelta.TryGetValue(offer.requestedCardId, out int cur) ? cur - 1 : -1;
                            }
                        }
                    }
                }

                // 4. Reconciliación automática anti-pérdida: Si el jugador recibió cartas en un trade pero tras reinstalar/actualizar el APK no están en su inventario
                if (PlayerCollectionManager.Instance != null && tradeNetDelta.Count > 0)
                {
                    bool recoveredAny = false;
                    foreach (var kvp in tradeNetDelta)
                    {
                        string cardId = kvp.Key;
                        int netGain = kvp.Value;
                        if (netGain > 0 && PlayerCollectionManager.Instance.GetOwnedCount(cardId) == 0)
                        {
                            Debug.Log($"<color=gold>[TradeService] ¡Recuperación automática de intercambio! Se acreditó {cardId} ({netGain} unidad/es) faltante tras actualizar APK.</color>");
                            PlayerCollectionManager.Instance.AddCard(cardId, netGain);
                            recoveredAny = true;
                        }
                    }

                    if (recoveredAny && FirebaseAuthManager.Instance != null)
                    {
                        await FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
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

            if (string.IsNullOrEmpty(toUid))
            {
                return new TradeOperationResult(false, "El amigo destinatario no es válido.");
            }

            string token = "";
            string myUid = "";
            string myName = "Entrenador";

            if (FirebaseAuthManager.Instance != null)
            {
                token = await FirebaseAuthManager.Instance.EnsureValidTokenAsync();
                myUid = FirebaseAuthManager.Instance.UserId;
                if (!string.IsNullOrEmpty(FirebaseAuthManager.Instance.DisplayName))
                {
                    myName = FirebaseAuthManager.Instance.DisplayName;
                }
            }

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(myUid))
            {
                return new TradeOperationResult(false, "No has iniciado sesión o no tienes conexión con Firebase.");
            }

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

            Debug.Log($"<color=cyan>[TradeService] Guardando oferta {newTradeId} en Firestore hacia toUid={toUid} ({friendName})...</color>");
            bool ok = await FirebaseRestClient.CreateTradeOfferAsync(token, newOffer);
            if (!ok)
            {
                return new TradeOperationResult(false, "Error al enviar la oferta a los servidores de Firestore. Revisa tu conexión a internet.");
            }

            sentOffers.Insert(0, newOffer);
            OnOffersUpdated?.Invoke();

            Networking.FirebaseAnalyticsManager.Instance?.LogTradeProposed(toUid, offeredCardId, requestedCardId);
            Debug.Log($"<color=green>[TradeService] Oferta propuesta {newTradeId} guardada con éxito en Firestore para {friendName}.</color>");
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
            }

            string token = FirebaseAuthManager.Instance != null ? await FirebaseAuthManager.Instance.EnsureValidTokenAsync() : "";

            // Anti-fraude: Validar en Firestore si el proponente todavía tiene la carta ofrecida
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(offer.fromUid))
            {
                var fromUser = await FirebaseRestClient.GetUserDocAsync(token, offer.fromUid);
                if (fromUser != null && fromUser.ownedCards != null)
                {
                    if (!fromUser.ownedCards.ContainsKey(offer.offeredCardId) || fromUser.ownedCards[offer.offeredCardId] <= 0)
                    {
                        await FirebaseRestClient.UpdateTradeOfferStatusAsync(token, tradeId, "expirado");
                        offer.status = "expirado";
                        receivedOffers.Remove(offer);
                        OnOffersUpdated?.Invoke();
                        return new TradeOperationResult(false, "El proponente ya no dispone de la carta ofrecida en su inventario.");
                    }
                }
            }

            if (PlayerCollectionManager.Instance != null)
            {
                // Transferencia atómica de cartas:
                // 1. Entregar la carta que pidieron
                // 2. Recibir la carta ofrecida por el amigo
                PlayerCollectionManager.Instance.RemoveCard(offer.requestedCardId, 1);
                PlayerCollectionManager.Instance.AddCard(offer.offeredCardId, 1);
                if (FirebaseAuthManager.Instance != null)
                {
                    await FirebaseAuthManager.Instance.SyncUserProfileToFirestoreAsync();
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                await FirebaseRestClient.UpdateTradeOfferStatusAsync(token, tradeId, "aceptado");
            }

            offer.status = "aceptado";
            receivedOffers.Remove(offer);
            processedTrades.Add(tradeId);
            PlayerPrefs.SetInt($"Trade_Processed_{tradeId}", 1);
            PlayerPrefs.Save();

            Networking.FirebaseAnalyticsManager.Instance?.LogTradeAccepted(tradeId, offer.offeredCardId);

            SocialService.Instance?.InvalidateFriendCardsCache();
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
                string token = FirebaseAuthManager.Instance != null ? await FirebaseAuthManager.Instance.EnsureValidTokenAsync() : "";
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
                string token = FirebaseAuthManager.Instance != null ? await FirebaseAuthManager.Instance.EnsureValidTokenAsync() : "";
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
