using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;

namespace JuegoTCG.Packs
{
    public class PackOpener : MonoBehaviour
    {
        [Header("UI Views")]
        [SerializeField] private GameObject closedPackView;
        [SerializeField] private GameObject revealView;
        [SerializeField] private GameObject summaryView;

        [Header("Top Bar & Counters")]
        [SerializeField] private TMP_Text packCounterText;
        [SerializeField] private Toggle forceHoloToggle;
        private int remainingPacks = 5;

        [Header("Progress Bar (5 Dots)")]
        [SerializeField] private Transform progressDotsContainer;
        [SerializeField] private GameObject dotPrefab;

        [Header("Reveal View Elements")]
        [SerializeField] private Transform singleCardContainer;
        [SerializeField] private TMP_Text continueHintText;
        [SerializeField] private TMP_Text tiltHintText;
        [SerializeField] private GameObject cardPrefab;

        [Header("Summary View Elements")]
        [SerializeField] private TMP_Text summarySubtitleText;
        [SerializeField] private Transform summaryCardContainer;
        [SerializeField] private Button openAnotherButton;

        [Header("Overlay & Effects")]
        [SerializeField] private Image flashOverlay;

        [Header("Catalog Cards")]
        [SerializeField] private List<CardData> cardCatalog = new List<CardData>();

        [Header("Runtime State")]
        private bool isBusy = false;
        private int currentCardIndex = 0;
        private List<CardData> generatedCards = new List<CardData>();
        private GameObject activeRevealCardGO;
        private List<GameObject> summaryCardGOs = new List<GameObject>();
        private List<Image> progressDotImages = new List<Image>();

        private void Start()
        {
            UpdatePackCountUI();
            ResetToClosedView();
        }

        private void UpdatePackCountUI()
        {
            if (packCounterText != null)
            {
                packCounterText.text = $"★ {remainingPacks} sobres";
            }
        }

        public void ResetToClosedView()
        {
            isBusy = false;
            currentCardIndex = 0;

            if (closedPackView != null) closedPackView.SetActive(true);
            if (revealView != null) revealView.SetActive(false);
            if (summaryView != null) summaryView.SetActive(false);

            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);
                Color c = flashOverlay.color;
                c.a = 0f;
                flashOverlay.color = c;
            }

