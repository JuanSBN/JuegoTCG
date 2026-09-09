using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Cards;
using JuegoTCG.Networking;
using JuegoTCG.Social;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Intercambios (TradeScreen).
    /// Maneja el filtrado dinámico entre ofertas Recibidas y Enviadas, badges de no leídas,
    /// aceptación y rechazo de ofertas con respuesta visual, sincronización en vivo con Firestore
    /// y modal para proponer nuevos intercambios directamente a amigos.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitTradeController : MonoBehaviour
    {
        public static string PreselectedFriendUid;
        public static string PreselectedFriendName;

        private UIDocument uiDocument;
        private VisualElement root;

        private Button backBtn;
        private Button tabReceivedBtn;
        private Button tabSentBtn;
        private VisualElement badgeReceived;
        private Label badgeCountReceived;

        private VisualElement emptyState;
        private Label emptyTitle;
        private Label emptyDesc;

        // Ranuras de ofertas Recibidas (1..3)
        private VisualElement[] receivedSlots;
        private Button[] receivedAcceptBtns;
        private Button[] receivedRejectBtns;

        // Ranuras de ofertas Enviadas (1..3)
        private VisualElement[] sentSlots;
        private Button[] sentCancelBtns;

        private Button btnNewTrade;

        // Modal: Proponer Nuevo Intercambio
        private VisualElement newTradeModal;
        private DropdownField dropdownFriend;
        private DropdownField dropdownOfferCard;
        private DropdownField dropdownRequestCard;
        private Label modalFeedbackLabel;
        private Button btnSendTradeOffer;
        private Button btnCloseNewTradeModal;

        // Modal: Feedback / Alertas
        private VisualElement tradeFeedbackModal;
        private Label tradeFeedbackTitle;
        private Label tradeFeedbackMessage;
        private Button btnCloseTradeFeedback;

        private readonly List<FriendData> cachedFriends = new List<FriendData>();
        private readonly List<CardCatalogItem> cachedOwnedCards = new List<CardCatalogItem>();
        private readonly List<CardCatalogItem> cachedCatalogCards = new List<CardCatalogItem>();

        private int selectedFriendIndex = 0;
        private int selectedOfferCardIndex = 0;
        private int selectedRequestCardIndex = 0;

        private bool isReceivedTab = true;
        private Coroutine pollCoroutine;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            TradeService.EnsureExists();
            SocialService.EnsureExists();
            PlayerCollectionManager.EnsureExists();

            BindUI();

            TradeService.Instance.OnOffersUpdated += RefreshCurrentTab;
            TradeService.Instance.OnTradeCompleted += OnTradeCompletedHandler;

            if (pollCoroutine != null) StopCoroutine(pollCoroutine);
            pollCoroutine = StartCoroutine(PollCloudTradesRoutine());

            // Actualizar ofertas y lista de amigos en segundo plano
            _ = TradeService.Instance.RefreshCloudTradesAsync();
            _ = SocialService.Instance.RefreshCloudRequestsAndFriendsAsync();

            // Si el usuario vino desde la pantalla de Amigos para intercambiar con alguien específico:
            if (!string.IsNullOrEmpty(PreselectedFriendUid))
            {
                string fUid = PreselectedFriendUid;
                string fName = PreselectedFriendName;
                PreselectedFriendUid = null;
                PreselectedFriendName = null;
                OpenNewTradeModal(fUid, fName);
            }

            RefreshCurrentTab();
        }

        private void OnDisable()
        {
            if (TradeService.Instance != null)
            {
                TradeService.Instance.OnOffersUpdated -= RefreshCurrentTab;
                TradeService.Instance.OnTradeCompleted -= OnTradeCompletedHandler;
            }

            if (pollCoroutine != null)
            {
                StopCoroutine(pollCoroutine);
                pollCoroutine = null;
            }
        }

        private IEnumerator PollCloudTradesRoutine()
        {
            while (true)
            {
                if (TradeService.Instance != null)
                {
                    _ = TradeService.Instance.RefreshCloudTradesAsync();
                }
                yield return new WaitForSecondsRealtime(4f);
            }
        }

        private void BindUI()
        {
            // Botón Volver a Comunidad
            backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunitySceneUIToolkit");
            }

            // Pestañas
            tabReceivedBtn = root.Q<Button>("Tab_Received");
            tabSentBtn = root.Q<Button>("Tab_Sent");
            badgeReceived = root.Q<VisualElement>("Badge_Received");
            badgeCountReceived = root.Q<Label>("BadgeCount_Received");

            if (tabReceivedBtn != null) tabReceivedBtn.clicked += () => SwitchTab(true);
            if (tabSentBtn != null) tabSentBtn.clicked += () => SwitchTab(false);

            // Estado vacío
            emptyState = root.Q<VisualElement>("TradeEmptyState");
            if (emptyState != null)
            {
                emptyTitle = emptyState.Q<Label>("EmptyTitle");
                emptyDesc = emptyState.Q<Label>("EmptyDesc");
            }

            // Ranuras Recibidas
            receivedSlots = new VisualElement[3];
            receivedAcceptBtns = new Button[3];
            receivedRejectBtns = new Button[3];

            for (int i = 0; i < 3; i++)
            {
                int index = i + 1;
                int slotIndex = i;
                receivedSlots[i] = root.Q<VisualElement>($"Card_Trade_{index}");
                receivedAcceptBtns[i] = root.Q<Button>($"Btn_Accept_{index}");
                receivedRejectBtns[i] = root.Q<Button>($"Btn_Reject_{index}");

                if (receivedAcceptBtns[i] != null)
                {
                    receivedAcceptBtns[i].clicked += () => OnAcceptClicked(slotIndex);
                }
                if (receivedRejectBtns[i] != null)
                {
                    receivedRejectBtns[i].clicked += () => OnRejectClicked(slotIndex);
                }
            }

            // Ranuras Enviadas
            sentSlots = new VisualElement[3];
            sentCancelBtns = new Button[3];

            for (int i = 0; i < 3; i++)
            {
                int index = i + 1;
                int slotIndex = i;
                sentSlots[i] = root.Q<VisualElement>($"Card_Trade_Sent_{index}");
                sentCancelBtns[i] = root.Q<Button>($"Btn_Cancel_{index}");

                if (sentCancelBtns[i] != null)
                {
                    sentCancelBtns[i].clicked += () => OnCancelClicked(slotIndex);
                }
            }

            // Botón Flotante: Proponer Intercambio
            btnNewTrade = root.Q<Button>("Btn_NewTrade");
            if (btnNewTrade != null)
            {
                btnNewTrade.clicked += () => OpenNewTradeModal();
            }

            // Modal de nuevo intercambio
            newTradeModal = root.Q<VisualElement>("NewTradeModal");
            dropdownFriend = root.Q<DropdownField>("Dropdown_Friend");
            dropdownOfferCard = root.Q<DropdownField>("Dropdown_OfferCard");
            dropdownRequestCard = root.Q<DropdownField>("Dropdown_RequestCard");
            modalFeedbackLabel = root.Q<Label>("ModalFeedbackLabel");
            btnSendTradeOffer = root.Q<Button>("Btn_SendTradeOffer");
            btnCloseNewTradeModal = root.Q<Button>("Btn_CloseNewTradeModal");

            if (dropdownFriend != null)
            {
                dropdownFriend.RegisterValueChangedCallback(evt =>
                {
                    if (dropdownFriend.choices != null && !string.IsNullOrEmpty(evt.newValue))
                    {
                        int idx = dropdownFriend.choices.IndexOf(evt.newValue);
                        if (idx >= 0) selectedFriendIndex = idx;
                    }
                });
            }

            if (dropdownOfferCard != null)
            {
                dropdownOfferCard.RegisterValueChangedCallback(evt =>
                {
                    if (dropdownOfferCard.choices != null && !string.IsNullOrEmpty(evt.newValue))
                    {
                        int idx = dropdownOfferCard.choices.IndexOf(evt.newValue);
                        if (idx >= 0) selectedOfferCardIndex = idx;
                    }
                });
            }

            if (dropdownRequestCard != null)
            {
                dropdownRequestCard.RegisterValueChangedCallback(evt =>
                {
                    if (dropdownRequestCard.choices != null && !string.IsNullOrEmpty(evt.newValue))
                    {
                        int idx = dropdownRequestCard.choices.IndexOf(evt.newValue);
                        if (idx >= 0) selectedRequestCardIndex = idx;
                    }
                });
            }

            if (btnSendTradeOffer != null)
            {
                btnSendTradeOffer.clicked += async () => await SendTradeOfferAsync();
            }

            if (btnCloseNewTradeModal != null)
            {
                btnCloseNewTradeModal.clicked += () =>
                {
                    if (newTradeModal != null) newTradeModal.style.display = DisplayStyle.None;
                };
            }

            // Modal de Feedback / Notificaciones
            tradeFeedbackModal = root.Q<VisualElement>("TradeFeedbackModal");
            if (tradeFeedbackModal != null)
            {
                tradeFeedbackTitle = tradeFeedbackModal.Q<Label>("TradeFeedbackTitle");
                tradeFeedbackMessage = tradeFeedbackModal.Q<Label>("TradeFeedbackMessage");
                btnCloseTradeFeedback = tradeFeedbackModal.Q<Button>("Btn_CloseTradeFeedback");
                if (btnCloseTradeFeedback != null)
                {
                    btnCloseTradeFeedback.clicked += () =>
                    {
                        if (tradeFeedbackModal != null) tradeFeedbackModal.style.display = DisplayStyle.None;
                    };
                }
            }

            // Barra de Navegación Inferior (Comunidad)
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }

        private void SwitchTab(bool showReceived)
        {
            isReceivedTab = showReceived;

            if (isReceivedTab)
            {
                tabReceivedBtn?.AddToClassList("trade-tab-pill-active");
                tabSentBtn?.RemoveFromClassList("trade-tab-pill-active");
            }
            else
            {
                tabSentBtn?.AddToClassList("trade-tab-pill-active");
                tabReceivedBtn?.RemoveFromClassList("trade-tab-pill-active");
            }

            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            UpdateBadgeDisplay();

            if (isReceivedTab)
            {
                RenderReceivedTab();
            }
            else
            {
                RenderSentTab();
            }
        }

        private void RenderReceivedTab()
        {
            // Ocultar todas las ranuras enviadas
            if (sentSlots != null)
            {
                for (int i = 0; i < sentSlots.Length; i++)
                {
                    if (sentSlots[i] != null) sentSlots[i].style.display = DisplayStyle.None;
                }
            }

            var offers = TradeService.Instance != null ? TradeService.Instance.ReceivedOffers : null;
            int count = offers != null ? offers.Count : 0;

            if (count == 0)
            {
                if (emptyState != null)
                {
                    emptyState.style.display = DisplayStyle.Flex;
                    if (emptyTitle != null) emptyTitle.text = "No tienes intercambios recibidos.";
                    if (emptyDesc != null) emptyDesc.text = "Cuando un amigo te proponga un trato en la nube, aparecerá aquí.";
                }

                if (receivedSlots != null)
                {
                    for (int i = 0; i < receivedSlots.Length; i++)
                    {
                        if (receivedSlots[i] != null) receivedSlots[i].style.display = DisplayStyle.None;
                    }
                }
                return;
            }

            if (emptyState != null) emptyState.style.display = DisplayStyle.None;

            for (int i = 0; i < 3; i++)
            {
                var slot = receivedSlots[i];
                if (slot == null) continue;

                if (i >= count)
                {
                    slot.style.display = DisplayStyle.None;
                    continue;
                }

                slot.style.display = DisplayStyle.Flex;
                var offer = offers[i];

                // Header
                var userLbl = slot.Q<Label>(className: "trade-user-name");
                if (userLbl != null) userLbl.text = offer.fromDisplayName;

                var avatarLbl = slot.Q<Label>(className: "trade-avatar-text");
                if (avatarLbl != null) avatarLbl.text = GetInitials(offer.fromDisplayName);

                var timeLbl = slot.Q<Label>(className: "trade-time-label");
                if (timeLbl != null) timeLbl.text = offer.timeAgo;

                // Validar si el usuario local posee la carta requerida
                bool ownsRequested = PlayerCollectionManager.Instance != null && PlayerCollectionManager.Instance.IsCardOwned(offer.requestedCardId);

                // Tú Das (lo que el proponente solicita)
                var giveCol = slot.Q<VisualElement>(className: "trade-side-col");
                if (giveCol != null)
                {
                    var giveTitle = giveCol.Q<Label>(className: "trade-side-title");
                    if (giveTitle != null)
                    {
                        giveTitle.text = ownsRequested 
                            ? $"TÚ DAS: {offer.requestedCardName}" 
                            : $"TÚ DAS: {offer.requestedCardName} (No disponible)";
                    }

                    var tokens = giveCol.Query<VisualElement>(className: "trade-token").ToList();
                    if (tokens.Count > 0)
                    {
                        var token = tokens[0];
                        token.style.display = DisplayStyle.Flex;
                        var tokenLbl = token.Q<Label>(className: "trade-token-text");
                        SetTokenStyle(token, tokenLbl, offer.requestedRarity, offer.requestedCardName);
                        for (int t = 1; t < tokens.Count; t++) tokens[t].style.display = DisplayStyle.None;
                    }
                }

                // Tú Recibes (lo que el proponente te entrega)
                var receiveCol = slot.Q<VisualElement>(className: "trade-side-col-right");
                if (receiveCol != null)
                {
                    var receiveTitle = receiveCol.Q<Label>(className: "trade-side-title");
                    if (receiveTitle != null) receiveTitle.text = $"TÚ RECIBES: {offer.offeredCardName}";

                    var tokens = receiveCol.Query<VisualElement>(className: "trade-token").ToList();
                    if (tokens.Count > 0)
                    {
                        var token = tokens[0];
                        token.style.display = DisplayStyle.Flex;
                        var tokenLbl = token.Q<Label>(className: "trade-token-text");
                        SetTokenStyle(token, tokenLbl, offer.offeredRarity, offer.offeredCardName);
                        for (int t = 1; t < tokens.Count; t++) tokens[t].style.display = DisplayStyle.None;
                    }
                }

                // Estado visual de los botones Aceptar / Rechazar
                var btnAccept = receivedAcceptBtns[i];
                var btnReject = receivedRejectBtns[i];

                if (btnAccept != null)
                {
                    btnAccept.SetEnabled(true);
                    var btnAcceptText = btnAccept.Q<Label>(className: "trade-btn-accept-text");
                    if (!ownsRequested)
                    {
                        if (btnAcceptText != null) btnAcceptText.text = "NO LA TIENES";
                        btnAccept.AddToClassList("trade-btn-disabled");
                    }
                    else
                    {
                        if (btnAcceptText != null) btnAcceptText.text = "ACEPTAR";
                        btnAccept.RemoveFromClassList("trade-btn-disabled");
                    }
                }

                if (btnReject != null)
                {
                    btnReject.SetEnabled(true);
                }
            }
        }

        private void RenderSentTab()
        {
            // Ocultar todas las ranuras recibidas
            if (receivedSlots != null)
            {
                for (int i = 0; i < receivedSlots.Length; i++)
                {
                    if (receivedSlots[i] != null) receivedSlots[i].style.display = DisplayStyle.None;
                }
            }

            var offers = TradeService.Instance != null ? TradeService.Instance.SentOffers : null;
            int count = offers != null ? offers.Count : 0;

            if (count == 0)
            {
                if (emptyState != null)
                {
                    emptyState.style.display = DisplayStyle.Flex;
                    if (emptyTitle != null) emptyTitle.text = "No has enviado ninguna oferta.";
                    if (emptyDesc != null) emptyDesc.text = "Propón un intercambio con el botón '+ NUEVO INTERCAMBIO'.";
                }

                if (sentSlots != null)
                {
                    for (int i = 0; i < sentSlots.Length; i++)
                    {
                        if (sentSlots[i] != null) sentSlots[i].style.display = DisplayStyle.None;
                    }
                }
                return;
            }

            if (emptyState != null) emptyState.style.display = DisplayStyle.None;

            for (int i = 0; i < 3; i++)
            {
                var slot = sentSlots[i];
                if (slot == null) continue;

                if (i >= count)
                {
                    slot.style.display = DisplayStyle.None;
                    continue;
                }

                slot.style.display = DisplayStyle.Flex;
                var offer = offers[i];

                // Header
                var userLbl = slot.Q<Label>(className: "trade-user-name");
                if (userLbl != null) userLbl.text = offer.toDisplayName;

                var avatarLbl = slot.Q<Label>(className: "trade-avatar-text");
                if (avatarLbl != null) avatarLbl.text = GetInitials(offer.toDisplayName);

                var timeLbl = slot.Q<Label>(className: "trade-time-label");
                if (timeLbl != null) timeLbl.text = offer.timeAgo;

                // Tú Das (lo que estás ofreciendo)
                var giveCol = slot.Q<VisualElement>(className: "trade-side-col");
                if (giveCol != null)
                {
                    var giveTitle = giveCol.Q<Label>(className: "trade-side-title");
                    if (giveTitle != null) giveTitle.text = $"TÚ DAS: {offer.offeredCardName}";

                    var tokens = giveCol.Query<VisualElement>(className: "trade-token").ToList();
                    if (tokens.Count > 0)
                    {
                        var token = tokens[0];
                        token.style.display = DisplayStyle.Flex;
                        var tokenLbl = token.Q<Label>(className: "trade-token-text");
                        SetTokenStyle(token, tokenLbl, offer.offeredRarity, offer.offeredCardName);
                        for (int t = 1; t < tokens.Count; t++) tokens[t].style.display = DisplayStyle.None;
                    }
                }

                // Tú Recibes (lo que pides a cambio)
                var receiveCol = slot.Q<VisualElement>(className: "trade-side-col-right");
                if (receiveCol != null)
                {
                    var receiveTitle = receiveCol.Q<Label>(className: "trade-side-title");
                    if (receiveTitle != null) receiveTitle.text = $"TÚ RECIBES: {offer.requestedCardName}";

                    var tokens = receiveCol.Query<VisualElement>(className: "trade-token").ToList();
                    if (tokens.Count > 0)
                    {
                        var token = tokens[0];
                        token.style.display = DisplayStyle.Flex;
                        var tokenLbl = token.Q<Label>(className: "trade-token-text");
                        SetTokenStyle(token, tokenLbl, offer.requestedRarity, offer.requestedCardName);
                        for (int t = 1; t < tokens.Count; t++) tokens[t].style.display = DisplayStyle.None;
                    }
                }

                // Botón Cancelar
                var btnCancel = sentCancelBtns[i];
                if (btnCancel != null)
                {
                    btnCancel.SetEnabled(true);
                }
            }
        }

        private async void OnAcceptClicked(int slotIndex)
        {
            var offers = TradeService.Instance != null ? TradeService.Instance.ReceivedOffers : null;
            if (offers == null || slotIndex >= offers.Count) return;

            var offer = offers[slotIndex];
            var slot = (receivedSlots != null && slotIndex < receivedSlots.Length) ? receivedSlots[slotIndex] : null;
            var btn = (receivedAcceptBtns != null && slotIndex < receivedAcceptBtns.Length) ? receivedAcceptBtns[slotIndex] : null;

            bool ownsRequested = PlayerCollectionManager.Instance != null && PlayerCollectionManager.Instance.IsCardOwned(offer.requestedCardId);
            if (!ownsRequested)
            {
                ShowFeedbackModal("Carta No Disponible", 
                    $"Tu amigo '{offer.fromDisplayName}' solicita tu carta '{offer.requestedCardName}', pero actualmente no tienes ninguna copia en tu inventario.\n\nConsíguela abriendo sobres en la tienda para poder completar este intercambio.", 
                    isError: true);
                return;
            }

            await AcceptTradeOffer(offer, slot, btn);
        }

        private async void OnRejectClicked(int slotIndex)
        {
            var offers = TradeService.Instance != null ? TradeService.Instance.ReceivedOffers : null;
            if (offers == null || slotIndex >= offers.Count) return;

            var offer = offers[slotIndex];
            var slot = (receivedSlots != null && slotIndex < receivedSlots.Length) ? receivedSlots[slotIndex] : null;
            var btn = (receivedRejectBtns != null && slotIndex < receivedRejectBtns.Length) ? receivedRejectBtns[slotIndex] : null;

            if (btn != null) btn.SetEnabled(false);
            var result = await TradeService.Instance.RejectTradeAsync(offer.tradeId);
            if (slot != null) slot.style.display = DisplayStyle.None;
            RefreshCurrentTab();
            Debug.Log($"<color=yellow>[TradeController] {result.message}</color>");
            ShowFeedbackModal("Intercambio Rechazado", $"Has rechazado la oferta de intercambio de {offer.fromDisplayName}.", isError: false);
        }

        private async void OnCancelClicked(int slotIndex)
        {
            var offers = TradeService.Instance != null ? TradeService.Instance.SentOffers : null;
            if (offers == null || slotIndex >= offers.Count) return;

            var offer = offers[slotIndex];
            var slot = (sentSlots != null && slotIndex < sentSlots.Length) ? sentSlots[slotIndex] : null;
            var btn = (sentCancelBtns != null && slotIndex < sentCancelBtns.Length) ? sentCancelBtns[slotIndex] : null;

            if (btn != null) btn.SetEnabled(false);
            var result = await TradeService.Instance.CancelSentTradeAsync(offer.tradeId);
            if (slot != null) slot.style.display = DisplayStyle.None;
            RefreshCurrentTab();
            Debug.Log($"<color=orange>[TradeController] {result.message}</color>");
            ShowFeedbackModal("Oferta Cancelada", $"Has cancelado tu propuesta enviada a {offer.toDisplayName}.", isError: false);
        }

        private async Task AcceptTradeOffer(TradeOfferItem offer, VisualElement slot, Button btn)
        {
            if (btn != null)
            {
                btn.SetEnabled(false);
                var lbl = btn.Q<Label>(className: "trade-btn-accept-text");
                if (lbl != null) lbl.text = "PROCESANDO...";
            }

            var result = await TradeService.Instance.AcceptTradeAsync(offer.tradeId);
            if (result.success)
            {
                if (slot != null) slot.style.display = DisplayStyle.None;
                Debug.Log($"<color=green>[TradeController] {result.message}</color>");
                ShowFeedbackModal("¡Intercambio Completado!", 
                    $"¡Has completado el intercambio con {offer.fromDisplayName} exitosamente!\n\nEntregaste: {offer.requestedCardName}\nRecibiste: {offer.offeredCardName}", 
                    isError: false);
            }
            else
            {
                if (btn != null)
                {
                    btn.SetEnabled(true);
                    var lbl = btn.Q<Label>(className: "trade-btn-accept-text");
                    if (lbl != null) lbl.text = "ACEPTAR";
                }
                Debug.LogWarning($"[TradeController] Fallo al aceptar intercambio: {result.message}");
                ShowFeedbackModal("No Se Pudo Intercambiar", result.message, isError: true);
            }
            RefreshCurrentTab();
        }

        private void ShowFeedbackModal(string title, string message, bool isError = false)
        {
            if (tradeFeedbackModal == null) return;
            if (tradeFeedbackTitle != null)
            {
                tradeFeedbackTitle.text = title;
                tradeFeedbackTitle.style.color = isError 
                    ? new StyleColor(new Color(1f, 0.42f, 0.42f)) 
                    : new StyleColor(new Color(0.91f, 0.66f, 0.12f));
            }
            if (tradeFeedbackMessage != null)
            {
                tradeFeedbackMessage.text = message;
            }
            tradeFeedbackModal.style.display = DisplayStyle.Flex;
            tradeFeedbackModal.BringToFront();
        }

        private void OnTradeCompletedHandler(TradeOfferItem completedTrade)
        {
            Debug.Log($"<color=green>[TradeController] ¡Intercambio {completedTrade.tradeId} completado exitosamente!</color>");
            RefreshCurrentTab();
            if (FirebaseAuthManager.Instance != null && completedTrade.fromUid == FirebaseAuthManager.Instance.UserId)
            {
                ShowFeedbackModal("¡Intercambio Aceptado!", 
                    $"¡Tu amigo {completedTrade.toDisplayName} aceptó tu propuesta de intercambio!\n\nRecibiste: {completedTrade.requestedCardName}", 
                    isError: false);
            }
        }

        private void UpdateBadgeDisplay()
        {
            int count = TradeService.Instance != null ? TradeService.Instance.ReceivedOffers.Count : 0;
            if (badgeCountReceived != null)
            {
                badgeCountReceived.text = count.ToString();
            }
            if (badgeReceived != null)
            {
                badgeReceived.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void OpenNewTradeModal(string preselectedUid = null, string preselectedName = null)
        {
            if (newTradeModal == null) return;

            // 1. Cargar amigos desde SocialService
            cachedFriends.Clear();
            var friends = SocialService.Instance != null ? SocialService.Instance.Friends : null;
            List<string> friendChoices = new List<string>();

            if (friends != null && friends.Count > 0)
            {
                foreach (var f in friends)
                {
                    cachedFriends.Add(f);
                    friendChoices.Add($"{f.DisplayName} (Nv. {f.level})");
                }
            }
            else
            {
                friendChoices.Add("No tienes amigos agregados");
            }

            if (dropdownFriend != null)
            {
                dropdownFriend.choices = friendChoices;
                selectedFriendIndex = 0;

                if (!string.IsNullOrEmpty(preselectedUid))
                {
                    int foundIdx = cachedFriends.FindIndex(f => f.friendUid == preselectedUid);
                    if (foundIdx >= 0)
                    {
                        selectedFriendIndex = foundIdx;
                    }
                    else
                    {
                        var preselectedFriend = new FriendData
                        {
                            friendUid = preselectedUid,
                            displayName = !string.IsNullOrEmpty(preselectedName) ? preselectedName : "Amigo",
                            level = 1
                        };
                        if (friendChoices.Count == 1 && friendChoices[0] == "No tienes amigos agregados")
                        {
                            friendChoices.Clear();
                        }
                        cachedFriends.Add(preselectedFriend);
                        friendChoices.Add($"{preselectedFriend.DisplayName} (Nv. {preselectedFriend.level})");
                        dropdownFriend.choices = friendChoices;
                        selectedFriendIndex = friendChoices.Count - 1;
                    }
                }

                if (selectedFriendIndex >= 0 && selectedFriendIndex < friendChoices.Count)
                {
                    dropdownFriend.SetValueWithoutNotify(friendChoices[selectedFriendIndex]);
                    dropdownFriend.index = selectedFriendIndex;
                }
            }

            // 2. Cargar cartas poseídas por el jugador
            cachedOwnedCards.Clear();
            var catalog = PlayerCollectionManager.Instance != null ? PlayerCollectionManager.Instance.GetCatalog() : null;
            List<string> offerChoices = new List<string>();

            if (catalog != null)
            {
                foreach (var card in catalog)
                {
                    int ownedCount = PlayerCollectionManager.Instance.GetOwnedCount(card.cardId);
                    if (ownedCount > 0)
                    {
                        cachedOwnedCards.Add(card);
                        offerChoices.Add($"{card.playerName} ({card.rarity}) x{ownedCount}");
                    }
                }
            }

            if (offerChoices.Count == 0)
            {
                offerChoices.Add("No tienes cartas para ofrecer");
            }

            if (dropdownOfferCard != null)
            {
                dropdownOfferCard.choices = offerChoices;
                selectedOfferCardIndex = 0;
                if (offerChoices.Count > 0)
                {
                    dropdownOfferCard.SetValueWithoutNotify(offerChoices[0]);
                    dropdownOfferCard.index = 0;
                }
            }

            // 3. Cargar catálogo de cartas para pedir
            cachedCatalogCards.Clear();
            List<string> requestChoices = new List<string>();

            if (catalog != null)
            {
                foreach (var card in catalog)
                {
                    cachedCatalogCards.Add(card);
                    requestChoices.Add($"{card.playerName} ({card.rarity})");
                }
            }

            if (dropdownRequestCard != null)
            {
                dropdownRequestCard.choices = requestChoices;
                selectedRequestCardIndex = 0;
                if (requestChoices.Count > 0)
                {
                    dropdownRequestCard.SetValueWithoutNotify(requestChoices[0]);
                    dropdownRequestCard.index = 0;
                }
            }

            // Resetear feedback
            if (modalFeedbackLabel != null)
            {
                modalFeedbackLabel.text = "";
                modalFeedbackLabel.style.display = DisplayStyle.None;
            }

            btnSendTradeOffer?.SetEnabled(true);
            newTradeModal.style.display = DisplayStyle.Flex;
        }

        private async Task SendTradeOfferAsync()
        {
            // Resolver índices exactos comparando value contra choices o usando el índice rastreado
            int friendIdx = selectedFriendIndex;
            if (dropdownFriend != null && dropdownFriend.choices != null && !string.IsNullOrEmpty(dropdownFriend.value))
            {
                int valIdx = dropdownFriend.choices.IndexOf(dropdownFriend.value);
                if (valIdx >= 0) friendIdx = valIdx;
            }

            int offerIdx = selectedOfferCardIndex;
            if (dropdownOfferCard != null && dropdownOfferCard.choices != null && !string.IsNullOrEmpty(dropdownOfferCard.value))
            {
                int valIdx = dropdownOfferCard.choices.IndexOf(dropdownOfferCard.value);
                if (valIdx >= 0) offerIdx = valIdx;
            }

            int requestIdx = selectedRequestCardIndex;
            if (dropdownRequestCard != null && dropdownRequestCard.choices != null && !string.IsNullOrEmpty(dropdownRequestCard.value))
            {
                int valIdx = dropdownRequestCard.choices.IndexOf(dropdownRequestCard.value);
                if (valIdx >= 0) requestIdx = valIdx;
            }

            if (cachedFriends.Count == 0 || friendIdx < 0 || friendIdx >= cachedFriends.Count)
            {
                ShowModalFeedback("Debes tener al menos un amigo para proponer un intercambio.", true);
                return;
            }

            if (cachedOwnedCards.Count == 0 || offerIdx < 0 || offerIdx >= cachedOwnedCards.Count)
            {
                ShowModalFeedback("No posees ninguna carta para ofrecer.", true);
                return;
            }

            if (cachedCatalogCards.Count == 0 || requestIdx < 0 || requestIdx >= cachedCatalogCards.Count)
            {
                ShowModalFeedback("Selecciona una carta válida para pedir.", true);
                return;
            }

            var friend = cachedFriends[friendIdx];
            var offered = cachedOwnedCards[offerIdx];
            var requested = cachedCatalogCards[requestIdx];

            if (offered.cardId == requested.cardId)
            {
                ShowModalFeedback("No puedes ofrecer y pedir la misma carta.", true);
                return;
            }

            btnSendTradeOffer?.SetEnabled(false);
            ShowModalFeedback("Enviando oferta a la nube...", false);

            Debug.Log($"<color=cyan>[TradeController] Enviando oferta: Amigo={friend.DisplayName} (UID={friend.friendUid}), Ofreces={offered.playerName} ({offered.cardId}), Pides={requested.playerName} ({requested.cardId})</color>");

            var result = await TradeService.Instance.ProposeTradeAsync(
                friend.friendUid,
                friend.DisplayName,
                offered.cardId,
                offered.playerName,
                offered.rarity.ToString(),
                requested.cardId,
                requested.playerName,
                requested.rarity.ToString()
            );

            if (result.success)
            {
                ShowModalFeedback("¡Oferta de intercambio enviada!", false);
                await Task.Delay(500);
                if (newTradeModal != null) newTradeModal.style.display = DisplayStyle.None;
                SwitchTab(false); // Llevar al usuario a ver su oferta enviada
            }
            else
            {
                btnSendTradeOffer?.SetEnabled(true);
                ShowModalFeedback(result.message, true);
            }
        }

        private void ShowModalFeedback(string msg, bool isError)
        {
            if (modalFeedbackLabel == null) return;
            modalFeedbackLabel.text = msg;
            modalFeedbackLabel.style.color = isError ? new StyleColor(new Color(1f, 0.35f, 0.35f)) : new StyleColor(new Color(0.96f, 0.77f, 0.19f));
            modalFeedbackLabel.style.display = DisplayStyle.Flex;
        }

        private void SetTokenStyle(VisualElement token, Label tokenText, string rarityStr, string cardName)
        {
            if (token == null) return;

            token.RemoveFromClassList("trade-token-mythic");
            token.RemoveFromClassList("trade-token-rare");
            token.RemoveFromClassList("trade-token-uncommon");
            token.RemoveFromClassList("trade-token-common");

            string lower = (rarityStr ?? "").ToLower();
            if (lower.Contains("mitica") || lower.Contains("mythic") || lower.Contains("fullart") || lower.Contains("legendaria"))
            {
                token.AddToClassList("trade-token-mythic");
            }
            else if (lower.Contains("epica") || lower.Contains("epic") || lower.Contains("especial") || lower.Contains("rare"))
            {
                token.AddToClassList("trade-token-rare");
            }
            else
            {
                token.AddToClassList("trade-token-common");
            }

            if (tokenText != null)
            {
                tokenText.text = GetInitials(cardName);
            }
        }

        private string GetInitials(string text)
        {
            if (string.IsNullOrEmpty(text)) return "??";
            var parts = text.Trim().Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            }
            return text.Length >= 2 ? text.Substring(0, 2).ToUpper() : text.ToUpper();
        }
    }
}
