using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JuegoTCG.Cards;
using JuegoTCG.Networking;

namespace JuegoTCG.UI
{
    public class MyCardsScreenController : MonoBehaviour
    {
        [Header("Header & Counters")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text totalCardsCountText;
        [SerializeField] private Button searchButton;

        [Header("Filters")]
        [SerializeField] private Button[] filterButtons;
        [SerializeField] private Button scrollLeftBtn;
        [SerializeField] private Button scrollRightBtn;
        [SerializeField] private ScrollRect filterScrollRect;

        [Header("Card Grid Container")]
        [SerializeField] private Transform cardGridContainer;

        [Header("Bottom Tabs (5 Tabs)")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private string activeFilter = "Todas";

        private void Awake()
        {
            FindReferencesIfMissing();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            EnsureCollectionManager();
            InitializeHeader();
            BindNavigationEvents();
            RenderAlbumGrid();
        }

        private void EnsureCollectionManager()
        {
            if (PlayerCollectionManager.Instance == null)
            {
                GameObject cmGO = new GameObject("PlayerCollectionManager");
                cmGO.AddComponent<PlayerCollectionManager>();
            }

            PlayerCollectionManager.Instance.OnCollectionUpdated -= OnCollectionChanged;
            PlayerCollectionManager.Instance.OnCollectionUpdated += OnCollectionChanged;
        }

        private void OnDestroy()
        {
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated -= OnCollectionChanged;
            }
        }

        private void OnCollectionChanged()
        {
            InitializeHeader();
            RenderAlbumGrid();
        }

        private void FindReferencesIfMissing()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (tabInicioButton == null && b.name.Contains("Inicio")) tabInicioButton = b;
                else if (tabCartasButton == null && b.name.Contains("cartas")) tabCartasButton = b;
                else if (tabTiendaButton == null && b.name.Contains("Tienda")) tabTiendaButton = b;
                else if (tabComunidadButton == null && b.name.Contains("Comunidad")) tabComunidadButton = b;
                else if (tabPerfilButton == null && b.name.Contains("Perfil")) tabPerfilButton = b;
            }

            if (cardGridContainer == null)
            {
                var gridTransform = canvas.transform.Find("GridContainer") ?? canvas.transform.Find("Scroll View/Viewport/Content");
                if (gridTransform != null) cardGridContainer = gridTransform;
            }
        }

        private void InitializeHeader()
        {
            if (titleText != null)
            {
                titleText.text = "MIS CARTAS";
                RectTransform rt = titleText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Mantener el título arriba dentro de su Header sin bajar a tapar los filtros
                    rt.anchoredPosition = new Vector2(0f, 0f);
                }

                // Ajustar el contenedor Header completo hacia abajo para el Safe Area
                Transform parentHeader = titleText.transform.parent;
                if (parentHeader != null && parentHeader.name.Contains("Header"))
                {
                    RectTransform hRect = parentHeader.GetComponent<RectTransform>();
                    if (hRect != null)
                    {
                        hRect.anchoredPosition = new Vector2(0f, -85f);
                    }
                }
            }

            int owned = 0, total = 10;
            float percent = 0f;

            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.GetAlbumProgress(out owned, out total, out percent);
            }

            if (totalCardsCountText != null)
            {
                totalCardsCountText.text = $"{owned}/{total} Cartas ({Mathf.RoundToInt(percent * 100)}%)";
            }

            // GDD 10.1 Momento 2: Recordatorio suave tras completar el primer álbum
            CheckAlbumCompletedLinkingReminder(owned, total);
        }

        public void RenderAlbumGrid()
        {
            if (PlayerCollectionManager.Instance == null) return;

            var catalog = PlayerCollectionManager.Instance.GetCatalog();
            int ownedCount = 0;

            foreach (var item in catalog)
            {
                bool isOwned = PlayerCollectionManager.Instance.IsCardOwned(item.cardId);
                int qty = PlayerCollectionManager.Instance.GetOwnedCount(item.cardId);
                if (isOwned) ownedCount++;

                string statusIcon = isOwned ? $"[DESBLOQUEADA x{qty}]" : "[BLOQUEADA 🔒]";
                Debug.Log($"<color={(isOwned ? "green" : "gray")}>[Álbum] {statusIcon} #{item.cardId} - {item.playerName} ({item.rarity} - {item.position})</color>");
            }

            Debug.Log($"<color=gold>[Álbum] Cuadrícula renderizada: {ownedCount}/{catalog.Count} cartas desbloqueadas.</color>");
        }

        public void CheckAlbumCompletedLinkingReminder(int current, int total)
        {
            if (current >= total && total > 0)
            {
                if (FirebaseAuthManager.Instance != null && !FirebaseAuthManager.Instance.IsLinked)
                {
                    Debug.Log("<color=cyan>[GDD 10.1 Momento 2] ¡Álbum completado! Recordatorio suave: Vincula tu cuenta con Google para asegurar tu colección permanente.</color>");
                }
            }
        }

        private void BindNavigationEvents()
        {
            if (tabInicioButton != null)
            {
                tabInicioButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Regresando a Pantalla de Inicio...</color>");
                    SceneManager.LoadScene("HomeScreenScene");
                });
            }

            if (tabTiendaButton != null)
            {
                tabTiendaButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Navegando a Tienda...</color>");
                    SceneManager.LoadScene("StoreScene");
                });
            }

            if (tabComunidadButton != null)
            {
                tabComunidadButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Navegando a Comunidad...</color>");
                    SceneManager.LoadScene("CommunityScene");
                });
            }

            if (tabPerfilButton != null)
            {
                tabPerfilButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=green>[MyCardsScreen] Navegando a Perfil...</color>");
                    SceneManager.LoadScene("ProfileScene");
                });
            }

            if (scrollLeftBtn != null && filterScrollRect != null)
            {
                scrollLeftBtn.onClick.AddListener(() =>
                {
                    filterScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(filterScrollRect.horizontalNormalizedPosition - 0.25f);
                });
            }

            if (scrollRightBtn != null && filterScrollRect != null)
            {
                scrollRightBtn.onClick.AddListener(() =>
                {
                    filterScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(filterScrollRect.horizontalNormalizedPosition + 0.25f);
                });
            }

            if (searchButton != null)
            {
                searchButton.onClick.AddListener(() =>
                {
                    Debug.Log("<color=yellow>[MyCardsScreen] Buscar cartas activado</color>");
                });
            }
        }

        public void SetActiveFilter(string filterName)
        {
            activeFilter = filterName;
            Debug.Log($"<color=gold>[MyCardsScreen] Filtro seleccionado: {filterName}</color>");
            RenderAlbumGrid();
        }
    }
}
