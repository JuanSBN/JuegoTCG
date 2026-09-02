using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;
using JuegoTCG.UI;
using JuegoTCG.Networking;

namespace JuegoTCG.Packs
{
    public class PackOpener : MonoBehaviour
    {
        [Header("Screen Container (For Shake)")]
        [SerializeField] private RectTransform screenContainer;

        [Header("UI Views")]
        [SerializeField] private GameObject closedPackView;
        [SerializeField] private GameObject revealView;
        [SerializeField] private GameObject summaryView;

        [Header("Top Bar & Counters")]
        [SerializeField] private TMP_Text packCounterText;
        [SerializeField] private Toggle forceHoloToggle;
        private int remainingPacks = 5;

        [Header("Closed Pack Elements")]
        [SerializeField] private RectTransform packGraphicTransform;

        [Header("Progress Bar (5 Dots)")]
        [SerializeField] private Transform progressDotsContainer;
        [SerializeField] private Sprite dotSprite;
        private List<Image> progressDotImages = new List<Image>();

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
        private bool isCardFlipped = false;
        private List<CardData> generatedCards = new List<CardData>();
        private GameObject activeRevealCardGO;
        private List<GameObject> summaryCardGOs = new List<GameObject>();

        private void Start()
        {
            UpdatePackCountUI();
            ResetToClosedView();
        }

        private void UpdatePackCountUI()
        {
            if (packCounterText != null)
            {
                packCounterText.text = $"{remainingPacks} sobres";
            }
        }

        public void ResetToClosedView()
        {
            isBusy = false;
            currentCardIndex = 0;
            isCardFlipped = false;

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

            if (screenContainer != null)
            {
                screenContainer.anchoredPosition = Vector2.zero;
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

            // 1. Pack anticipation shake and scale pop
            if (packGraphicTransform != null)
            {
                Vector3 originalPos = packGraphicTransform.localPosition;
                float shakeTimer = 0f;
                float shakeDuration = 0.22f;

                while (shakeTimer < shakeDuration)
                {
                    shakeTimer += Time.deltaTime;
                    float progress = shakeTimer / shakeDuration;
                    float offsetX = Random.Range(-12f, 12f) * (1f + progress);
                    float offsetY = Random.Range(-8f, 8f) * (1f + progress);
                    packGraphicTransform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
                    packGraphicTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.15f, progress);
                    yield return null;
                }
                packGraphicTransform.localPosition = originalPos;
                packGraphicTransform.localScale = Vector3.one;
            }

            // 2. Bright White Rip Flash & Screen Shake
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(true);
                float timer = 0f;
                while (timer < 0.12f)
                {
                    timer += Time.deltaTime;
                    Color c = flashOverlay.color;
                    c.a = Mathf.Lerp(0f, 1.0f, timer / 0.12f);
                    flashOverlay.color = c;
                    yield return null;
                }
            }

            // Generate 5 Cards from Cloud Function openPack (TDD 2.6 y 6)
            generatedCards.Clear();
            bool forceHolo = (forceHoloToggle != null && forceHoloToggle.isOn);

            if (FirebaseCloudFunctionsClient.Instance == null)
            {
                GameObject fcGO = new GameObject("FirebaseCloudFunctionsClient");
                fcGO.AddComponent<FirebaseCloudFunctionsClient>();
            }

            var serverTask = FirebaseCloudFunctionsClient.Instance.CallOpenPackAsync("pack_oro");
            while (!serverTask.IsCompleted)
            {
                yield return null;
            }

            var serverResponse = serverTask.Result;

