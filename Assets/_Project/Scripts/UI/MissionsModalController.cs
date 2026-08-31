using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    [Serializable]
    public class MissionItemData
    {
        public string id;
        public string title;
        public int current;
        public int total;

        public bool IsCompleted => current >= total;
        public float ProgressNormalized => total > 0 ? Mathf.Clamp01((float)current / total) : 0f;
    }

    public class MissionsModalController : MonoBehaviour
    {
        [Header("Modal Containers")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private CanvasGroup modalCanvasGroup;
        [SerializeField] private RectTransform modalBoxRect;
        [SerializeField] private Button backdropCloseButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RawImage blurBackdropImage;

        [Header("Header & Stats")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text completedCountText;
        [SerializeField] private TMP_Text resetTimerText;

        [Header("Milestone Progress Track (0-4)")]
        [SerializeField] private Slider milestoneSlider;
        [SerializeField] private Image milestone1Dot;
        [SerializeField] private Image milestone2Dot;
        [SerializeField] private Image milestone1GiftBox;
        [SerializeField] private Image milestone2GiftBox;

        [Header("Mission Rows")]
        [SerializeField] private List<MissionRowView> missionRows = new List<MissionRowView>();

        [Header("Colors & Sprites")]
        [SerializeField] private Color goldColor = new Color(0.910f, 0.659f, 0.125f);       // #e8a820
        [SerializeField] private Color normalBorderColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Sprite cardNormalSprite;
        [SerializeField] private Sprite cardDoneSprite;

        private List<MissionItemData> currentMissions = new List<MissionItemData>();
        private Texture2D capturedBlurTexture;

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (backdropCloseButton != null)
            {
                backdropCloseButton.onClick.RemoveAllListeners();
                backdropCloseButton.onClick.AddListener(Hide);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
        }

        public void InitializeDefaultMissions()
        {
            currentMissions = new List<MissionItemData>
            {
                new MissionItemData { id = "m1", title = "Abre 1 sobre", current = 0, total = 1 },
                new MissionItemData { id = "m2", title = "Consigue 1 carta rara o superior", current = 0, total = 1 },
                new MissionItemData { id = "m3", title = "Vende 3 cartas duplicadas", current = 1, total = 3 },
                new MissionItemData { id = "m4", title = "Intercambia 1 carta con un amigo", current = 0, total = 1 }
            };

            UpdateDisplay();
        }

        public void ShowWithBlur(Texture2D blurTex)
        {
            if (capturedBlurTexture != null && capturedBlurTexture != blurTex)
            {
                Destroy(capturedBlurTexture);
            }
            capturedBlurTexture = blurTex;

            if (blurBackdropImage != null)
            {
                if (blurTex != null)
                {
                    blurBackdropImage.texture = blurTex;
                    blurBackdropImage.color = Color.white;
                    blurBackdropImage.gameObject.SetActive(true);
                }
                else
                {
                    blurBackdropImage.gameObject.SetActive(false);
                }
            }

            DoShowModal();
        }

        public void Show()
        {
            ShowWithBlur(null);
        }

        private void DoShowModal()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
                modalRoot.transform.SetAsLastSibling();
            }

            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 1f;
                modalCanvasGroup.blocksRaycasts = true;
                modalCanvasGroup.interactable = true;
            }

            if (modalBoxRect != null)
            {
                modalBoxRect.localScale = Vector3.one;
            }

            if (currentMissions == null || currentMissions.Count == 0)
            {
                InitializeDefaultMissions();
            }
            else
            {
                UpdateDisplay();
            }

            Debug.Log("<color=green>[MissionsModal] ¡Sub-pantalla de Misiones mostrada en pantalla con fondo desenfocado!</color>");
        }

        public void Hide()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            else gameObject.SetActive(false);

            if (blurBackdropImage != null) blurBackdropImage.gameObject.SetActive(false);
            if (capturedBlurTexture != null)
            {
                Destroy(capturedBlurTexture);
                capturedBlurTexture = null;
            }
        }

        public void UpdateDisplay()
        {
            int completedCount = 0;
            for (int i = 0; i < currentMissions.Count; i++)
            {
                if (currentMissions[i].IsCompleted) completedCount++;
            }

            // Update stats
            if (completedCountText != null)
            {
                completedCountText.text = $"Completadas: <color=white><b>{completedCount}</b></color>";
            }

            if (resetTimerText != null)
            {
                resetTimerText.text = "Se reinicia en 05h 41min";
            }

            // Update Milestone Slider (Max 4 missions)
            if (milestoneSlider != null)
            {
                milestoneSlider.minValue = 0;
                milestoneSlider.maxValue = 4;
                milestoneSlider.value = completedCount;
            }

            // Milestone 1 (2 missions) Checkpoint
            bool m1Reached = completedCount >= 2;
            if (milestone1Dot != null) milestone1Dot.color = m1Reached ? goldColor : new Color(0.2f, 0.25f, 0.2f);
            if (milestone1GiftBox != null) milestone1GiftBox.color = m1Reached ? Color.white : new Color(1f, 1f, 1f, 0.7f);

            // Milestone 2 (4 missions) Checkpoint
            bool m2Reached = completedCount >= 4;
            if (milestone2Dot != null) milestone2Dot.color = m2Reached ? goldColor : new Color(0.2f, 0.25f, 0.2f);
            if (milestone2GiftBox != null) milestone2GiftBox.color = m2Reached ? Color.white : new Color(1f, 1f, 1f, 0.7f);

            // Bind Mission Rows
            for (int i = 0; i < missionRows.Count; i++)
            {
                if (i < currentMissions.Count)
                {
                    missionRows[i].gameObject.SetActive(true);
                    missionRows[i].Setup(currentMissions[i], goldColor, cardNormalSprite, cardDoneSprite);
                }
                else
                {
                    missionRows[i].gameObject.SetActive(false);
                }
            }
        }

        private void LeanTweenScale(RectTransform rect, Vector3 targetScale, float duration)
        {
            rect.localScale = targetScale;
        }

        private void LeanTweenAlpha(CanvasGroup cg, float targetAlpha, float duration)
        {
            cg.alpha = targetAlpha;
        }
    }
}
