using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Networking;
using JuegoTCG.Social;
using FriendData = JuegoTCG.Social.FriendData;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Amigos (FriendsScreen).
    /// Permite copiar código de amigo, agregar nuevos amigos, aceptar o rechazar solicitudes
    /// con actualización en vivo del badge, comparar colecciones e iniciar intercambios directos.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitFriendsController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private VisualElement root;

        private Button backBtn;
        private Label myFriendCodeLabel;
        private Button btnCopyCode;
        private Label btnCopyCodeText;
        private TextField searchFriendInput;
        private Button btnAddFriend;
        private VisualElement friendsEmptyState;

        // Solicitudes
        private VisualElement requestsSection;
        private VisualElement requestsBadge;
        private Label requestsBadgeCount;
        private VisualElement cardRequest1;
        private VisualElement cardRequest2;
        private Button btnAccept1;
        private Button btnReject1;
        private Button btnAccept2;
        private Button btnReject2;
        private int pendingRequestsCount = 2;

        // Modal de Comparar Lado a Lado
        private VisualElement compareModal;
        private Label compareModalTitle;
        private Label compareModalDesc;
        private Button btnCloseCompare;
        private Button btnCloseCompareBottom;

        // Head to Head Elements
        private Label compareMeAvatar;
        private Label compareMeName;
        private Label compareMeCount;
        private Label compareMeProgressPct;
        private Label compareDiffTag;
        private Label compareFriendAvatar;
        private Label compareFriendName;
        private Label compareFriendCount;
        private Label compareFriendProgressPct;
        private VisualElement compareDualProgressMe;
        private VisualElement compareDualProgressFriend;

        // Filtros
        private Button btnFilterAll;
        private Label lblFilterAll;
        private Button btnFilterNeed;
        private Label lblFilterNeed;
        private Button btnFilterOffers;
        private Label lblFilterOffers;
        private VisualElement compareCardsList;

        // Intercambio
        private Button btnTradeWithFriend;
        private Label btnTradeWithFriendText;

        private AlbumComparisonData currentComparison;
        private enum CompareFilterMode { All, Need, Offers }
        private CompareFilterMode currentFilter = CompareFilterMode.All;
        private string activeComparedFriend = "GoldenShot_7";

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            BindUI();
        }

        private void BindUI()
        {
            // Back button
            backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunitySceneUIToolkit");
            }

            // Friend Code Display & Copy
            myFriendCodeLabel = root.Q<Label>("MyFriendCode");
            btnCopyCode = root.Q<Button>("Btn_CopyCode");
            btnCopyCodeText = root.Q<Label>("Btn_CopyCodeText");
            friendsEmptyState = root.Q<VisualElement>("FriendsEmptyState");

            SocialService.EnsureExists();
            UpdateFriendCodeUI();

            if (btnCopyCode != null)
            {
                btnCopyCode.clicked += CopyFriendCode;
            }

            // Add friend
            searchFriendInput = root.Q<TextField>("SearchFriendInput");
            btnAddFriend = root.Q<Button>("Btn_AddFriend");
            if (btnAddFriend != null)
            {
                btnAddFriend.clicked += AddFriendByCode;
            }

            // Solicitudes
            requestsSection = root.Q<VisualElement>("RequestsSection");
            requestsBadge = root.Q<VisualElement>("RequestsBadge");
            requestsBadgeCount = root.Q<Label>("RequestsBadgeCount");
            cardRequest1 = root.Q<VisualElement>("Card_Request_1");
            cardRequest2 = root.Q<VisualElement>("Card_Request_2");
            btnAccept1 = root.Q<Button>("Btn_Accept_1");
            btnReject1 = root.Q<Button>("Btn_Reject_1");
            btnAccept2 = root.Q<Button>("Btn_Accept_2");
            btnReject2 = root.Q<Button>("Btn_Reject_2");

            if (btnAccept1 != null) btnAccept1.clicked += () => ResolveRequest(cardRequest1, "NuevoJugador_99", true);
            if (btnReject1 != null) btnReject1.clicked += () => ResolveRequest(cardRequest1, "NuevoJugador_99", false);
            if (btnAccept2 != null) btnAccept2.clicked += () => ResolveRequest(cardRequest2, "FutbolFan_77", true);
            if (btnReject2 != null) btnReject2.clicked += () => ResolveRequest(cardRequest2, "FutbolFan_77", false);

            // Mis Amigos (Comparar e Intercambiar)
            WireFriend(1, "GoldenShot_7", 24, 89);
            WireFriend(2, "ElChampion", 18, 71);
            WireFriend(3, "MiAmigo_01", 12, 52);
            WireFriend(4, "FutbolFan_22", 8, 34);

            // Modal Lado a Lado
            compareModal = root.Q<VisualElement>("CompareModal");
            compareModalTitle = root.Q<Label>("CompareModalTitle");
            compareModalDesc = root.Q<Label>("CompareModalDesc");
            btnCloseCompare = root.Q<Button>("Btn_CloseCompare");
            btnCloseCompareBottom = root.Q<Button>("Btn_CloseCompareBottom");

            if (btnCloseCompare != null)
            {
                btnCloseCompare.clicked += CloseCompareModal;
            }
            if (btnCloseCompareBottom != null)
            {
                btnCloseCompareBottom.clicked += CloseCompareModal;
            }

            // Head to head refs
            compareMeAvatar = root.Q<Label>("CompareMeAvatar");
            compareMeName = root.Q<Label>("CompareMeName");
            compareMeCount = root.Q<Label>("CompareMeCount");
            compareMeProgressPct = root.Q<Label>("CompareMeProgressPct");
            compareDiffTag = root.Q<Label>("CompareDiffTag");

            compareFriendAvatar = root.Q<Label>("CompareFriendAvatar");
            compareFriendName = root.Q<Label>("CompareFriendName");
            compareFriendCount = root.Q<Label>("CompareFriendCount");
            compareFriendProgressPct = root.Q<Label>("CompareFriendProgressPct");

            compareDualProgressMe = root.Q<VisualElement>("CompareDualProgressMe");
            compareDualProgressFriend = root.Q<VisualElement>("CompareDualProgressFriend");

            // Filter refs
            btnFilterAll = root.Q<Button>("Btn_Filter_All");
            lblFilterAll = root.Q<Label>("Lbl_Filter_All");
            btnFilterNeed = root.Q<Button>("Btn_Filter_Need");
            lblFilterNeed = root.Q<Label>("Lbl_Filter_Need");
            btnFilterOffers = root.Q<Button>("Btn_Filter_Offers");
            lblFilterOffers = root.Q<Label>("Lbl_Filter_Offers");
            compareCardsList = root.Q<VisualElement>("CompareCardsList");

            if (btnFilterAll != null) btnFilterAll.clicked += () => SetFilter(CompareFilterMode.All);
            if (btnFilterNeed != null) btnFilterNeed.clicked += () => SetFilter(CompareFilterMode.Need);
            if (btnFilterOffers != null) btnFilterOffers.clicked += () => SetFilter(CompareFilterMode.Offers);

            // Trade with friend button
            btnTradeWithFriend = root.Q<Button>("Btn_TradeWithFriend");
            btnTradeWithFriendText = root.Q<Label>("Btn_TradeWithFriendText");
            if (btnTradeWithFriend != null)
            {
                btnTradeWithFriend.clicked += () =>
                {
                    Debug.Log($"<color=gold>[Comparar] Proponiendo intercambio con {activeComparedFriend}...</color>");
                    SceneManager.LoadScene("TradeSceneUIToolkit");
                };
            }

            // Actualizar Ranking de Amigos por Poder de Colección (GDD 7.2)
            UpdateRankingUI();

            Cards.PlayerCollectionManager.EnsureExists();
            if (Cards.PlayerCollectionManager.Instance != null)
            {
                Cards.PlayerCollectionManager.Instance.OnCollectionPowerUpdated += (p) => UpdateRankingUI();
            }

            // Bottom Nav
            var navCtrl = GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navCtrl.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }

        private void UpdateRankingUI()
        {
            SocialService.EnsureExists();
            var ranking = SocialService.Instance.GetFriendsRanking();
            if (ranking == null || ranking.Count == 0) return;

            for (int i = 1; i <= 5; i++)
            {
                var row = root.Q<VisualElement>($"Ranking_Row_{i}");
                if (row == null || i > ranking.Count) continue;

                var item = ranking[i - 1];

                var posLbl = row.Q<Label>(className: "ranking-pos");
                var avatarBox = row.Q<VisualElement>(className: "ranking-avatar");
                var avatarLbl = row.Q<Label>(className: "ranking-avatar-text");
                var nameLbl = row.Q<Label>(className: "ranking-name");
                var powerLbl = row.Q<Label>(className: "ranking-power-text");

                if (posLbl != null)
                {
                    posLbl.text = item.rank.ToString();
                    posLbl.RemoveFromClassList("ranking-pos-first");
                    posLbl.RemoveFromClassList("ranking-pos-second");
                    if (item.rank == 1) posLbl.AddToClassList("ranking-pos-first");
                    else if (item.rank == 2) posLbl.AddToClassList("ranking-pos-second");
                }

                if (avatarLbl != null) avatarLbl.text = item.avatar;
                if (nameLbl != null) nameLbl.text = item.displayName;
                if (powerLbl != null) powerLbl.text = item.power.ToString();

                if (item.isMe)
                {
                    row.AddToClassList("ranking-row-me");
                    avatarBox?.AddToClassList("ranking-avatar-me");
                    avatarLbl?.AddToClassList("ranking-avatar-text-me");
                    nameLbl?.AddToClassList("ranking-name-me");
                    powerLbl?.AddToClassList("ranking-power-text-me");
                }
                else
                {
                    row.RemoveFromClassList("ranking-row-me");
                    avatarBox?.RemoveFromClassList("ranking-avatar-me");
                    avatarLbl?.RemoveFromClassList("ranking-avatar-text-me");
                    nameLbl?.RemoveFromClassList("ranking-name-me");
                    powerLbl?.RemoveFromClassList("ranking-power-text-me");
                }
            }
        }

        private void CloseCompareModal()
        {
            if (compareModal != null)
            {
                compareModal.AddToClassList("modal-hidden");
            }
        }

        private void UpdateFriendCodeUI()
        {
            if (myFriendCodeLabel != null)
            {
                string code = FirebaseAuthManager.Instance != null 
                    ? FirebaseAuthManager.Instance.FriendCode 
                    : (SocialService.Instance != null ? SocialService.Instance.MyFriendCode : "FCX-2847");
                myFriendCodeLabel.text = code;
            }
        }

        private void CopyFriendCode()
        {
            string code = FirebaseAuthManager.Instance != null 
                ? FirebaseAuthManager.Instance.FriendCode 
                : (SocialService.Instance != null ? SocialService.Instance.MyFriendCode : "FCX-2847");
            GUIUtility.systemCopyBuffer = code;
            if (btnCopyCodeText != null)
            {
                btnCopyCodeText.text = "¡COPIADO!";
                StartCoroutine(ResetCopyText());
            }
            Debug.Log($"<color=gold>[Amigos] Código {code} copiado al portapapeles.</color>");
        }

        private IEnumerator ResetCopyText()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (btnCopyCodeText != null)
            {
                btnCopyCodeText.text = "COPIAR";
            }
        }

        private async void AddFriendByCode()
        {
            string code = searchFriendInput?.value?.Trim();
            if (string.IsNullOrEmpty(code))
            {
                ShowSimpleModal("CÓDIGO VACÍO", "Escribe el código de amigo que deseas agregar.");
                return;
            }

            SocialService.EnsureExists();
            var result = await SocialService.Instance.SendFriendRequestByCodeAsync(code);
            if (result.success)
            {
                if (searchFriendInput != null) searchFriendInput.value = string.Empty;
                ShowSimpleModal("SOLICITUD ENVIADA", result.message);
            }
            else
            {
                ShowSimpleModal("NO SE PUDO AGREGAR", result.message);
            }
        }

        private void ResolveRequest(VisualElement card, string name, bool accepted)
        {
            if (card != null) card.style.display = DisplayStyle.None;
            pendingRequestsCount = Mathf.Max(0, pendingRequestsCount - 1);

            if (requestsBadgeCount != null) requestsBadgeCount.text = pendingRequestsCount.ToString();
            if (requestsBadge != null) requestsBadge.style.display = pendingRequestsCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            string action = accepted ? "aceptada" : "rechazada";
            Debug.Log($"<color=cyan>[Amigos] Solicitud de {name} {action}.</color>");

            if (accepted)
            {
                SocialService.EnsureExists();
                SocialService.Instance.AddFriend(new JuegoTCG.Social.FriendData
                {
                    displayName = name,
                    friendCode = "FC-" + UnityEngine.Random.Range(1000, 9999),
                    level = UnityEngine.Random.Range(5, 15),
                    collectionPower = UnityEngine.Random.Range(2000, 5000),
                    albumProgress = UnityEngine.Random.Range(30, 70)
                });
            }
        }

        private void WireFriend(int index, string friendName, int level, int progressPct)
        {
            var btnCompare = root.Q<Button>($"Btn_Compare_{index}");
            var btnTrade = root.Q<Button>($"Btn_Trade_{index}");

            if (btnCompare != null)
            {
                btnCompare.clicked += () => OpenCompareModal(friendName, level, progressPct);
            }

            if (btnTrade != null)
            {
                btnTrade.clicked += () =>
                {
                    Debug.Log($"<color=gold>[Amigos] Redirigiendo a Intercambio con {friendName}...</color>");
                    SceneManager.LoadScene("TradeSceneUIToolkit");
                };
            }
        }

        /// <summary>
        /// Abre el modal de comparación lado a lado con el amigo seleccionado.
        /// </summary>
        public void OpenCompareModal(string friendName, int level, int progressPct)
        {
            activeComparedFriend = friendName;
            SocialService.EnsureExists();

            currentComparison = SocialService.Instance.GetFriendAlbumComparison(friendName, level, progressPct);

            if (compareModalTitle != null) compareModalTitle.text = "COMPARAR COLECCIÓN";
            if (compareModalDesc != null) compareModalDesc.text = $"Álbum Piloto • Comparando con {friendName}";

            // Lado TÚ
            if (compareMeName != null) compareMeName.text = "Tú";
            if (compareMeCount != null) compareMeCount.text = $"{currentComparison.myUnique} / {currentComparison.totalCards} cartas";
            if (compareMeProgressPct != null) compareMeProgressPct.text = $"{Mathf.RoundToInt(currentComparison.myProgressPct)}%";

            // Lado AMIGO
            if (compareFriendAvatar != null)
            {
                string initials = friendName.Length >= 2 ? friendName.Substring(0, 2).ToUpper() : "AM";
                compareFriendAvatar.text = initials;
            }
            if (compareFriendName != null) compareFriendName.text = friendName;
            if (compareFriendCount != null) compareFriendCount.text = $"{currentComparison.friendUnique} / {currentComparison.totalCards} cartas";
            if (compareFriendProgressPct != null) compareFriendProgressPct.text = $"{Mathf.RoundToInt(currentComparison.friendProgressPct)}%";

            // Centro: Diferencia
            int diff = currentComparison.myUnique - currentComparison.friendUnique;
            if (compareDiffTag != null)
            {
                if (diff > 0)
                {
                    compareDiffTag.text = $"+{diff} ventaja";
                    compareDiffTag.style.color = new StyleColor(new Color(0.06f, 0.72f, 0.5f)); // Verde
                }
                else if (diff < 0)
                {
                    compareDiffTag.text = $"{diff} cartas";
                    compareDiffTag.style.color = new StyleColor(new Color(0.93f, 0.26f, 0.26f)); // Rojo
                }
                else
                {
                    compareDiffTag.text = "Empatados";
                    compareDiffTag.style.color = new StyleColor(new Color(0.91f, 0.66f, 0.12f)); // Oro
                }
            }

            // Barra dual
            if (compareDualProgressMe != null && compareDualProgressFriend != null)
            {
                float total = currentComparison.myProgressPct + currentComparison.friendProgressPct;
                float meRatio = total > 0 ? (currentComparison.myProgressPct / total) * 100f : 50f;
                float friendRatio = 100f - meRatio;

                compareDualProgressMe.style.width = new StyleLength(new Length(meRatio, LengthUnit.Percent));
                compareDualProgressFriend.style.width = new StyleLength(new Length(friendRatio, LengthUnit.Percent));
            }

            // Actualizar etiquetas de los filtros
            if (lblFilterAll != null) lblFilterAll.text = $"TODAS ({currentComparison.items.Count})";
            if (lblFilterNeed != null) lblFilterNeed.text = $"TE FALTAN ({currentComparison.missingForMeCount})";
            if (lblFilterOffers != null) lblFilterOffers.text = $"LE FALTAN ({currentComparison.missingForFriendCount})";

            if (btnTradeWithFriendText != null)
            {
                btnTradeWithFriendText.text = $"INTERCAMBIAR CON {friendName.ToUpper()}";
            }

            SetFilter(CompareFilterMode.All);

            if (compareModal != null)
            {
                compareModal.RemoveFromClassList("modal-hidden");
            }
        }

        private void SetFilter(CompareFilterMode mode)
        {
            currentFilter = mode;

            btnFilterAll?.RemoveFromClassList("compare-tab-active");
            btnFilterNeed?.RemoveFromClassList("compare-tab-active");
            btnFilterOffers?.RemoveFromClassList("compare-tab-active");

            switch (mode)
            {
                case CompareFilterMode.All:
                    btnFilterAll?.AddToClassList("compare-tab-active");
                    break;
                case CompareFilterMode.Need:
                    btnFilterNeed?.AddToClassList("compare-tab-active");
                    break;
                case CompareFilterMode.Offers:
                    btnFilterOffers?.AddToClassList("compare-tab-active");
                    break;
            }

            RenderCardsList();
        }

        private void RenderCardsList()
        {
            if (compareCardsList == null || currentComparison == null) return;

            compareCardsList.Clear();

            foreach (var item in currentComparison.items)
            {
                if (currentFilter == CompareFilterMode.Need && item.status != CardComparisonStatus.MissingForMe)
                    continue;

                if (currentFilter == CompareFilterMode.Offers && item.status != CardComparisonStatus.MissingForFriend)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("compare-card-row");

                // Info izquierda (Avatar + detalles)
                var infoBox = new VisualElement();
                infoBox.AddToClassList("compare-card-info");

                var avatarBox = new VisualElement();
                avatarBox.AddToClassList("compare-card-avatar");
                var avatarTxt = new Label(item.initials);
                avatarTxt.AddToClassList("compare-card-avatar-text");
                avatarBox.Add(avatarTxt);

                var detailsBox = new VisualElement();
                detailsBox.AddToClassList("compare-card-details");
                var nameLbl = new Label(item.playerName);
                nameLbl.AddToClassList("compare-card-player-name");
                var teamLbl = new Label($"{item.teamName} • {item.position} • {item.rarityText}");
                teamLbl.AddToClassList("compare-card-team");
                detailsBox.Add(nameLbl);
                detailsBox.Add(teamLbl);

                infoBox.Add(avatarBox);
                infoBox.Add(detailsBox);

                // Conteo Tú vs Amigo
                var countsBox = new VisualElement();
                countsBox.AddToClassList("compare-card-counts");

                var meCol = new VisualElement();
                meCol.AddToClassList("compare-count-col");
                var meLbl = new Label("Tú");
                meLbl.AddToClassList("compare-count-label");
                var meVal = new Label(item.myCount > 0 ? $"x{item.myCount}" : "0");
                meVal.AddToClassList("compare-count-value");
                if (item.myCount > 0) meVal.style.color = new StyleColor(new Color(0.91f, 0.66f, 0.12f));
                meCol.Add(meLbl);
                meCol.Add(meVal);

                var friendCol = new VisualElement();
                friendCol.AddToClassList("compare-count-col");
                var friendLbl = new Label("Amigo");
                friendLbl.AddToClassList("compare-count-label");
                var friendVal = new Label(item.friendCount > 0 ? $"x{item.friendCount}" : "0");
                friendVal.AddToClassList("compare-count-value");
                if (item.friendCount > 0) friendVal.style.color = new StyleColor(new Color(0.38f, 0.65f, 0.98f));
                friendCol.Add(friendLbl);
                friendCol.Add(friendVal);

                // Badge de estado
                var tag = new Label();
                tag.AddToClassList("compare-tag");

                switch (item.status)
                {
                    case CardComparisonStatus.MissingForMe:
                        tag.text = "¡Te falta!";
                        tag.AddToClassList("compare-tag-missing-me");
                        break;
                    case CardComparisonStatus.MissingForFriend:
                        tag.text = item.canTrade ? "¡Ofrecer!" : "Le falta";
                        tag.AddToClassList("compare-tag-missing-friend");
                        break;
                    case CardComparisonStatus.BothOwned:
                        tag.text = "Ambos tienen";
                        tag.AddToClassList("compare-tag-both");
                        break;
                    default:
                        tag.text = "Ninguno";
                        tag.AddToClassList("compare-tag-neither");
                        break;
                }

                countsBox.Add(meCol);
                countsBox.Add(friendCol);
                countsBox.Add(tag);

                row.Add(infoBox);
                row.Add(countsBox);

                compareCardsList.Add(row);
            }
        }

        private void ShowSimpleModal(string title, string desc)
        {
            if (compareModalTitle != null) compareModalTitle.text = title;
            if (compareModalDesc != null) compareModalDesc.text = desc;
            if (compareModal != null) compareModal.RemoveFromClassList("modal-hidden");
        }
    }
}