            if (serverResponse != null && serverResponse.cards != null && serverResponse.cards.Count > 0)
            {
                for (int i = 0; i < serverResponse.cards.Count; i++)
                {
                    var sCard = serverResponse.cards[i];
                    CardData matchingCard = cardCatalog.Find(c => c != null && (c.cardId == sCard.cardId || c.playerName.Contains(sCard.name)));

                    if (matchingCard == null)
                    {
                        Rarity mappedRarity = Rarity.Comun;
                        if (sCard.rarity == "poco_comun" || sCard.rarity == "especial") mappedRarity = Rarity.Especial;
                        else if (sCard.rarity == "rara" || sCard.rarity == "epica") mappedRarity = Rarity.Epica;
                        else if (sCard.rarity == "legendaria") mappedRarity = Rarity.Legendaria;
                        else if (sCard.rarity == "mitica") mappedRarity = Rarity.Mitica;
                        else if (sCard.rarity == "full_art") mappedRarity = Rarity.FullArt;

                        matchingCard = ScriptableObject.CreateInstance<CardData>();
                        matchingCard.cardId = sCard.cardId;
                        matchingCard.playerName = sCard.name;
                        matchingCard.teamName = sCard.team;
                        matchingCard.position = sCard.position;
                        matchingCard.rarity = mappedRarity;
                    }

                    if (i == 4 && forceHolo)
                    {
                        matchingCard.rarity = Rarity.Mitica;
                    }

                    generatedCards.Add(matchingCard);
                }
            }
            else
            {
                // Fallback de seguridad local
                for (int i = 0; i < 5; i++)
                {
                    Rarity r = (i == 4 && forceHolo) ? Rarity.Mitica : WeightedRNG.GetRandomRarity();
                    CardData c = WeightedRNG.SelectRandomCardByRarity(r, cardCatalog);
                    if (c != null) generatedCards.Add(c);
                }
            }

            // Guardar cartas en el inventario real del álbum (Fase 6 y 7)
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.AddCards(generatedCards);
            }

            if (closedPackView != null) closedPackView.SetActive(false);
            if (revealView != null) revealView.SetActive(true);

            InitProgressDots();

            // 3. Fade Out Flash smoothly
            if (flashOverlay != null)
            {
                float timer = 0f;
                while (timer < 0.35f)
                {
                    timer += Time.deltaTime;
                    Color c = flashOverlay.color;
                    c.a = Mathf.Lerp(1.0f, 0f, timer / 0.35f);
                    flashOverlay.color = c;
                    yield return null;
                }
                flashOverlay.gameObject.SetActive(false);
            }

            // 4. Show First Card face-down
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
                dotRect.sizeDelta = new Vector2(18, 18);
                Image dotImg = dotGO.AddComponent<Image>();
                if (dotSprite != null) dotImg.sprite = dotSprite;
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
                    progressDotImages[i].color = Color.white; // Current white enlarged
                    progressDotImages[i].transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
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
            isCardFlipped = false;

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
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(1.25f, 1.25f, 1.25f);

            CardDisplay display = activeRevealCardGO.GetComponent<CardDisplay>();
            CardData currentCard = generatedCards[currentCardIndex];
            if (display != null && currentCard != null)
            {
                display.SetCard(currentCard);
                // Start Face-Down (Showing Back)
                display.ShowBack(true);
            }

            // Disable tilt during face-down
            var tilt = activeRevealCardGO.GetComponent<HolographicTilt>();
            if (tilt != null) tilt.enabled = false;