            ClearActiveCard();
            ClearSummaryCards();
        }

        private void ClearActiveCard()
        {
            if (activeRevealCardGO != null)
            {
                Destroy(activeRevealCardGO);
                activeRevealCardGO = null;
            }
        }

        private void ClearSummaryCards()
        {
            foreach (var go in summaryCardGOs)
            {
                if (go != null) Destroy(go);
            }
            summaryCardGOs.Clear();
        }

        public void OnClickOpenPack()
        {
            if (isBusy || remainingPacks <= 0) return;
            remainingPacks--;
            UpdatePackCountUI();
            StartCoroutine(SequenceOpenPack());
        }

        private IEnumerator SequenceOpenPack()
        {
            isBusy = true;

            // 1. Rip Flash Effect (Destello blanco rápido)
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(true);
                float timer = 0f;
                while (timer < 0.2f)
                {
                    timer += Time.deltaTime;
                    Color c = flashOverlay.color;
                    c.a = Mathf.Lerp(0f, 0.95f, timer / 0.2f);
                    flashOverlay.color = c;
                    yield return null;
                }
            }

            // Generate 5 Cards via Weighted RNG (con soporte para toggle de prueba)
            generatedCards.Clear();
            bool forceHolo = (forceHoloToggle != null && forceHoloToggle.isOn);

            for (int i = 0; i < 5; i++)
            {
                bool isLastSlot = (i == 4);
                Rarity rarity;
                if (isLastSlot && forceHolo)
                {
                    rarity = Rarity.Mitica;
                }
                else
                {
                    rarity = WeightedRNG.GetRandomRarity();
                }

                CardData card = WeightedRNG.SelectRandomCardByRarity(rarity, cardCatalog);
                generatedCards.Add(card);
            }

            if (closedPackView != null) closedPackView.SetActive(false);
            if (revealView != null) revealView.SetActive(true);

            // Initialize Progress Dots
            InitProgressDots();

            // Fade Out Flash
            if (flashOverlay != null)
            {
                float timer = 0f;
                while (timer < 0.3f)
                {
                    timer += Time.deltaTime;
                    Color c = flashOverlay.color;
                    c.a = Mathf.Lerp(0.95f, 0f, timer / 0.3f);
                    flashOverlay.color = c;
                    yield return null;
                }
                flashOverlay.gameObject.SetActive(false);
            }

            // 2. Start Revealing Cards One by One
            currentCardIndex = 0;
            ShowNextCardToReveal();
            isBusy = false;
        }

        private void InitProgressDots()
        {
            progressDotImages.Clear();
            if (progressDotsContainer == null) return;

            foreach (Transform child in progressDotsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < 5; i++)
            {
                GameObject dotGO = new GameObject($"Dot_{i}");
                dotGO.transform.SetParent(progressDotsContainer, false);
                RectTransform dotRect = dotGO.AddComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(16, 16);
                Image dotImg = dotGO.AddComponent<Image>();
                dotImg.color = new Color(1f, 1f, 1f, 0.25f);
                progressDotImages.Add(dotImg);
            }
        }

        private void UpdateProgressDots()
        {
            for (int i = 0; i < progressDotImages.Count; i++)
            {
                if (progressDotImages[i] == null) continue;

                if (i < currentCardIndex)
                {
                    progressDotImages[i].color = new Color(0.96f, 0.65f, 0.14f, 1f); // Gold done
                    progressDotImages[i].transform.localScale = Vector3.one;
                }
                else if (i == currentCardIndex)
                {
                    progressDotImages[i].color = Color.white; // Current white
                    progressDotImages[i].transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                }
                else
                {
                    progressDotImages[i].color = new Color(1f, 1f, 1f, 0.25f);
                    progressDotImages[i].transform.localScale = Vector3.one;
                }
            }
        }

        private void ShowNextCardToReveal()
        {
            ClearActiveCard();

            if (currentCardIndex >= generatedCards.Count)
            {
                ShowSummaryView();
                return;
            }

            UpdateProgressDots();

            // Instantiate single card in center
            activeRevealCardGO = Instantiate(cardPrefab, singleCardContainer);
            RectTransform rect = activeRevealCardGO.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity; // 100% Recta
            rect.localScale = new Vector3(1.25f, 1.25f, 1.25f);

            CardDisplay display = activeRevealCardGO.GetComponent<CardDisplay>();
            CardData currentCard = generatedCards[currentCardIndex];
            if (display != null && currentCard != null)
            {
                display.SetCard(currentCard);
            }

            // Hints
            if (continueHintText != null) continueHintText.text = "Toca la pantalla para revelar la siguiente carta";
            if (tiltHintText != null)
            {
                bool isHolo = (currentCard != null && (currentCard.rarity == Rarity.Epica || currentCard.rarity == Rarity.Legendaria || currentCard.rarity == Rarity.Mitica || currentCard.rarity == Rarity.FullArt));
                tiltHintText.gameObject.SetActive(isHolo);
                if (isHolo) tiltHintText.text = "✦ Mueve el ratón / dedo sobre la carta para ver el efecto holográfico";
            }
        }

        public void OnClickCardInReveal()
        {
            if (isBusy) return;
            StartCoroutine(SequenceFlipAndNext());
        }

        private IEnumerator SequenceFlipAndNext()
        {
            isBusy = true;

            if (activeRevealCardGO != null)
            {
                RectTransform rect = activeRevealCardGO.GetComponent<RectTransform>();
                
                // 3D Flip animation (Rotate Y 0 -> 180 -> 0)
                float duration = 0.35f;
                float timer = 0f;

                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / duration;
                    float angle = Mathf.Lerp(0f, 180f, progress);
                    rect.localRotation = Quaternion.Euler(0f, angle, 0f);
                    yield return null;
                }

                rect.localRotation = Quaternion.identity;
            }

            yield return new WaitForSeconds(0.2f);
            currentCardIndex++;
            ShowNextCardToReveal();
            isBusy = false;
        }

        private void ShowSummaryView()
        {
            if (revealView != null) revealView.SetActive(false);
            if (summaryView != null) summaryView.SetActive(true);

            ClearSummaryCards();

            // Calculate Best Rarity for Subtitle (like in HTML prototype)
            Rarity bestRarity = Rarity.Comun;
            foreach (var c in generatedCards)
            {
                if (c != null && c.rarity > bestRarity) bestRarity = c.rarity;
            }

            if (summarySubtitleText != null)
            {
                if (bestRarity >= Rarity.Mitica)
                    summarySubtitleText.text = "¡Increíble, conseguiste una carta MÍTICA / FULL ART!";
                else if (bestRarity >= Rarity.Epica)
                    summarySubtitleText.text = "¡Muy buena tanda, salió una carta ÉPICA / LEGENDARIA!";
                else
                    summarySubtitleText.text = "Sigue abriendo sobres para completar tu álbum";
            }

            if (openAnotherButton != null)
            {
                openAnotherButton.interactable = (remainingPacks > 0);
            }

            // Display 5 Cards in a Straight Row (100% Rectas y ordenadas)
            int count = generatedCards.Count;
            float spacing = 190f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject cardGO = Instantiate(cardPrefab, summaryCardContainer);
                summaryCardGOs.Add(cardGO);
                RectTransform rect = cardGO.GetComponent<RectTransform>();
                
                rect.anchoredPosition = new Vector2(startX + i * spacing, 0f);
                rect.localRotation = Quaternion.identity; // Recta sin ladeado
                rect.localScale = new Vector3(0.52f, 0.52f, 0.52f);

                CardDisplay display = cardGO.GetComponent<CardDisplay>();
                if (display != null && generatedCards[i] != null)
                {
                    display.SetCard(generatedCards[i]);
                }
            }
        }
    }
}
