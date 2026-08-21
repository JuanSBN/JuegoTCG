using System.Collections;
using System.Collections.Generic;
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

        [Header("Overlay & Effects")]
        [SerializeField] private Image flashOverlay;

        [Header("Card Containers")]
        [SerializeField] private Transform singleCardContainer;
        [SerializeField] private Transform summaryCardContainer;
        [SerializeField] private GameObject cardPrefab;

        [Header("Catalog Cards")]
        [SerializeField] private List<CardData> cardCatalog = new List<CardData>();

        [Header("Runtime State")]
        private bool isBusy = false;
        private int currentCardIndex = 0;
        private List<CardData> generatedCards = new List<CardData>();
        private GameObject activeRevealCardGO;
        private List<GameObject> summaryCardGOs = new List<GameObject>();

        private void Start()
        {
            ResetToClosedView();
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
            if (isBusy) return;
            StartCoroutine(SequenceOpenPack());
        }

        private IEnumerator SequenceOpenPack()
        {
            isBusy = true;

            // 1. Rip Flash Effect (Destello blanco)
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

            // Generate 5 Cards via Weighted RNG (GDD 5.2)
            generatedCards.Clear();
            for (int i = 0; i < 5; i++)
            {
                Rarity rarity = WeightedRNG.GetRandomRarity();
                CardData card = WeightedRNG.SelectRandomCardByRarity(rarity, cardCatalog);
                generatedCards.Add(card);
            }

            if (closedPackView != null) closedPackView.SetActive(false);
            if (revealView != null) revealView.SetActive(true);

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

            // 2. Start Revealing Cards One by One (Single Card Centered)
            currentCardIndex = 0;
            ShowNextCardToReveal();
            isBusy = false;
        }

        private void ShowNextCardToReveal()
        {
            ClearActiveCard();

            if (currentCardIndex >= generatedCards.Count)
            {
                ShowSummaryView();
                return;
            }

            // Instantiate single card in center
            activeRevealCardGO = Instantiate(cardPrefab, singleCardContainer);
            RectTransform rect = activeRevealCardGO.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity; // Recta sin inclinación
            rect.localScale = new Vector3(1.3f, 1.3f, 1.3f); // Tamaño centrado amplio

            CardDisplay display = activeRevealCardGO.GetComponent<CardDisplay>();
            if (display != null && generatedCards[currentCardIndex] != null)
            {
                display.SetCard(generatedCards[currentCardIndex]);
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

                rect.localRotation = Quaternion.identity; // Recta 100% sin inclinación
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

            // Display 5 Cards in a Straight Row (Rectas sin ladeado)
            int count = generatedCards.Count;
            float spacing = 190f; // Espaciado horizontal limpio
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                GameObject cardGO = Instantiate(cardPrefab, summaryCardContainer);
                summaryCardGOs.Add(cardGO);
                RectTransform rect = cardGO.GetComponent<RectTransform>();
                
                rect.anchoredPosition = new Vector2(startX + i * spacing, 0f);
                rect.localRotation = Quaternion.identity; // 100% RECTA, sin estar ladeada
                rect.localScale = new Vector3(0.52f, 0.52f, 0.52f); // Escala para ajustar perfectamente en fila

                CardDisplay display = cardGO.GetComponent<CardDisplay>();
                if (display != null && generatedCards[i] != null)
                {
                    display.SetCard(generatedCards[i]);
                }
            }
        }
    }
}
