using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// Maneja el filtrado entre ofertas Recibidas y Enviadas, sincronización en vivo con Firestore
    /// y el nuevo flujo de intercambio en 4 pasos según Figma (Pantallas 2.5):
    /// 1. Elegir Amigo (con atajo directo desde Amigos)
    /// 2. Elige qué dar (con filtros Álbum/Rareza/Cantidad y selección de carta)
    /// 3. Elige qué recibir (con tira de contexto "DAS: Carta [Editar]" y cartas del amigo)
    /// 4. Confirmar oferta (desglose visual completo de ambas cartas y envío a Firestore).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitTradeController : MonoBehaviour
    {
        public static string PreselectedFriendUid;
        public static string PreselectedFriendName;

        private enum TradeFlowStep
        {
            None,
            ChooseFriend,
            GiveCards,
            ReceiveCards,
            ConfirmOffer
        }

        private enum CardFilterType
        {
            Album,
            Rarity,
            Count
        }

        private UIDocument uiDocument;
        private VisualElement root;

        // Navegación principal y Tabs
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

        // ---------------------------------------------------------------------
        // NUEVO FLUJO DE INTERCAMBIO (4 PASOS)
        // ---------------------------------------------------------------------
        private VisualElement newTradeFlowView;
        private TradeFlowStep currentStep = TradeFlowStep.None;

        // Paso 0: Elegir Amigo
        private VisualElement stepChooseFriend;
        private Button btnBackChooseFriend;
        private VisualElement chooseFriendList;
        private VisualElement chooseFriendEmpty;

        // Paso 1: Elige qué dar
        private VisualElement stepGiveCards;
        private Button btnBackGiveCards;
        private Label giveCardsSubtitle;
        private Label giveCardsCounter;
        private Button filterGiveAlbum;
        private Button filterGiveRarity;
        private Button filterGiveCount;
        private VisualElement giveCardsGrid;
        private Button btnGiveContinue;

        // Paso 2: Elige qué recibir
        private VisualElement stepReceiveCards;
        private Button btnBackReceiveCards;
        private Label receiveCardsSubtitle;
        private Label receiveCardsCounter;
        private Button filterReceiveAlbum;
        private Button filterReceiveRarity;
        private Button filterReceiveCount;
        private Label givingStripCardName;
        private Button btnEditGivenCard;
        private VisualElement receiveCardsGrid;
        private VisualElement receiveCardsEmpty;
        private Label receiveCardsEmptyText;
        private Button btnReceiveReview;
        private List<FriendCardItem> currentFriendCachedCards;
        private bool isLoadingFriendCards;

        // Paso 3: Confirmar Oferta
        private VisualElement stepConfirmOffer;
        private Button btnBackConfirmOffer;
        private Label confirmFriendInitials;
        private Label confirmFriendName;
        private Label confirmFriendLevel;
        private VisualElement confirmGiveCardBox;
        private VisualElement confirmGiveCardPhoto;
        private VisualElement confirmGiveCardFrame;
        private VisualElement confirmGiveCardAvatar;
        private Label confirmGiveCardInitials;
        private Label confirmGiveCardName;
        private VisualElement confirmReceiveCardBox;
        private VisualElement confirmReceiveCardPhoto;
        private VisualElement confirmReceiveCardFrame;
        private VisualElement confirmReceiveCardAvatar;
        private Label confirmReceiveCardInitials;
        private Label confirmReceiveCardName;
        private Button btnSubmitOffer;
        private Button btnCancelConfirm;

        // Modal de Feedback / Alertas
        private VisualElement tradeFeedbackModal;
        private Label tradeFeedbackTitle;
        private Label tradeFeedbackMessage;
        private Button btnCloseTradeFeedback;

        // Datos del intercambio en curso
        private FriendData currentFriend;
        private CardCatalogItem currentGivenCard;
        private CardCatalogItem currentReceivedCard;
        private bool friendWasPreselected = false;
        private CardFilterType giveFilter = CardFilterType.Album;
        private CardFilterType receiveFilter = CardFilterType.Album;

        // Assets y Marcos Holográficos
        private readonly List<CardData> loadedCardAssets = new List<CardData>();
        private Sprite[] rarityFrames;
        private readonly Dictionary<int, RenderTexture> holoFrameRTs = new Dictionary<int, RenderTexture>();
        private Material holoMaterial;

        private static readonly string[] FrameGuids = new string[]
        {
            "9c7a6e14704b2c140938ff5d568c07e0", // 0: Comun
            "817c0936f0dbce54cba9be88a24dbb7b", // 1: Especial
            "52467f1396b27d4499d141e6e96904f4", // 2: Epica
            "d1d1f0578aa6c4f4280da7ef42aa2ce5", // 3: Legendaria
            "3f7a83d7fc03ce6439e440409a471ee5", // 4: Mitica
            "52467f1396b27d4499d141e6e96904f4"  // 5: FullArt
        };

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

            LoadCardAssets();
            EnsureRarityFrames();
            EnsureHoloMaterial();

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
                StartTradeFlow(fUid, fName);
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

        private void Update()
        {
            // Actualizar marcos holográficos animados en tiempo real
            if (holoMaterial != null && rarityFrames != null && holoFrameRTs.Count > 0)
            {
                foreach (var kvp in holoFrameRTs)
                {
                    int r = kvp.Key;
                    RenderTexture rt = kvp.Value;
                    if (r >= 0 && r < rarityFrames.Length && rarityFrames[r] != null && rt != null)
                    {
                        Graphics.Blit(rarityFrames[r].texture, rt, holoMaterial);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var rt in holoFrameRTs.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
            holoFrameRTs.Clear();

            if (holoMaterial != null)
            {
                Destroy(holoMaterial);
                holoMaterial = null;
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

            // Botón Flotante: Proponer Intercambio (+ NUEVO INTERCAMBIO)
            btnNewTrade = root.Q<Button>("Btn_NewTrade");
            if (btnNewTrade != null)
            {
                btnNewTrade.clicked += () => StartTradeFlow();
            }

            // =================================================================
            // BINDING DEL NUEVO FLUJO DE INTERCAMBIO (4 PASOS)
            // =================================================================
            newTradeFlowView = root.Q<VisualElement>("NewTradeFlowView");

            // Paso 0: Elegir Amigo
            stepChooseFriend = root.Q<VisualElement>("Step_ChooseFriend");
            btnBackChooseFriend = root.Q<Button>("Btn_Back_ChooseFriend");
            chooseFriendList = root.Q<VisualElement>("ChooseFriendList");
            chooseFriendEmpty = root.Q<VisualElement>("ChooseFriendEmpty");

            if (btnBackChooseFriend != null)
            {
                btnBackChooseFriend.clicked += CloseTradeFlow;
            }

            // Paso 1: Elige qué dar
            stepGiveCards = root.Q<VisualElement>("Step_GiveCards");
            btnBackGiveCards = root.Q<Button>("Btn_Back_GiveCards");
            giveCardsSubtitle = root.Q<Label>("GiveCardsSubtitle");
            giveCardsCounter = root.Q<Label>("GiveCardsCounter");
            filterGiveAlbum = root.Q<Button>("Filter_Give_Album");
            filterGiveRarity = root.Q<Button>("Filter_Give_Rarity");
            filterGiveCount = root.Q<Button>("Filter_Give_Count");
            giveCardsGrid = root.Q<VisualElement>("GiveCardsGrid");
            btnGiveContinue = root.Q<Button>("Btn_GiveContinue");

            if (btnBackGiveCards != null)
            {
                btnBackGiveCards.clicked += () =>
                {
                    if (friendWasPreselected)
                    {
                        CloseTradeFlow();
                    }
                    else
                    {
                        ShowTradeStep(TradeFlowStep.ChooseFriend);
                    }
                };
            }

            if (filterGiveAlbum != null)
            {
                filterGiveAlbum.clicked += () =>
                {
                    giveFilter = CardFilterType.Album;
                    UpdateFilterGiveStyles();
                    PopulateGiveCardsGrid();
                };
            }

            if (filterGiveRarity != null)
            {
                filterGiveRarity.clicked += () =>
                {
                    giveFilter = CardFilterType.Rarity;
                    UpdateFilterGiveStyles();
                    PopulateGiveCardsGrid();
                };
            }

            if (filterGiveCount != null)
            {
                filterGiveCount.clicked += () =>
                {
                    giveFilter = CardFilterType.Count;
                    UpdateFilterGiveStyles();
                    PopulateGiveCardsGrid();
                };
            }

            if (btnGiveContinue != null)
            {
                btnGiveContinue.clicked += () =>
                {
                    if (currentGivenCard != null)
                    {
                        ShowTradeStep(TradeFlowStep.ReceiveCards);
                    }
                };
            }

            // Paso 2: Elige qué recibir
            stepReceiveCards = root.Q<VisualElement>("Step_ReceiveCards");
            btnBackReceiveCards = root.Q<Button>("Btn_Back_ReceiveCards");
            receiveCardsSubtitle = root.Q<Label>("ReceiveCardsSubtitle");
            receiveCardsCounter = root.Q<Label>("ReceiveCardsCounter");
            filterReceiveAlbum = root.Q<Button>("Filter_Receive_Album");
            filterReceiveRarity = root.Q<Button>("Filter_Receive_Rarity");
            filterReceiveCount = root.Q<Button>("Filter_Receive_Count");
            givingStripCardName = root.Q<Label>("GivingStripCardName");
            btnEditGivenCard = root.Q<Button>("Btn_EditGivenCard");
            receiveCardsGrid = root.Q<VisualElement>("ReceiveCardsGrid");
            receiveCardsEmpty = root.Q<VisualElement>("ReceiveCardsEmpty");
            receiveCardsEmptyText = root.Q<Label>("ReceiveCardsEmptyText");
            btnReceiveReview = root.Q<Button>("Btn_ReceiveReview");

            if (btnBackReceiveCards != null)
            {
                btnBackReceiveCards.clicked += () => ShowTradeStep(TradeFlowStep.GiveCards);
            }

            if (btnEditGivenCard != null)
            {
                btnEditGivenCard.clicked += () => ShowTradeStep(TradeFlowStep.GiveCards);
            }

            if (filterReceiveAlbum != null)
            {
                filterReceiveAlbum.clicked += () =>
                {
                    receiveFilter = CardFilterType.Album;
                    UpdateFilterReceiveStyles();
                    PopulateReceiveCardsGrid();
                };
            }

            if (filterReceiveRarity != null)
            {
                filterReceiveRarity.clicked += () =>
                {
                    receiveFilter = CardFilterType.Rarity;
                    UpdateFilterReceiveStyles();
                    PopulateReceiveCardsGrid();
                };
            }

            if (filterReceiveCount != null)
            {
                filterReceiveCount.clicked += () =>
                {
                    receiveFilter = CardFilterType.Count;
                    UpdateFilterReceiveStyles();
                    PopulateReceiveCardsGrid();
                };
            }

            if (btnReceiveReview != null)
            {
                btnReceiveReview.clicked += () =>
                {
                    if (currentReceivedCard != null)
                    {
                        ShowTradeStep(TradeFlowStep.ConfirmOffer);
                    }
                };
            }

            // Paso 3: Confirmar Oferta
            stepConfirmOffer = root.Q<VisualElement>("Step_ConfirmOffer");
            btnBackConfirmOffer = root.Q<Button>("Btn_Back_ConfirmOffer");
            confirmFriendInitials = root.Q<Label>("ConfirmFriendInitials");
            confirmFriendName = root.Q<Label>("ConfirmFriendName");
            confirmFriendLevel = root.Q<Label>("ConfirmFriendLevel");

            confirmGiveCardBox = root.Q<VisualElement>("ConfirmGiveCardBox");
            confirmGiveCardPhoto = root.Q<VisualElement>("ConfirmGiveCardPhoto");
            confirmGiveCardFrame = root.Q<VisualElement>("ConfirmGiveCardFrame");
            confirmGiveCardAvatar = root.Q<VisualElement>("ConfirmGiveCardAvatar");
            confirmGiveCardInitials = root.Q<Label>("ConfirmGiveCardInitials");
            confirmGiveCardName = root.Q<Label>("ConfirmGiveCardName");

            confirmReceiveCardBox = root.Q<VisualElement>("ConfirmReceiveCardBox");
            confirmReceiveCardPhoto = root.Q<VisualElement>("ConfirmReceiveCardPhoto");
            confirmReceiveCardFrame = root.Q<VisualElement>("ConfirmReceiveCardFrame");
            confirmReceiveCardAvatar = root.Q<VisualElement>("ConfirmReceiveCardAvatar");
            confirmReceiveCardInitials = root.Q<Label>("ConfirmReceiveCardInitials");
            confirmReceiveCardName = root.Q<Label>("ConfirmReceiveCardName");

            btnSubmitOffer = root.Q<Button>("Btn_SubmitOffer");
            btnCancelConfirm = root.Q<Button>("Btn_CancelConfirm");

            if (btnBackConfirmOffer != null)
            {
                btnBackConfirmOffer.clicked += () => ShowTradeStep(TradeFlowStep.ReceiveCards);
            }

            if (btnCancelConfirm != null)
            {
                btnCancelConfirm.clicked += CloseTradeFlow;
            }

            if (btnSubmitOffer != null)
            {
                btnSubmitOffer.clicked += async () => await SubmitOfferAsync();
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

        #region Trade Flow Navigation & Steps

        public void StartTradeFlow(string preselectedUid = null, string preselectedName = null)
        {
            if (newTradeFlowView == null) return;

            currentGivenCard = null;
            currentReceivedCard = null;

            if (!string.IsNullOrEmpty(preselectedUid))
            {
                friendWasPreselected = true;

                // Buscar amigo en SocialService
                var friends = SocialService.Instance != null ? SocialService.Instance.Friends : null;
                currentFriend = null;
                if (friends != null)
                {
                    for (int i = 0; i < friends.Count; i++)
                    {
                        if (friends[i] != null && friends[i].friendUid == preselectedUid)
                        {
                            currentFriend = friends[i];
                            break;
                        }
                    }
                }

                if (currentFriend == null)
                {
                    currentFriend = new FriendData
                    {
                        friendUid = preselectedUid,
                        displayName = !string.IsNullOrEmpty(preselectedName) ? preselectedName : "Amigo",
                        level = 1,
                        albumProgress = 50
                    };
                }

                currentFriendCachedCards = null;
                _ = LoadFriendCardsAsync(forceRefresh: true);
                ShowTradeStep(TradeFlowStep.GiveCards);
            }
            else
            {
                friendWasPreselected = false;
                currentFriend = null;
                currentFriendCachedCards = null;
                ShowTradeStep(TradeFlowStep.ChooseFriend);
            }
        }

        // Compatibilidad hacia atrás
        public void OpenNewTradeModal(string preselectedUid = null, string preselectedName = null)
        {
            StartTradeFlow(preselectedUid, preselectedName);
        }

        public void OpenTradeFlow(string preselectedUid = null, string preselectedName = null)
        {
            StartTradeFlow(preselectedUid, preselectedName);
        }

        public void OpenTradeFlow(FriendData friend)
        {
            if (friend != null)
            {
                StartTradeFlow(friend.friendUid, friend.DisplayName);
            }
            else
            {
                StartTradeFlow();
            }
        }

        private void ShowTradeStep(TradeFlowStep step)
        {
            currentStep = step;

            if (newTradeFlowView != null)
            {
                newTradeFlowView.style.display = DisplayStyle.Flex;
                newTradeFlowView.BringToFront();
            }

            // Mantener barra inferior visible como en Figma y al frente
            var navBar = root.Q<VisualElement>("BottomNavBar");
            if (navBar != null)
            {
                navBar.style.display = DisplayStyle.Flex;
                navBar.BringToFront();
            }
            if (btnNewTrade != null) btnNewTrade.style.display = DisplayStyle.None;

            if (stepChooseFriend != null) stepChooseFriend.style.display = (step == TradeFlowStep.ChooseFriend) ? DisplayStyle.Flex : DisplayStyle.None;
            if (stepGiveCards != null) stepGiveCards.style.display = (step == TradeFlowStep.GiveCards) ? DisplayStyle.Flex : DisplayStyle.None;
            if (stepReceiveCards != null) stepReceiveCards.style.display = (step == TradeFlowStep.ReceiveCards) ? DisplayStyle.Flex : DisplayStyle.None;
            if (stepConfirmOffer != null) stepConfirmOffer.style.display = (step == TradeFlowStep.ConfirmOffer) ? DisplayStyle.Flex : DisplayStyle.None;

            switch (step)
            {
                case TradeFlowStep.ChooseFriend:
                    PopulateChooseFriendList();
                    break;
                case TradeFlowStep.GiveCards:
                    if (giveCardsSubtitle != null)
                        giveCardsSubtitle.text = "Con " + (currentFriend != null ? currentFriend.DisplayName : "Amigo");
                    UpdateFilterGiveStyles();
                    PopulateGiveCardsGrid();
                    UpdateGiveContinueButton();
                    break;
                case TradeFlowStep.ReceiveCards:
                    if (receiveCardsSubtitle != null)
                        receiveCardsSubtitle.text = "Cartas de " + (currentFriend != null ? currentFriend.DisplayName : "Amigo");
                    if (givingStripCardName != null)
                        givingStripCardName.text = currentGivenCard != null ? currentGivenCard.playerName : "-";
                    UpdateFilterReceiveStyles();
                    PopulateReceiveCardsGrid();
                    UpdateReceiveReviewButton();
                    break;
                case TradeFlowStep.ConfirmOffer:
                    PopulateConfirmOffer();
                    break;
            }
        }

        public void CloseTradeFlow()
        {
            currentStep = TradeFlowStep.None;
            currentFriend = null;
            currentGivenCard = null;
            currentReceivedCard = null;
            currentFriendCachedCards = null;
            friendWasPreselected = false;

            if (newTradeFlowView != null) newTradeFlowView.style.display = DisplayStyle.None;

            var navBar = root.Q<VisualElement>("BottomNavBar");
            if (navBar != null) navBar.style.display = DisplayStyle.Flex;
            if (btnNewTrade != null) btnNewTrade.style.display = DisplayStyle.Flex;
        }

        #endregion

        #region Step 0: Choose Friend

        private void PopulateChooseFriendList()
        {
            if (chooseFriendList == null) return;
            chooseFriendList.Clear();

            var friends = SocialService.Instance != null ? SocialService.Instance.Friends : null;

            if (friends == null || friends.Count == 0)
            {
                if (chooseFriendEmpty != null) chooseFriendEmpty.style.display = DisplayStyle.Flex;
                return;
            }

            if (chooseFriendEmpty != null) chooseFriendEmpty.style.display = DisplayStyle.None;

            foreach (var friend in friends)
            {
                var friendData = friend;
                var row = new VisualElement();
                row.AddToClassList("trade-friend-item");

                var avatar = new VisualElement();
                avatar.AddToClassList("trade-friend-avatar");
                var initials = new Label(friendData.Initials);
                initials.AddToClassList("trade-friend-initials");
                avatar.Add(initials);
                row.Add(avatar);

                var info = new VisualElement();
                info.AddToClassList("trade-friend-info");

                var nameLbl = new Label(friendData.DisplayName);
                nameLbl.AddToClassList("trade-friend-name");
                info.Add(nameLbl);

                var metaRow = new VisualElement();
                metaRow.AddToClassList("trade-friend-meta");

                var lvlTag = new Label($"NVL {friendData.level}");
                lvlTag.AddToClassList("trade-friend-level-tag");
                metaRow.Add(lvlTag);

                int cardCount = Mathf.Max(6, Mathf.RoundToInt((Mathf.Max(friendData.albumProgress, 50) / 100f) * 10));
                var cardsLbl = new Label($"{cardCount} CARTAS");
                cardsLbl.AddToClassList("trade-friend-cards-count");
                metaRow.Add(cardsLbl);

                info.Add(metaRow);

                row.Add(info);

                var chevron = new Label("›");
                chevron.AddToClassList("trade-friend-chevron");
                row.Add(chevron);

                row.RegisterCallback<ClickEvent>(evt =>
                {
                    currentFriend = friendData;
                    currentFriendCachedCards = null;
                    _ = LoadFriendCardsAsync(forceRefresh: true);
                    ShowTradeStep(TradeFlowStep.GiveCards);
                });

                chooseFriendList.Add(row);
            }
        }

        #endregion

        #region Step 1: Give Cards

        private void UpdateFilterGiveStyles()
        {
            if (filterGiveAlbum != null)
            {
                if (giveFilter == CardFilterType.Album) filterGiveAlbum.AddToClassList("pick-filter-chip-active");
                else filterGiveAlbum.RemoveFromClassList("pick-filter-chip-active");
            }
            if (filterGiveRarity != null)
            {
                if (giveFilter == CardFilterType.Rarity) filterGiveRarity.AddToClassList("pick-filter-chip-active");
                else filterGiveRarity.RemoveFromClassList("pick-filter-chip-active");
            }
            if (filterGiveCount != null)
            {
                if (giveFilter == CardFilterType.Count) filterGiveCount.AddToClassList("pick-filter-chip-active");
                else filterGiveCount.RemoveFromClassList("pick-filter-chip-active");
            }
        }

        private void PopulateGiveCardsGrid()
        {
            if (giveCardsGrid == null) return;
            giveCardsGrid.Clear();

            var catalog = PlayerCollectionManager.Instance != null ? PlayerCollectionManager.Instance.GetCatalog() : null;
            var ownedList = new List<CardCatalogItem>();

            if (catalog != null)
            {
                foreach (var card in catalog)
                {
                    int owned = PlayerCollectionManager.Instance.GetOwnedCount(card.cardId);
                    if (owned > 0)
                    {
                        ownedList.Add(card);
                    }
                }
            }

            // Aplicar ordenamiento según el filtro
            switch (giveFilter)
            {
                case CardFilterType.Album:
                    ownedList.Sort((a, b) =>
                    {
                        int c = string.Compare(a.albumId, b.albumId, StringComparison.OrdinalIgnoreCase);
                        return c != 0 ? c : string.Compare(a.playerName, b.playerName, StringComparison.OrdinalIgnoreCase);
                    });
                    break;
                case CardFilterType.Rarity:
                    ownedList.Sort((a, b) => ((int)b.rarity).CompareTo((int)a.rarity));
                    break;
                case CardFilterType.Count:
                    ownedList.Sort((a, b) =>
                    {
                        int cntA = PlayerCollectionManager.Instance.GetOwnedCount(a.cardId);
                        int cntB = PlayerCollectionManager.Instance.GetOwnedCount(b.cardId);
                        int c = cntB.CompareTo(cntA);
                        return c != 0 ? c : ((int)b.rarity).CompareTo((int)a.rarity);
                    });
                    break;
            }

            if (ownedList.Count == 0 && catalog != null && catalog.Count > 0)
            {
                // Modo pruebas/desarrollo: si el inventario está vacío, llenar 15 cartas (5 filas de 3)
                for (int i = 0; i < 15; i++)
                {
                    ownedList.Add(catalog[i % catalog.Count]);
                }
            }

            if (ownedList.Count == 0)
            {
                var emptyLabel = new Label("No posees cartas disponibles para intercambiar.");
                emptyLabel.AddToClassList("trade-empty-desc");
                emptyLabel.style.width = new Length(100, LengthUnit.Percent);
                emptyLabel.style.marginTop = 60;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                giveCardsGrid.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < ownedList.Count; i++)
            {
                var currentCard = ownedList[i];
                bool isSelected = currentGivenCard != null && currentGivenCard.cardId == currentCard.cardId;

                int count = PlayerCollectionManager.Instance != null ? PlayerCollectionManager.Instance.GetOwnedCount(currentCard.cardId) : 0;
                if (count <= 0) count = 1;

                var cardEl = CreatePickCardElement(currentCard, count, isSelected, () =>
                {
                    currentGivenCard = currentCard;
                    PopulateGiveCardsGrid();
                    UpdateGiveContinueButton();
                });

                cardEl.AddToClassList($"trade-card-col-{i % 3}");
                giveCardsGrid.Add(cardEl);
            }
        }

        private void UpdateGiveContinueButton()
        {
            if (btnGiveContinue == null) return;

            if (currentGivenCard != null)
            {
                btnGiveContinue.RemoveFromClassList("btn-trade-cta-disabled");
                if (giveCardsCounter != null) giveCardsCounter.text = "1 carta seleccionada";
            }
            else
            {
                btnGiveContinue.AddToClassList("btn-trade-cta-disabled");
                if (giveCardsCounter != null) giveCardsCounter.text = "Elige al menos 1 carta";
            }
        }

        #endregion

        #region Step 2: Receive Cards

        private void UpdateFilterReceiveStyles()
        {
            if (filterReceiveAlbum != null)
            {
                if (receiveFilter == CardFilterType.Album) filterReceiveAlbum.AddToClassList("pick-filter-chip-active");
                else filterReceiveAlbum.RemoveFromClassList("pick-filter-chip-active");
            }
            if (filterReceiveRarity != null)
            {
                if (receiveFilter == CardFilterType.Rarity) filterReceiveRarity.AddToClassList("pick-filter-chip-active");
                else filterReceiveRarity.RemoveFromClassList("pick-filter-chip-active");
            }
            if (filterReceiveCount != null)
            {
                if (receiveFilter == CardFilterType.Count) filterReceiveCount.AddToClassList("pick-filter-chip-active");
                else filterReceiveCount.RemoveFromClassList("pick-filter-chip-active");
            }
        }

        private async Task LoadFriendCardsAsync(bool forceRefresh = false)
        {
            if (currentFriend == null)
            {
                currentFriendCachedCards = new List<FriendCardItem>();
                return;
            }

            if (isLoadingFriendCards) return;
            isLoadingFriendCards = true;

            if (receiveCardsEmpty != null)
            {
                if (receiveCardsEmptyText != null) 
                    receiveCardsEmptyText.text = "Consultando cartas del amigo...";
                receiveCardsEmpty.style.display = DisplayStyle.Flex;
            }

            try
            {
                if (SocialService.Instance != null)
                {
                    currentFriendCachedCards = await SocialService.Instance.GetFriendCardsAsync(
                        currentFriend.friendUid,
                        currentFriend.DisplayName,
                        currentFriend.level > 0 ? currentFriend.level : 10,
                        currentFriend.albumProgress > 0 ? currentFriend.albumProgress : 50,
                        forceRefresh
                    );
                }
                else
                {
                    currentFriendCachedCards = new List<FriendCardItem>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TradeController] Error cargando cartas de amigo: {ex.Message}");
                currentFriendCachedCards = new List<FriendCardItem>();
            }
            finally
            {
                isLoadingFriendCards = false;
                if (receiveCardsEmptyText != null)
                    receiveCardsEmptyText.text = "Este amigo aún no tiene cartas para intercambiar.";
            }

            // Si la carta que teníamos seleccionada ya no existe en el inventario del amigo, deseleccionarla
            if (currentReceivedCard != null && currentFriendCachedCards != null)
            {
                bool stillHasIt = currentFriendCachedCards.Any(c => c != null && c.card != null && c.card.cardId == currentReceivedCard.cardId && c.count > 0);
                if (!stillHasIt)
                {
                    currentReceivedCard = null;
                    UpdateReceiveReviewButton();
                }
            }

            PopulateReceiveCardsGrid();
        }

        private void PopulateReceiveCardsGrid()
        {
            if (receiveCardsGrid == null) return;
            receiveCardsGrid.Clear();

            if (currentFriend == null) return;

            if (currentFriendCachedCards == null)
            {
                _ = LoadFriendCardsAsync(forceRefresh: true);
                return;
            }

            var friendCards = new List<FriendCardItem>(currentFriendCachedCards);

            // Excluir la carta que el usuario ya está dando para evitar intercambiar la misma
            if (currentGivenCard != null)
            {
                friendCards.RemoveAll(c => c.card != null && c.card.cardId == currentGivenCard.cardId);
            }

            switch (receiveFilter)
            {
                case CardFilterType.Album:
                    friendCards.Sort((a, b) =>
                    {
                        if (a.card == null || b.card == null) return 0;
                        int c = string.Compare(a.card.albumId, b.card.albumId, StringComparison.OrdinalIgnoreCase);
                        return c != 0 ? c : string.Compare(a.card.playerName, b.card.playerName, StringComparison.OrdinalIgnoreCase);
                    });
                    break;
                case CardFilterType.Rarity:
                    friendCards.Sort((a, b) =>
                    {
                        if (a.card == null || b.card == null) return 0;
                        return ((int)b.card.rarity).CompareTo((int)a.card.rarity);
                    });
                    break;
                case CardFilterType.Count:
                    friendCards.Sort((a, b) => b.count.CompareTo(a.count));
                    break;
            }

            if (friendCards.Count == 0)
            {
                if (receiveCardsEmpty != null) receiveCardsEmpty.style.display = DisplayStyle.Flex;
                return;
            }

            if (receiveCardsEmpty != null) receiveCardsEmpty.style.display = DisplayStyle.None;

            for (int i = 0; i < friendCards.Count; i++)
            {
                var entry = friendCards[i];
                var currentCard = entry.card;
                if (currentCard == null) continue;

                bool isSelected = currentReceivedCard != null && currentReceivedCard.cardId == currentCard.cardId;

                var cardEl = CreatePickCardElement(currentCard, entry.count, isSelected, () =>
                {
                    currentReceivedCard = currentCard;
                    PopulateReceiveCardsGrid();
                    UpdateReceiveReviewButton();
                });

                cardEl.AddToClassList($"trade-card-col-{i % 3}");
                receiveCardsGrid.Add(cardEl);
            }
        }

        private void UpdateReceiveReviewButton()
        {
            if (btnReceiveReview == null) return;

            if (currentReceivedCard != null)
            {
                btnReceiveReview.RemoveFromClassList("btn-trade-cta-disabled");
                if (receiveCardsCounter != null) receiveCardsCounter.text = "1 carta seleccionada";
            }
            else
            {
                btnReceiveReview.AddToClassList("btn-trade-cta-disabled");
                if (receiveCardsCounter != null) receiveCardsCounter.text = "Elige al menos 1 carta";
            }
        }

        #endregion

        #region Step 3: Confirm Offer

        private void PopulateConfirmOffer()
        {
            if (currentFriend != null)
            {
                if (confirmFriendInitials != null) confirmFriendInitials.text = GetInitials(currentFriend.DisplayName);
                if (confirmFriendName != null) confirmFriendName.text = currentFriend.DisplayName;
                if (confirmFriendLevel != null) confirmFriendLevel.text = $"Nivel {currentFriend.level}";
            }

            PopulateConfirmCard(
                confirmGiveCardBox, 
                confirmGiveCardPhoto, 
                confirmGiveCardFrame, 
                confirmGiveCardAvatar, 
                confirmGiveCardInitials, 
                confirmGiveCardName, 
                currentGivenCard
            );

            PopulateConfirmCard(
                confirmReceiveCardBox, 
                confirmReceiveCardPhoto, 
                confirmReceiveCardFrame, 
                confirmReceiveCardAvatar, 
                confirmReceiveCardInitials, 
                confirmReceiveCardName, 
                currentReceivedCard
            );

            if (btnSubmitOffer != null)
            {
                btnSubmitOffer.SetEnabled(true);
                var lbl = btnSubmitOffer.Q<Label>(className: "btn-confirm-submit-text");
                if (lbl != null) lbl.text = "ENVIAR OFERTA";
            }
        }

        private void PopulateConfirmCard(
            VisualElement box,
            VisualElement photo,
            VisualElement frame,
            VisualElement avatar,
            Label initials,
            Label name,
            CardCatalogItem card)
        {
            if (card == null) return;

            if (name != null) name.text = card.playerName;

            CardData asset = loadedCardAssets.Find(c => c != null && c.cardId == card.cardId);
            if (asset == null && !string.IsNullOrEmpty(card.playerName))
            {
                asset = loadedCardAssets.Find(c => c != null && c.playerName != null && c.playerName.Equals(card.playerName, StringComparison.OrdinalIgnoreCase));
            }
            Sprite defaultArt = asset != null ? asset.defaultArt : null;
            Sprite resolvedArt = DataPackManager.GetCardArt(card.cardId, defaultArt);

            int rIndex = (int)card.rarity;
            bool isHolo = (card.rarity == Rarity.Epica || card.rarity == Rarity.Legendaria || card.rarity == Rarity.Mitica || card.rarity == Rarity.FullArt);

            EnsureRarityFrames();
            EnsureHoloMaterial();

            if (resolvedArt != null)
            {
                if (photo != null)
                {
                    photo.style.display = DisplayStyle.Flex;
                    photo.RemoveFromClassList("trade-card-art-photo-default");
                    photo.style.backgroundImage = new StyleBackground(resolvedArt);
                }
                if (avatar != null) avatar.style.display = DisplayStyle.None;

                if (frame != null)
                {
                    frame.style.display = DisplayStyle.Flex;
                    if (isHolo)
                    {
                        frame.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(GetHoloFrameRT(rIndex)));
                    }
                    else if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                    {
                        frame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                    }
                }
            }
            else
            {
                if (photo != null)
                {
                    photo.style.display = DisplayStyle.Flex;
                    photo.AddToClassList("trade-card-art-photo-default");
                    photo.style.backgroundImage = StyleKeyword.Null;
                }
                if (avatar != null)
                {
                    avatar.style.display = DisplayStyle.Flex;
                }
                if (initials != null)
                {
                    initials.text = !string.IsNullOrEmpty(card.initials) ? card.initials : GetInitials(card.playerName);
                }
                if (frame != null) frame.style.display = DisplayStyle.None;
            }
        }

        private async Task SubmitOfferAsync()
        {
            if (currentFriend == null || currentGivenCard == null || currentReceivedCard == null)
            {
                ShowFeedbackModal("Error en Intercambio", "Faltan datos para enviar la oferta.", isError: true);
                return;
            }

            if (btnSubmitOffer != null)
            {
                btnSubmitOffer.SetEnabled(false);
                var lbl = btnSubmitOffer.Q<Label>(className: "btn-confirm-submit-text");
                if (lbl != null) lbl.text = "ENVIANDO...";
            }

            Debug.Log($"<color=cyan>[TradeController] Proponiendo intercambio: Amigo={currentFriend.DisplayName} (UID={currentFriend.friendUid}), Ofreces={currentGivenCard.playerName}, Pides={currentReceivedCard.playerName}</color>");

            var result = await TradeService.Instance.ProposeTradeAsync(
                currentFriend.friendUid,
                currentFriend.DisplayName,
                currentGivenCard.cardId,
                currentGivenCard.playerName,
                currentGivenCard.rarity.ToString(),
                currentReceivedCard.cardId,
                currentReceivedCard.playerName,
                currentReceivedCard.rarity.ToString()
            );

            if (result.success)
            {
                CloseTradeFlow();
                SwitchTab(false); // Llevar al usuario a ver su oferta enviada
                ShowFeedbackModal("¡Oferta Enviada!", $"Has enviado tu propuesta de intercambio a {currentFriend.DisplayName} exitosamente.", isError: false);
            }
            else
            {
                if (btnSubmitOffer != null)
                {
                    btnSubmitOffer.SetEnabled(true);
                    var lbl = btnSubmitOffer.Q<Label>(className: "btn-confirm-submit-text");
                    if (lbl != null) lbl.text = "ENVIAR OFERTA";
                }
                ShowFeedbackModal("No Se Pudo Enviar", result.message, isError: true);
            }
        }

        #endregion

        #region Card Element Creation (Grid)

        private VisualElement CreatePickCardElement(CardCatalogItem card, int ownedCount, bool isSelected, Action onClick)
        {
            var cardEl = new VisualElement();
            cardEl.AddToClassList("trade-pick-card");
            if (isSelected) cardEl.AddToClassList("trade-pick-card-selected");

            // Clase de borde según rareza
            switch (card.rarity)
            {
                case Rarity.Mitica:
                case Rarity.FullArt:
                    cardEl.AddToClassList("trade-card-mythic");
                    break;
                case Rarity.Legendaria:
                case Rarity.Epica:
                    cardEl.AddToClassList("trade-card-rare");
                    break;
                case Rarity.Especial:
                    cardEl.AddToClassList("trade-card-uncommon");
                    break;
                default:
                    cardEl.AddToClassList("trade-card-common");
                    break;
            }

            CardData asset = loadedCardAssets.Find(c => c != null && c.cardId == card.cardId);
            if (asset == null && !string.IsNullOrEmpty(card.playerName))
            {
                asset = loadedCardAssets.Find(c => c != null && c.playerName != null && c.playerName.Equals(card.playerName, StringComparison.OrdinalIgnoreCase));
            }
            Sprite defaultArt = asset != null ? asset.defaultArt : null;
            Sprite resolvedArt = DataPackManager.GetCardArt(card.cardId, defaultArt);

            int rIndex = (int)card.rarity;
            bool isHolo = (card.rarity == Rarity.Epica || card.rarity == Rarity.Legendaria || card.rarity == Rarity.Mitica || card.rarity == Rarity.FullArt);

            if (resolvedArt != null)
            {
                // Foto de la carta
                var photo = new VisualElement();
                photo.AddToClassList("trade-card-art-photo");
                photo.style.backgroundImage = new StyleBackground(resolvedArt);
                cardEl.Add(photo);

                // Marco oficial de rareza (con holográfico si aplica)
                var frame = new VisualElement();
                frame.AddToClassList("trade-card-art-frame");
                if (isHolo)
                {
                    EnsureHoloMaterial();
                    frame.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(GetHoloFrameRT(rIndex)));
                }
                else if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    frame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                }
                cardEl.Add(frame);
            }
            else
            {
                // Arte por defecto (fondo táctico de campo + círculo de avatar con iniciales)
                var photo = new VisualElement();
                photo.AddToClassList("trade-card-art-photo");
                photo.AddToClassList("trade-card-art-photo-default");
                cardEl.Add(photo);

                var avatar = new VisualElement();
                avatar.AddToClassList("trade-card-avatar");
                var initialsLbl = new Label(!string.IsNullOrEmpty(card.initials) ? card.initials : GetInitials(card.playerName));
                initialsLbl.AddToClassList("trade-card-initials");
                avatar.Add(initialsLbl);
                cardEl.Add(avatar);
            }

            // Badge de posición (DEL, MED, DEF, POR)
            if (!string.IsNullOrEmpty(card.position))
            {
                var posLbl = new Label(card.position);
                posLbl.AddToClassList("trade-card-pos");
                cardEl.Add(posLbl);
            }

            // Contador de copias si posee más de 1
            if (ownedCount > 1)
            {
                var countLbl = new Label($"x{ownedCount}");
                countLbl.AddToClassList("trade-card-count-badge");
                cardEl.Add(countLbl);
            }

            // Nombre del jugador
            var nameLbl = new Label(card.playerName);
            nameLbl.AddToClassList("trade-card-name");
            cardEl.Add(nameLbl);

            // Badge de selección
            if (isSelected)
            {
                var badge = new VisualElement();
                badge.AddToClassList("trade-check-badge");
                var checkIcon = new Label("✓");
                checkIcon.AddToClassList("trade-check-icon");
                badge.Add(checkIcon);
                cardEl.Add(badge);
            }

            cardEl.RegisterCallback<ClickEvent>(evt => onClick?.Invoke());
            return cardEl;
        }

        #endregion

        #region Holographic & Asset Loading

        private void LoadCardAssets()
        {
            loadedCardAssets.Clear();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null && !loadedCardAssets.Exists(c => c.cardId == card.cardId))
                {
                    loadedCardAssets.Add(card);
                }
            }
#endif

            CardData[] resourcePilotCards = Resources.LoadAll<CardData>("PilotAlbum");
            if (resourcePilotCards != null)
            {
                foreach (var card in resourcePilotCards)
                {
                    if (card != null && !loadedCardAssets.Exists(c => c.cardId == card.cardId))
                    {
                        loadedCardAssets.Add(card);
                    }
                }
            }

            CardData[] resourceAlbumCards = Resources.LoadAll<CardData>("Albums");
            if (resourceAlbumCards != null)
            {
                foreach (var card in resourceAlbumCards)
                {
                    if (card != null && !loadedCardAssets.Exists(c => c.cardId == card.cardId))
                    {
                        loadedCardAssets.Add(card);
                    }
                }
            }
        }

        private void EnsureRarityFrames()
        {
            if (rarityFrames == null || rarityFrames.Length < 6 || rarityFrames[0] == null)
            {
                GameObject prefab = Resources.Load<GameObject>("CardPrefab");
                if (prefab != null)
                {
                    CardDisplay cd = prefab.GetComponent<CardDisplay>();
                    if (cd != null && cd.RarityFrames != null && cd.RarityFrames.Length >= 6 && cd.RarityFrames[0] != null)
                    {
                        rarityFrames = cd.RarityFrames;
                    }
                }

#if UNITY_EDITOR
                if (rarityFrames == null || rarityFrames.Length < 6 || rarityFrames[0] == null)
                {
                    rarityFrames = new Sprite[6];
                    for (int i = 0; i < 6; i++)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(FrameGuids[i]);
                        rarityFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                }
#endif
            }
        }

        private void EnsureHoloMaterial()
        {
            if (holoMaterial == null)
            {
                GameObject prefab = Resources.Load<GameObject>("CardPrefab");
                if (prefab != null)
                {
                    CardDisplay cd = prefab.GetComponent<CardDisplay>();
                    if (cd != null && cd.HolographicMaterial != null)
                    {
                        holoMaterial = new Material(cd.HolographicMaterial);
                    }
                }

#if UNITY_EDITOR
                if (holoMaterial == null)
                {
                    Material baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/HolographicFoilMaterial.mat");
                    if (baseMat != null) holoMaterial = new Material(baseMat);
                }
#endif
                if (holoMaterial == null)
                {
                    Shader s = Shader.Find("Shader Graphs/HolographicFoilShader");
                    if (s != null) holoMaterial = new Material(s);
                }

                if (holoMaterial != null)
                {
                    holoMaterial.SetFloat("_HoloIntensity", 0.85f);
                    holoMaterial.SetFloat("_ShimmerSpeed", 1.6f);
                    holoMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    holoMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                }
            }
        }

        private RenderTexture GetHoloFrameRT(int rarityIndex)
        {
            if (!holoFrameRTs.TryGetValue(rarityIndex, out RenderTexture rt) || rt == null)
            {
                rt = new RenderTexture(360, 540, 0, RenderTextureFormat.ARGB32);
                rt.name = $"HoloFrame_Trade_RT_{rarityIndex}";
                rt.antiAliasing = 1;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = prev;

                holoFrameRTs[rarityIndex] = rt;
            }
            return rt;
        }

        #endregion

        #region Main Trade Tabs (Received / Sent)

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

                var userLbl = slot.Q<Label>(className: "trade-user-name");
                if (userLbl != null) userLbl.text = offer.fromDisplayName;

                var avatarLbl = slot.Q<Label>(className: "trade-avatar-text");
                if (avatarLbl != null) avatarLbl.text = GetInitials(offer.fromDisplayName);

                var timeLbl = slot.Q<Label>(className: "trade-time-label");
                if (timeLbl != null) timeLbl.text = offer.timeAgo;

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
                        SetTradeCardPreview(token, tokenLbl, offer.requestedCardId, offer.requestedCardName, offer.requestedRarity);
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
                        SetTradeCardPreview(token, tokenLbl, offer.offeredCardId, offer.offeredCardName, offer.offeredRarity);
                        for (int t = 1; t < tokens.Count; t++) tokens[t].style.display = DisplayStyle.None;
                    }
                }

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
                        SetTradeCardPreview(token, tokenLbl, offer.offeredCardId, offer.offeredCardName, offer.offeredRarity);
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
                        SetTradeCardPreview(token, tokenLbl, offer.requestedCardId, offer.requestedCardName, offer.requestedRarity);
                        for (int t = 1; t < tokens.Count; t++) tokens[t].style.display = DisplayStyle.None;
                    }
                }

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
            currentFriendCachedCards = null;
            SocialService.Instance?.InvalidateFriendCardsCache();
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

        private void SetTradeCardPreview(VisualElement token, Label tokenText, string cardId, string cardName, string rarityStr)
        {
            if (token == null) return;

            if (loadedCardAssets == null || loadedCardAssets.Count == 0)
            {
                LoadCardAssets();
            }

            // 1. Resolve CardData asset and CardCatalogItem
            CardData asset = null;
            if (!string.IsNullOrEmpty(cardId))
            {
                asset = loadedCardAssets.Find(c => c != null && c.cardId == cardId);
            }
            if (asset == null && !string.IsNullOrEmpty(cardName))
            {
                asset = loadedCardAssets.Find(c => c != null && !string.IsNullOrEmpty(c.playerName) &&
                    (c.playerName.Equals(cardName, StringComparison.OrdinalIgnoreCase) ||
                     cardName.IndexOf(c.playerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     c.playerName.IndexOf(cardName, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            CardCatalogItem catalogItem = null;
            if (PlayerCollectionManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(cardId))
                {
                    catalogItem = PlayerCollectionManager.Instance.GetCard(cardId);
                }
                if (catalogItem == null && !string.IsNullOrEmpty(cardName))
                {
                    var catalog = PlayerCollectionManager.Instance.GetCatalog();
                    if (catalog != null)
                    {
                        catalogItem = catalog.Find(c => c != null && !string.IsNullOrEmpty(c.playerName) &&
                            (c.playerName.Equals(cardName, StringComparison.OrdinalIgnoreCase) ||
                             cardName.IndexOf(c.playerName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                             c.playerName.IndexOf(cardName, StringComparison.OrdinalIgnoreCase) >= 0));
                    }
                }
            }

            Sprite defaultArt = asset != null ? asset.defaultArt : null;
            string lookupId = !string.IsNullOrEmpty(cardId) ? cardId : (catalogItem != null ? catalogItem.cardId : (asset != null ? asset.cardId : null));
            Sprite resolvedArt = !string.IsNullOrEmpty(lookupId) ? DataPackManager.GetCardArt(lookupId, defaultArt) : defaultArt;
            if (resolvedArt == null && defaultArt != null)
            {
                resolvedArt = defaultArt;
            }

            // 2. Resolve Rarity and Holo
            int rIndex = 0;
            bool isHolo = false;
            if (catalogItem != null)
            {
                rIndex = (int)catalogItem.rarity;
                isHolo = (catalogItem.rarity == Rarity.Epica || catalogItem.rarity == Rarity.Legendaria || catalogItem.rarity == Rarity.Mitica || catalogItem.rarity == Rarity.FullArt);
            }
            else if (asset != null)
            {
                rIndex = (int)asset.rarity;
                isHolo = (asset.rarity == Rarity.Epica || asset.rarity == Rarity.Legendaria || asset.rarity == Rarity.Mitica || asset.rarity == Rarity.FullArt);
            }
            else
            {
                string lower = (rarityStr ?? "").ToLower();
                if (lower.Contains("mitica") || lower.Contains("mythic")) { rIndex = (int)Rarity.Mitica; isHolo = true; }
                else if (lower.Contains("legendaria") || lower.Contains("legendary")) { rIndex = (int)Rarity.Legendaria; isHolo = true; }
                else if (lower.Contains("fullart")) { rIndex = (int)Rarity.FullArt; isHolo = true; }
                else if (lower.Contains("epica") || lower.Contains("epic")) { rIndex = (int)Rarity.Epica; isHolo = true; }
                else if (lower.Contains("especial") || lower.Contains("rara") || lower.Contains("rare") || lower.Contains("uncommon") || lower.Contains("pococomin")) { rIndex = (int)Rarity.Especial; }
                else { rIndex = (int)Rarity.Comun; }
            }

            EnsureRarityFrames();
            EnsureHoloMaterial();

            var photo = token.Q<VisualElement>(className: "trade-token-photo");
            if (photo == null)
            {
                photo = new VisualElement();
                photo.AddToClassList("trade-token-photo");
                token.Insert(0, photo);
            }

            var frame = token.Q<VisualElement>(className: "trade-token-frame");
            if (frame == null)
            {
                frame = new VisualElement();
                frame.AddToClassList("trade-token-frame");
                token.Insert(1, frame);
            }

            if (tokenText == null)
            {
                tokenText = token.Q<Label>(className: "trade-token-text");
            }

            token.RemoveFromClassList("trade-token-mythic");
            token.RemoveFromClassList("trade-token-rare");
            token.RemoveFromClassList("trade-token-uncommon");
            token.RemoveFromClassList("trade-token-common");

            if (resolvedArt != null)
            {
                photo.style.display = DisplayStyle.Flex;
                photo.style.backgroundImage = new StyleBackground(resolvedArt);

                frame.style.display = DisplayStyle.Flex;
                if (isHolo)
                {
                    frame.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(GetHoloFrameRT(rIndex)));
                }
                else if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    frame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                }
                else
                {
                    frame.style.display = DisplayStyle.None;
                }

                if (tokenText != null)
                {
                    tokenText.style.display = DisplayStyle.None;
                }
                token.style.borderTopWidth = 0f;
                token.style.borderRightWidth = 0f;
                token.style.borderBottomWidth = 0f;
                token.style.borderLeftWidth = 0f;
            }
            else
            {
                photo.style.display = DisplayStyle.None;
                photo.style.backgroundImage = StyleKeyword.Null;

                frame.style.display = DisplayStyle.None;
                frame.style.backgroundImage = StyleKeyword.Null;

                token.style.borderTopWidth = 2f;
                token.style.borderRightWidth = 2f;
                token.style.borderBottomWidth = 2f;
                token.style.borderLeftWidth = 2f;

                if (isHolo || rIndex >= (int)Rarity.Legendaria)
                {
                    token.AddToClassList("trade-token-mythic");
                }
                else if (rIndex >= (int)Rarity.Especial)
                {
                    token.AddToClassList("trade-token-rare");
                }
                else
                {
                    token.AddToClassList("trade-token-common");
                }

                if (tokenText != null)
                {
                    tokenText.style.display = DisplayStyle.Flex;
                    tokenText.text = !string.IsNullOrEmpty(catalogItem?.initials) ? catalogItem.initials : GetInitials(cardName);
                }
            }
        }

        private void SetTokenStyle(VisualElement token, Label tokenText, string rarityStr, string cardName)
        {
            SetTradeCardPreview(token, tokenText, null, cardName, rarityStr);
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

        #endregion
    }
}
