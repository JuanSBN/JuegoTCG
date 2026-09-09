using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using JuegoTCG.Networking;
using JuegoTCG.Social;
using JuegoTCG.Cards;

namespace JuegoTCG.UI
{
    /// <summary>
    /// Controlador moderno UI Toolkit para la Pantalla de Vitrinas Públicas (Showcase).
    /// Permite a los jugadores ver vitrinas populares y de amigos, abrir el detalle cinemático,
    /// dar Me Gusta en la nube y configurar/editar su propia vitrina con cartas de su colección.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitVitrinesController : MonoBehaviour
    {
        private UIDocument uiDoc;
        private VisualElement root;

        // Views
        private VisualElement catalogView;
        private VisualElement detailView;

        // Catalog elements
        private Button backBtn;
        private TextField searchInput;
        private VisualElement popularGrid;
        private VisualElement friendsGrid;

        // My Vitrine Banner
        private VisualElement myVitrineSection;
        private Label myVitrineAvatar;
        private Label myVitrineLikesLabel;
        private Button btnOpenEditVitrine;
        private VisualElement myVitrineCardsRow;

        // Detail elements
        private Label detailAvatarText;
        private Label detailUserName;
        private Label detailCardCount;
        private Button detailCloseBtn;
        private Button floatingLikeBtn;
        private Label detailLikeCount;
        private VisualElement detailCardsGrid;

        // Edit Vitrine Modal
        private VisualElement modalEditVitrine;
        private Label editVitrineSelectedCount;
        private VisualElement editCardsGrid;
        private Button btnCancelEditVitrine;
        private Button btnSaveVitrine;

        private VitrineCloudData activeDetailVitrine;
        private readonly List<string> selectedVitrineCardIds = new List<string>();
        private string activeFilterQuery = "";

        private void OnEnable()
        {
            uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            if (root == null) return;

            BindElements();
            RegisterCallbacks();

            VitrineService.EnsureExists();
            PlayerCollectionManager.EnsureExists();
            SocialService.EnsureExists();

            if (SocialService.Instance != null)
            {
                _ = SocialService.Instance.RefreshCloudRequestsAndFriendsAsync();
            }

            if (VitrineService.Instance != null)
            {
                VitrineService.Instance.OnVitrinesUpdated -= RenderCatalog;
                VitrineService.Instance.OnVitrinesUpdated += RenderCatalog;
                VitrineService.Instance.OnMyVitrineUpdated -= RenderMyVitrine;
                VitrineService.Instance.OnMyVitrineUpdated += RenderMyVitrine;

                _ = VitrineService.Instance.RefreshVitrinesAsync();
            }

            RenderCatalog();
        }

        private void OnDisable()
        {
            if (VitrineService.Instance != null)
            {
                VitrineService.Instance.OnVitrinesUpdated -= RenderCatalog;
                VitrineService.Instance.OnMyVitrineUpdated -= RenderMyVitrine;
            }
        }

        private void BindElements()
        {
            catalogView = root.Q<VisualElement>("CatalogView");
            detailView = root.Q<VisualElement>("DetailView");

            backBtn = root.Q<Button>("BackBtn");
            searchInput = root.Q<TextField>("SearchInput");
            popularGrid = root.Q<VisualElement>("PopularGrid");
            friendsGrid = root.Q<VisualElement>("FriendsGrid");

            // My Vitrine
            myVitrineSection = root.Q<VisualElement>("MyVitrineSection");
            myVitrineAvatar = root.Q<Label>("MyVitrineAvatar");
            myVitrineLikesLabel = root.Q<Label>("MyVitrineLikesLabel");
            btnOpenEditVitrine = root.Q<Button>("Btn_OpenEditVitrine");
            myVitrineCardsRow = root.Q<VisualElement>("MyVitrineCardsRow");

            // Detail
            detailAvatarText = root.Q<Label>("DetailAvatarText");
            detailUserName = root.Q<Label>("DetailUserName");
            detailCardCount = root.Q<Label>("DetailCardCount");
            detailCloseBtn = root.Q<Button>("DetailCloseBtn");
            floatingLikeBtn = root.Q<Button>("FloatingLikeBtn");
            detailLikeCount = root.Q<Label>("DetailLikeCount");
            detailCardsGrid = root.Q<VisualElement>("DetailCardsGrid");

            // Edit Modal
            modalEditVitrine = root.Q<VisualElement>("ModalEditVitrine");
            editVitrineSelectedCount = root.Q<Label>("EditVitrineSelectedCount");
            editCardsGrid = root.Q<VisualElement>("EditCardsGrid");
            btnCancelEditVitrine = root.Q<Button>("Btn_CancelEditVitrine");
            btnSaveVitrine = root.Q<Button>("Btn_SaveVitrine");
        }

        private void RegisterCallbacks()
        {
            if (backBtn != null)
            {
                backBtn.clicked += () => SceneManager.LoadScene("CommunitySceneUIToolkit");
            }

            if (btnOpenEditVitrine != null)
            {
                btnOpenEditVitrine.clicked += OpenEditModal;
            }

            if (btnCancelEditVitrine != null)
            {
                btnCancelEditVitrine.clicked += CloseEditModal;
            }

            if (btnSaveVitrine != null)
            {
                btnSaveVitrine.clicked += OnClickSaveVitrine;
            }

            if (detailCloseBtn != null)
            {
                detailCloseBtn.clicked += CloseDetailView;
            }

            if (floatingLikeBtn != null)
            {
                floatingLikeBtn.clicked += OnClickToggleLike;
            }

            if (searchInput != null)
            {
                searchInput.RegisterValueChangedCallback(evt =>
                {
                    activeFilterQuery = (evt.newValue ?? "").Trim().ToLower();
                    RenderCatalog();
                });
            }

            // Bottom Navigation
            var navBarController = gameObject.GetComponent<LiquidGlassNavBarController>() ?? gameObject.AddComponent<LiquidGlassNavBarController>();
            navBarController.Initialize(root, LiquidGlassNavBarController.TabType.Comunidad);
        }

        private void RenderMyVitrine(VitrineCloudData data)
        {
            if (data == null || myVitrineSection == null) return;

            if (myVitrineAvatar != null) myVitrineAvatar.text = data.avatarText;
            if (myVitrineLikesLabel != null) myVitrineLikesLabel.text = $"{data.likesCount} Me Gusta";

            if (myVitrineCardsRow != null)
            {
                myVitrineCardsRow.Clear();
                int count = data.cardIds != null ? data.cardIds.Count : 0;
                for (int i = 0; i < count && i < 6; i++)
                {
                    var preview = new VisualElement();
                    preview.AddToClassList("mini-card-preview");
                    preview.AddToClassList(GetMiniCardRarityClass(data.cardIds[i]));
                    preview.style.width = Length.Percent(15);
                    preview.style.height = 110;
                    myVitrineCardsRow.Add(preview);
                }
            }
        }

        private void RenderCatalog()
        {
            if (VitrineService.Instance == null) return;

            // 1. Render My Vitrine
            if (VitrineService.Instance.MyVitrine != null)
            {
                RenderMyVitrine(VitrineService.Instance.MyVitrine);
            }

            // 2. Render Popular
            if (popularGrid != null)
            {
                popularGrid.Clear();
                var list = VitrineService.Instance.PopularVitrines;
                int count = 0;
                foreach (var vitrine in list)
                {
                    if (MatchesFilter(vitrine))
                    {
                        var card = CreateVitrineCardElement(vitrine);
                        popularGrid.Add(card);
                        count++;
                    }
                }

                if (count == 0)
                {
                    var empty = new Label(string.IsNullOrEmpty(activeFilterQuery)
                        ? "Aún no hay vitrinas públicas. ¡Sé el primero en configurar la tuya arriba!"
                        : "No se encontraron vitrinas que coincidan con la búsqueda.");
                    empty.style.color = new Color(1f, 1f, 1f, 0.4f);
                    empty.style.fontSize = 24;
                    empty.style.marginTop = 16;
                    popularGrid.Add(empty);
                }
            }

            // 3. Render Friends
            if (friendsGrid != null)
            {
                friendsGrid.Clear();
                var list = VitrineService.Instance.FriendVitrines;
                int count = 0;
                foreach (var vitrine in list)
                {
                    if (MatchesFilter(vitrine))
                    {
                        var card = CreateVitrineCardElement(vitrine);
                        friendsGrid.Add(card);
                        count++;
                    }
                }

                if (count == 0)
                {
                    var empty = new Label(string.IsNullOrEmpty(activeFilterQuery)
                        ? "Tus amigos aún no han publicado su vitrina. ¡Invítalos a compartirla!"
                        : "No se encontraron amigos con ese nombre.");
                    empty.style.color = new Color(1f, 1f, 1f, 0.4f);
                    empty.style.fontSize = 24;
                    empty.style.marginTop = 16;
                    friendsGrid.Add(empty);
                }
            }
        }

        private Button CreateVitrineCardElement(VitrineCloudData vitrine)
        {
            var btn = new Button();
            btn.AddToClassList("vitrine-card");

            // Top: Avatar + Name
            var top = new VisualElement();
            top.AddToClassList("vitrine-card-top");

            var circle = new VisualElement();
            circle.AddToClassList("card-avatar-circle");
            var avText = new Label(vitrine.avatarText);
            avText.AddToClassList("card-avatar-text");
            circle.Add(avText);
            top.Add(circle);

            var nameLbl = new Label(vitrine.displayName);
            nameLbl.AddToClassList("card-username");
            top.Add(nameLbl);

            btn.Add(top);

            // Middle: Mini cards
            var row = new VisualElement();
            row.AddToClassList("mini-cards-row");

            int numCards = vitrine.cardIds != null ? vitrine.cardIds.Count : 0;
            for (int i = 0; i < 3; i++)
            {
                var mini = new VisualElement();
                mini.AddToClassList("mini-card-preview");
                if (i < numCards)
                {
                    mini.AddToClassList(GetMiniCardRarityClass(vitrine.cardIds[i]));
                }
                else
                {
                    mini.AddToClassList("mini-card-common");
                    mini.style.opacity = 0.3f;
                }
                row.Add(mini);
            }
            btn.Add(row);

            // Bottom: Likes
            var bottom = new VisualElement();
            bottom.AddToClassList("vitrine-card-bottom");

            var likeIcon = new VisualElement();
            likeIcon.AddToClassList("catalog-like-icon");
            bottom.Add(likeIcon);

            var likeText = new Label(vitrine.likesCount.ToString());
            likeText.AddToClassList("catalog-like-text");
            bottom.Add(likeText);

            btn.Add(bottom);

            var vCopy = vitrine;
            btn.clicked += () => OpenDetailView(vCopy);

            return btn;
        }

        private bool MatchesFilter(VitrineCloudData vitrine)
        {
            if (string.IsNullOrEmpty(activeFilterQuery)) return true;
            if (vitrine == null) return false;
            if (!string.IsNullOrEmpty(vitrine.displayName) && vitrine.displayName.ToLower().Contains(activeFilterQuery)) return true;
            if (!string.IsNullOrEmpty(vitrine.friendCode) && vitrine.friendCode.ToLower().Contains(activeFilterQuery)) return true;
            return false;
        }

        public void OpenDetailView(VitrineCloudData vitrine)
        {
            if (vitrine == null) return;
            activeDetailVitrine = vitrine;

            if (detailAvatarText != null) detailAvatarText.text = vitrine.avatarText;
            if (detailUserName != null) detailUserName.text = vitrine.displayName.ToUpper();
            int numCards = vitrine.cardIds != null ? vitrine.cardIds.Count : 0;
            if (detailCardCount != null) detailCardCount.text = $"Vitrina pública · {numCards} cartas";
            if (detailLikeCount != null) detailLikeCount.text = vitrine.likesCount.ToString();

            UpdateLikeButtonVisual();

            // Populate showcase cards
            if (detailCardsGrid != null)
            {
                detailCardsGrid.Clear();
                if (vitrine.cardIds != null && PlayerCollectionManager.Instance != null)
                {
                    var catalog = PlayerCollectionManager.Instance.GetCatalog();
                    for (int i = 0; i < vitrine.cardIds.Count; i++)
                    {
                        string cid = vitrine.cardIds[i];
                        var catItem = catalog.Find(c => c.cardId == cid);
                        if (catItem != null)
                        {
                            var cardEl = CreateShowcaseCardElement(catItem);
                            detailCardsGrid.Add(cardEl);
                        }
                    }
                }
            }

            if (detailView != null)
            {
                detailView.RemoveFromClassList("detail-view-hidden");
                var scroll = root.Q<ScrollView>("DetailScrollView");
                if (scroll != null) scroll.scrollOffset = Vector2.zero;
            }

            Debug.Log($"<color=gold>[Vitrinas] Abriendo vitrina pública de {vitrine.displayName}</color>");
        }

        private VisualElement CreateShowcaseCardElement(CardCatalogItem item)
        {
            var card = new VisualElement();
            card.AddToClassList("showcase-card");
            card.AddToClassList(GetMiniCardRarityClass(item.cardId));

            if (item.rarity == Rarity.Mitica || item.rarity == Rarity.Legendaria)
            {
                var star = new VisualElement();
                star.AddToClassList("showcase-card-star");
                card.Add(star);
            }

            var initials = new Label(item.initials);
            initials.AddToClassList("showcase-initials");
            card.Add(initials);

            var rarity = new Label(item.rarity.ToString().ToUpper());
            rarity.AddToClassList("showcase-rarity");
            rarity.style.color = GetRarityColor(item.rarity);
            card.Add(rarity);

            var name = new Label(item.playerName);
            name.AddToClassList("showcase-name");
            card.Add(name);

            return card;
        }

        public void CloseDetailView()
        {
            if (detailView != null)
            {
                detailView.AddToClassList("detail-view-hidden");
            }
            activeDetailVitrine = null;
        }

        private async void OnClickToggleLike()
        {
            if (activeDetailVitrine == null || VitrineService.Instance == null) return;
            string targetUid = activeDetailVitrine.userId;

            await VitrineService.Instance.ToggleLikeAsync(targetUid);

            if (detailLikeCount != null)
            {
                detailLikeCount.text = activeDetailVitrine.likesCount.ToString();
            }
            UpdateLikeButtonVisual();
        }

        private void UpdateLikeButtonVisual()
        {
            if (floatingLikeBtn == null || activeDetailVitrine == null || FirebaseAuthManager.Instance == null) return;
            string myUid = FirebaseAuthManager.Instance.UserId;
            bool isLiked = activeDetailVitrine.IsLikedBy(myUid);

            var icon = floatingLikeBtn.Q<VisualElement>(className: "floating-like-icon");
            if (isLiked)
            {
                floatingLikeBtn.style.backgroundColor = new Color(0.910f, 0.659f, 0.125f);
                if (icon != null) icon.style.unityBackgroundImageTintColor = Color.white;
                if (detailLikeCount != null) detailLikeCount.style.color = Color.white;
            }
            else
            {
                floatingLikeBtn.style.backgroundColor = new Color(0.08f, 0.16f, 0.11f, 0.95f);
                if (icon != null) icon.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, 0.6f);
                if (detailLikeCount != null) detailLikeCount.style.color = Color.white;
            }
        }

