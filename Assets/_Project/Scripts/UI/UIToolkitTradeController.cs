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

        private readonly List<FriendData> cachedFriends = new List<FriendData>();
        private readonly List<CardCatalogItem> cachedOwnedCards = new List<CardCatalogItem>();
        private readonly List<CardCatalogItem> cachedCatalogCards = new List<CardCatalogItem>();

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

            // Si el usuario vino desde la pantalla de Amigos para intercambiar con alguien específico:
            if (!string.IsNullOrEmpty(PreselectedFriendUid))
            {
                string fUid = PreselectedFriendUid;
                string fName = PreselectedFriendName;
                PreselectedFriendUid = null;
                PreselectedFriendName = null;
                OpenNewTradeModal(fUid, fName);
            }
            else
            {
                _ = TradeService.Instance.RefreshCloudTradesAsync();
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
                receivedSlots[i] = root.Q<VisualElement>($"Card_Trade_{index}");
                receivedAcceptBtns[i] = root.Q<Button>($"Btn_Accept_{index}");
                receivedRejectBtns[i] = root.Q<Button>($"Btn_Reject_{index}");
            }

            // Ranuras Enviadas
            sentSlots = new VisualElement[3];
            sentCancelBtns = new Button[3];

            for (int i = 0; i < 3; i++)
            {
                int index = i + 1;
                sentSlots[i] = root.Q<VisualElement>($"Card_Trade_Sent_{index}");
                sentCancelBtns[i] = root.Q<Button>($"Btn_Cancel_{index}");
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

                // Tú Das (lo que el proponente solicita)
                var giveCol = slot.Q<VisualElement>(className: "trade-side-col");
                if (giveCol != null)
                {
                    var giveTitle = giveCol.Q<Label>(className: "trade-side-title");
                    if (giveTitle != null) giveTitle.text = $"TÚ DAS: {offer.requestedCardName}";

                    var token = giveCol.Q<VisualElement>(className: "trade-token");
                    var tokenLbl = token?.Q<Label>(className: "trade-token-text");
                    SetTokenStyle(token, tokenLbl, offer.requestedRarity, offer.requestedCardName);
                }

                // Tú Recibes (lo que el proponente te entrega)
                var receiveCol = slot.Q<VisualElement>(className: "trade-side-col-right");
                if (receiveCol != null)
                {
                    var receiveTitle = receiveCol.Q<Label>(className: "trade-side-title");
                    if (receiveTitle != null) receiveTitle.text = $"TÚ RECIBES: {offer.offeredCardName}";

                    var token = receiveCol.Q<VisualElement>(className: "trade-token");
                    var tokenLbl = token?.Q<Label>(className: "trade-token-text");
                    SetTokenStyle(token, tokenLbl, offer.offeredRarity, offer.offeredCardName);
                }

                // Botones Aceptar / Rechazar
                string currentTradeId = offer.tradeId;
                var btnAccept = receivedAcceptBtns[i];
                var btnReject = receivedRejectBtns[i];

                if (btnAccept != null)
                {
                    btnAccept.SetEnabled(true);
                    btnAccept.clickable = new Clickable(async () => await AcceptTradeOffer(currentTradeId, slot, btnAccept));
                }

                if (btnReject != null)
                {
                    btnReject.SetEnabled(true);
                    btnReject.clickable = new Clickable(async () => await RejectTradeOffer(currentTradeId, slot, btnReject));
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

                    var token = giveCol.Q<VisualElement>(className: "trade-token");
                    var tokenLbl = token?.Q<Label>(className: "trade-token-text");
                    SetTokenStyle(token, tokenLbl, offer.offeredRarity, offer.offeredCardName);
                }

                // Tú Recibes (lo que pides a cambio)
                var receiveCol = slot.Q<VisualElement>(className: "trade-side-col-right");
                if (receiveCol != null)
                {
                    var receiveTitle = receiveCol.Q<Label>(className: "trade-side-title");
                    if (receiveTitle != null) receiveTitle.text = $"TÚ RECIBES: {offer.requestedCardName}";

                    var token = receiveCol.Q<VisualElement>(className: "trade-token");
                    var tokenLbl = token?.Q<Label>(className: "trade-token-text");
                    SetTokenStyle(token, tokenLbl, offer.requestedRarity, offer.requestedCardName);
                }

                // Botón Cancelar
                string currentTradeId = offer.tradeId;
                var btnCancel = sentCancelBtns[i];
                if (btnCancel != null)
                {
                    btnCancel.SetEnabled(true);
                    btnCancel.clickable = new Clickable(async () => await CancelSentTradeOffer(currentTradeId, slot, btnCancel));
                }
            }
        }

        private async Task AcceptTradeOffer(string tradeId, VisualElement slot, Button btn)
        {
            if (btn != null) btn.SetEnabled(false);
            var result = await TradeService.Instance.AcceptTradeAsync(tradeId);
            if (result.success)
            {
                if (slot != null) slot.style.display = DisplayStyle.None;
                Debug.Log($"<color=green>[TradeController] {result.message}</color>");
            }
            else
            {
                if (btn != null) btn.SetEnabled(true);
                Debug.LogWarning($"[TradeController] Fallo al aceptar intercambio: {result.message}");
            }
            RefreshCurrentTab();
        }

        private async Task RejectTradeOffer(string tradeId, VisualElement slot, Button btn)
        {
            if (btn != null) btn.SetEnabled(false);
            var result = await TradeService.Instance.RejectTradeAsync(tradeId);
            if (slot != null) slot.style.display = DisplayStyle.None;
            RefreshCurrentTab();
            Debug.Log($"<color=yellow>[TradeController] {result.message}</color>");
        }

        private async Task CancelSentTradeOffer(string tradeId, VisualElement slot, Button btn)
        {
            if (btn != null) btn.SetEnabled(false);
            var result = await TradeService.Instance.CancelSentTradeAsync(tradeId);
            if (slot != null) slot.style.display = DisplayStyle.None;
            RefreshCurrentTab();
            Debug.Log($"<color=orange>[TradeController] {result.message}</color>");
        }

        private void OnTradeCompletedHandler(TradeOfferItem completedTrade)
        {
            Debug.Log($"<color=green>[TradeController] ¡Intercambio {completedTrade.tradeId} completado exitosamente!</color>");
            RefreshCurrentTab();
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
                dropdownFriend.index = 0;

                if (!string.IsNullOrEmpty(preselectedUid))
                {
                    int foundIdx = cachedFriends.FindIndex(f => f.friendUid == preselectedUid);
                    if (foundIdx >= 0) dropdownFriend.index = foundIdx;
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
                dropdownOfferCard.index = 0;
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
                dropdownRequestCard.index = 0;
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
            if (cachedFriends.Count == 0 || dropdownFriend == null || dropdownFriend.index < 0 || dropdownFriend.index >= cachedFriends.Count)
            {
                ShowModalFeedback("Debes tener al menos un amigo para proponer un intercambio.", true);
                return;
            }

            if (cachedOwnedCards.Count == 0 || dropdownOfferCard == null || dropdownOfferCard.index < 0 || dropdownOfferCard.index >= cachedOwnedCards.Count)
            {
                ShowModalFeedback("No posees ninguna carta para ofrecer.", true);
                return;
            }

            if (cachedCatalogCards.Count == 0 || dropdownRequestCard == null || dropdownRequestCard.index < 0 || dropdownRequestCard.index >= cachedCatalogCards.Count)
            {
                ShowModalFeedback("Selecciona una carta válida para pedir.", true);
                return;
            }

            var friend = cachedFriends[dropdownFriend.index];
            var offered = cachedOwnedCards[dropdownOfferCard.index];
            var requested = cachedCatalogCards[dropdownRequestCard.index];

            if (offered.cardId == requested.cardId)
            {
                ShowModalFeedback("No puedes ofrecer y pedir la misma carta.", true);
                return;
            }

            btnSendTradeOffer?.SetEnabled(false);
            ShowModalFeedback("Enviando oferta a la nube...", false);

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
