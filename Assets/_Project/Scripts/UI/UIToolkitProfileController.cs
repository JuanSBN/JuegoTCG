using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Cards;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Perfil (ProfileScreen).
    /// Maneja el avatar, nombre de usuario, copiado de código de amigo,
    /// la cancha táctica interactiva con el 11 Ideal (apertura de sub-pantallas modales,
    /// selección y desasignación de cartas poseídas con shader holográfico en tiempo real),
    /// cartas destacadas y navegación global.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitProfileController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        // Header & User Data
        private Button btnSettings;
        private Button btnEditAvatar;
        private Button btnEditUsername;
        private Button btnCopyFriendCode;
        private Label friendCodeText;
        private Label usernameText;
        private VisualElement profileAvatarCircle;
        private VisualElement profileAvatarIcon;

        // 11 Ideal Pitch
        private Label formationCountText;
        private static readonly string[] SlotNames = new string[]
        {
            "Slot_F1", "Slot_F2", "Slot_F3",
            "Slot_M1", "Slot_M2", "Slot_M3",
            "Slot_D1", "Slot_D2", "Slot_D3", "Slot_D4",
            "Slot_G1"
        };

        // Pitch Card Selector Modal (Figma Sub-screen)
        private VisualElement pitchCardSelectorModal;
        private Label pitchModalBadgeText;
        private Label pitchModalTitle;
        private Button btnClosePitchModal;
        private ScrollView pitchModalScrollView;
        private VisualElement pitchModalCardsGrid;
        private VisualElement pitchModalEmptyState;
        private Button btnRemoveFromPitch;
        private int currentSelectedSlotIndex = -1;

        // Featured Cards & Selector Modal (Figma Sub-screen)
        private VisualElement featuredCardSelectorModal;
        private Button btnCloseFeaturedModal;
        private Button btnFilterAlbum;
        private Button btnFilterRecientes;
        private Button btnFilterRareza;
        private Button btnFilterCantidad;
        private ScrollView featuredModalScrollView;
        private VisualElement featuredModalCardsGrid;
        private VisualElement featuredModalEmptyState;
        private Button btnRemoveFromFeatured;
        private int currentSelectedFeaturedSlotIndex = -1;
        private FeaturedSortMode currentFeaturedSortMode = FeaturedSortMode.Album;

        private enum FeaturedSortMode
        {
            Album,
            Recientes,
            Rareza,
            Cantidad
        }

        // Feedback Modal
        private VisualElement feedbackModal;
        private Label modalTitle;
        private Label modalDesc;
        private Button btnCloseModal;

        // Card Art & Shaders
        private Sprite[] pitchFrames;
        private Sprite[] rarityFrames;
        private Material holoMaterial;
        private readonly Dictionary<int, RenderTexture> holoFrameRTs = new Dictionary<int, RenderTexture>();
        private readonly Dictionary<int, RenderTexture> modalHoloFrameRTs = new Dictionary<int, RenderTexture>();
        private readonly Dictionary<int, RenderTexture> pitchHoloFrameRTs = new Dictionary<int, RenderTexture>();
        private readonly List<CardData> loadedCardAssets = new List<CardData>();

        private static readonly string[] FrameGuids = new string[]
        {
            "4edcac4ad7f822e4aa7b10b2dd755926", // Comun
            "586794b59d6595341aa4a2f2b59209ce", // Especial
            "ab60ad89df16072448c901abb76cbe3a", // Epica
            "2a89c6d7166430641b49f80a84ac2cd8", // Legendaria
            "8ab77af7592605c48b2e119ccdb7dcb3", // Mitica
            "ae059fc1520988141a79cb933243639f"  // Full Art
        };

        private static readonly string[] PitchFrameGuids = new string[]
        {
            "0c785707e701a7f1656070341226b928", // 0: Comun
            "16312b7e054849a8c1e3f5382de78a82", // 1: Especial
            "ec3a1652ae25986a9fbe3dc418b28939", // 2: Epica
            "03ac12eb306f86fde6ce1c5bfb26ee80", // 3: Legendaria
            "3b98472c66f4025f273b8054e99a7916", // 4: Mitica
            "dbfc7bdadfbc7348d8b9d4871e113582"  // 5: FullArt
        };

        private static readonly string[] PitchFrameResourceNames = new string[]
        {
            "PitchFrames/PitchFrame_Comun",
            "PitchFrames/PitchFrame_Especial",
            "PitchFrames/PitchFrame_Epica",
            "PitchFrames/PitchFrame_Legendaria",
            "PitchFrames/PitchFrame_Mitica",
            "PitchFrames/PitchFrame_FullArt"
        };

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            PlayerCollectionManager.EnsureExists();
            Ideal11SquadManager.EnsureExists();
            FeaturedCardsManager.EnsureExists();

            LoadCardAssets();
            EnsurePitchFrames();
            EnsureRarityFrames();
            EnsureHoloMaterial();

            BindUI();

            if (Ideal11SquadManager.Instance != null)
            {
                Ideal11SquadManager.Instance.OnSquadUpdated -= RefreshPitchDisplay;
                Ideal11SquadManager.Instance.OnSquadUpdated += RefreshPitchDisplay;
            }

            if (FeaturedCardsManager.Instance != null)
            {
                FeaturedCardsManager.Instance.OnFeaturedUpdated -= RefreshFeaturedCardsDisplay;
                FeaturedCardsManager.Instance.OnFeaturedUpdated += RefreshFeaturedCardsDisplay;
            }

            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated -= RefreshPitchDisplay;
                PlayerCollectionManager.Instance.OnCollectionUpdated += RefreshPitchDisplay;

                PlayerCollectionManager.Instance.OnCollectionUpdated -= RefreshFeaturedCardsDisplay;
                PlayerCollectionManager.Instance.OnCollectionUpdated += RefreshFeaturedCardsDisplay;
            }

            DataPackManager.OnDataPackReloaded -= HandleDataPackReloaded;
            DataPackManager.OnDataPackReloaded += HandleDataPackReloaded;

            RefreshPitchDisplay();
            RefreshFeaturedCardsDisplay();
        }

        private void HandleDataPackReloaded()
        {
            RefreshPitchDisplay();
            RefreshFeaturedCardsDisplay();
        }

        private void OnDisable()
        {
            DataPackManager.OnDataPackReloaded -= HandleDataPackReloaded;

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnAvatarChanged -= OnAvatarChanged;
            }

            if (Ideal11SquadManager.Instance != null)
            {
                Ideal11SquadManager.Instance.OnSquadUpdated -= RefreshPitchDisplay;
            }

            if (FeaturedCardsManager.Instance != null)
            {
                FeaturedCardsManager.Instance.OnFeaturedUpdated -= RefreshFeaturedCardsDisplay;
            }

            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated -= RefreshPitchDisplay;
                PlayerCollectionManager.Instance.OnCollectionUpdated -= RefreshFeaturedCardsDisplay;
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

            foreach (var rt in modalHoloFrameRTs.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
            modalHoloFrameRTs.Clear();

            foreach (var rt in pitchHoloFrameRTs.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
            pitchHoloFrameRTs.Clear();

            if (holoMaterial != null)
            {
                Destroy(holoMaterial);
                holoMaterial = null;
            }
        }

        private void Update()
        {
            // Actualizar marcos holográficos animados en tiempo real a 60 FPS
            if (holoMaterial != null)
            {
                // 1. Marcos oficiales para cartas destacadas (holoFrameRTs con rarityFrames)
                if (rarityFrames != null && holoFrameRTs.Count > 0)
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

                // 2. Marcos para el modal selector de cartas (modalHoloFrameRTs con rarityFrames)
                if (rarityFrames != null && modalHoloFrameRTs.Count > 0)
                {
                    foreach (var kvp in modalHoloFrameRTs)
                    {
                        int r = kvp.Key;
                        RenderTexture rt = kvp.Value;
                        if (r >= 0 && r < rarityFrames.Length && rarityFrames[r] != null && rt != null)
                        {
                            Graphics.Blit(rarityFrames[r].texture, rt, holoMaterial);
                        }
                    }
                }

                // 3. Marcos para la cancha táctica (pitchFrames)
                if (pitchFrames != null && pitchHoloFrameRTs.Count > 0)
                {
                    foreach (var kvp in pitchHoloFrameRTs)
                    {
                        int r = kvp.Key;
                        RenderTexture rt = kvp.Value;
                        if (r >= 0 && r < pitchFrames.Length && pitchFrames[r] != null && rt != null)
                        {
                            Graphics.Blit(pitchFrames[r].texture, rt, holoMaterial);
                        }
                    }
                }
            }
        }

        private void BindUI()
        {
            // Settings Button
            btnSettings = root.Q<Button>("Btn_Settings");
            if (btnSettings != null)
            {
                btnSettings.clicked += () => SceneManager.LoadScene("SettingsSceneUIToolkit");
            }

            // Copy Friend Code
            btnCopyFriendCode = root.Q<Button>("Btn_CopyFriendCode");
            friendCodeText = root.Q<Label>("FriendCodeText");
            if (btnCopyFriendCode != null)
            {
                btnCopyFriendCode.clicked += CopyFriendCode;
            }

            // Edit Profile Actions & Avatar
            btnEditAvatar = root.Q<Button>("Btn_EditAvatar");
            btnEditUsername = root.Q<Button>("Btn_EditUsername");
            usernameText = root.Q<Label>("UsernameText");
            profileAvatarCircle = root.Q<VisualElement>("ProfileAvatarCircle") ?? root.Q<VisualElement>(className: "profile-avatar-circle");
            profileAvatarIcon = root.Q<VisualElement>("ProfileAvatarIcon") ?? root.Q<VisualElement>(className: "profile-avatar-icon");

            UpdateProfileData();

            if (btnEditAvatar != null)
            {
                btnEditAvatar.clicked += () => ShowFeedbackModal("EDITAR AVATAR", "Elige un nuevo marco o ícono de jugador para personalizar tu perfil.");
            }

            if (btnEditUsername != null)
            {
                btnEditUsername.clicked += () => ShowFeedbackModal("EDITAR NOMBRE", "Puedes modificar tu apodo de entrenador desde Ajustes.");
            }

            // 11 Ideal Elements
            formationCountText = root.Q<Label>("FormationCountText");

            // Wire 11 Slots to Open Tactical Selector Sub-screen
            for (int i = 0; i < SlotNames.Length; i++)
            {
                int slotIdx = i;
                VisualElement slotEl = root.Q<VisualElement>(SlotNames[i]);
                if (slotEl != null)
                {
                    slotEl.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        OpenCardSelectorForSlot(slotIdx);
                    });
                    slotEl.RegisterCallback<PointerUpEvent>(evt =>
                    {
                        if (evt.button == 0 && (pitchCardSelectorModal == null || pitchCardSelectorModal.style.display == DisplayStyle.None))
                        {
                            OpenCardSelectorForSlot(slotIdx);
                        }
                    });
                }
            }

            // Pitch Card Selector Modal
            pitchCardSelectorModal = root.Q<VisualElement>("PitchCardSelectorModal");
            pitchModalBadgeText = root.Q<Label>("PitchModalBadgeText");
            pitchModalTitle = root.Q<Label>("PitchModalTitle");
            btnClosePitchModal = root.Q<Button>("Btn_ClosePitchModal");
            pitchModalScrollView = root.Q<ScrollView>("PitchModalScrollView");
            pitchModalCardsGrid = root.Q<VisualElement>("PitchModalCardsGrid");
            pitchModalEmptyState = root.Q<VisualElement>("PitchModalEmptyState");
            btnRemoveFromPitch = root.Q<Button>("Btn_RemoveFromPitch");

            if (pitchCardSelectorModal != null)
            {
                pitchCardSelectorModal.style.display = DisplayStyle.None;
                pitchCardSelectorModal.AddToClassList("modal-hidden");

                pitchCardSelectorModal.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == pitchCardSelectorModal)
                    {
                        ClosePitchModal();
                    }
                });
            }

            if (btnClosePitchModal != null)
            {
                btnClosePitchModal.clicked += ClosePitchModal;
            }

            if (btnRemoveFromPitch != null)
            {
                btnRemoveFromPitch.clicked += () =>
                {
                    if (currentSelectedSlotIndex >= 0 && Ideal11SquadManager.Instance != null)
                    {
                        Ideal11SquadManager.Instance.RemoveCardFromSlot(currentSelectedSlotIndex);
                        RefreshPitchDisplay();
                        ClosePitchModal();
                    }
                };
            }

            // Wire Featured Cards (3 Slots)
            for (int i = 0; i < FeaturedCardsManager.SlotCount; i++)
            {
                int slotIdx = i;
                VisualElement slotEl = root.Q<VisualElement>($"Featured_Card_{i + 1}");
                if (slotEl != null)
                {
                    slotEl.RegisterCallback<ClickEvent>(evt =>
                    {
                        evt.StopPropagation();
                        OpenFeaturedCardSelector(slotIdx);
                    });
                    slotEl.RegisterCallback<PointerUpEvent>(evt =>
                    {
                        if (evt.button == 0 && (featuredCardSelectorModal == null || featuredCardSelectorModal.style.display == DisplayStyle.None))
                        {
                            OpenFeaturedCardSelector(slotIdx);
                        }
                    });
                }
            }

            // Featured Card Selector Modal Elements
            featuredCardSelectorModal = root.Q<VisualElement>("FeaturedCardSelectorModal");
            btnCloseFeaturedModal = root.Q<Button>("Btn_CloseFeaturedModal");
            btnFilterAlbum = root.Q<Button>("Filter_Album");
            btnFilterRecientes = root.Q<Button>("Filter_Recientes");
            btnFilterRareza = root.Q<Button>("Filter_Rareza");
            btnFilterCantidad = root.Q<Button>("Filter_Cantidad");
            featuredModalScrollView = root.Q<ScrollView>("FeaturedModalScrollView");
            featuredModalCardsGrid = root.Q<VisualElement>("FeaturedModalCardsGrid");
            featuredModalEmptyState = root.Q<VisualElement>("FeaturedModalEmptyState");
            btnRemoveFromFeatured = root.Q<Button>("Btn_RemoveFromFeatured");

            if (featuredCardSelectorModal != null)
            {
                featuredCardSelectorModal.style.display = DisplayStyle.None;
                featuredCardSelectorModal.AddToClassList("modal-hidden");

                featuredCardSelectorModal.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == featuredCardSelectorModal)
                    {
                        CloseFeaturedModal();
                    }
                });
            }

            if (btnCloseFeaturedModal != null)
            {
                btnCloseFeaturedModal.clicked += CloseFeaturedModal;
            }

            if (btnFilterAlbum != null) btnFilterAlbum.clicked += () => SetFeaturedSortMode(FeaturedSortMode.Album);
            if (btnFilterRecientes != null) btnFilterRecientes.clicked += () => SetFeaturedSortMode(FeaturedSortMode.Recientes);
            if (btnFilterRareza != null) btnFilterRareza.clicked += () => SetFeaturedSortMode(FeaturedSortMode.Rareza);
            if (btnFilterCantidad != null) btnFilterCantidad.clicked += () => SetFeaturedSortMode(FeaturedSortMode.Cantidad);

            if (btnRemoveFromFeatured != null)
            {
                btnRemoveFromFeatured.clicked += () =>
                {
                    if (currentSelectedFeaturedSlotIndex >= 0 && FeaturedCardsManager.Instance != null)
                    {
                        FeaturedCardsManager.Instance.RemoveCardFromSlot(currentSelectedFeaturedSlotIndex);
                        RefreshFeaturedCardsDisplay();
                        CloseFeaturedModal();
                    }
                };
            }

            // Feedback Modal
            feedbackModal = root.Q<VisualElement>("ProfileFeedbackModal");
            modalTitle = root.Q<Label>("ModalTitle");
            modalDesc = root.Q<Label>("ModalDesc");
            btnCloseModal = root.Q<Button>("Btn_CloseModal");

            if (btnCloseModal != null)
            {
                btnCloseModal.clicked += () => feedbackModal.AddToClassList("modal-hidden");
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Perfil);
        }

        // =========================================================================
        // 11 IDEAL: PITCH RENDERING & INTERACTION
        // =========================================================================

        public void RefreshPitchDisplay()
        {
            if (root == null) return;

            Ideal11SquadManager.EnsureExists();
            PlayerCollectionManager.EnsureExists();
            Ideal11SquadManager.Instance.ValidateOwnedCards();

            int filledSlots = Ideal11SquadManager.Instance.GetFilledSlotsCount();
            if (formationCountText != null)
            {
                formationCountText.text = $"{filledSlots} / 11 espacios";
            }

            for (int i = 0; i < SlotNames.Length; i++)
            {
                VisualElement slotEl = root.Q<VisualElement>(SlotNames[i]);
                if (slotEl == null) continue;

                Label slotPlus = slotEl.Q<Label>(className: "slot-empty-plus");
                VisualElement slotOccupiedContainer = slotEl.Q<VisualElement>(className: "slot-occupied-container");
                VisualElement slotPhoto = slotEl.Q<VisualElement>(className: "slot-art-photo");
                VisualElement slotFrame = slotEl.Q<VisualElement>(className: "slot-art-frame");
                Label slotFramedName = slotEl.Q<Label>(className: "slot-player-name-framed");
                VisualElement slotFooterBox = slotEl.Q<VisualElement>(className: "slot-frame-footer-box");
                Label slotTeamLabel = slotEl.Q<Label>(className: "slot-frame-team-name");
                Label slotPosLabel = slotEl.Q<Label>(className: "slot-frame-pos-text");
                Label slotRarityLabel = slotEl.Q<Label>(className: "slot-frame-rarity-text");
                VisualElement slotFallback = slotEl.Q<VisualElement>(className: "slot-avatar-fallback");
                Label slotInitials = slotEl.Q<Label>(className: "slot-avatar-initials");
                Label slotName = slotEl.Q<Label>(className: "slot-player-name");

                slotEl.RemoveFromClassList("slot-occupied");
                slotEl.RemoveFromClassList("slot-has-art");
                slotEl.RemoveFromClassList("slot-mythic");
                slotEl.RemoveFromClassList("slot-rare");
                slotEl.RemoveFromClassList("slot-uncommon");
                slotEl.RemoveFromClassList("slot-common");

                PitchSlotData slotData = Ideal11SquadManager.Instance.GetSlot(i);
                if (slotData == null || string.IsNullOrEmpty(slotData.assignedCardId))
                {
                    if (slotPlus != null) slotPlus.style.display = DisplayStyle.Flex;
                    if (slotOccupiedContainer != null) slotOccupiedContainer.style.display = DisplayStyle.None;
                    if (slotFramedName != null) slotFramedName.style.display = DisplayStyle.None;
                    if (slotFooterBox != null) slotFooterBox.style.display = DisplayStyle.None;
                    if (slotName != null) slotName.style.display = DisplayStyle.None;
                    continue;
                }

                string cardId = slotData.assignedCardId;
                CardCatalogItem cardItem = PlayerCollectionManager.Instance.GetCard(cardId);
                if (cardItem == null || !PlayerCollectionManager.Instance.IsCardOwned(cardId))
                {
                    if (slotPlus != null) slotPlus.style.display = DisplayStyle.Flex;
                    if (slotOccupiedContainer != null) slotOccupiedContainer.style.display = DisplayStyle.None;
                    if (slotFramedName != null) slotFramedName.style.display = DisplayStyle.None;
                    if (slotFooterBox != null) slotFooterBox.style.display = DisplayStyle.None;
                    if (slotName != null) slotName.style.display = DisplayStyle.None;
                    continue;
                }

                slotEl.AddToClassList("slot-occupied");
                slotEl.AddToClassList(GetSlotRarityClass(cardItem.rarity));

                if (slotPlus != null) slotPlus.style.display = DisplayStyle.None;
                if (slotOccupiedContainer != null) slotOccupiedContainer.style.display = DisplayStyle.Flex;

                CardData asset = loadedCardAssets.Find(c => c.cardId == cardId);
                Sprite cardArt = DataPackManager.GetCardArt(cardId, asset != null ? asset.defaultArt : null);

                EnsureRarityFrames();
                EnsureHoloMaterial();

                int rIndex = (int)cardItem.rarity;
                bool isHolo = (cardItem.rarity == Rarity.Epica || cardItem.rarity == Rarity.Legendaria || cardItem.rarity == Rarity.Mitica || cardItem.rarity == Rarity.FullArt);

                // Marco oficial uniforme para TODAS las cartas del 11 Ideal (con foto y sin foto)
                if (slotFrame != null)
                {
                    if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                    {
                        if (isHolo)
                        {
                            RenderTexture holoRT = GetHoloFrameRT(rIndex);
                            slotFrame.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(holoRT));
                        }
                        else
                        {
                            slotFrame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                        }
                        slotFrame.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        slotFrame.style.display = DisplayStyle.None;
                    }
                }

                // Foto vs Fallback de iniciales
                if (cardArt != null)
                {
                    slotEl.AddToClassList("slot-has-art");

                    if (slotPhoto != null)
                    {
                        slotPhoto.RemoveFromClassList("slot-art-photo-default");
                        slotPhoto.style.backgroundImage = new StyleBackground(cardArt);
                        slotPhoto.style.display = DisplayStyle.Flex;
                    }
                    if (slotFallback != null) slotFallback.style.display = DisplayStyle.None;
                }
                else
                {
                    slotEl.RemoveFromClassList("slot-has-art");

                    if (slotPhoto != null)
                    {
                        slotPhoto.style.backgroundImage = null;
                        slotPhoto.AddToClassList("slot-art-photo-default");
                        slotPhoto.style.display = DisplayStyle.Flex;
                    }
                    if (slotFallback != null)
                    {
                        slotFallback.style.display = DisplayStyle.Flex;
                        if (slotInitials != null) slotInitials.text = cardItem.DisplayInitials;
                    }
                }

                if (slotFramedName != null)
                {
                    slotFramedName.text = cardItem.DisplayPlayerName;
                    slotFramedName.style.display = DisplayStyle.Flex;
                }

                if (slotFooterBox != null)
                {
                    if (slotTeamLabel != null) slotTeamLabel.text = !string.IsNullOrEmpty(cardItem.DisplayTeamName) ? cardItem.DisplayTeamName : (asset != null ? asset.DisplayTeamName : "FC Barca");
                    if (slotPosLabel != null) slotPosLabel.text = cardItem.DisplayPosition.ToString().ToUpper();
                    if (slotRarityLabel != null) slotRarityLabel.text = cardItem.rarity.ToString().ToUpper();
                    slotFooterBox.style.display = DisplayStyle.Flex;
                }

                if (slotName != null)
                {
                    slotName.style.display = DisplayStyle.None;
                }
            }
        }

        private void OpenCardSelectorForSlot(int slotIndex)
        {
            Debug.Log($"<color=cyan>[Profile] Abriendo selector para slot #{slotIndex}...</color>");
            currentSelectedSlotIndex = slotIndex;

            Ideal11SquadManager.EnsureExists();
            PlayerCollectionManager.EnsureExists();

            PitchSlotData slotData = Ideal11SquadManager.Instance != null ? Ideal11SquadManager.Instance.GetSlot(slotIndex) : null;
            TacticalPosition targetLine;
            if (slotData != null && !string.IsNullOrEmpty(slotData.positionCode))
            {
                targetLine = TacticalPositionHelper.FromSlotCode(slotData.positionCode);
            }
            else
            {
                if (slotIndex <= 2) targetLine = TacticalPosition.DEL;
                else if (slotIndex <= 5) targetLine = TacticalPosition.MED;
                else if (slotIndex <= 9) targetLine = TacticalPosition.DEF;
                else targetLine = TacticalPosition.POR;
            }

            if (pitchModalBadgeText != null)
            {
                pitchModalBadgeText.text = TacticalPositionHelper.GetDisplayName(targetLine);
            }

            if (pitchModalTitle != null)
            {
                pitchModalTitle.text = TacticalPositionHelper.GetModalTitle(targetLine);
            }

            string assignedCardId = slotData != null ? slotData.assignedCardId : "";
            if (btnRemoveFromPitch != null)
            {
                btnRemoveFromPitch.style.display = !string.IsNullOrEmpty(assignedCardId) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (pitchModalCardsGrid != null)
            {
                pitchModalCardsGrid.Clear();
            }

            // Filtrado estricto: Cartas REALES del juego en inventario del jugador (GetOwnedCount > 0) que coincidan con targetLine
            List<CardCatalogItem> allCatalog = PlayerCollectionManager.Instance != null ? PlayerCollectionManager.Instance.GetCatalog() : new List<CardCatalogItem>();
            List<CardCatalogItem> eligibleCards = new List<CardCatalogItem>();
            foreach (var card in allCatalog)
            {
                if (PlayerCollectionManager.Instance != null && PlayerCollectionManager.Instance.GetOwnedCount(card.cardId) > 0 && card.TacticalLine == targetLine)
                {
                    eligibleCards.Add(card);
                }
            }

            // Ordenar por rareza descendente (Mítica > Legendaria > Épica > Especial > Común)
            eligibleCards.Sort((a, b) => ((int)b.rarity).CompareTo((int)a.rarity));

            if (eligibleCards.Count == 0)
            {
                if (pitchModalCardsGrid != null) pitchModalCardsGrid.style.display = DisplayStyle.None;
                if (pitchModalEmptyState != null) pitchModalEmptyState.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (pitchModalCardsGrid != null) pitchModalCardsGrid.style.display = DisplayStyle.Flex;
                if (pitchModalEmptyState != null) pitchModalEmptyState.style.display = DisplayStyle.None;

                foreach (var cardItem in eligibleCards)
                {
                    Button cardBtn = CreateModalCardItem(cardItem, assignedCardId);
                    if (pitchModalCardsGrid != null) pitchModalCardsGrid.Add(cardBtn);
                }
            }

            if (pitchCardSelectorModal != null)
            {
                pitchCardSelectorModal.style.display = DisplayStyle.Flex;
                pitchCardSelectorModal.RemoveFromClassList("modal-hidden");
                pitchCardSelectorModal.BringToFront();
                Debug.Log($"<color=green>[Profile] Modal de {targetLine} ABIERTO Y VISIBLE (Cartas elegibles: {eligibleCards.Count})</color>");
            }
            else
            {
                Debug.LogError("[Profile] pitchCardSelectorModal no encontrado en el UIDocument!");
            }
        }

        private Button CreateModalCardItem(CardCatalogItem item, string currentAssignedId)
        {
            Button cardBtn = new Button();
            cardBtn.AddToClassList("pitch-selector-card");
            cardBtn.AddToClassList(GetCardRarityClass(item.rarity));

            if (item.cardId == currentAssignedId)
            {
                cardBtn.AddToClassList("card-selected");
            }

            CardData asset = loadedCardAssets.Find(c => c.cardId == item.cardId);
            Sprite cardArt = DataPackManager.GetCardArt(item.cardId, asset != null ? asset.defaultArt : null);

            if (cardArt != null)
            {
                // 1. Foto del Jugador (Data Pack o defaultArt)
                VisualElement photoEl = new VisualElement();
                photoEl.AddToClassList("pitch-card-photo");
                photoEl.style.backgroundImage = new StyleBackground(cardArt);
                cardBtn.Add(photoEl);

                // 2. Marco con Shader Holográfico si corresponde
                int rIndex = (int)item.rarity;
                bool isHolo = (item.rarity == Rarity.Epica || item.rarity == Rarity.Legendaria || item.rarity == Rarity.Mitica || item.rarity == Rarity.FullArt);

                EnsureRarityFrames();
                EnsureHoloMaterial();

                if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    VisualElement frameEl = new VisualElement();
                    frameEl.AddToClassList("pitch-card-frame");
                    if (isHolo)
                    {
                        RenderTexture holoRT = GetModalHoloFrameRT(rIndex);
                        frameEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(holoRT));
                    }
                    else
                    {
                        frameEl.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                    }
                    cardBtn.Add(frameEl);
                }

                // 3. Overlay inferior con Nombre y Píldora de Rareza
                VisualElement overlay = new VisualElement();
                overlay.AddToClassList("pitch-card-art-overlay");

                Label nameLbl = new Label(item.DisplayPlayerName);
                nameLbl.AddToClassList("pitch-card-art-name");
                overlay.Add(nameLbl);

                VisualElement pill = new VisualElement();
                pill.AddToClassList("pitch-card-art-pill");
                Label rarityLbl = new Label(GetRarityDisplayName(item.rarity));
                rarityLbl.AddToClassList("pitch-card-art-rarity-text");
                pill.Add(rarityLbl);
                overlay.Add(pill);

                cardBtn.Add(overlay);
            }
            else
            {
                // Fallback de Arte por Defecto (Círculo con iniciales, Nombre y Píldora)
                VisualElement fallbackContainer = new VisualElement();
                fallbackContainer.AddToClassList("pitch-card-fallback-container");

                VisualElement circle = new VisualElement();
                circle.AddToClassList("pitch-card-avatar-circle");
                Label inits = new Label(item.DisplayInitials);
                inits.AddToClassList("pitch-card-avatar-initials");
                circle.Add(inits);
                fallbackContainer.Add(circle);

                Label nameLbl = new Label(item.DisplayPlayerName);
                nameLbl.AddToClassList("pitch-card-name-label");
                fallbackContainer.Add(nameLbl);

                VisualElement pill = new VisualElement();
                pill.AddToClassList("pitch-card-rarity-pill");
                Label rarityLbl = new Label(GetRarityDisplayName(item.rarity));
                rarityLbl.AddToClassList("pitch-card-rarity-text");
                pill.Add(rarityLbl);
                fallbackContainer.Add(pill);

                cardBtn.Add(fallbackContainer);
            }

            // Indicador si la carta ya está asignada en el 11 Ideal
            if (Ideal11SquadManager.Instance.IsCardInSquad(item.cardId))
            {
                VisualElement inSquadBadge = new VisualElement();
                inSquadBadge.AddToClassList("pitch-card-in-squad-badge");
                Label inSquadText = new Label(item.cardId == currentAssignedId ? "ACTUAL" : "EN 11");
                inSquadText.AddToClassList("pitch-card-in-squad-text");
                inSquadBadge.Add(inSquadText);
                cardBtn.Add(inSquadBadge);
            }

            // Selección de carta
            cardBtn.clicked += () =>
            {
                if (currentSelectedSlotIndex >= 0 && Ideal11SquadManager.Instance != null)
                {
                    Ideal11SquadManager.Instance.AssignCardToSlot(currentSelectedSlotIndex, item.cardId);
                    RefreshPitchDisplay();
                    ClosePitchModal();
                }
            };

            return cardBtn;
        }

        private void ClosePitchModal()
        {
            if (pitchCardSelectorModal != null)
            {
                pitchCardSelectorModal.style.display = DisplayStyle.None;
                pitchCardSelectorModal.AddToClassList("modal-hidden");
            }
            currentSelectedSlotIndex = -1;
        }

        // =========================================================================
        // CARTAS DESTACADAS: RENDERING, MODAL & FILTERS (FIGMA)
        // =========================================================================

        public void RefreshFeaturedCardsDisplay()
        {
            if (root == null) return;

            FeaturedCardsManager.EnsureExists();
            PlayerCollectionManager.EnsureExists();
            FeaturedCardsManager.Instance.ValidateOwnedCards();

            EnsureRarityFrames();
            EnsureHoloMaterial();

            for (int i = 0; i < FeaturedCardsManager.SlotCount; i++)
            {
                VisualElement cardEl = root.Q<VisualElement>($"Featured_Card_{i + 1}");
                if (cardEl == null) continue;

                VisualElement photoEl = cardEl.Q<VisualElement>(className: "featured-art-photo");
                VisualElement frameEl = cardEl.Q<VisualElement>(className: "featured-art-frame");
                Label framedNameLabel = cardEl.Q<Label>(className: "featured-player-name-framed");
                VisualElement bottomRow = cardEl.Q<VisualElement>(className: "featured-bottom-row");
                VisualElement flagEl = cardEl.Q<VisualElement>(className: "featured-flag-image");
                Label s1Title = cardEl.Q<Label>($"Featured_Stat1Title_{i + 1}");
                Label s1Val = cardEl.Q<Label>($"Featured_Stat1Val_{i + 1}");
                Label s2Title = cardEl.Q<Label>($"Featured_Stat2Title_{i + 1}");
                Label s2Val = cardEl.Q<Label>($"Featured_Stat2Val_{i + 1}");
                Label s3Title = cardEl.Q<Label>($"Featured_Stat3Title_{i + 1}");
                Label s3Val = cardEl.Q<Label>($"Featured_Stat3Val_{i + 1}");
                Label s4Title = cardEl.Q<Label>($"Featured_Stat4Title_{i + 1}");
                Label s4Val = cardEl.Q<Label>($"Featured_Stat4Val_{i + 1}");
                VisualElement avatarCircle = cardEl.Q<VisualElement>(className: "featured-avatar-circle");
                Label avatarText = cardEl.Q<Label>(className: "featured-avatar-text");
                VisualElement emptyContainer = cardEl.Q<VisualElement>(className: "featured-empty-container");
                Label nameLabel = cardEl.Q<Label>(className: "featured-card-name");

                cardEl.RemoveFromClassList("featured-card-empty");
                cardEl.RemoveFromClassList("featured-card-mythic");
                cardEl.RemoveFromClassList("featured-card-rare");
                cardEl.RemoveFromClassList("featured-card-uncommon");
                cardEl.RemoveFromClassList("featured-card-common");
                cardEl.RemoveFromClassList("featured-card-has-photo");
                cardEl.RemoveFromClassList("featured-card-occupied");

                string cardId = FeaturedCardsManager.Instance.GetSlotCardId(i);
                if (string.IsNullOrEmpty(cardId) || !PlayerCollectionManager.Instance.IsCardOwned(cardId))
                {
                    cardEl.AddToClassList("featured-card-empty");
                    if (photoEl != null) photoEl.style.display = DisplayStyle.None;
                    if (frameEl != null) frameEl.style.display = DisplayStyle.None;
                    if (framedNameLabel != null) framedNameLabel.style.display = DisplayStyle.None;
                    if (bottomRow != null) bottomRow.style.display = DisplayStyle.None;
                    if (avatarCircle != null) avatarCircle.style.display = DisplayStyle.None;
                    if (emptyContainer != null) emptyContainer.style.display = DisplayStyle.Flex;
                    if (nameLabel != null) nameLabel.style.display = DisplayStyle.None;
                    continue;
                }

                CardCatalogItem cardItem = PlayerCollectionManager.Instance.GetCard(cardId);
                if (cardItem == null)
                {
                    cardEl.AddToClassList("featured-card-empty");
                    if (photoEl != null) photoEl.style.display = DisplayStyle.None;
                    if (frameEl != null) frameEl.style.display = DisplayStyle.None;
                    if (framedNameLabel != null) framedNameLabel.style.display = DisplayStyle.None;
                    if (bottomRow != null) bottomRow.style.display = DisplayStyle.None;
                    if (avatarCircle != null) avatarCircle.style.display = DisplayStyle.None;
                    if (emptyContainer != null) emptyContainer.style.display = DisplayStyle.Flex;
                    if (nameLabel != null) nameLabel.style.display = DisplayStyle.None;
                    continue;
                }

                if (emptyContainer != null) emptyContainer.style.display = DisplayStyle.None;
                cardEl.AddToClassList("featured-card-occupied");
                cardEl.AddToClassList(GetFeaturedCardRarityClass(cardItem.rarity));

                CardData asset = loadedCardAssets.Find(c => c.cardId == cardId);
                Sprite cardArt = DataPackManager.GetCardArt(cardId, asset != null ? asset.defaultArt : null);

                int rIndex = (int)cardItem.rarity;
                bool isHolo = (cardItem.rarity == Rarity.Epica || cardItem.rarity == Rarity.Legendaria || cardItem.rarity == Rarity.Mitica || cardItem.rarity == Rarity.FullArt);

                if (cardArt != null)
                {
                    cardEl.AddToClassList("featured-card-has-photo");
                    if (photoEl != null)
                    {
                        photoEl.RemoveFromClassList("featured-art-photo-default");
                        photoEl.style.backgroundImage = new StyleBackground(cardArt);
                        photoEl.style.display = DisplayStyle.Flex;
                    }
                    if (avatarCircle != null) avatarCircle.style.display = DisplayStyle.None;
                }
                else
                {
                    if (photoEl != null)
                    {
                        photoEl.style.backgroundImage = null;
                        photoEl.AddToClassList("featured-art-photo-default");
                        photoEl.style.display = DisplayStyle.Flex;
                    }
                    if (avatarCircle != null)
                    {
                        avatarCircle.style.display = DisplayStyle.Flex;
                        if (avatarText != null) avatarText.text = cardItem.DisplayInitials;
                    }
                }

                if (frameEl != null && rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    if (isHolo)
                    {
                        RenderTexture holoRT = GetHoloFrameRT(rIndex);
                        frameEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(holoRT));
                    }
                    else
                    {
                        frameEl.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                    }
                    frameEl.style.display = DisplayStyle.Flex;
                }
                else if (frameEl != null)
                {
                    frameEl.style.display = DisplayStyle.None;
                }

                if (framedNameLabel != null)
                {
                    framedNameLabel.text = cardItem.DisplayPlayerName;
                    framedNameLabel.style.display = DisplayStyle.Flex;
                }

                // Fila Inferior: Bandera afuera a la izquierda + Estadísticas dentro del marco
                if (bottomRow != null)
                {
                    string countryCode = !string.IsNullOrEmpty(cardItem.DisplayCountryCode) ? cardItem.DisplayCountryCode : (asset != null ? asset.DisplayCountryCode : "ES");
                    Sprite flagSprite = CountryFlagService.GetFlag(countryCode);
                    if (flagEl != null)
                    {
                        if (flagSprite != null)
                        {
                            flagEl.style.backgroundImage = new StyleBackground(flagSprite);
                            flagEl.style.display = DisplayStyle.Flex;
                        }
                        else
                        {
                            flagEl.style.display = DisplayStyle.None;
                        }
                    }

                    var stats = cardItem.GetDisplayStats();
                    if (s1Title != null) s1Title.text = stats.stat1Name;
                    if (s1Val != null) s1Val.text = stats.stat1Value.ToString();
                    if (s2Title != null) s2Title.text = stats.stat2Name;
                    if (s2Val != null) s2Val.text = stats.stat2Value.ToString();
                    if (s3Title != null) s3Title.text = stats.stat3Name;
                    if (s3Val != null) s3Val.text = stats.stat3Value.ToString();
                    if (s4Title != null) s4Title.text = stats.stat4Name;
                    if (s4Val != null) s4Val.text = stats.stat4Value.ToString();

                    bottomRow.style.display = DisplayStyle.Flex;
                }

                if (nameLabel != null) nameLabel.style.display = DisplayStyle.None;
            }
        }

        private void OpenFeaturedCardSelector(int slotIndex)
        {
            Debug.Log($"<color=cyan>[Profile] Abriendo selector de cartas destacadas para slot #{slotIndex}...</color>");
            currentSelectedFeaturedSlotIndex = slotIndex;

            FeaturedCardsManager.EnsureExists();
            PlayerCollectionManager.EnsureExists();

            string assignedCardId = FeaturedCardsManager.Instance.GetSlotCardId(slotIndex);
            if (btnRemoveFromFeatured != null)
            {
                btnRemoveFromFeatured.style.display = !string.IsNullOrEmpty(assignedCardId) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateFilterPillsUI();
            PopulateFeaturedModalGrid();

            if (featuredCardSelectorModal != null)
            {
                featuredCardSelectorModal.style.display = DisplayStyle.Flex;
                featuredCardSelectorModal.RemoveFromClassList("modal-hidden");
                featuredCardSelectorModal.BringToFront();
            }
        }

        private void CloseFeaturedModal()
        {
            if (featuredCardSelectorModal != null)
            {
                featuredCardSelectorModal.style.display = DisplayStyle.None;
                featuredCardSelectorModal.AddToClassList("modal-hidden");
            }
            currentSelectedFeaturedSlotIndex = -1;
        }

        private void SetFeaturedSortMode(FeaturedSortMode sortMode)
        {
            currentFeaturedSortMode = sortMode;
            UpdateFilterPillsUI();
            PopulateFeaturedModalGrid();
        }

        private void UpdateFilterPillsUI()
        {
            btnFilterAlbum?.RemoveFromClassList("filter-pill-active");
            btnFilterRecientes?.RemoveFromClassList("filter-pill-active");
            btnFilterRareza?.RemoveFromClassList("filter-pill-active");
            btnFilterCantidad?.RemoveFromClassList("filter-pill-active");

            switch (currentFeaturedSortMode)
            {
                case FeaturedSortMode.Album:
                    btnFilterAlbum?.AddToClassList("filter-pill-active");
                    break;
                case FeaturedSortMode.Recientes:
                    btnFilterRecientes?.AddToClassList("filter-pill-active");
                    break;
                case FeaturedSortMode.Rareza:
                    btnFilterRareza?.AddToClassList("filter-pill-active");
                    break;
                case FeaturedSortMode.Cantidad:
                    btnFilterCantidad?.AddToClassList("filter-pill-active");
                    break;
            }
        }

        private void PopulateFeaturedModalGrid()
        {
            if (featuredModalCardsGrid != null)
            {
                featuredModalCardsGrid.Clear();
            }

            if (PlayerCollectionManager.Instance == null) return;

            List<CardCatalogItem> allCatalog = PlayerCollectionManager.Instance.GetCatalog();
            List<CardCatalogItem> ownedCards = new List<CardCatalogItem>();

            foreach (var card in allCatalog)
            {
                if (PlayerCollectionManager.Instance.GetOwnedCount(card.cardId) > 0)
                {
                    ownedCards.Add(card);
                }
            }

            switch (currentFeaturedSortMode)
            {
                case FeaturedSortMode.Album:
                    ownedCards.Sort((a, b) => string.Compare(a.cardId, b.cardId, StringComparison.Ordinal));
                    break;
                case FeaturedSortMode.Recientes:
                    ownedCards.Reverse();
                    break;
                case FeaturedSortMode.Rareza:
                    ownedCards.Sort((a, b) =>
                    {
                        int rComp = ((int)b.rarity).CompareTo((int)a.rarity);
                        if (rComp != 0) return rComp;
                        return string.Compare(a.playerName, b.playerName, StringComparison.Ordinal);
                    });
                    break;
                case FeaturedSortMode.Cantidad:
                    ownedCards.Sort((a, b) =>
                    {
                        int countA = PlayerCollectionManager.Instance.GetOwnedCount(a.cardId);
                        int countB = PlayerCollectionManager.Instance.GetOwnedCount(b.cardId);
                        int cComp = countB.CompareTo(countA);
                        if (cComp != 0) return cComp;
                        return ((int)b.rarity).CompareTo((int)a.rarity);
                    });
                    break;
            }

            if (ownedCards.Count == 0)
            {
                if (featuredModalCardsGrid != null) featuredModalCardsGrid.style.display = DisplayStyle.None;
                if (featuredModalEmptyState != null) featuredModalEmptyState.style.display = DisplayStyle.Flex;
                return;
            }

            if (featuredModalCardsGrid != null) featuredModalCardsGrid.style.display = DisplayStyle.Flex;
            if (featuredModalEmptyState != null) featuredModalEmptyState.style.display = DisplayStyle.None;

            string currentAssignedId = currentSelectedFeaturedSlotIndex >= 0 ? FeaturedCardsManager.Instance.GetSlotCardId(currentSelectedFeaturedSlotIndex) : "";

            foreach (var cardItem in ownedCards)
            {
                Button cardBtn = CreateFeaturedModalCardItem(cardItem, currentAssignedId);
                if (featuredModalCardsGrid != null) featuredModalCardsGrid.Add(cardBtn);
            }
        }

        private Button CreateFeaturedModalCardItem(CardCatalogItem item, string currentAssignedId)
        {
            Button cardBtn = new Button();
            cardBtn.AddToClassList("pitch-selector-card");
            cardBtn.AddToClassList(GetCardRarityClass(item.rarity));

            if (item.cardId == currentAssignedId)
            {
                cardBtn.AddToClassList("card-selected");

                VisualElement checkBadge = new VisualElement();
                checkBadge.AddToClassList("featured-check-badge");
                Label checkIcon = new Label("✓");
                checkIcon.AddToClassList("featured-check-icon");
                checkBadge.Add(checkIcon);
                cardBtn.Add(checkBadge);
            }
            else if (FeaturedCardsManager.Instance != null && FeaturedCardsManager.Instance.IsCardFeatured(item.cardId))
            {
                VisualElement inSlotBadge = new VisualElement();
                inSlotBadge.AddToClassList("pitch-card-in-squad-badge");
                Label inSlotText = new Label("DESTACADA");
                inSlotText.AddToClassList("pitch-card-in-squad-text");
                inSlotBadge.Add(inSlotText);
                cardBtn.Add(inSlotBadge);
            }

            CardData asset = loadedCardAssets.Find(c => c.cardId == item.cardId);
            Sprite cardArt = DataPackManager.GetCardArt(item.cardId, asset != null ? asset.defaultArt : null);

            if (cardArt != null)
            {
                // 1. Foto
                VisualElement photoEl = new VisualElement();
                photoEl.AddToClassList("pitch-card-photo");
                photoEl.style.backgroundImage = new StyleBackground(cardArt);
                cardBtn.Add(photoEl);

                // 2. Marco con shader holográfico si corresponde
                int rIndex = (int)item.rarity;
                bool isHolo = (item.rarity == Rarity.Epica || item.rarity == Rarity.Legendaria || item.rarity == Rarity.Mitica || item.rarity == Rarity.FullArt);

                EnsureRarityFrames();
                EnsureHoloMaterial();

                if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    VisualElement frameEl = new VisualElement();
                    frameEl.AddToClassList("pitch-card-frame");
                    if (isHolo)
                    {
                        RenderTexture holoRT = GetModalHoloFrameRT(rIndex);
                        frameEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(holoRT));
                    }
                    else
                    {
                        frameEl.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                    }
                    cardBtn.Add(frameEl);
                }

                // 3. Overlay inferior
                VisualElement overlay = new VisualElement();
                overlay.AddToClassList("pitch-card-art-overlay");

                Label nameLbl = new Label(item.DisplayPlayerName);
                nameLbl.AddToClassList("pitch-card-art-name");
                overlay.Add(nameLbl);

                VisualElement pill = new VisualElement();
                pill.AddToClassList("pitch-card-art-pill");
                Label rarityLbl = new Label(GetRarityDisplayName(item.rarity));
                rarityLbl.AddToClassList("pitch-card-art-rarity-text");
                pill.Add(rarityLbl);
                overlay.Add(pill);

                cardBtn.Add(overlay);
            }
            else
            {
                // Fallback de Arte por Defecto
                VisualElement fallbackContainer = new VisualElement();
                fallbackContainer.AddToClassList("pitch-card-fallback-container");

                VisualElement circle = new VisualElement();
                circle.AddToClassList("pitch-card-avatar-circle");
                Label inits = new Label(item.DisplayInitials);
                inits.AddToClassList("pitch-card-avatar-initials");
                circle.Add(inits);
                fallbackContainer.Add(circle);

                Label nameLbl = new Label(item.DisplayPlayerName);
                nameLbl.AddToClassList("pitch-card-name-label");
                fallbackContainer.Add(nameLbl);

                VisualElement pill = new VisualElement();
                pill.AddToClassList("pitch-card-rarity-pill");
                Label rarityLbl = new Label(GetRarityDisplayName(item.rarity));
                rarityLbl.AddToClassList("pitch-card-rarity-text");
                pill.Add(rarityLbl);
                fallbackContainer.Add(pill);

                cardBtn.Add(fallbackContainer);
            }

            cardBtn.clicked += () =>
            {
                if (currentSelectedFeaturedSlotIndex >= 0 && FeaturedCardsManager.Instance != null)
                {
                    FeaturedCardsManager.Instance.AssignCardToSlot(currentSelectedFeaturedSlotIndex, item.cardId);
                    RefreshFeaturedCardsDisplay();
                    CloseFeaturedModal();
                }
            };

            return cardBtn;
        }

        private string GetFeaturedCardRarityClass(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica:
                case Rarity.FullArt: return "featured-card-mythic";
                case Rarity.Legendaria:
                case Rarity.Epica: return "featured-card-rare";
                case Rarity.Especial: return "featured-card-uncommon";
                default: return "featured-card-common";
            }
        }

        // =========================================================================
        // CARD ASSETS & HOLOGRAPHIC FRAME ENGINE
        // =========================================================================

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

        private void EnsurePitchFrames()
        {
            if (pitchFrames == null || pitchFrames.Length < 6 || pitchFrames[0] == null)
            {
                pitchFrames = new Sprite[6];
                for (int i = 0; i < 6; i++)
                {
                    pitchFrames[i] = Resources.Load<Sprite>(PitchFrameResourceNames[i]);
                }

#if UNITY_EDITOR
                if (pitchFrames == null || pitchFrames.Length < 6 || pitchFrames[0] == null)
                {
                    pitchFrames = new Sprite[6];
                    for (int i = 0; i < 6; i++)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(PitchFrameGuids[i]);
                        pitchFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                }
#endif
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
                rt.name = $"ProfileFeaturedHoloFrame_RT_{rarityIndex}";
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

        private RenderTexture GetPitchHoloFrameRT(int rarityIndex)
        {
            if (!pitchHoloFrameRTs.TryGetValue(rarityIndex, out RenderTexture rt) || rt == null)
            {
                rt = new RenderTexture(360, 540, 0, RenderTextureFormat.ARGB32);
                rt.name = $"ProfilePitchHoloFrame_RT_{rarityIndex}";
                rt.antiAliasing = 1;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = prev;

                pitchHoloFrameRTs[rarityIndex] = rt;
            }
            return rt;
        }

        private RenderTexture GetModalHoloFrameRT(int rarityIndex)
        {
            if (!modalHoloFrameRTs.TryGetValue(rarityIndex, out RenderTexture rt) || rt == null)
            {
                rt = new RenderTexture(360, 540, 0, RenderTextureFormat.ARGB32);
                rt.name = $"ProfileModalHoloFrame_RT_{rarityIndex}";
                rt.antiAliasing = 1;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = prev;

                modalHoloFrameRTs[rarityIndex] = rt;
            }
            return rt;
        }

        private string GetSlotRarityClass(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica:
                case Rarity.FullArt: return "slot-mythic";
                case Rarity.Legendaria:
                case Rarity.Epica: return "slot-rare";
                case Rarity.Especial: return "slot-uncommon";
                default: return "slot-common";
            }
        }

        private string GetCardRarityClass(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica:
                case Rarity.FullArt: return "card-mythic";
                case Rarity.Legendaria:
                case Rarity.Epica: return "card-rare";
                case Rarity.Especial: return "card-uncommon";
                default: return "card-common";
            }
        }

        private string GetRarityDisplayName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica: return "Mítica";
                case Rarity.FullArt: return "Full Art";
                case Rarity.Legendaria: return "Legendaria";
                case Rarity.Epica: return "Rara";
                case Rarity.Especial: return "Poco común";
                default: return "Común";
            }
        }

        // =========================================================================
        // USER PROFILE & FEEDBACK
        // =========================================================================

        private void CopyFriendCode()
        {
            string code = FirebaseAuthManager.Instance != null ? FirebaseAuthManager.Instance.FriendCode : "4872-1093";
            GUIUtility.systemCopyBuffer = code;
            ShowFeedbackModal("CÓDIGO COPIADO", $"Tu código de amigo ({code}) se ha copiado al portapapeles.");
            Debug.Log($"<color=gold>[Perfil] Código {code} copiado.</color>");
        }

        private void UpdateProfileData()
        {
            if (FirebaseAuthManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(FirebaseAuthManager.Instance.DisplayName) && usernameText != null)
                {
                    usernameText.text = FirebaseAuthManager.Instance.DisplayName.ToUpper();
                }

                if (friendCodeText != null)
                {
                    friendCodeText.text = FirebaseAuthManager.Instance.FriendCode;
                }

                FirebaseAuthManager.Instance.OnAvatarChanged -= OnAvatarChanged;
                FirebaseAuthManager.Instance.OnAvatarChanged += OnAvatarChanged;

                UserAvatarLoader.LoadAvatar(this, profileAvatarCircle, profileAvatarIcon);
            }
        }

        private void OnAvatarChanged(string newPhotoUrl)
        {
            UserAvatarLoader.LoadAvatar(this, profileAvatarCircle, profileAvatarIcon);
        }

        private void ShowFeedbackModal(string title, string desc)
        {
            if (modalTitle != null) modalTitle.text = title;
            if (modalDesc != null) modalDesc.text = desc;
            if (feedbackModal != null) feedbackModal.RemoveFromClassList("modal-hidden");
        }
    }
}