        // =========================================================================
        // EDIT VITRINE MODAL LOGIC
        // =========================================================================

        private void OpenEditModal()
        {
            selectedVitrineCardIds.Clear();
            if (VitrineService.Instance != null && VitrineService.Instance.MyVitrine != null && VitrineService.Instance.MyVitrine.cardIds != null)
            {
                selectedVitrineCardIds.AddRange(VitrineService.Instance.MyVitrine.cardIds);
            }

            UpdateEditCountDisplay();
            PopulateEditCardsGrid();

            if (modalEditVitrine != null)
            {
                modalEditVitrine.RemoveFromClassList("modal-hidden");
            }
        }

        private void CloseEditModal()
        {
            if (modalEditVitrine != null)
            {
                modalEditVitrine.AddToClassList("modal-hidden");
            }
        }

        private void PopulateEditCardsGrid()
        {
            if (editCardsGrid == null || PlayerCollectionManager.Instance == null) return;
            editCardsGrid.Clear();

            var catalog = PlayerCollectionManager.Instance.GetCatalog();
            int ownedAvailable = 0;

            foreach (var card in catalog)
            {
                bool isOwned = PlayerCollectionManager.Instance.IsCardOwned(card.cardId);
                if (!isOwned) continue;

                ownedAvailable++;
                bool isSelected = selectedVitrineCardIds.Contains(card.cardId);

                var itemBtn = new Button();
                itemBtn.AddToClassList("pick-card-item");
                if (isSelected) itemBtn.AddToClassList("pick-card-selected");

                var circle = new VisualElement();
                circle.AddToClassList("card-avatar-circle");
                circle.style.width = 60;
                circle.style.height = 60;
                circle.style.borderTopLeftRadius = 30;
                circle.style.borderTopRightRadius = 30;
                circle.style.borderBottomLeftRadius = 30;
                circle.style.borderBottomRightRadius = 30;
                var inLbl = new Label(card.initials);
                inLbl.AddToClassList("card-avatar-text");
                inLbl.style.fontSize = 22;
                circle.Add(inLbl);
                itemBtn.Add(circle);

                var nameLbl = new Label(card.playerName);
                nameLbl.AddToClassList("card-player-name");
                nameLbl.style.fontSize = 20;
                nameLbl.style.marginTop = 6;
                itemBtn.Add(nameLbl);

                var rarityLbl = new Label(card.rarity.ToString().ToUpper());
                rarityLbl.style.fontSize = 16;
                rarityLbl.style.color = GetRarityColor(card.rarity);
                itemBtn.Add(rarityLbl);

                if (isSelected)
                {
                    var badge = new Label((selectedVitrineCardIds.IndexOf(card.cardId) + 1).ToString());
                    badge.AddToClassList("pick-card-badge");
                    itemBtn.Add(badge);
                }

                string cid = card.cardId;
                itemBtn.clicked += () =>
                {
                    if (selectedVitrineCardIds.Contains(cid))
                    {
                        selectedVitrineCardIds.Remove(cid);
                    }
                    else
                    {
                        if (selectedVitrineCardIds.Count < 6)
                        {
                            selectedVitrineCardIds.Add(cid);
                        }
                    }
                    UpdateEditCountDisplay();
                    PopulateEditCardsGrid();
                };

                editCardsGrid.Add(itemBtn);
            }

            if (ownedAvailable == 0)
            {
                var empty = new Label("No tienes cartas en tu colección todavía. ¡Abre sobres en la Tienda para conseguirlas!");
                empty.style.color = new Color(1f, 1f, 1f, 0.6f);
                empty.style.fontSize = 22;
                empty.style.whiteSpace = WhiteSpace.Normal;
                editCardsGrid.Add(empty);
            }
        }

