using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class VitrinesScreenController : MonoBehaviour
    {
        [Header("Header & Search")]
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_InputField searchInputField;

        [Header("Card Lists")]
        [SerializeField] private List<VitrineCardView> popularCardViews = new List<VitrineCardView>();
        [SerializeField] private List<VitrineCardView> friendCardViews = new List<VitrineCardView>();

        [Header("Subscreens / Detail")]
        [SerializeField] private VitrineDetailController detailModal;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private List<VitrineData> popularData = new List<VitrineData>();
        private List<VitrineData> friendsData = new List<VitrineData>();

        private void Awake()
        {
            FindReferencesIfMissing();
            InitializeData();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            BindEvents();
            PopulateLists();
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

            if (detailModal == null)
            {
                detailModal = canvas.GetComponentInChildren<VitrineDetailController>(true);
            }
        }

        private void InitializeData()
        {
            popularData = new List<VitrineData>
            {
                new VitrineData { userName = "ProPlayer_99", avatarText = "PP", cardRarities = new List<string> { "Mítica", "Rara", "Rara" }, likesCount = 234 },
                new VitrineData { userName = "FutbolFan_22", avatarText = "FF", cardRarities = new List<string> { "Rara", "Poco común", "Común" }, likesCount = 189 },
                new VitrineData { userName = "CardMaster_X", avatarText = "CM", cardRarities = new List<string> { "Mítica", "Mítica", "Rara" }, likesCount = 512 },
                new VitrineData { userName = "GoldenShot_7", avatarText = "GS", cardRarities = new List<string> { "Rara", "Rara", "Poco común" }, likesCount = 97 }
            };

            friendsData = new List<VitrineData>
            {
                new VitrineData { userName = "MiAmigo_01", avatarText = "MA", cardRarities = new List<string> { "Común", "Rara", "Poco común" }, likesCount = 45 },
                new VitrineData { userName = "ElChampion", avatarText = "EC", cardRarities = new List<string> { "Mítica", "Rara", "Común" }, likesCount = 78 }
            };
        }

        private void BindEvents()
        {
            if (backButton != null) backButton.onClick.AddListener(OnClickBack);
            if (searchInputField != null) searchInputField.onValueChanged.AddListener(OnSearchChanged);

            if (tabInicioButton != null) tabInicioButton.onClick.AddListener(() => SceneManager.LoadScene("HomeScreenScene"));
            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(() => SceneManager.LoadScene("MyCardsScene"));
            if (tabTiendaButton != null) tabTiendaButton.onClick.AddListener(() => SceneManager.LoadScene("StoreScene"));
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(() => SceneManager.LoadScene("CommunityScene"));
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(() => SceneManager.LoadScene("ProfileScene"));
        }

        public void PopulateLists()
        {
            FilterAndDisplay("");
        }

        private void OnSearchChanged(string query)
        {
            FilterAndDisplay(query);
        }

        private void FilterAndDisplay(string query)
        {
            string q = query.Trim().ToLower();

            // Popular filter
            for (int i = 0; i < popularCardViews.Count; i++)
            {
                if (i < popularData.Count)
                {
                    bool match = string.IsNullOrEmpty(q) || popularData[i].userName.ToLower().Contains(q);
                    popularCardViews[i].gameObject.SetActive(match);
                    if (match)
                    {
                        popularCardViews[i].Setup(popularData[i], OpenVitrineDetail);
                    }
                }
                else
                {
                    popularCardViews[i].gameObject.SetActive(false);
                }
            }

            // Friends filter
            for (int i = 0; i < friendCardViews.Count; i++)
            {
                if (i < friendsData.Count)
                {
                    bool match = string.IsNullOrEmpty(q) || friendsData[i].userName.ToLower().Contains(q);
                    friendCardViews[i].gameObject.SetActive(match);
                    if (match)
                    {
                        friendCardViews[i].Setup(friendsData[i], OpenVitrineDetail);
                    }
                }
                else
                {
                    friendCardViews[i].gameObject.SetActive(false);
                }
            }
        }

        public void OpenVitrineDetail(VitrineData vitrine)
        {
            if (detailModal != null)
            {
                detailModal.Show(vitrine);
            }
            else
            {
                Debug.LogWarning("[Vitrinas] No se encontró el componente VitrineDetailController!");
            }
        }

        public void OnClickBack()
        {
            Debug.Log("<color=green>[Vitrinas] Regresando a Comunidad...</color>");
            SceneManager.LoadScene("CommunityScene");
        }
    }
}