            // Hints
            if (continueHintText != null) continueHintText.text = "Toca la carta para revelar";
            if (tiltHintText != null) tiltHintText.gameObject.SetActive(false);
        }

        public void OnClickCardInReveal()
        {
            if (isBusy) return;

            if (!isCardFlipped)
            {
                StartCoroutine(SequenceFlipCard());
            }
            else
            {
                StartCoroutine(SequenceDismissAndNext());
            }
        }

        private IEnumerator SequenceFlipCard()
        {
            isBusy = true;

            if (activeRevealCardGO != null)
            {
                RectTransform rect = activeRevealCardGO.GetComponent<RectTransform>();
                CardDisplay display = activeRevealCardGO.GetComponent<CardDisplay>();
                CardData currentCard = generatedCards[currentCardIndex];

                // 3D Flip Step 1: Rotate Y 0 -> 90 degrees
                float durationHalf = 0.18f;
                float timer = 0f;
                while (timer < durationHalf)
                {
                    timer += Time.deltaTime;
                    float progress = timer / durationHalf;
                    float angle = Mathf.Lerp(0f, 90f, progress);
                    rect.localRotation = Quaternion.Euler(0f, angle, 0f);
                    yield return null;
                }

                // Switch to Front Face at 90 degrees
                if (display != null)
                {
                    display.ShowBack(false);
                }

                // 3D Flip Step 2: Rotate Y 90 -> 0 degrees
                timer = 0f;
                while (timer < durationHalf)
                {
                    timer += Time.deltaTime;
                    float progress = timer / durationHalf;
                    float angle = Mathf.Lerp(90f, 0f, progress);
                    rect.localRotation = Quaternion.Euler(0f, angle, 0f);
                    yield return null;
                }
                rect.localRotation = Quaternion.identity;

                isCardFlipped = true;

                // Check for Rare / Holographic effects
                bool isHolo = (currentCard != null && (currentCard.rarity == Rarity.Epica || currentCard.rarity == Rarity.Legendaria || currentCard.rarity == Rarity.Mitica || currentCard.rarity == Rarity.FullArt));

                if (isHolo)
                {
                    // Enable Holographic Tilt component
                    var tilt = activeRevealCardGO.GetComponent<HolographicTilt>();
                    if (tilt != null) tilt.enabled = true;

                    // Trigger Screen Shake
                    StartCoroutine(DoScreenShake(0.35f, 10f));

                    // Trigger UI Particle Burst
                    Color burstColor = GetRarityBurstColor(currentCard.rarity);
                    if (UIParticleBurst.Instance != null)
                    {
                        UIParticleBurst.Instance.PlayBurst(Vector2.zero, 32, burstColor);
                    }
                }

                if (continueHintText != null)
                {
                    continueHintText.text = "Toca para continuar";
                }
            }

            isBusy = false;
        }

        private Color GetRarityBurstColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Mitica:
                case Rarity.FullArt:
                    return new Color(0.96f, 0.65f, 0.14f); // Golden Amber
                case Rarity.Legendaria:
                    return new Color(0.55f, 0.36f, 0.96f); // Royal Purple
                case Rarity.Epica:
                    return new Color(0.24f, 0.56f, 0.87f); // Deep Azure
                default:
                    return Color.white;
            }
        }

        private IEnumerator SequenceDismissAndNext()
        {
            isBusy = true;

            // Slide up and fade active card
            if (activeRevealCardGO != null)
            {
                RectTransform rect = activeRevealCardGO.GetComponent<RectTransform>();
                Vector2 startPos = rect.anchoredPosition;
                Vector2 targetPos = startPos + new Vector2(0f, 140f);
                float timer = 0f;
                float duration = 0.18f;

                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    float progress = timer / duration;
                    rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);
                    rect.localScale = Vector3.Lerp(new Vector3(1.25f, 1.25f, 1.25f), Vector3.zero, progress);
                    yield return null;
                }
            }

            currentCardIndex++;
            ShowNextCardToReveal();
            isBusy = false;
        }

        private IEnumerator DoScreenShake(float duration, float magnitude)
        {
            if (screenContainer == null) yield break;

            Vector2 originalPos = Vector2.zero;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                float currentMag = Mathf.Lerp(magnitude, 0f, progress);

                float offsetX = Random.Range(-currentMag, currentMag);
                float offsetY = Random.Range(-currentMag, currentMag);
                screenContainer.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

                yield return null;
            }

            screenContainer.anchoredPosition = originalPos;
        }

        private void ShowSummaryView()
        {
            if (revealView != null) revealView.SetActive(false);
            if (summaryView != null) summaryView.SetActive(true);

            ClearSummaryCards();

            // Calculate Best Rarity for Subtitle
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

            // Display 5 Cards in a Mobile Portrait 2-Row Layout (3 Top, 2 Bottom Centered)
            int count = generatedCards.Count;
            float scale = 0.65f; // Perfect readable size for 1080x1920 portrait mobile screen

            // Top Row (Cards 0, 1, 2)
            float topY = 180f;
            float topSpacingX = 265f;
            float topStartX = -topSpacingX;

            // Bottom Row (Cards 3, 4)
            float bottomY = -200f;
            float bottomSpacingX = 280f;
            float bottomStartX = -bottomSpacingX * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject cardGO = Instantiate(cardPrefab, summaryCardContainer);
                summaryCardGOs.Add(cardGO);
                RectTransform rect = cardGO.GetComponent<RectTransform>();

                Vector2 pos;
                if (i < 3)
                {
                    // Top row: 3 cards centered
                    pos = new Vector2(topStartX + i * topSpacingX, topY);
                }
                else
                {
                    // Bottom row: 2 cards centered
                    pos = new Vector2(bottomStartX + (i - 3) * bottomSpacingX, bottomY);
                }

                rect.anchoredPosition = pos;
                rect.localRotation = Quaternion.identity;
                rect.localScale = new Vector3(scale, scale, scale);

                CardDisplay display = cardGO.GetComponent<CardDisplay>();
                if (display != null && generatedCards[i] != null)
                {
                    display.SetCard(generatedCards[i]);
                    display.ShowBack(false);
                }
            }
        }
    }
}