        private void UpdateEditCountDisplay()
        {
            if (editVitrineSelectedCount != null)
            {
                editVitrineSelectedCount.text = $"{selectedVitrineCardIds.Count} / 6 seleccionadas";
            }
        }

        private async void OnClickSaveVitrine()
        {
            if (VitrineService.Instance == null) return;
            if (btnSaveVitrine != null) btnSaveVitrine.SetEnabled(false);

            bool ok = await VitrineService.Instance.SaveMyVitrineAsync(selectedVitrineCardIds);
            if (btnSaveVitrine != null) btnSaveVitrine.SetEnabled(true);

            if (ok)
            {
                CloseEditModal();
                RenderCatalog();
            }
        }

        // =========================================================================
        // HELPERS
        // =========================================================================

        private string GetMiniCardRarityClass(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || PlayerCollectionManager.Instance == null) return "mini-card-common";
            var item = PlayerCollectionManager.Instance.GetCatalog().Find(c => c.cardId == cardId);
            if (item == null) return "mini-card-common";

            switch (item.rarity)
            {
                case Rarity.Mitica: return "mini-card-mythic";
                case Rarity.Legendaria:
                case Rarity.Epica:
                case Rarity.Especial:
                    return "mini-card-rare";
                case Rarity.Comun:
                default:
                    return "mini-card-common";
            }
        }

        private Color GetRarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica: return new Color(0.910f, 0.659f, 0.125f);
                case Rarity.Legendaria: return new Color(0.85f, 0.45f, 0.15f);
                case Rarity.Epica: return new Color(0.678f, 0.369f, 0.941f);
                case Rarity.Especial: return new Color(0.188f, 0.820f, 0.345f);
                case Rarity.Comun:
                default:
                    return new Color(0.7f, 0.7f, 0.7f);
            }
        }
    }
}
