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
        private VisualElement inspectAvatarCircle;
        private Label inspectAvatarInitials;
        private Label inspectPlayerName;
        private Label inspectRarityBadge;
        private Label inspectStatPosition;
        private Label inspectStatTeam;
        private Label inspectStatCopies;
        private Button inspectCloseBtn;

        private string currentFilter = "Album";
        private string searchQuery = "";
        private List<CardData> loadedCardAssets = new List<CardData>();

        private void OnEnable()
        {
            PlayerCollectionManager.EnsureExists();
            LoadCardAssets();

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null || uiDoc.rootVisualElement == null) return;
            root = uiDoc.rootVisualElement;

            cardsGrid = root.Q<VisualElement>("CardsGrid");
            cardInspectModal = root.Q<VisualElement>("CardInspectModal");
            cardsCountLabel = root.Q<Label>("CardsCountLabel");
            searchField = root.Q<TextField>("SearchField");

            inspectHeroCard = root.Q<VisualElement>("InspectHeroCard");
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

        private void OnDisable()
        {
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.OnCollectionUpdated -= PopulateAlbumGrid;
            }
        }

        private void LoadCardAssets()
        {
            loadedCardAssets.Clear();
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData", new[] { "Assets/_Project/ScriptableObjects/PilotAlbum" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null) loadedCardAssets.Add(card);
            }
#endif
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

                if (isOwned)
                {
                    string rarityClass = GetRarityClass(item.rarity);
                    cardBtn.AddToClassList(rarityClass);

                    // Avatar Circle
                    VisualElement avatarCircle = new VisualElement();
                    avatarCircle.AddToClassList("card-avatar-circle");

                    if (asset != null && asset.defaultArt != null)
                    {
                        avatarCircle.style.backgroundImage = new StyleBackground(asset.defaultArt);
                    }
                    else
                    {
                        Label initials = new Label(item.initials);
                        initials.AddToClassList("card-avatar-initials");
                        initials.AddToClassList(GetInitialsClass(item.rarity));
                        avatarCircle.Add(initials);
                    }
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

            if (inspectAvatarCircle != null)
            {
                if (asset != null && asset.defaultArt != null)
                {
                    inspectAvatarCircle.style.backgroundImage = new StyleBackground(asset.defaultArt);
                    if (inspectAvatarInitials != null) inspectAvatarInitials.style.display = DisplayStyle.None;
                }
                else
                {
                    inspectAvatarCircle.style.backgroundImage = null;
                    if (inspectAvatarInitials != null) inspectAvatarInitials.style.display = DisplayStyle.Flex;
                }
            }

            if (inspectHeroCard != null)
            {
                inspectHeroCard.RemoveFromClassList("card-mythic");
                inspectHeroCard.RemoveFromClassList("card-rare");
                inspectHeroCard.RemoveFromClassList("card-uncommon");
                inspectHeroCard.RemoveFromClassList("card-common");
                inspectHeroCard.AddToClassList(GetRarityClass(item.rarity));
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
