using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Cards;

namespace JuegoTCG.UI
{
    public class UIToolkitMyCardsController : MonoBehaviour
    {
        private VisualElement root;
        private VisualElement cardsGrid;
        private VisualElement cardInspectModal;
        private Label cardsCountLabel;
        private TextField searchField;

        // Modal elements
        private VisualElement inspectHeroCard;
        private VisualElement inspectArtContainer;
        private VisualElement inspectArtPhoto;
        private VisualElement inspectArtFrame;
        private VisualElement inspectArtHolo;
        private VisualElement inspectAvatarCircle;
        private Label inspectAvatarInitials;
        private Label inspectPlayerName;
        private Label inspectRarityBadge;
        private Label inspectStatPosition;
        private Label inspectStatTeam;
        private Label inspectStatCopies;
        private Button inspectCloseBtn;

        [Header("Marcos Oficiales de Rareza")]
        [SerializeField] private Sprite[] rarityFrames = new Sprite[6];
        private static readonly string[] FrameGuids = new string[]
        {
            "4edcac4ad7f822e4aa7b10b2dd755926", // Comun
            "586794b59d6595341aa4a2f2b59209ce", // Especial
            "ab60ad89df16072448c901abb76cbe3a", // Epica
            "2a89c6d7166430641b49f80a84ac2cd8", // Legendaria
            "8ab77af7592605c48b2e119ccdb7dcb3", // Mitica
            "ae059fc1520988141a79cb933243639f"  // Full Art
        };

        private Dictionary<int, RenderTexture> holoFrameRTs = new Dictionary<int, RenderTexture>();
        private Material holoMaterial;

        private string currentFilter = "Album";
        private string searchQuery = "";
        private List<CardData> loadedCardAssets = new List<CardData>();

        private void OnEnable()
        {
            PlayerCollectionManager.EnsureExists();
            EnsureRarityFrames();
            EnsureHoloMaterial();
            LoadCardAssets();

#if UNITY_EDITOR
            // Para pruebas inmediatas: asegurar que Lamine Yamal (card_10) esté en posesión
            if (PlayerCollectionManager.Instance != null && !PlayerCollectionManager.Instance.IsCardOwned("card_10"))
            {
                PlayerCollectionManager.Instance.AddCard("card_10", 1);
            }
#endif

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null || uiDoc.rootVisualElement == null) return;
            root = uiDoc.rootVisualElement;

            cardsGrid = root.Q<VisualElement>("CardsGrid");
            cardInspectModal = root.Q<VisualElement>("CardInspectModal");
            cardsCountLabel = root.Q<Label>("CardsCountLabel");
            searchField = root.Q<TextField>("SearchField");

            inspectHeroCard = root.Q<VisualElement>("InspectHeroCard");
            inspectArtContainer = root.Q<VisualElement>("InspectArtContainer");
            inspectArtPhoto = root.Q<VisualElement>("InspectArtPhoto");
            inspectArtFrame = root.Q<VisualElement>("InspectArtFrame");
            inspectArtHolo = root.Q<VisualElement>("InspectArtHolo");

            inspectAvatarCircle = root.Q<VisualElement>("InspectAvatarCircle");
            inspectAvatarInitials = root.Q<Label>("InspectAvatarInitials");
            inspectPlayerName = root.Q<Label>("InspectPlayerName");
            inspectRarityBadge = root.Q<Label>("InspectRarityBadge");
            inspectStatPosition = root.Q<Label>("InspectStatPosition");
            inspectStatTeam = root.Q<Label>("InspectStatTeam");
            inspectStatCopies = root.Q<Label>("InspectStatCopies");
            inspectCloseBtn = root.Q<Button>("InspectCloseBtn");

            if (inspectCloseBtn != null)
            {
                inspectCloseBtn.clicked += CloseInspectModal;
            }

