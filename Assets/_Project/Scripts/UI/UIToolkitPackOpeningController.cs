using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using JuegoTCG.Cards;
using JuegoTCG.Packs;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador cinematografico UI Toolkit para la Apertura de Sobres (1080x2400).
    /// Combina el entorno moderno UI Toolkit (fondo tactico, sobre animado, destellos,
    /// dots de progreso y pantalla de resumen final) con el CardPrefab nativo oficial
    /// que incluye los marcos de rareza originales, arte de jugadores, TextMeshPro
    /// y el shader HolographicFoil con HolographicTilt interactivo.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitPackOpeningController : MonoBehaviour
    {
        // Sesión de Apertura
        private static string sessionPackName = "Sobre Estrella Piloto";
        private static int sessionPackCount = 1;
        private static bool sessionForceHolo = false;
        private static bool sessionConfigured = false;

        public static void ConfigureSession(string packName, int count = 1, bool forceHolo = false)
        {
            sessionPackName = packName;
            sessionPackCount = count;
            sessionForceHolo = forceHolo;
            sessionConfigured = true;
        }
        [Header("Prefab de Carta Oficial y Canvas")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject cardRevealCanvas;
        [SerializeField] private RectTransform cardSpawnContainer;
        [SerializeField] private List<CardData> cardCatalog = new List<CardData>();
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

        [Header("Configuracion de Sobres")]
        [SerializeField] private int packCount = 5;
        [SerializeField] private bool forceHolo = false;

        private UIDocument uiDoc;
        private VisualElement root;

        // Vistas principales de UI Toolkit
        private VisualElement closedView;
        private VisualElement revealView;
        private VisualElement summaryView;
        private VisualElement screenFlash;
        private VisualElement stageContainer;

        // Topbar
        private Toggle toggleForceHolo;
        private Label packCountText;

        // Sobre Cerrado
        private VisualElement packElement;
        private VisualElement packRays;
        private Label ctaLabel;

        // Reveal
        private VisualElement revealGodRays;
        private VisualElement progressContainer;
        private VisualElement[] dots = new VisualElement[5];
        private Label continueHint;
        private Label tiltHint;

        // Resumen
        private Label summaryTitle;
        private Label summarySub;
        private VisualElement summaryGrid;
        private Button btnOpenAnother;
        private Button btnBackToShop;

        // Estado de ejecucion
        private List<CardData> currentPack = new List<CardData>();
        private int currentCardIndex = 0;
        private bool isFlipped = false;
        private bool isBusy = false;

        // Instancia activa de la carta en pantalla
        private GameObject activeCardGO;
        private RectTransform activeCardRect;
        private CardDisplay activeCardDisplay;
        private HolographicTilt activeCardTilt;

        private void OnEnable()
        {
            PlayerCollectionManager.EnsureExists();

            if (sessionConfigured)
            {
                packCount = sessionPackCount;
                forceHolo = sessionForceHolo;
                sessionConfigured = false;
            }

            uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            if (root == null) return;

            EnsureRuntimeCanvasAndPrefab();
            LoadCatalogIfEmpty();
            BindUI();
            ResetToClosedView();
        }

        private void EnsureRuntimeCanvasAndPrefab()
        {
            if (cardPrefab == null)
            {
#if UNITY_EDITOR
                cardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Cards/CardPrefab.prefab");
#endif
                if (cardPrefab == null)
                    cardPrefab = Resources.Load<GameObject>("CardPrefab");
            }

            if (cardRevealCanvas == null)
            {
                Canvas existing = FindAnyObjectByType<Canvas>();
                if (existing != null && existing.name == "CardRevealCanvas")
                {
                    cardRevealCanvas = existing.gameObject;
                    Transform spawn = existing.transform.Find("CardSpawnContainer");
                    if (spawn != null) cardSpawnContainer = spawn.GetComponent<RectTransform>();
                }
                else
                {
                    // Crear Canvas dinamicamente compatible con mobile
                    GameObject cGO = new GameObject("CardRevealCanvas");
                    Canvas c = cGO.AddComponent<Canvas>();

                    // Usar ScreenSpaceCamera para que funcione con UI Toolkit en la misma escena
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        c.renderMode = RenderMode.ScreenSpaceCamera;
                        c.worldCamera = mainCam;
                        c.planeDistance = 5f;
                    }
                    else
                    {
                        c.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                    c.sortingOrder = 10;

                    UnityEngine.UI.CanvasScaler s = cGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                    s.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    s.referenceResolution = new Vector2(1080, 2400);
                    s.matchWidthOrHeight = 0.0f;

                    cGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    cardRevealCanvas = cGO;

                    GameObject sGO = new GameObject("CardSpawnContainer");
                    sGO.transform.SetParent(cGO.transform, false);
                    RectTransform r = sGO.AddComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.5f, 0.5f);
                    r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.pivot = new Vector2(0.5f, 0.5f);
                    r.anchoredPosition = new Vector2(0, 30);
                    r.sizeDelta = new Vector2(680, 960);
                    cardSpawnContainer = r;

                    if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                    {
                        GameObject es = new GameObject("EventSystem");
                        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }
                }
            }
        }


        private void LoadCatalogIfEmpty()
        {
            // Fallback en runtime (mobile/build): cargar desde Resources si el catalogo esta vacio
            if (cardCatalog == null || cardCatalog.Count == 0)
            {
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/_Project/ScriptableObjects/PilotAlbum" });
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                    if (card != null) cardCatalog.Add(card);
                }
#endif
                // Fallback universal para builds (funciona en mobile)
                if (cardCatalog == null || cardCatalog.Count == 0)
                {
                    CardData[] resourceCards = Resources.LoadAll<CardData>("PilotAlbum");
                    if (resourceCards != null)
                    {
                        foreach (var c in resourceCards)
                        {
                            if (c != null) cardCatalog.Add(c);
                        }
                    }
                }
            }

            // Fallback para el prefab de carta
            if (cardPrefab == null)
            {
#if UNITY_EDITOR
                cardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Cards/CardPrefab.prefab");
#endif
                if (cardPrefab == null)
                {
                    cardPrefab = Resources.Load<GameObject>("CardPrefab");
                }
            }

            if (rarityFrames == null || rarityFrames.Length < 6 || rarityFrames[0] == null)
            {
                rarityFrames = new Sprite[6];
#if UNITY_EDITOR
                for (int i = 0; i < 6; i++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(FrameGuids[i]);
                    rarityFrames[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
#endif
            }
        }


        private void BindUI()
        {
            stageContainer = root.Q<VisualElement>("StageContainer");
            closedView = root.Q<VisualElement>("ClosedView");
            revealView = root.Q<VisualElement>("RevealView");
            summaryView = root.Q<VisualElement>("SummaryView");
            screenFlash = root.Q<VisualElement>("ScreenFlash");

            // Topbar
            toggleForceHolo = root.Q<Toggle>("Toggle_ForceHolo");
            packCountText = root.Q<Label>("PackCountText");
            if (toggleForceHolo != null)
            {
                toggleForceHolo.value = forceHolo;
                toggleForceHolo.RegisterValueChangedCallback(evt => forceHolo = evt.newValue);
            }

            // Sobre cerrado
            packElement = root.Q<VisualElement>("PackElement");
            packRays = root.Q<VisualElement>("PackRays");
            ctaLabel = root.Q<Label>("CtaLabel");

            if (packElement != null)
            {
                packElement.RegisterCallback<ClickEvent>(evt => OnClickPack());
            }

            // Reveal
            revealGodRays = root.Q<VisualElement>("RevealGodRays");
            progressContainer = root.Q<VisualElement>("ProgressContainer");
            for (int i = 0; i < 5; i++)
            {
                dots[i] = root.Q<VisualElement>($"Dot_{i}");
            }
            continueHint = root.Q<Label>("ContinueHint");
            tiltHint = root.Q<Label>("TiltHint");

            if (revealView != null)
            {
                revealView.RegisterCallback<ClickEvent>(evt => OnClickCard());
            }

            // Resumen
            summaryTitle = root.Q<Label>("SummaryTitle");
            summarySub = root.Q<Label>("SummarySub");
            summaryGrid = root.Q<VisualElement>("SummaryGrid");
            btnOpenAnother = root.Q<Button>("Btn_OpenAnother");
            btnBackToShop = root.Q<Button>("Btn_BackToShop");

            if (btnOpenAnother != null)
            {
                btnOpenAnother.RegisterCallback<ClickEvent>(evt =>
                {
                    if (packCount > 0)
                    {
                        ResetToClosedView();
                    }
                    else
                    {
                        SceneManager.LoadScene("StoreSceneUIToolkit");
                    }
                });
            }

            if (btnBackToShop != null)
            {
                btnBackToShop.RegisterCallback<ClickEvent>(evt =>
                {
                    SceneManager.LoadScene("StoreSceneUIToolkit");
                });
            }
        }

        private void Update()
        {
            float t = Time.time;

            // 1. Animacion continua de levitacion y rayos del sobre cerrado
            if (closedView != null && closedView.style.display == DisplayStyle.Flex)
            {
                if (packElement != null)
                {
                    float offsetY = Mathf.Sin(t * 2.8f) * 14f;
                    packElement.style.translate = new StyleTranslate(new Translate(0, offsetY, 0));
                }

                if (packRays != null)
                {
                    packRays.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(t * 20f)));
                }

                if (ctaLabel != null)
                {
                    ctaLabel.style.opacity = 0.65f + Mathf.Sin(t * 3.2f) * 0.35f;
                }
            }

            // 2. Rotacion de rayos celestiales detras de la carta
            if (revealGodRays != null && revealGodRays.ClassListContains("reveal-god-rays-visible"))
            {
                revealGodRays.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(-t * 26f)));
            }
        }

        private void UpdatePackCountUI()
        {
            if (packCountText != null)
            {
                packCountText.text = $"{packCount} sobres";
            }

            if (packElement != null)
            {
                packElement.SetEnabled(packCount > 0);
            }
        }

        public void ResetToClosedView()
        {
            isBusy = false;
            isFlipped = false;
            currentCardIndex = 0;

            ClearActiveCard();

            if (cardRevealCanvas != null)
            {
                cardRevealCanvas.SetActive(false);
            }

            if (revealGodRays != null)
            {
                revealGodRays.RemoveFromClassList("reveal-god-rays-visible");
                revealGodRays.AddToClassList("reveal-god-rays-hidden");
                revealGodRays.style.display = DisplayStyle.None;
            }

            ShowOnlyView(closedView);
            UpdatePackCountUI();
        }

        private void ShowOnlyView(VisualElement target)
        {
            if (closedView != null) SetViewActive(closedView, closedView == target);
            if (revealView != null) SetViewActive(revealView, revealView == target);
            if (summaryView != null) SetViewActive(summaryView, summaryView == target);
        }

        private void SetViewActive(VisualElement view, bool active)
        {
            if (view == null) return;
            view.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            view.style.visibility = active ? Visibility.Visible : Visibility.Hidden;
            if (active)
            {
                view.RemoveFromClassList("stage-view-hidden");
                view.AddToClassList("stage-view-active");
            }
            else
            {
                view.RemoveFromClassList("stage-view-active");
                view.AddToClassList("stage-view-hidden");
            }
        }

        // =====================================================================
        // GENERACION DEL SOBRE CON EL CATALOGO REAL
        // =====================================================================
        private List<CardData> GeneratePack()
        {
            List<CardData> pack = new List<CardData>();

            if (cardCatalog != null && cardCatalog.Count > 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    CardData picked;
                    if (i == 4 && forceHolo)
                    {
                        picked = cardCatalog.Find(c => c.rarity == Rarity.Mitica) ?? cardCatalog.Find(c => c.rarity == Rarity.Legendaria) ?? cardCatalog[cardCatalog.Count - 1];
                    }
                    else
                    {
                        Rarity r = WeightedRNG.GetRandomRarity();
                        picked = WeightedRNG.SelectRandomCardByRarity(r, cardCatalog);
                    }
                    if (picked != null) pack.Add(picked);
                }
            }
            return pack;
        }

        // =====================================================================
        // ACCION: ABRIR SOBRE (ANTICIPACION + FLASH POP)
        // =====================================================================
        public void OnClickPack()
        {
            if (isBusy || packCount <= 0) return;
            StartCoroutine(SequenceOpenPack());
        }

        private IEnumerator SequenceOpenPack()
        {
            isBusy = true;
            packCount = Mathf.Max(0, packCount - 1);
            UpdatePackCountUI();

            // 1. Anticipacion: El sobre tiembla y se infla
            float shakeDuration = 0.28f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / shakeDuration;

                if (packElement != null)
                {
                    float shakeX = Mathf.Sin(elapsed * 55f) * 16f * (1f - p);
                    float scale = Mathf.Lerp(1.0f, 1.15f, p);
                    packElement.style.translate = new StyleTranslate(new Translate(shakeX, 0, 0));
                    packElement.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
                }
                yield return null;
            }

            if (packElement != null)
            {
                packElement.style.translate = new StyleTranslate(new Translate(0, 0, 0));
                packElement.style.scale = new StyleScale(new Scale(Vector3.one));
            }

            // 2. Destello blanco instantaneo
            if (screenFlash != null)
            {
                screenFlash.style.display = DisplayStyle.Flex;
                screenFlash.style.visibility = Visibility.Visible;
                screenFlash.style.opacity = 0.98f;
            }

            yield return new WaitForSeconds(0.08f);

            // 3. Pasar a RevealView y preparar cartas
            // Asegurar catalogo antes de generar (fallback mobile)
            LoadCatalogIfEmpty();

            if (cardCatalog == null || cardCatalog.Count == 0)
            {
                Debug.LogError("[PackOpening] ¡Catálogo de cartas vacío! Verifica que las CardData estén en Resources/PilotAlbum/ o asignadas en el Inspector.");
                isBusy = false;
                yield break;
            }

            currentPack = GeneratePack();
            currentCardIndex = 0;

            Debug.Log($"[PackOpening] Sobre generado con {currentPack.Count} cartas. Catálogo: {cardCatalog.Count} cartas.");

            // Guardar cartas obtenidas en la colección persistente del jugador
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.AddCards(currentPack);
            }

            ShowOnlyView(revealView);
            UpdateProgressDots(0);

            // Activar Canvas uGUI del CardPrefab
            if (cardRevealCanvas != null)
            {
                cardRevealCanvas.SetActive(true);
            }

            if (cardPrefab == null)
            {
                Debug.LogError("[PackOpening] ¡cardPrefab es null! Asigna CardPrefab.prefab en el Inspector o colócalo en Resources/CardPrefab.prefab.");
            }

            SpawnCurrentCard(isEntryAnim: true);


            // Desvanecer destello blanco suavemente
            if (screenFlash != null)
            {
                float fadeDuration = 0.35f;
                elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.98f, 0f, elapsed / fadeDuration);
                    screenFlash.style.opacity = alpha;
                    yield return null;
                }
                screenFlash.style.opacity = 0f;
                screenFlash.style.display = DisplayStyle.None;
                screenFlash.style.visibility = Visibility.Hidden;
            }

            isBusy = false;
        }

        // =====================================================================
        // INSTANCIACION DE LA CARTA OFICIAL (CardPrefab)
        // =====================================================================
        private void SpawnCurrentCard(bool isEntryAnim)
        {
            ClearActiveCard();
            isFlipped = false;

            if (currentCardIndex >= currentPack.Count)
            {
                ShowSummary();
                return;
            }

            CardData cardData = currentPack[currentCardIndex];

            if (cardPrefab != null && cardSpawnContainer != null)
            {
                activeCardGO = Instantiate(cardPrefab, cardSpawnContainer);
                activeCardRect = activeCardGO.GetComponent<RectTransform>();
                activeCardDisplay = activeCardGO.GetComponent<CardDisplay>();
                activeCardTilt = activeCardGO.GetComponent<HolographicTilt>();

                // Configurar posicion, rotacion y escala generosa (1.75x para 1080x2400)
                activeCardRect.anchoredPosition = Vector2.zero;
                activeCardRect.localRotation = Quaternion.identity;
                activeCardRect.localScale = new Vector3(1.75f, 1.75f, 1.75f);

                // Configurar datos oficiales del ScriptableObject
                if (activeCardDisplay != null && cardData != null)
                {
                    activeCardDisplay.SetCard(cardData);
                    activeCardDisplay.ShowBack(true); // Arranca boca abajo
                }

                // Deshabilitar tilt mientras esta de espaldas (nunca se puede mover por detras)
                if (activeCardTilt != null)
                {
                    activeCardTilt.CanTilt = false;
                    activeCardTilt.enabled = false;
                }

                // Si tiene Button en el prefab o se pulsa, registrar click
                UnityEngine.UI.Button btn = activeCardGO.GetComponent<UnityEngine.UI.Button>();
                if (btn == null)
                {
                    btn = activeCardGO.AddComponent<UnityEngine.UI.Button>();
                    btn.transition = UnityEngine.UI.Selectable.Transition.None;
                }
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClickCard);

                if (isEntryAnim)
                {
                    StartCoroutine(EntrySpringRoutine(activeCardRect, 1.75f));
                }
            }

            // Ocultar rayos celestiales
            if (revealGodRays != null)
            {
                revealGodRays.RemoveFromClassList("reveal-god-rays-visible");
                revealGodRays.AddToClassList("reveal-god-rays-hidden");
                revealGodRays.style.display = DisplayStyle.None;
            }

            if (continueHint != null)
            {
                continueHint.text = "Toca la carta para revelar";
            }

            if (tiltHint != null)
            {
                tiltHint.RemoveFromClassList("tilt-hint-visible");
                tiltHint.AddToClassList("tilt-hint-hidden");
            }
        }

        private IEnumerator EntrySpringRoutine(RectTransform rect, float targetScale)
        {
            float duration = 0.22f;
            float elapsed = 0f;
            rect.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / duration;
                float s = Mathf.Lerp(0f, targetScale * 1.06f, p);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rect.localScale = new Vector3(targetScale, targetScale, 1f);
        }

        private void ClearActiveCard()
        {
            if (activeCardGO != null)
            {
                Destroy(activeCardGO);
                activeCardGO = null;
                activeCardRect = null;
                activeCardDisplay = null;
                activeCardTilt = null;
            }
        }

        private void UpdateProgressDots(int currentIndex)
        {
            for (int i = 0; i < 5; i++)
            {
                if (dots[i] == null) continue;
                dots[i].RemoveFromClassList("progress-dot-done");
                dots[i].RemoveFromClassList("progress-dot-current");

                if (i < currentIndex)
                {
                    dots[i].AddToClassList("progress-dot-done");
                }
                else if (i == currentIndex)
                {
                    dots[i].AddToClassList("progress-dot-current");
                }
            }
        }

        // =====================================================================
        // ACCION: CLIC EN LA CARTA (GIRAR 3D O PASAR A LA SIGUIENTE)
        // =====================================================================
        public void OnClickCard()
        {
            if (isBusy) return;

            if (!isFlipped)
            {
                StartCoroutine(SequenceFlipCard());
            }
            else
            {
                StartCoroutine(SequenceDismissAndNext());
            }
        }

        // =====================================================================
        // SECUENCIA DE GIRO 3D CINEMATICO
        // =====================================================================
        private IEnumerator SequenceFlipCard()
        {
            isBusy = true;
            CardData currentCard = (currentCardIndex < currentPack.Count) ? currentPack[currentCardIndex] : null;
            bool isRare = currentCard != null && (currentCard.rarity == Rarity.Epica || currentCard.rarity == Rarity.Legendaria || currentCard.rarity == Rarity.Mitica || currentCard.rarity == Rarity.FullArt);

            float halfDuration = 0.16f;
            float elapsed = 0f;

            // 1. Giro 0 -> 90 grados Y (perfil)
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / halfDuration;
                float angle = Mathf.Lerp(0f, 90f, p);
                if (activeCardRect != null)
                {
                    activeCardRect.localRotation = Quaternion.Euler(0f, angle, 0f);
                }
                yield return null;
            }

            // 2. En el perfil exacto: cambiar a cara frontal
            if (activeCardDisplay != null)
            {
                activeCardDisplay.ShowBack(false);
            }

            // 3. Giro 90 -> 0 grados con aterrizaje elastico
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / halfDuration;
                float angle = Mathf.Lerp(90f, 0f, p);
                if (activeCardRect != null)
                {
                    activeCardRect.localRotation = Quaternion.Euler(0f, angle, 0f);
                }
                yield return null;
            }

            if (activeCardRect != null)
            {
                activeCardRect.localRotation = Quaternion.identity;
            }

            isFlipped = true;
            UpdateProgressDots(currentCardIndex + 1);

            // 4. Efectos de Carta Especial
            if (isRare)
            {
                StartCoroutine(DoScreenShake(0.35f, 18f));

                if (revealGodRays != null)
                {
                    revealGodRays.style.display = DisplayStyle.Flex;
                    revealGodRays.RemoveFromClassList("reveal-god-rays-hidden");
                    revealGodRays.AddToClassList("reveal-god-rays-visible");
                }

                if (tiltHint != null)
                {
                    tiltHint.text = "✦ ¡CARTA ESPECIAL! Mueve el dedo sobre la carta para ver el efecto holográfico";
                    tiltHint.RemoveFromClassList("tilt-hint-hidden");
                    tiltHint.AddToClassList("tilt-hint-visible");
                }
            }
            else
            {
                // Cartas comunes/especiales sin shader: no se mueven con el dedo, ocultar tiltHint
                if (tiltHint != null)
                {
                    tiltHint.RemoveFromClassList("tilt-hint-visible");
                    tiltHint.AddToClassList("tilt-hint-hidden");
                }
            }

            // Habilitar HolographicTilt interactivo solo si tiene shader holográfico (isRare)
            if (activeCardTilt != null)
            {
                activeCardTilt.enabled = isRare;
                activeCardTilt.CanTilt = isRare;
            }

            if (continueHint != null)
            {
                continueHint.text = (currentCardIndex >= 4) ? "Toca para ver el resumen del sobre" : "Toca para continuar";
            }

            isBusy = false;
        }

        // =====================================================================
        // SECUENCIA DE SALIDA Y SIGUIENTE CARTA
        // =====================================================================
        private IEnumerator SequenceDismissAndNext()
        {
            isBusy = true;

            if (activeCardTilt != null)
            {
                activeCardTilt.CanTilt = false;
                activeCardTilt.enabled = false;
            }

            // Salida disparada hacia arriba
            if (activeCardRect != null)
            {
                float duration = 0.18f;
                float elapsed = 0f;
                Vector2 startPos = activeCardRect.anchoredPosition;
                Vector2 targetPos = startPos + new Vector2(0, 1400f);

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float p = elapsed / duration;
                    activeCardRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, p * p);
                    float s = Mathf.Lerp(1.75f, 1.2f, p);
                    activeCardRect.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
            }

            currentCardIndex++;
            if (currentCardIndex >= currentPack.Count)
            {
                ShowSummary();
            }
            else
            {
                SpawnCurrentCard(isEntryAnim: true);
            }

            isBusy = false;
        }

        // =====================================================================
        // SCREEN SHAKE CINEMATICO
        // =====================================================================
        private IEnumerator DoScreenShake(float duration, float magnitude)
        {
            if (stageContainer == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = 1f - (elapsed / duration);
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * percent;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude * percent;
                stageContainer.style.translate = new StyleTranslate(new Translate(x, y, 0));
                yield return null;
            }
            stageContainer.style.translate = new StyleTranslate(new Translate(0, 0, 0));
        }

        // =====================================================================
        // RESUMEN FINAL CON CASCADA ESCALONADA
        // =====================================================================
        private void ShowSummary()
        {
            ClearActiveCard();

            if (cardRevealCanvas != null)
            {
                cardRevealCanvas.SetActive(false);
            }

            ShowOnlyView(summaryView);
            StartCoroutine(StaggeredSummaryRoutine());
        }

        private IEnumerator StaggeredSummaryRoutine()
        {
            if (summaryGrid != null)
            {
                summaryGrid.Clear();
                foreach (var card in currentPack)
                {
                    VisualElement miniCard = new VisualElement();
                    miniCard.AddToClassList("mini-card");
                    string rarityClass = card != null ? card.rarity.ToString().ToLower() : "comun";
                    miniCard.AddToClassList($"mini-rarity-{rarityClass}");
                    miniCard.style.scale = new StyleScale(new Scale(Vector3.zero));

                    // 1. Arte / Foto del Jugador o Avatar con iniciales
                    if (card != null && card.defaultArt != null)
                    {
                        VisualElement cardArt = new VisualElement();
                        cardArt.AddToClassList("mini-card-art");
                        cardArt.style.backgroundImage = new StyleBackground(card.defaultArt);
                        miniCard.Add(cardArt);
                    }
                    else
                    {
                        VisualElement avatar = new VisualElement();
                        avatar.AddToClassList("mini-card-avatar");
                        Label initials = new Label(GetInitials(card != null ? card.playerName : "FC"));
                        initials.AddToClassList("mini-card-initials");
                        avatar.Add(initials);
                        miniCard.Add(avatar);
                    }

                    // 2. Marco Oficial de Rareza (Frame Image)
                    int rIndex = card != null ? (int)card.rarity : 0;
                    if (rarityFrames != null && rIndex >= 0 && rIndex < rarityFrames.Length && rarityFrames[rIndex] != null)
                    {
                        VisualElement frame = new VisualElement();
                        frame.AddToClassList("mini-card-frame");
                        frame.style.backgroundImage = new StyleBackground(rarityFrames[rIndex]);
                        miniCard.Add(frame);
                    }

                    // 3. Insignia de Rareza en esquina
                    bool isHolo = card != null && (card.rarity == Rarity.Epica || card.rarity == Rarity.Legendaria || card.rarity == Rarity.Mitica || card.rarity == Rarity.FullArt);
                    Label badge = new Label(isHolo ? "★ HOLO" : "★");
                    badge.AddToClassList("mini-card-badge");
                    miniCard.Add(badge);

                    // 4. Footer con Nombre y Rareza
                    VisualElement footer = new VisualElement();
                    footer.AddToClassList("mini-card-footer");

                    string displayName = card != null && !string.IsNullOrEmpty(card.playerName) ? card.playerName.ToUpper() : "CARTA";
                    Label nameLbl = new Label(displayName);
                    nameLbl.AddToClassList("mini-card-name");
                    footer.Add(nameLbl);

                    string rarityLabelText = card != null ? card.rarity.ToString().ToUpper() : "COMUN";
                    Label rarityLbl = new Label(rarityLabelText);
                    rarityLbl.AddToClassList("mini-card-rarity");
                    rarityLbl.AddToClassList($"mini-rarity-{rarityClass}");
                    footer.Add(rarityLbl);

                    miniCard.Add(footer);
                    summaryGrid.Add(miniCard);
                }

                // Despliegue en cascada con pop elastico
                for (int i = 0; i < summaryGrid.childCount; i++)
                {
                    VisualElement child = summaryGrid[i];
                    float t = 0f;
                    while (t < 0.14f)
                    {
                        t += Time.deltaTime;
                        float scale = Mathf.Lerp(0f, 1.08f, t / 0.14f);
                        child.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
                        yield return null;
                    }
                    child.style.scale = new StyleScale(new Scale(Vector3.one));
                    yield return new WaitForSeconds(0.04f);
                }
            }
        }

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "FC";
            string[] parts = fullName.Trim().Split(' ');
            if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
        }
    }
}