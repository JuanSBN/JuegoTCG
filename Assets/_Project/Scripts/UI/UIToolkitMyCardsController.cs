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

        // Modal elements (Hero Card Showcase)
        private VisualElement inspectCardStage;
        private VisualElement inspectHeroCard;
        private Label inspectTiltHint;
        private Button inspectCloseBtn;

        // OVR Header Tab
        private VisualElement inspectOvrBox;
        private Label inspectOvrVal;
        private Label inspectOvrTitle;

        // Framed card elements
        private VisualElement inspectArtContainer;
        private VisualElement inspectArtPhoto;
        private VisualElement inspectPlaceholderAvatar;
        private Label inspectAvatarInitials;
        private VisualElement inspectArtFrame;
        private Label inspectPlayerNameFramed;
        private VisualElement inspectFrameFooterBox;
        private Label inspectFrameTeamName;
        private Label inspectFramePosText;
        private Label inspectFrameRarityText;

        // Flag & Stats Banner in Inspect Modal
        private VisualElement inspectFlagImage;
        private Label inspectStat1Title;
        private Label inspectStat1Value;
        private Label inspectStat2Title;
        private Label inspectStat2Value;
        private Label inspectStat3Title;
        private Label inspectStat3Value;
        private Label inspectStat4Title;
        private Label inspectStat4Value;

        // Badges
        private Label inspectCopiesBadge;

        // Touch & 3D Tilt interaction state
        private bool isInspectOpen = false;
        private bool isInspectDragging = false;
        private bool hasMovedDrag = false;
        private Vector3 dragStartPos;
        private float targetTiltX = 0f;
        private float targetTiltY = 0f;
        private float currentTiltX = 0f;
        private float currentTiltY = 0f;
        private float currentInspectScale = 1.0f;
        private bool currentInspectIsHolo = false;
        private int currentInspectRIndex = -1;
        private RenderTexture inspectHoloRT;
        private Material inspectHoloMaterial;

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

            inspectCardStage = root.Q<VisualElement>("InspectCardStage");
            inspectHeroCard = root.Q<VisualElement>("InspectHeroCard");
            inspectTiltHint = root.Q<Label>("InspectTiltHint");
            inspectCloseBtn = root.Q<Button>("InspectCloseBtn");

            inspectArtContainer = root.Q<VisualElement>("InspectArtContainer");
            inspectArtPhoto = root.Q<VisualElement>("InspectArtPhoto");
            inspectPlaceholderAvatar = root.Q<VisualElement>("InspectPlaceholderAvatar");
            inspectAvatarInitials = root.Q<Label>("InspectAvatarInitials");
            inspectArtFrame = root.Q<VisualElement>("InspectArtFrame");
            inspectOvrBox = root.Q<VisualElement>("InspectOvrBox");
            inspectOvrVal = root.Q<Label>("InspectOvrVal");
            inspectOvrTitle = root.Q<Label>("InspectOvrTitle");
            inspectPlayerNameFramed = root.Q<Label>("InspectPlayerNameFramed");
            inspectFrameFooterBox = root.Q<VisualElement>("InspectFrameFooterBox");
            inspectFrameTeamName = root.Q<Label>("InspectFrameTeamName");
            inspectFramePosText = root.Q<Label>("InspectFramePosText");
            inspectFrameRarityText = root.Q<Label>("InspectFrameRarityText");

            // Flag & Stats Banner
            inspectFlagImage = root.Q<VisualElement>("InspectFlagImage");
            inspectStat1Title = root.Q<Label>("InspectStat1Title");
            inspectStat1Value = root.Q<Label>("InspectStat1Value");
            inspectStat2Title = root.Q<Label>("InspectStat2Title");
            inspectStat2Value = root.Q<Label>("InspectStat2Value");
            inspectStat3Title = root.Q<Label>("InspectStat3Title");
            inspectStat3Value = root.Q<Label>("InspectStat3Value");
            inspectStat4Title = root.Q<Label>("InspectStat4Title");
            inspectStat4Value = root.Q<Label>("InspectStat4Value");

            inspectCopiesBadge = root.Q<Label>("InspectCopiesBadge");

            if (inspectCloseBtn != null)
            {
                inspectCloseBtn.clicked += CloseInspectModal;
            }

            if (cardInspectModal != null)
            {
                cardInspectModal.RegisterCallback<ClickEvent>(evt =>
                {
                    if (!hasMovedDrag && (evt.target == cardInspectModal || evt.target == inspectCardStage || (evt.target is VisualElement ve && ve.name == "InspectDismissHint")))
                    {
                        CloseInspectModal();
                    }
                });
            }

            var cardsScrollView = root.Q<ScrollView>("CardsScrollView");
            if (cardsScrollView != null)
            {
                JuegoTCG.Core.MobilePerformanceOptimizer.ApplyOptimalScrollSettings(cardsScrollView);
            }

            if (inspectCardStage != null)
            {
                inspectCardStage.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (!isInspectOpen) return;
                    isInspectDragging = true;
                    hasMovedDrag = false;
                    dragStartPos = evt.position;
                    inspectCardStage.CapturePointer(evt.pointerId);
                    UpdateTiltFromPointer(evt.localPosition);
                });

                inspectCardStage.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!isInspectOpen || !isInspectDragging) return;
                    if ((evt.position - dragStartPos).sqrMagnitude > 25f)
                    {
                        hasMovedDrag = true;
                    }
                    UpdateTiltFromPointer(evt.localPosition);
                });

                inspectCardStage.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!isInspectDragging) return;
                    isInspectDragging = false;
                    inspectCardStage.ReleasePointer(evt.pointerId);
                    targetTiltX = 0f;
                    targetTiltY = 0f;
                });

                inspectCardStage.RegisterCallback<PointerCaptureOutEvent>(evt =>
                {
                    isInspectDragging = false;
                    targetTiltX = 0f;
                    targetTiltY = 0f;
                });
            }

            WireFilterPills();
            WireSearch();
            WireBottomNav();

            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated += PopulateAlbumGrid;
            }
            DataPackManager.OnDataPackReloaded += PopulateAlbumGrid;

            PopulateAlbumGrid();
        }

        private void OnDisable()
        {
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated -= PopulateAlbumGrid;
            }
            DataPackManager.OnDataPackReloaded -= PopulateAlbumGrid;
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

            if (inspectHoloMaterial == null && holoMaterial != null)
            {
                inspectHoloMaterial = new Material(holoMaterial);
                inspectHoloMaterial.SetFloat("_HoloIntensity", 1.0f);
                inspectHoloMaterial.SetFloat("_ShimmerSpeed", 1.2f);
                inspectHoloMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                inspectHoloMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            }
        }

        private void UpdateTiltFromPointer(Vector2 localPos)
        {
            if (inspectCardStage == null) return;
            float w = inspectCardStage.contentRect.width;
            float h = inspectCardStage.contentRect.height;
            if (w <= 0f || h <= 0f) return;

            // Mapear localPos (0 a w, 0 a h) a [-1, 1]
            float normX = (localPos.x / w) * 2f - 1f;
            float normY = (localPos.y / h) * 2f - 1f;

            targetTiltX = Mathf.Clamp(normX, -1f, 1f);
            targetTiltY = Mathf.Clamp(normY, -1f, 1f);
        }

        private RenderTexture GetInspectHoloRT()
        {
            if (inspectHoloRT == null)
            {
                inspectHoloRT = new RenderTexture(720, 1080, 0, RenderTextureFormat.ARGB32);
                inspectHoloRT.name = "InspectHolo_RT";
                inspectHoloRT.antiAliasing = 1;
                inspectHoloRT.wrapMode = TextureWrapMode.Clamp;
                inspectHoloRT.filterMode = FilterMode.Bilinear;
                inspectHoloRT.Create();

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = inspectHoloRT;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = prev;
            }
            return inspectHoloRT;
        }

        private RenderTexture GetHoloFrameRT(int rarityIndex)
        {
            if (!holoFrameRTs.TryGetValue(rarityIndex, out RenderTexture rt) || rt == null)
            {
                // Para las cartas de la cuadrícula, 360x540 ofrece una nitidez retina cristalina
                // y consume 4 veces menos ancho de banda de GPU móvil que 720x1080
                rt = new RenderTexture(360, 540, 0, RenderTextureFormat.ARGB32);
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
            // 1. Actualizar marcos holográficos animados en la cuadrícula de álbum
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

            // 2. 3D Tilt y reacción holográfica en Hero Card Showcase (sólo cuando la inspección está activa)
            if (isInspectOpen)
            {
                float lerpSpeed = isInspectDragging ? 18f : 8f;
                currentTiltX = Mathf.Lerp(currentTiltX, targetTiltX, Time.deltaTime * lerpSpeed);
                currentTiltY = Mathf.Lerp(currentTiltY, targetTiltY, Time.deltaTime * lerpSpeed);

                float targetScale = isInspectDragging ? 1.03f : 1.0f;
                currentInspectScale = Mathf.Lerp(currentInspectScale, targetScale, Time.deltaTime * 12f);

                if (inspectHeroCard != null)
                {
                    // 3D Pitch (rot X invertido) & Roll (rot Y) suave con leve Yaw (rot Z) estilo Pokémon Pocket
                    // Un ángulo sutil de 4.5° evita que el recorte ortográfico 2D corte los bordes laterales
                    inspectHeroCard.transform.rotation = Quaternion.Euler(-currentTiltY * 4.5f, currentTiltX * 4.5f, currentTiltX * 2.5f);
                    inspectHeroCard.transform.position = new Vector3(currentTiltX * 20f, -currentTiltY * 20f, 0f);
                    inspectHeroCard.transform.scale = new Vector3(currentInspectScale, currentInspectScale, 1f);
                }

                if (currentInspectIsHolo && inspectHoloMaterial != null && inspectHoloRT != null && rarityFrames != null && currentInspectRIndex >= 0 && currentInspectRIndex < rarityFrames.Length && rarityFrames[currentInspectRIndex] != null)
                {
                    // Inyectar inclinación táctil interactiva al shader del marco holográfico
                    inspectHoloMaterial.SetVector("_TiltPos", new Vector4(currentTiltX * 0.9f, currentTiltY * 0.9f, 0, 0));
                    Graphics.Blit(rarityFrames[currentInspectRIndex].texture, inspectHoloRT, inspectHoloMaterial);
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

            if (inspectHoloRT != null)
            {
                inspectHoloRT.Release();
                Destroy(inspectHoloRT);
                inspectHoloRT = null;
            }

            if (holoMaterial != null)
            {
                Destroy(holoMaterial);
                holoMaterial = null;
            }

            if (inspectHoloMaterial != null)
            {
                Destroy(inspectHoloMaterial);
                inspectHoloMaterial = null;
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
                    cardBtn.AddToClassList("card-item-art");

                    VisualElement artContainer = new VisualElement();
                    artContainer.AddToClassList("card-art-container");

                    // 1. Foto del Jugador o Arte por defecto si no tiene imagen
                    VisualElement photoEl = new VisualElement();
                    photoEl.AddToClassList("card-art-photo");
                    if (cardArt != null)
                    {
                        photoEl.style.backgroundImage = new StyleBackground(cardArt);
                    }
                    else
                    {
                        photoEl.AddToClassList("inspect-art-photo-default");
                        VisualElement placeholderAvatar = new VisualElement();
                        placeholderAvatar.AddToClassList("card-placeholder-avatar");
                        Label initialsLbl = new Label(item.DisplayInitials);
                        initialsLbl.AddToClassList("card-avatar-initials");
                        initialsLbl.AddToClassList(GetInitialsClass(item.rarity));
                        placeholderAvatar.Add(initialsLbl);
                        photoEl.Add(placeholderAvatar);
                    }
                    artContainer.Add(photoEl);

                    // 2. Marco Oficial de Rareza (Con Shader Holográfico activo en tiempo real si es Épica/Legendaria/Mítica/FullArt)
                    int rIndex = (int)item.rarity;
                    bool isHolo = (item.rarity == Rarity.Epica || item.rarity == Rarity.Legendaria || item.rarity == Rarity.Mitica || item.rarity == Rarity.FullArt);

                    VisualElement frameEl = new VisualElement();
                    frameEl.AddToClassList("card-art-frame");

                    EnsureRarityFrames();
                    EnsureHoloMaterial();

                    if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
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
                    }
                    artContainer.Add(frameEl);

                    // 3. Media Global (OVR) en la pestaña superior derecha del marco
                    VisualElement ovrBox = new VisualElement();
                    ovrBox.AddToClassList("card-ovr-box");
                    Label ovrValLbl = new Label(item.OverallRating.ToString());
                    ovrValLbl.AddToClassList("card-ovr-val");
                    Label ovrTitleLbl = new Label("GRL");
                    ovrTitleLbl.AddToClassList("card-ovr-title");
                    ovrBox.Add(ovrValLbl);
                    ovrBox.Add(ovrTitleLbl);
                    artContainer.Add(ovrBox);

                    // 4. Nombre del Jugador (Ubicado justo arriba de la fila inferior)
                    Label nameLbl = new Label(item.DisplayPlayerName);
                    nameLbl.AddToClassList("card-player-name-framed");
                    artContainer.Add(nameLbl);

                    // 4. Fila Inferior: Bandera afuera a la izquierda + Estadísticas dentro de la pestaña del marco
                    VisualElement cardBottomRow = new VisualElement();
                    cardBottomRow.AddToClassList("card-bottom-row");

                    // Bandera afuera a la izquierda
                    VisualElement flagEl = new VisualElement();
                    flagEl.AddToClassList("card-flag-image");
                    string countryCode = !string.IsNullOrEmpty(item.DisplayCountryCode) ? item.DisplayCountryCode : (asset != null ? asset.DisplayCountryCode : "ES");
                    Sprite flagSprite = CountryFlagService.GetFlag(countryCode);
                    if (flagSprite != null)
                    {
                        flagEl.style.backgroundImage = new StyleBackground(flagSprite);
                    }
                    cardBottomRow.Add(flagEl);

                    // Pestaña del marco: Las 4 estadísticas van aquí dentro (sustituyendo equipo/posición/rareza)
                    VisualElement footerBox = new VisualElement();
                    footerBox.AddToClassList("card-frame-footer-box");

                    VisualElement statsGroup = new VisualElement();
                    statsGroup.AddToClassList("card-stats-group");

                    var statsSummary = item.GetDisplayStats();
                    AddGridStatCol(statsGroup, statsSummary.stat1Name, statsSummary.stat1Value);
                    AddGridStatCol(statsGroup, statsSummary.stat2Name, statsSummary.stat2Value);
                    AddGridStatCol(statsGroup, statsSummary.stat3Name, statsSummary.stat3Value);
                    AddGridStatCol(statsGroup, statsSummary.stat4Name, statsSummary.stat4Value);

                    footerBox.Add(statsGroup);
                    cardBottomRow.Add(footerBox);
                    artContainer.Add(cardBottomRow);

                    // 5. Badge de copias
                    Label countBadge = new Label($"×{count}");
                    countBadge.AddToClassList("card-copies-badge-framed");
                    artContainer.Add(countBadge);

                    cardBtn.Add(artContainer);

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

                cardBtn.usageHints = UsageHints.DynamicTransform;
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
                case Rarity.Mitica: return "card-mitica";
                case Rarity.FullArt: return "card-fullart";
                case Rarity.Legendaria: return "card-legendaria";
                case Rarity.Epica: return "card-epica";
                case Rarity.Especial: return "card-especial";
                default: return "card-comun";
            }
        }

        private string GetInitialsClass(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica: return "initials-mitica";
                case Rarity.FullArt: return "initials-fullart";
                case Rarity.Legendaria: return "initials-legendaria";
                case Rarity.Epica: return "initials-epica";
                case Rarity.Especial: return "initials-especial";
                default: return "initials-comun";
            }
        }

        public void OpenInspectModal(CardCatalogItem item, int count, CardData asset)
        {
            if (cardInspectModal == null) return;

            // Reset transforms & tilt
            isInspectOpen = true;
            isInspectDragging = false;
            hasMovedDrag = false;
            targetTiltX = 0f;
            targetTiltY = 0f;
            currentTiltX = 0f;
            currentTiltY = 0f;
            currentInspectScale = 1.0f;

            if (inspectHeroCard != null)
            {
                inspectHeroCard.transform.rotation = Quaternion.identity;
                inspectHeroCard.transform.position = Vector3.zero;
                inspectHeroCard.transform.scale = Vector3.one;

                inspectHeroCard.RemoveFromClassList("card-comun");
                inspectHeroCard.RemoveFromClassList("card-common");
                inspectHeroCard.RemoveFromClassList("card-especial");
                inspectHeroCard.RemoveFromClassList("card-uncommon");
                inspectHeroCard.RemoveFromClassList("card-epica");
                inspectHeroCard.RemoveFromClassList("card-rare");
                inspectHeroCard.RemoveFromClassList("card-legendaria");
                inspectHeroCard.RemoveFromClassList("card-mitica");
                inspectHeroCard.RemoveFromClassList("card-mythic");
                inspectHeroCard.RemoveFromClassList("card-fullart");
                inspectHeroCard.AddToClassList(GetRarityClass(item.rarity));
            }

            if (inspectCopiesBadge != null)
            {
                inspectCopiesBadge.text = $"×{count}";
            }

            int rIndex = (int)item.rarity;
            currentInspectRIndex = rIndex;
            currentInspectIsHolo = (item.rarity == Rarity.Epica || item.rarity == Rarity.Legendaria || item.rarity == Rarity.Mitica || item.rarity == Rarity.FullArt);

            Sprite inspectArt = DataPackManager.GetCardArt(item.cardId, asset != null ? asset.defaultArt : null);

            EnsureRarityFrames();
            EnsureHoloMaterial();

            // El mensaje de brillo SOLO se muestra si la carta cuenta con shader holográfico
            if (inspectTiltHint != null)
            {
                if (currentInspectIsHolo)
                {
                    inspectTiltHint.text = "✦ Mueve el dedo sobre la carta para ver el brillo";
                    inspectTiltHint.style.display = DisplayStyle.Flex;
                }
                else
                {
                    inspectTiltHint.style.display = DisplayStyle.None;
                }
            }

            // Información del jugador y metadatos (siempre visible en el marco oficial)
            if (inspectPlayerNameFramed != null) inspectPlayerNameFramed.text = item.DisplayPlayerName;
            if (inspectOvrVal != null) inspectOvrVal.text = item.OverallRating.ToString();
            if (inspectOvrTitle != null) inspectOvrTitle.text = "GRL";
            if (inspectFrameTeamName != null) inspectFrameTeamName.text = string.IsNullOrEmpty(item.DisplayTeamName) ? "SIN EQUIPO" : item.DisplayTeamName.ToUpper();
            if (inspectFramePosText != null) inspectFramePosText.text = string.IsNullOrEmpty(item.DisplayPosition) ? "MED" : item.DisplayPosition.ToUpper();
            if (inspectFrameRarityText != null) inspectFrameRarityText.text = item.rarity.ToString().ToUpper();

            // Bandera & Stats en Modal Inspect
            string cCode = !string.IsNullOrEmpty(item.DisplayCountryCode) ? item.DisplayCountryCode : (asset != null ? asset.DisplayCountryCode : "ES");
            Sprite inspFlag = CountryFlagService.GetFlag(cCode);
            if (inspectFlagImage != null)
            {
                if (inspFlag != null)
                {
                    inspectFlagImage.style.backgroundImage = new StyleBackground(inspFlag);
                    inspectFlagImage.style.display = DisplayStyle.Flex;
                }
                else
                {
                    inspectFlagImage.style.display = DisplayStyle.None;
                }
            }

            var inspStats = item.GetDisplayStats();
            if (inspectStat1Title != null) inspectStat1Title.text = inspStats.stat1Name;
            if (inspectStat1Value != null) inspectStat1Value.text = inspStats.stat1Value.ToString();
            if (inspectStat2Title != null) inspectStat2Title.text = inspStats.stat2Name;
            if (inspectStat2Value != null) inspectStat2Value.text = inspStats.stat2Value.ToString();
            if (inspectStat3Title != null) inspectStat3Title.text = inspStats.stat3Name;
            if (inspectStat3Value != null) inspectStat3Value.text = inspStats.stat3Value.ToString();
            if (inspectStat4Title != null) inspectStat4Title.text = inspStats.stat4Name;
            if (inspectStat4Value != null) inspectStat4Value.text = inspStats.stat4Value.ToString();

            if (inspectArt != null)
            {
                // Tiene imagen personalizada (DataPack o defaultArt)
                if (inspectArtPhoto != null)
                {
                    inspectArtPhoto.RemoveFromClassList("inspect-art-photo-default");
                    inspectArtPhoto.style.backgroundImage = new StyleBackground(inspectArt);
                }
                if (inspectPlaceholderAvatar != null)
                {
                    inspectPlaceholderAvatar.style.display = DisplayStyle.None;
                }
            }
            else
            {
                // No tiene imagen: Arte por defecto (fondo deportivo táctico + avatar con iniciales)
                if (inspectArtPhoto != null)
                {
                    inspectArtPhoto.AddToClassList("inspect-art-photo-default");
                    inspectArtPhoto.style.backgroundImage = StyleKeyword.Null;
                }
                if (inspectPlaceholderAvatar != null)
                {
                    inspectPlaceholderAvatar.style.display = DisplayStyle.Flex;
                }
                if (inspectAvatarInitials != null)
                {
                    inspectAvatarInitials.text = item.DisplayInitials;
                    inspectAvatarInitials.RemoveFromClassList("initials-comun");
                    inspectAvatarInitials.RemoveFromClassList("initials-common");
                    inspectAvatarInitials.RemoveFromClassList("initials-especial");
                    inspectAvatarInitials.RemoveFromClassList("initials-uncommon");
                    inspectAvatarInitials.RemoveFromClassList("initials-epica");
                    inspectAvatarInitials.RemoveFromClassList("initials-rare");
                    inspectAvatarInitials.RemoveFromClassList("initials-legendaria");
                    inspectAvatarInitials.RemoveFromClassList("initials-mitica");
                    inspectAvatarInitials.RemoveFromClassList("initials-mythic");
                    inspectAvatarInitials.RemoveFromClassList("initials-fullart");
                    inspectAvatarInitials.AddToClassList(GetInitialsClass(item.rarity));
                }
            }

            // Marco Oficial de Rareza (siempre visible, reactivo con shader si es holo)
            if (inspectArtFrame != null)
            {
                if (currentInspectIsHolo && inspectHoloMaterial != null && rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    RenderTexture hRT = GetInspectHoloRT();
                    inspectHoloMaterial.SetVector("_TiltPos", Vector4.zero);
                    Graphics.Blit(rarityFrames[rIndex].texture, hRT, inspectHoloMaterial);
                    inspectArtFrame.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(hRT));
                }
                else if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                {
                    inspectArtFrame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                }
                else
                {
                    inspectArtFrame.style.backgroundImage = null;
                }
            }

            cardInspectModal.RemoveFromClassList("modal-hidden");
        }

        public void CloseInspectModal()
        {
            isInspectOpen = false;
            isInspectDragging = false;
            hasMovedDrag = false;
            targetTiltX = 0f;
            targetTiltY = 0f;
            currentTiltX = 0f;
            currentTiltY = 0f;

            if (inspectHeroCard != null)
            {
                inspectHeroCard.transform.rotation = Quaternion.identity;
                inspectHeroCard.transform.position = Vector3.zero;
                inspectHeroCard.transform.scale = Vector3.one;
            }

            if (cardInspectModal != null)
            {
                cardInspectModal.AddToClassList("modal-hidden");
            }
        }

        private void AddGridStatCol(VisualElement parent, string title, int val)
        {
            var col = new VisualElement();
            col.AddToClassList("card-stat-col");

            var tLbl = new Label(title);
            tLbl.AddToClassList("card-stat-title");
            col.Add(tLbl);

            var vLbl = new Label(val.ToString());
            vLbl.AddToClassList("card-stat-value");
            col.Add(vLbl);

            parent.Add(col);
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
