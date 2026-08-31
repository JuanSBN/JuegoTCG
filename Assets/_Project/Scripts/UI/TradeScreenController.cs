using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class TradeScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button backButton;

        [Header("Tabs")]
        [SerializeField] private Button tabReceivedButton;
        [SerializeField] private Button tabSentButton;
        [SerializeField] private RoundedRectGraphic tabReceivedGraphic;
        [SerializeField] private RoundedRectGraphic tabSentGraphic;
        [SerializeField] private TMP_Text tabReceivedText;
        [SerializeField] private TMP_Text tabSentText;
        [SerializeField] private GameObject unreadBadgeGO;
        [SerializeField] private TMP_Text unreadBadgeText;

        [Header("Offers List")]
        [SerializeField] private List<TradeOfferCardView> offerCardViews = new List<TradeOfferCardView>();
        [SerializeField] private GameObject emptyStateGO;
        [SerializeField] private Button newTradeButton;

        [Header("Bottom Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        [Header("Colors")]
        [SerializeField] private Color goldColor = new Color(0.910f, 0.659f, 0.125f);
        [SerializeField] private Color darkBgColor = new Color(0.051f, 0.102f, 0.075f);
        [SerializeField] private Color textGrayColor = new Color(1f, 1f, 1f, 0.60f);

        private bool isReceivedTab = true;
        private List<TradeData> receivedTrades = new List<TradeData>();
        private List<TradeData> sentTrades = new List<TradeData>();

        private void Awake()
        {
            FindReferencesIfMissing();
            InitializeData();
        }

        private void Start()
        {
            FindReferencesIfMissing();
            BindEvents();
            UpdateView();
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
        }

        private void InitializeData()
        {
            receivedTrades = new List<TradeData>
            {
                new TradeData
                {
                    id = 1,
                    userName = "MiAmigo_01",
                    avatarText = "MA",
                    timeText = "hace 2 h",
                    isUnread = true,
                    youGive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Mítica", initials = "MÍ" },
                        new TradeCardData { rarity = "Rara", initials = "RA" }
                    },
                    youReceive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Mítica", initials = "MÍ" }
                    }
                },
                new TradeData
                {
                    id = 2,
                    userName = "ElChampion",
                    avatarText = "EC",
                    timeText = "hace 1 d",
                    isUnread = true,
                    youGive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Rara", initials = "RA" }
                    },
                    youReceive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Rara", initials = "RA" },
                        new TradeCardData { rarity = "Común", initials = "CO" }
                    }
                },
                new TradeData
                {
                    id = 3,
                    userName = "ProPlayer_99",
                    avatarText = "PP",
                    timeText = "hace 3 d",
                    isUnread = false,
                    youGive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Poco común", initials = "PO" },
                        new TradeCardData { rarity = "Común", initials = "CO" }
                    },
                    youReceive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Poco común", initials = "PO" }
                    }
                }
            };

            sentTrades = new List<TradeData>
            {
                new TradeData
                {
                    id = 4,
                    userName = "GoldenShot_7",
                    avatarText = "GS",
                    timeText = "hace 5 h",
                    isUnread = false,
                    youGive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Mítica", initials = "MÍ" }
                    },
                    youReceive = new List<TradeCardData>
                    {
                        new TradeCardData { rarity = "Rara", initials = "RA" },
                        new TradeCardData { rarity = "Rara", initials = "RA" }
                    }
                }
            };
        }

        private void BindEvents()
        {
            if (backButton != null) backButton.onClick.AddListener(OnClickBack);

            if (tabReceivedButton != null) tabReceivedButton.onClick.AddListener(() => SwitchTab(true));
            if (tabSentButton != null) tabSentButton.onClick.AddListener(() => SwitchTab(false));

            if (newTradeButton != null) newTradeButton.onClick.AddListener(OnClickNewTrade);

            if (tabInicioButton != null) tabInicioButton.onClick.AddListener(() => SceneManager.LoadScene("HomeScreenScene"));
            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(() => SceneManager.LoadScene("MyCardsScene"));
            if (tabTiendaButton != null) tabTiendaButton.onClick.AddListener(() => SceneManager.LoadScene("StoreScene"));
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(() => SceneManager.LoadScene("CommunityScene"));
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(() => SceneManager.LoadScene("ProfileScene"));
        }

        public void SwitchTab(bool received)
        {
            isReceivedTab = received;
            UpdateView();
        }

        private void UpdateView()
        {
            // Update Tab graphics
            if (tabReceivedGraphic != null)
            {
                tabReceivedGraphic.color = isReceivedTab ? goldColor : Color.clear;
                tabReceivedGraphic.BorderColor = isReceivedTab ? goldColor : new Color(1f, 1f, 1f, 0.15f);
            }
            if (tabSentGraphic != null)
            {
                tabSentGraphic.color = !isReceivedTab ? goldColor : Color.clear;
                tabSentGraphic.BorderColor = !isReceivedTab ? goldColor : new Color(1f, 1f, 1f, 0.15f);
            }

            if (tabReceivedText != null) tabReceivedText.color = isReceivedTab ? darkBgColor : textGrayColor;
            if (tabSentText != null) tabSentText.color = !isReceivedTab ? darkBgColor : textGrayColor;

            // Unread badge count
            int unreadCount = 0;
            foreach (var t in receivedTrades) if (t.isUnread) unreadCount++;

            if (unreadBadgeGO != null) unreadBadgeGO.SetActive(unreadCount > 0);
            if (unreadBadgeText != null) unreadBadgeText.text = unreadCount.ToString();

            // Populate cards
            List<TradeData> currentList = isReceivedTab ? receivedTrades : sentTrades;

            if (emptyStateGO != null) emptyStateGO.SetActive(currentList.Count == 0);

            for (int i = 0; i < offerCardViews.Count; i++)
            {
                if (i < currentList.Count)
                {
                    offerCardViews[i].gameObject.SetActive(true);
                    offerCardViews[i].Setup(currentList[i], isReceivedTab, OnAcceptTrade, OnRejectTrade, OnCancelTrade);
                }
                else
                {
                    offerCardViews[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnAcceptTrade(TradeData trade)
        {
            trade.isUnread = false;
            receivedTrades.Remove(trade);
            UpdateView();
            Debug.Log($"<color=green>[Intercambio] ¡Trato con {trade.userName} aceptado con éxito!</color>");
        }

        private void OnRejectTrade(TradeData trade)
        {
            receivedTrades.Remove(trade);
            UpdateView();
            Debug.Log($"<color=yellow>[Intercambio] Oferta de {trade.userName} rechazada.</color>");
        }

        private void OnCancelTrade(TradeData trade)
        {
            sentTrades.Remove(trade);
            UpdateView();
            Debug.Log($"<color=yellow>[Intercambio] Oferta enviada a {trade.userName} cancelada.</color>");
        }

        private void OnClickNewTrade()
        {
            Debug.Log("<color=gold>[Intercambio] Iniciando propuesta de nuevo intercambio...</color>");
        }

        public void OnClickBack()
        {
            Debug.Log("<color=green>[Intercambio] Regresando a Comunidad...</color>");
            SceneManager.LoadScene("CommunityScene");
        }
    }
}
