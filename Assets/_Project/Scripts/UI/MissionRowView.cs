using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.UI
{
    public class MissionRowView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressStatusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFillImage;

        public void Setup(MissionItemData data, Color goldColor, Sprite normalSprite, Sprite doneSprite)
        {
            if (titleText != null)
            {
                titleText.text = data.title;
                titleText.color = data.IsCompleted ? Color.white : new Color(1f, 1f, 1f, 0.85f);
                titleText.fontStyle = data.IsCompleted ? FontStyles.Bold : FontStyles.Normal;
            }

            if (progressStatusText != null)
            {
                if (data.IsCompleted)
                {
                    progressStatusText.text = "✓ Listo";
                    progressStatusText.color = goldColor;
                    progressStatusText.fontStyle = FontStyles.Bold;
                }
                else
                {
                    progressStatusText.text = $"{data.current}/{data.total}";
                    progressStatusText.color = new Color(1f, 1f, 1f, 0.55f);
                    progressStatusText.fontStyle = FontStyles.Normal;
                }
            }

            if (progressBar != null)
            {
                progressBar.minValue = 0;
                progressBar.maxValue = data.total;
                progressBar.value = data.current;
            }

            if (progressFillImage != null)
            {
                progressFillImage.color = data.IsCompleted ? goldColor : new Color(goldColor.r, goldColor.g, goldColor.b, 0.55f);
            }

            if (backgroundImage != null)
            {
                if (data.IsCompleted && doneSprite != null)
                {
                    backgroundImage.sprite = doneSprite;
                }
                else if (normalSprite != null)
                {
                    backgroundImage.sprite = normalSprite;
                }
            }
        }
    }
}
