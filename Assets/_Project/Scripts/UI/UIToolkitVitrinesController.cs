using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JuegoTCG.UI
{
    public class UIToolkitVitrinesController : MonoBehaviour
    {
        private UIDocument uiDoc;
        private VisualElement root;

        // Views
        private VisualElement catalogView;
        private VisualElement detailView;

        // Detail elements
        private Label detailAvatarText;
        private Label detailUserName;
        private Label detailCardCount;
        private Button detailCloseBtn;
        private Button floatingLikeBtn;
        private Label detailLikeCount;
        private VisualElement detailCardsGrid;

        // Catalog elements
        private Button backBtn;
        private TextField searchInput;
        private VisualElement popularGrid;
        private VisualElement friendsGrid;

        private int currentLikes = 234;
        private bool isLiked = false;

        private void OnEnable()
        {
            uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            if (root == null) return;

            BindElements();
            RegisterCallbacks();
        }

        private void BindElements()
        {
            catalogView = root.Q<VisualElement>("CatalogView");
            detailView = root.Q<VisualElement>("DetailView");

            backBtn = root.Q<Button>("BackBtn");
            searchInput = root.Q<TextField>("SearchInput");
            popularGrid = root.Q<VisualElement>("PopularGrid");
            friendsGrid = root.Q<VisualElement>("FriendsGrid");

            detailAvatarText = root.Q<Label>("DetailAvatarText");
            detailUserName = root.Q<Label>("DetailUserName");
            detailCardCount = root.Q<Label>("DetailCardCount");
            detailCloseBtn = root.Q<Button>("DetailCloseBtn");
            floatingLikeBtn = root.Q<Button>("FloatingLikeBtn");
            detailLikeCount = root.Q<Label>("DetailLikeCount");
            detailCardsGrid = root.Q<VisualElement>("DetailCardsGrid");
        }

        private void RegisterCallbacks()
        {
            // Back button
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunityScene");
            }

            // Close detail button
            if (detailCloseBtn != null)
            {
                detailCloseBtn.clicked += CloseDetailView;
            }

            // Floating Like button
            if (floatingLikeBtn != null)
            {
                floatingLikeBtn.clicked += ToggleLike;
            }

            // Search filter
            if (searchInput != null)
            {
                searchInput.RegisterValueChangedCallback(evt => FilterCards(evt.newValue));
            }

            // Wire Vitrine Cards to open Detail View
            WireVitrineCard("Card_ProPlayer_99", "PROPLAYER_99", "PP", 234);
            WireVitrineCard("Card_FutbolFan_22", "FUTBOLFAN_22", "FF", 189);
            WireVitrineCard("Card_CardMaster_X", "CARDMASTER_X", "CM", 512);
            WireVitrineCard("Card_GoldenShot_7", "GOLDENSHOT_7", "GS", 97);
            WireVitrineCard("Card_MiAmigo_01", "MIAMIGO_01", "MA", 45);
            WireVitrineCard("Card_ElChampion", "ELCHAMPION", "EC", 78);

            // Bottom Navigation
            root.Q<Button>("Nav_Inicio")?.RegisterCallback<ClickEvent>(e => SceneManager.LoadScene("HomeScreenScene"));
            root.Q<Button>("Nav_Cartas")?.RegisterCallback<ClickEvent>(e => SceneManager.LoadScene("MyCardsScene"));
            root.Q<Button>("Nav_Tienda")?.RegisterCallback<ClickEvent>(e => SceneManager.LoadScene("StoreScene"));
            root.Q<Button>("Nav_Comunidad")?.RegisterCallback<ClickEvent>(e => SceneManager.LoadScene("CommunityScene"));
            root.Q<Button>("Nav_Perfil")?.RegisterCallback<ClickEvent>(e => SceneManager.LoadScene("ProfileScene"));
        }

        private void WireVitrineCard(string buttonName, string userName, string avatar, int likes)
        {
            Button cardBtn = root.Q<Button>(buttonName);
            if (cardBtn != null)
            {
                cardBtn.clicked += () => OpenDetailView(userName, avatar, likes);
            }
        }

        public void OpenDetailView(string userName, string avatar, int likes)
        {
            currentLikes = likes;
            isLiked = false;

            if (detailAvatarText != null) detailAvatarText.text = avatar;
            if (detailUserName != null) detailUserName.text = userName;
            if (detailLikeCount != null) detailLikeCount.text = currentLikes.ToString();

            if (detailView != null)
            {
                detailView.RemoveFromClassList("detail-view-hidden");
                ScrollView detailScroll = root.Q<ScrollView>("DetailScrollView");
                if (detailScroll != null) detailScroll.scrollOffset = Vector2.zero;
            }

            Debug.Log($"<color=gold>[UI Toolkit] Abriendo vitrina de {userName}</color>");
        }

        public void CloseDetailView()
        {
            if (detailView != null)
            {
                detailView.AddToClassList("detail-view-hidden");
            }
        }

        private void ToggleLike()
        {
            isLiked = !isLiked;
            currentLikes += isLiked ? 1 : -1;
            if (detailLikeCount != null)
            {
                detailLikeCount.text = currentLikes.ToString();
            }
        }

        private void FilterCards(string query)
        {
            string q = (query ?? "").Trim().ToLower();
            FilterGridChildren(popularGrid, q);
            FilterGridChildren(friendsGrid, q);
        }

        private void FilterGridChildren(VisualElement grid, string query)
        {
            if (grid == null) return;
            foreach (var child in grid.Children())
            {
                if (child is Button card)
                {
                    Label nameLbl = card.Q<Label>(className: "card-username");
                    if (nameLbl != null)
                    {
                        bool matches = string.IsNullOrEmpty(query) || nameLbl.text.ToLower().Contains(query);
                        card.style.display = matches ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
        }
 }
}
