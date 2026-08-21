using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuegoTCG.Cards
{
    [ExecuteAlways]
    public class CardDisplay : MonoBehaviour
    {
        [Header("Card Data")]
        [SerializeField] private CardData cardData;

        [Header("UI Components")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image playerArtImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text teamText;
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private TMP_Text rarityText;

        [Header("Frame Sprites (Order: Comun, Especial, Epica, Legendaria, Mitica, FullArt)")]
        [SerializeField] private Sprite[] rarityFrames;

        [Header("Holographic Effects")]
        [SerializeField] private Material holographicMaterial;

        public CardData CardData => cardData;

        private void Awake()
        {
            EnsureHolographicMaterial();
        }

        private void Start()
        {
            if (cardData != null)
            {
                SetCard(cardData);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureHolographicMaterial();
            if (cardData != null)
            {
                SetCard(cardData);
            }
        }
#endif

        private void EnsureHolographicMaterial()
        {
            if (holographicMaterial == null)
            {
#if UNITY_EDITOR
                holographicMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/HolographicFoilMaterial.mat");
#endif
                if (holographicMaterial == null)
                {
                    Shader holoShader = Shader.Find("Shader Graphs/HolographicFoilShader");
                    if (holoShader != null)
                    {
                        holographicMaterial = new Material(holoShader);
                    }
                }
            }
        }

        public void SetCard(CardData data)
        {
            if (data == null) return;
            cardData = data;

            if (nameText != null) nameText.text = data.playerName;
            if (teamText != null) teamText.text = data.teamName;
            if (positionText != null) positionText.text = data.position;
            if (rarityText != null) rarityText.text = data.rarity.ToString().ToUpper();

            // Set player photo if available
            if (playerArtImage != null)
            {
                if (data.defaultArt != null)
                {
                    playerArtImage.sprite = data.defaultArt;
                    playerArtImage.gameObject.SetActive(true);
                }
                else
                {
                    playerArtImage.gameObject.SetActive(false);
                }
            }

            // Set frame sprite by rarity enum index
            int index = (int)data.rarity;
            if (frameImage != null && rarityFrames != null && index >= 0 && index < rarityFrames.Length)
            {
                if (rarityFrames[index] != null)
                {
                    frameImage.sprite = rarityFrames[index];
                }
            }

            // Apply Holographic Foil Material for Epica, Legendaria, Mitica and FullArt
            bool isHolo = (data.rarity == Rarity.Epica || data.rarity == Rarity.Legendaria || data.rarity == Rarity.Mitica || data.rarity == Rarity.FullArt);
            
            EnsureHolographicMaterial();
            Material targetMat = isHolo ? holographicMaterial : null;

            if (frameImage != null) frameImage.material = targetMat;
            if (playerArtImage != null) playerArtImage.material = targetMat;

            // Enable/disable HolographicTilt component
            var tiltComp = GetComponent<HolographicTilt>();
            if (tiltComp != null)
            {
                tiltComp.enabled = isHolo;
            }
        }
    }
}