            if (cardInspectModal != null)
            {
                cardInspectModal.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == cardInspectModal) CloseInspectModal();
                });
            }

            WireFilterPills();
            WireSearch();
            WireBottomNav();

            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated += PopulateAlbumGrid;
            }

            PopulateAlbumGrid();
        }

        private void EnsureRarityFrames()
        {
            if (rarityFrames == null || rarityFrames.Length < 6 || rarityFrames[0] == null)
            {
                // 1. Cargar desde CardPrefab en Resources (Garantizado en Android/iOS y WebGL)
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
                // 2. Fallback de Unity Editor vía AssetDatabase
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
                Debug.Log($"[MyCards] EnsureRarityFrames: {(rarityFrames != null && rarityFrames.Length >= 6 && rarityFrames[0] != null ? "EXITO" : "FALLO")}");
            }
        }

        private void EnsureHoloMaterial()
        {
            if (holoMaterial == null)
            {
                // 1. Cargar material desde CardPrefab en Resources (incluido en build de Android/iOS)
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
                // 2. Fallback de Unity Editor
                if (holoMaterial == null)
                {
                    Material baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/HolographicFoilMaterial.mat");
                    if (baseMat != null) holoMaterial = new Material(baseMat);
                }
#endif
                // 3. Fallback directo por Shader.Find
                if (holoMaterial == null)
                {
                    Shader s = Shader.Find("Shader Graphs/HolographicFoilShader");
                    if (s != null) holoMaterial = new Material(s);
                }

                if (holoMaterial != null)
                {
                    holoMaterial.SetFloat("_HoloIntensity", 0.85f);
                    holoMaterial.SetFloat("_ShimmerSpeed", 1.6f);
                    // Para Graphics.Blit hacia RenderTexture: sobrescribir directamente (Blend Off / One Zero)
                    // para que las zonas transparentes (alpha < 0.05) limpien cualquier basura de memoria GPU móvil
                    holoMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    holoMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                }
            }
        }

        private RenderTexture GetHoloFrameRT(int rarityIndex)
        {
            if (!holoFrameRTs.TryGetValue(rarityIndex, out RenderTexture rt) || rt == null)
            {
                rt = new RenderTexture(720, 1080, 0, RenderTextureFormat.ARGB32);
                rt.name = $"HoloFrame_RT_{rarityIndex}";
                rt.antiAliasing = 1;
                rt.wrapMode = TextureWrapMode.Clamp;
                rt.filterMode = FilterMode.Bilinear;
                rt.Create();

                // Limpiar explícitamente el buffer a transparente puro en GPU móvil
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = prev;

                holoFrameRTs[rarityIndex] = rt;
            }
            return rt;
        }

        private void Update()
        {
            if (holoMaterial == null || holoFrameRTs.Count == 0) return;

            foreach (var kvp in holoFrameRTs)
            {
                int rIndex = kvp.Key;
                RenderTexture rt = kvp.Value;
                if (rt != null && rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    Graphics.Blit(rarityFrames[rIndex].texture, rt, holoMaterial);
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
            }
        }

        private void LoadCardAssets()
        {
            loadedCardAssets.Clear();

            // 1. ScriptableObjects en Editor
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

            // 2. Resources (para builds de Android/iOS y fallback)
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

        private void WireFilterPills()
        {
            string[] filters = new string[] { "Album", "Recientes", "Rareza", "Cantidad", "Nacion" };
            foreach (var f in filters)
            {
                var pill = root.Q<Button>($"Filter_{f}");
                if (pill != null)
                {
                    pill.clicked += () => SelectFilter(f, pill);
                }
            }

            var prevBtn = root.Q<Button>("FilterPrevBtn");
            var nextBtn = root.Q<Button>("FilterNextBtn");
            var scroll = root.Q<ScrollView>("FilterScrollView");

            if (prevBtn != null && scroll != null)
            {
                prevBtn.clicked += () => scroll.scrollOffset = new Vector2(Mathf.Max(0, scroll.scrollOffset.x - 120), 0);
            }
            if (nextBtn != null && scroll != null)
            {
                nextBtn.clicked += () => scroll.scrollOffset = new Vector2(scroll.scrollOffset.x + 120, 0);
            }
        }

        private void SelectFilter(string filterName, Button clickedPill)
        {
            currentFilter = filterName;

            string[] filters = new string[] { "Album", "Recientes", "Rareza", "Cantidad", "Nacion" };
            foreach (var f in filters)
            {
                var pill = root.Q<Button>($"Filter_{f}");
                var label = pill?.Q<Label>();
                if (pill != null)
                {
                    if (pill == clickedPill)
                    {
                        pill.AddToClassList("filter-pill-active");
                        label?.AddToClassList("filter-pill-text-active");
                    }
                    else
                    {
                        pill.RemoveFromClassList("filter-pill-active");
                        label?.RemoveFromClassList("filter-pill-text-active");
                    }
                }
            }

            PopulateAlbumGrid();
        }

        private void WireSearch()
        {
            if (searchField == null) return;
            searchField.RegisterValueChangedCallback(evt =>
            {
                searchQuery = (evt.newValue ?? "").Trim().ToLower();
                PopulateAlbumGrid();
            });
        }

        public void PopulateAlbumGrid()
        {
            if (cardsGrid == null || PlayerCollectionManager.Instance == null) return;

            cardsGrid.Clear();
            var catalog = PlayerCollectionManager.Instance.GetCatalog();
            int visibleCount = 0;

            for (int i = 0; i < catalog.Count; i++)
            {
                var item = catalog[i];
                bool isOwned = PlayerCollectionManager.Instance.IsCardOwned(item.cardId);
                int count = PlayerCollectionManager.Instance.GetOwnedCount(item.cardId);

                // Search query match
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    if (!item.playerName.ToLower().Contains(searchQuery))
                    {
                        continue;
                    }
                }

                Button cardBtn = new Button();
                cardBtn.AddToClassList("card-item");

                CardData asset = loadedCardAssets.Find(c => c.cardId == item.cardId);
                Sprite cardArt = DataPackManager.GetCardArt(item.cardId, asset != null ? asset.defaultArt : null);

                if (isOwned)
                {
                    string rarityClass = GetRarityClass(item.rarity);
                    cardBtn.AddToClassList(rarityClass);

                    if (cardArt != null)
                    {
                        // ----------------------------------------------------
                        // CARTA CON ARTE OFICIAL (Lamine Yamal / Data Packs)
                        // ----------------------------------------------------
                        cardBtn.AddToClassList("card-item-art");

                        VisualElement artContainer = new VisualElement();
                        artContainer.AddToClassList("card-art-container");

                        // 1. Foto del Jugador (Full-bleed)
                        VisualElement photoEl = new VisualElement();
                        photoEl.AddToClassList("card-art-photo");
                        photoEl.style.backgroundImage = new StyleBackground(cardArt);
                        artContainer.Add(photoEl);

                        // 2. Marco Oficial de Rareza (Con Shader Holográfico activo en tiempo real si es Épica/Legendaria/Mítica/FullArt)
                        int rIndex = (int)item.rarity;
                        bool isHolo = (item.rarity == Rarity.Epica || item.rarity == Rarity.Legendaria || item.rarity == Rarity.Mitica || item.rarity == Rarity.FullArt);

                        VisualElement frameEl = new VisualElement();
                        frameEl.AddToClassList("card-art-frame");

                        EnsureRarityFrames();
                        EnsureHoloMaterial();

                        if (isHolo && holoMaterial != null && rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                        {
                            RenderTexture holoRT = GetHoloFrameRT(rIndex);
                            Graphics.Blit(rarityFrames[rIndex].texture, holoRT, holoMaterial);
                            frameEl.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(holoRT));
                        }
                        else if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                        {
                            frameEl.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                        }
                        artContainer.Add(frameEl);

                        // 3. Nombre del Jugador (Ubicado justo sobre la caja inferior, en blanco nítido con sombra)
                        Label nameLbl = new Label(item.playerName);
                        nameLbl.AddToClassList("card-player-name-framed");
                        artContainer.Add(nameLbl);

                        // 4. Caja de Información Inferior (Dentro de la caja holográfica de la carta, como en Imagen 2)
                        VisualElement footerBox = new VisualElement();
                        footerBox.AddToClassList("card-frame-footer-box");

                        Label teamLbl = new Label(!string.IsNullOrEmpty(item.teamName) ? item.teamName : (asset != null ? asset.teamName : "FC Barca"));
                        teamLbl.AddToClassList("card-frame-team-name");
                        footerBox.Add(teamLbl);

                        VisualElement footerSubRow = new VisualElement();
                        footerSubRow.AddToClassList("card-frame-sub-row");

                        Label posLbl = new Label(!string.IsNullOrEmpty(item.position) ? item.position : (asset != null ? asset.position : "Delantero"));
                        posLbl.AddToClassList("card-frame-pos-text");
                        footerSubRow.Add(posLbl);

                        Label rarityLbl = new Label(item.rarity.ToString().ToUpper());
                        rarityLbl.AddToClassList("card-frame-rarity-text");
                        footerSubRow.Add(rarityLbl);

                        footerBox.Add(footerSubRow);
                        artContainer.Add(footerBox);

                        // 5. Badge de copias (Esquina superior derecha)
                        Label countBadge = new Label($"×{count}");
                        countBadge.AddToClassList("card-copies-badge-framed");
                        artContainer.Add(countBadge);

                        cardBtn.Add(artContainer);
                    }
                    else
                    {
                        // ----------------------------------------------------
                        // DISEÑO FALLBACK ORIGINAL (Cartas sin imagen)
                        // ----------------------------------------------------
                        VisualElement avatarCircle = new VisualElement();
                        avatarCircle.AddToClassList("card-avatar-circle");

                        Label initials = new Label(item.initials);
                        initials.AddToClassList("card-avatar-initials");
                        initials.AddToClassList(GetInitialsClass(item.rarity));
                        avatarCircle.Add(initials);
                        cardBtn.Add(avatarCircle);

                        // Player Name
                        Label nameLbl = new Label(item.playerName);
                        nameLbl.AddToClassList("card-player-name");
                        cardBtn.Add(nameLbl);

                        // Rarity Badge
                        string badgeText = $"{item.rarity} ×{count}";
                        Label badgeLbl = new Label(badgeText);
                        badgeLbl.AddToClassList("card-rarity-badge");
                        badgeLbl.AddToClassList(GetInitialsClass(item.rarity));
                        cardBtn.Add(badgeLbl);
                    }

                    // Click to inspect
                    var currentItem = item;
                    var currentCount = count;
                    var currentAsset = asset;
                    cardBtn.clicked += () => OpenInspectModal(currentItem, currentCount, currentAsset);
                }
                else
                {
                    // Locked Card Slot
                    cardBtn.AddToClassList("card-locked");

                    VisualElement avatarCircle = new VisualElement();
                    avatarCircle.AddToClassList("card-avatar-circle");
                    Label lockIcon = new Label("🔒");
                    lockIcon.AddToClassList("card-avatar-initials");
                    avatarCircle.Add(lockIcon);
                    cardBtn.Add(avatarCircle);

                    Label nameLbl = new Label($"#{i + 1:D2} {item.playerName}");
                    nameLbl.AddToClassList("card-player-name");
                    cardBtn.Add(nameLbl);

                    Label badgeLbl = new Label("🔒 BLOQUEADA");
                    badgeLbl.AddToClassList("card-rarity-badge");
                    cardBtn.Add(badgeLbl);
                }

                cardsGrid.Add(cardBtn);
                visibleCount++;
            }

            // Update Progress Header
            PlayerCollectionManager.Instance.GetAlbumProgress(out int ownedUnique, out int totalCards, out float percentage);
            if (cardsCountLabel != null)
            {
                cardsCountLabel.text = $"{ownedUnique} de {totalCards} cartas ({Mathf.RoundToInt(percentage * 100)}%)";
            }
        }

        private string GetRarityClass(Rarity rarity)
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

        private string GetInitialsClass(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica:
                case Rarity.FullArt: return "initials-mythic";
                case Rarity.Legendaria:
                case Rarity.Epica: return "initials-rare";
                case Rarity.Especial: return "initials-uncommon";
                default: return "initials-common";
            }
        }

        public void OpenInspectModal(CardCatalogItem item, int count, CardData asset)
        {
            if (cardInspectModal == null) return;

            if (inspectPlayerName != null) inspectPlayerName.text = item.playerName;
            if (inspectAvatarInitials != null) inspectAvatarInitials.text = item.initials;
            if (inspectRarityBadge != null) inspectRarityBadge.text = item.rarity.ToString().ToUpper();
            if (inspectStatPosition != null) inspectStatPosition.text = item.position;
            if (inspectStatTeam != null) inspectStatTeam.text = item.teamName;
            if (inspectStatCopies != null) inspectStatCopies.text = $"×{count}";

            Sprite inspectArt = DataPackManager.GetCardArt(item.cardId, asset != null ? asset.defaultArt : null);

            if (inspectHeroCard != null)
            {
                inspectHeroCard.RemoveFromClassList("card-mythic");
                inspectHeroCard.RemoveFromClassList("card-rare");
                inspectHeroCard.RemoveFromClassList("card-uncommon");
                inspectHeroCard.RemoveFromClassList("card-common");
                inspectHeroCard.AddToClassList(GetRarityClass(item.rarity));
            }

            if (inspectArt != null)
            {
                // Modo Carta Completa con Arte
                if (inspectHeroCard != null) inspectHeroCard.AddToClassList("modal-hero-art");
                if (inspectArtContainer != null) inspectArtContainer.style.display = DisplayStyle.Flex;
                if (inspectArtPhoto != null) inspectArtPhoto.style.backgroundImage = new StyleBackground(inspectArt);
                if (inspectArtFrame != null)
                {
                    int rIndex = (int)item.rarity;
                    bool isHolo = (item.rarity == Rarity.Epica || item.rarity == Rarity.Legendaria || item.rarity == Rarity.Mitica || item.rarity == Rarity.FullArt);
                    EnsureRarityFrames();
                    EnsureHoloMaterial();

                    if (isHolo && holoMaterial != null && rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                    {
                        RenderTexture holoRT = GetHoloFrameRT(rIndex);
                        Graphics.Blit(rarityFrames[rIndex].texture, holoRT, holoMaterial);
                        inspectArtFrame.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(holoRT));
                    }
                    else if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                    {
                        inspectArtFrame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                    }
                }

                if (inspectArtHolo != null)
                {
                    inspectArtHolo.style.display = DisplayStyle.None;
                }

                if (inspectAvatarCircle != null) inspectAvatarCircle.style.display = DisplayStyle.None;
                if (inspectPlayerName != null) inspectPlayerName.style.display = DisplayStyle.None;
                if (inspectRarityBadge != null) inspectRarityBadge.style.display = DisplayStyle.None;
            }
            else
            {
                // Fallback a iniciales circulares
                if (inspectHeroCard != null) inspectHeroCard.RemoveFromClassList("modal-hero-art");
                if (inspectArtContainer != null) inspectArtContainer.style.display = DisplayStyle.None;
                if (inspectAvatarCircle != null)
                {
                    inspectAvatarCircle.style.display = DisplayStyle.Flex;
                    inspectAvatarCircle.style.backgroundImage = null;
                }
                if (inspectAvatarInitials != null) inspectAvatarInitials.style.display = DisplayStyle.Flex;
                if (inspectPlayerName != null) inspectPlayerName.style.display = DisplayStyle.Flex;
                if (inspectRarityBadge != null) inspectRarityBadge.style.display = DisplayStyle.Flex;
            }

            cardInspectModal.RemoveFromClassList("modal-hidden");
        }

        public void CloseInspectModal()
        {
            if (cardInspectModal != null)
            {
                cardInspectModal.AddToClassList("modal-hidden");
            }
        }

        private void WireBottomNav()
        {
            var navBarController = gameObject.GetComponent<LiquidGlassNavBarController>();
            if (navBarController == null)
            {
                navBarController = gameObject.AddComponent<LiquidGlassNavBarController>();
            }
            navBarController.Initialize(root, LiquidGlassNavBarController.TabType.Cartas);
        }
    }
}
