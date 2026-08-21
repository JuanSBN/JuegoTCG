using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;

namespace JuegoTCG.Packs
{
    public class PackOpener : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject closedPackUI;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject cardPrefab;

        [Header("Catalog Cards")]
        [SerializeField] private List<CardData> cardCatalog = new List<CardData>();

        [Header("State")]
        private bool isOpening = false;
        private List<GameObject> revealedCardGOs = new List<GameObject>();
        private List<CardData> generatedCards = new List<CardData>();

        private void Start()
        {
            ResetPackView();
        }

        public void ResetPackView()
        {
            isOpening = false;

            if (closedPackUI != null) closedPackUI.SetActive(true);
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);
                Color c = flashOverlay.color;
                c.a = 0f;
                flashOverlay.color = c;
            }

            foreach (var go in revealedCardGOs)
            {
                if (go != null) Destroy(go);
            }
            revealedCardGOs.Clear();
            generatedCards.Clear();
        }

        public void OnClickOpenPack()
        {
            if (isOpening) return;
            StartCoroutine(SequenceOpenPack());
        }

        private IEnumerator SequenceOpenPack()
        {
            isOpening = true;

            // 1. Pack Idle -> Rip Flash Effect
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(true);
                float timer = 0f;
                while (timer < 0.25f)
                {
                    timer += Time.deltaTime;
                    Color c = flashOverlay.color;
                    c.a = Mathf.Lerp(0f, 0.95f, timer / 0.25f);
                    flashOverlay.color = c;
                    yield return null;
                }
            }

            if (closedPackUI != null) closedPackUI.SetActive(false);

            // Generate 5 Cards via Weighted RNG
            for (int i = 0; i < 5; i++)
            {
                Rarity rarity = WeightedRNG.GetRandomRarity();
                CardData card = WeightedRNG.SelectRandomCardByRarity(rarity, cardCatalog);
                generatedCards.Add(card);
            }

            // Fade Out Flash
            if (flashOverlay != null)
            {
                float timer = 0f;
                while (timer < 0.35f)
                {
                    timer += Time.deltaTime;
                    Color c = flashOverlay.color;
                    c.a = Mathf.Lerp(0.95f, 0f, timer / 0.35f);
                    flashOverlay.color = c;
                    yield return null;
                }
                flashOverlay.gameObject.SetActive(false);
            }

            // 2. Reveal 5 Cards One by One with 3D Flip
            for (int i = 0; i < 5; i++)
            {
                GameObject cardGO = Instantiate(cardPrefab, cardContainer);
                revealedCardGOs.Add(cardGO);
                RectTransform rect = cardGO.GetComponent<RectTransform>();
                rect.anchoredPosition = Vector2.zero;

                CardDisplay display = cardGO.GetComponent<CardDisplay>();
                if (display != null && generatedCards[i] != null)
                {
                    display.SetCard(generatedCards[i]);
                }

                // 3D Flip Animation (Rotate Y 0 -> 90 -> 0)
                yield return StartCoroutine(AnimateCardFlip(rect));
                yield return new WaitForSeconds(0.4f);
            }

            // 3. Layout Summary Fan with Intended Imperfection (TDD 2.1)
            yield return StartCoroutine(LayoutSummaryFan());
        }

        private IEnumerator AnimateCardFlip(RectTransform rect)
        {
            float duration = 0.4f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                float angle = Mathf.Lerp(0f, 360f, progress);
                rect.localRotation = Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            rect.localRotation = Quaternion.identity;
        }

        private IEnumerator LayoutSummaryFan()
        {
            int count = revealedCardGOs.Count;
            if (count == 0) yield break;

            float spacing = 80f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                RectTransform rect = revealedCardGOs[i].GetComponent<RectTransform>();
                Vector2 targetPos = new Vector2(startX + i * spacing, 0f);

                // Imperfección intencional de rotación (TDD 2.1: entre -6° y +6°)
                float randomAngle = Random.Range(-6f, 6f);
                Quaternion targetRot = Quaternion.Euler(0f, 0f, randomAngle);

                float timer = 0f;
                Vector2 startPos = rect.anchoredPosition;

                while (timer < 0.5f)
                {
                    timer += Time.deltaTime;
                    float t = timer / 0.5f;
                    rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                    rect.localRotation = Quaternion.Slerp(Quaternion.identity, targetRot, t);
                    yield return null;
                }

                rect.anchoredPosition = targetPos;
                rect.localRotation = targetRot;
            }
        }
    }
}
