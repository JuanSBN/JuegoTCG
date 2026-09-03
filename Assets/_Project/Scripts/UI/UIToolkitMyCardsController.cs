using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using JuegoTCG.Cards;

namespace JuegoTCG.UI
{
    public class UIToolkitMyCardsController : MonoBehaviour
    {
        [System.Serializable]
        public struct CardInfo
        {
            public int id;
            public string name;
            public string initials;
            public string rarity;
            public int count;
            public string position;
            public string team;

            public CardInfo(int id, string name, string initials, string rarity, int count, string position, string team)
            {
                this.id = id;
                this.name = name;
                this.initials = initials;
                this.rarity = rarity;
                this.count = count;
                this.position = position;
                this.team = team;
            }
        }

        private static readonly CardInfo[] DefaultCards = new CardInfo[]
        {
            new CardInfo(1, "Luis Díaz", "LD", "Mítica", 1, "Delantero", "Liverpool / Colombia"),
            new CardInfo(2, "Vinicius Jr.", "VJ", "Rara", 2, "Delantero", "Real Madrid / Brasil"),
            new CardInfo(3, "Haaland", "EH", "Común", 5, "Delantero", "Man City / Noruega"),
            new CardInfo(4, "Mbappé", "KM", "Poco común", 3, "Delantero", "Real Madrid / Francia"),
            new CardInfo(5, "Pedri", "PE", "Rara", 1, "Mediocampista", "Barcelona / España"),
            new CardInfo(6, "Rodri", "RO", "Común", 4, "Mediocampista", "Man City / España"),
            new CardInfo(7, "Lamine Yamal", "LY", "Mítica", 1, "Delantero", "Barcelona / España"),
            new CardInfo(8, "Bellingham", "JB", "Rara", 2, "Mediocampista", "Real Madrid / Inglaterra"),
            new CardInfo(9, "Salah", "MS", "Poco común", 6, "Delantero", "Liverpool / Egipto"),
            new CardInfo(10, "De Bruyne", "KDB", "Rara", 1, "Mediocampista", "Man City / Bélgica"),
            new CardInfo(11, "Musiala", "JM", "Común", 3, "Mediocampista", "Bayern / Alemania"),
            new CardInfo(12, "Osimhen", "VO", "Poco común", 2, "Delantero", "Galatasaray / Nigeria")
        };

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

        private string currentFilter = "Rareza";
        private string searchQuery = "";

        private void OnEnable()
        {
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
            WireCards();
            WireBottomNav();
            UpdateTotalCount();
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

            // Update active styling
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

            FilterCards();
        }

        private void WireSearch()
        {
            if (searchField == null) return;
            searchField.RegisterValueChangedCallback(evt =>
            {
                searchQuery = (evt.newValue ?? "").Trim().ToLower();
                FilterCards();
            });
        }

        private void WireCards()
        {
            for (int i = 0; i < DefaultCards.Length; i++)
            {
                int index = i;
                var card = DefaultCards[index];
                var cardBtn = root.Q<Button>($"Card_{card.id}");
                if (cardBtn != null)
                {
                    cardBtn.clicked += () => OpenInspectModal(card);
                }
            }
        }

        private void FilterCards()
        {
            int visibleCount = 0;
            for (int i = 0; i < DefaultCards.Length; i++)
            {
                var card = DefaultCards[i];
                var cardBtn = root.Q<Button>($"Card_{card.id}");
                if (cardBtn == null) continue;

                bool matchesSearch = string.IsNullOrEmpty(searchQuery) || card.name.ToLower().Contains(searchQuery);
                bool matchesFilter = true;

                if (currentFilter == "Album" && card.rarity == "Común") matchesFilter = true;

                if (matchesSearch && matchesFilter)
                {
                    cardBtn.style.display = DisplayStyle.Flex;
                    visibleCount++;
                }
                else
                {
                    cardBtn.style.display = DisplayStyle.None;
                }
            }

            if (cardsCountLabel != null)
            {
                cardsCountLabel.text = $"{visibleCount} de 1,232 cartas";
            }
        }

        public void OpenInspectModal(CardInfo card)
        {
            if (cardInspectModal == null) return;

            if (inspectPlayerName != null) inspectPlayerName.text = card.name;
            if (inspectAvatarInitials != null) inspectAvatarInitials.text = card.initials;
            if (inspectRarityBadge != null) inspectRarityBadge.text = card.rarity.ToUpper();
            if (inspectStatPosition != null) inspectStatPosition.text = card.position;
            if (inspectStatTeam != null) inspectStatTeam.text = card.team;
            if (inspectStatCopies != null) inspectStatCopies.text = $"×{card.count}";

            // Rarity classes on hero card
            if (inspectHeroCard != null)
            {
                inspectHeroCard.RemoveFromClassList("card-mythic");
                inspectHeroCard.RemoveFromClassList("card-rare");
                inspectHeroCard.RemoveFromClassList("card-uncommon");
                inspectHeroCard.RemoveFromClassList("card-common");

                switch (card.rarity)
                {
                    case "Mítica": inspectHeroCard.AddToClassList("card-mythic"); break;
                    case "Rara": inspectHeroCard.AddToClassList("card-rare"); break;
                    case "Poco común": inspectHeroCard.AddToClassList("card-uncommon"); break;
                    default: inspectHeroCard.AddToClassList("card-common"); break;
                }
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

        private void UpdateTotalCount()
        {
            if (cardsCountLabel != null)
            {
                cardsCountLabel.text = "1,232 cartas";
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
