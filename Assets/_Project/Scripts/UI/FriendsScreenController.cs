using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class FriendsScreenController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button backButton;

        [Header("Friend Code Box")]
        [SerializeField] private TMP_Text myFriendCodeText;
        [SerializeField] private Button copyCodeButton;
        [SerializeField] private TMP_Text copyButtonText;
        [SerializeField] private TMP_InputField addCodeInputField;
        [SerializeField] private Button addFriendButton;

        [Header("Requests Section")]
        [SerializeField] private GameObject requestsSectionGO;
        [SerializeField] private TMP_Text requestsBadgeText;
        [SerializeField] private List<FriendRequestCardView> requestCardViews = new List<FriendRequestCardView>();

        [Header("Friends Section")]
        [SerializeField] private List<FriendCardView> friendCardViews = new List<FriendCardView>();

        [Header("Ranking Section")]
        [SerializeField] private List<FriendRankingRowView> rankingRowViews = new List<FriendRankingRowView>();

        [Header("Bottom Navigation Tabs")]
        [SerializeField] private Button tabInicioButton;
        [SerializeField] private Button tabCartasButton;
        [SerializeField] private Button tabTiendaButton;
        [SerializeField] private Button tabComunidadButton;
        [SerializeField] private Button tabPerfilButton;

        private const string MyFriendCode = "FCX-2847";
        private const int MyPower = 5430;

        private List<FriendRequestData> allRequests = new List<FriendRequestData>();
        private List<FriendData> allFriends = new List<FriendData>();

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
            if (myFriendCodeText != null) myFriendCodeText.text = MyFriendCode;

            allRequests = new List<FriendRequestData>
            {
                new FriendRequestData { id = 1, userName = "NuevoJugador_01", avatar = "NJ" },
                new FriendRequestData { id = 2, userName = "FutbolFan_77", avatar = "F7" }
            };

            allFriends = new List<FriendData>
            {
                new FriendData { id = 1, userName = "GoldenShot_7", avatar = "GS", level = 24, cardsCount = 445, albumPct = 89, power = 9120 },
                new FriendData { id = 2, userName = "ElChampion", avatar = "EC", level = 18, cardsCount = 312, albumPct = 71, power = 6840 },
                new FriendData { id = 3, userName = "MiAmigo_01", avatar = "MA", level = 12, cardsCount = 187, albumPct = 52, power = 4250 },
                new FriendData { id = 4, userName = "FutbolFan_22", avatar = "FF", level = 9, cardsCount = 98, albumPct = 28, power = 2180 }
            };
        }

        private void BindEvents()
        {
            if (backButton != null) backButton.onClick.AddListener(OnClickBack);
            if (copyCodeButton != null) copyCodeButton.onClick.AddListener(OnClickCopyCode);
            if (addFriendButton != null) addFriendButton.onClick.AddListener(OnClickAddFriend);

            if (tabInicioButton != null) tabInicioButton.onClick.AddListener(() => SceneManager.LoadScene("HomeScreenScene"));
            if (tabCartasButton != null) tabCartasButton.onClick.AddListener(() => SceneManager.LoadScene("MyCardsScene"));
            if (tabTiendaButton != null) tabTiendaButton.onClick.AddListener(() => SceneManager.LoadScene("StoreScene"));
            if (tabComunidadButton != null) tabComunidadButton.onClick.AddListener(() => SceneManager.LoadScene("CommunityScene"));
            if (tabPerfilButton != null) tabPerfilButton.onClick.AddListener(() => SceneManager.LoadScene("ProfileScene"));
        }

        private void UpdateView()
        {
            // Requests
            if (requestsBadgeText != null) requestsBadgeText.text = allRequests.Count.ToString();
            if (requestsSectionGO != null) requestsSectionGO.SetActive(allRequests.Count > 0);

            for (int i = 0; i < requestCardViews.Count; i++)
            {
                if (i < allRequests.Count)
                {
                    requestCardViews[i].gameObject.SetActive(true);
                    requestCardViews[i].Setup(allRequests[i], OnAcceptRequest, OnRejectRequest);
                }
                else
                {
                    requestCardViews[i].gameObject.SetActive(false);
                }
            }

            // Friends
            for (int i = 0; i < friendCardViews.Count; i++)
            {
                if (i < allFriends.Count)
                {
                    friendCardViews[i].gameObject.SetActive(true);
                    friendCardViews[i].Setup(allFriends[i], OnCompareFriend, OnTradeWithFriend);
                }
                else
                {
                    friendCardViews[i].gameObject.SetActive(false);
                }
            }

            // Ranking Calculation
            List<RankingEntryData> ranking = new List<RankingEntryData>
            {
                new RankingEntryData { userName = "Tú", avatar = "YO", power = MyPower, isMe = true }
            };

            foreach (var f in allFriends)
            {
                ranking.Add(new RankingEntryData { userName = f.userName, avatar = f.avatar, power = f.power, isMe = false });
            }

            ranking.Sort((a, b) => b.power.CompareTo(a.power));

            for (int i = 0; i < ranking.Count; i++)
            {
                ranking[i].position = i + 1;
            }

            for (int i = 0; i < rankingRowViews.Count; i++)
            {
                if (i < ranking.Count)
                {
                    rankingRowViews[i].gameObject.SetActive(true);
                    rankingRowViews[i].Setup(ranking[i]);
                }
                else
                {
                    rankingRowViews[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnClickCopyCode()
        {
            GUIUtility.systemCopyBuffer = MyFriendCode;
            if (copyButtonText != null) copyButtonText.text = "¡COPIADO!";
            Debug.Log($"<color=green>[Amigos] Código {MyFriendCode} copiado al portapapeles.</color>");
            CancelInvoke(nameof(ResetCopyText));
            Invoke(nameof(ResetCopyText), 2f);
        }

        private void ResetCopyText()
        {
            if (copyButtonText != null) copyButtonText.text = "COPIAR";
        }

        private void OnClickAddFriend()
        {
            if (addCodeInputField != null && !string.IsNullOrEmpty(addCodeInputField.text))
            {
                Debug.Log($"<color=green>[Amigos] Solicitud enviada al código: {addCodeInputField.text}</color>");
                addCodeInputField.text = "";
            }
        }

        private void OnAcceptRequest(FriendRequestData req)
        {
            allRequests.Remove(req);
            allFriends.Add(new FriendData
            {
                id = (int)DateTime.UtcNow.Ticks,
                userName = req.userName,
                avatar = req.avatar,
                level = 1,
                cardsCount = 12,
                albumPct = 5,
                power = 320
            });
            UpdateView();
            Debug.Log($"<color=green>[Amigos] Solicitud aceptada de {req.userName}.</color>");
        }

        private void OnRejectRequest(FriendRequestData req)
        {
            allRequests.Remove(req);
            UpdateView();
            Debug.Log($"<color=yellow>[Amigos] Solicitud rechazada de {req.userName}.</color>");
        }

        private void OnCompareFriend(FriendData friend)
        {
            Debug.Log($"<color=cyan>[Amigos] Comparando colección con {friend.userName}...</color>");
        }

        private void OnTradeWithFriend(FriendData friend)
        {
            Debug.Log($"<color=green>[Amigos] Navegando a Intercambio con {friend.userName}...</color>");
            SceneManager.LoadScene("TradeScene");
        }

        public void OnClickBack()
        {
            Debug.Log("<color=green>[Amigos] Regresando a Comunidad...</color>");
            SceneManager.LoadScene("CommunityScene");
        }
    }
}
