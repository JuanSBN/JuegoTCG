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

        [Header("Faces")]
        [SerializeField] private GameObject frontContainer;
        [SerializeField] private GameObject backContainer;

        [Header("Frame & Artwork")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Sprite[] rarityFrames; // Index maps to (int)Rarity
        [SerializeField] private Image playerArtImage;
        [SerializeField] private GameObject placeholderAvatar;
        [SerializeField] private TMP_Text playerInitialsText;

        [Header("Typography")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text teamText;
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private TMP_Text rarityText;

        [Header("Holographic Effects")]
        [SerializeField] private Material holographicMaterial;
        private Material holoInstance;

        public CardData CardData => cardData;
        public bool IsShowingBack => backContainer != null && backContainer.activeSelf;

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

        public void ShowBack(bool showBack)
        {
            if (backContainer != null) backContainer.SetActive(showBack);
            if (frontContainer != null) frontContainer.SetActive(!showBack);
        }

        public void SetCard(CardData data)
        {
            if (data == null) return;
            cardData = data;

            // 1. Set Frame according to Rarity
            int rarityIndex = (int)data.rarity;
            if (frameImage != null && rarityFrames != null && rarityIndex >= 0 && rarityIndex < rarityFrames.Length)
            {
                if (rarityFrames[rarityIndex] != null)
                {
                    frameImage.sprite = rarityFrames[rarityIndex];
                }
            }

            // 2. Player Artwork vs Placeholder
            if (data.defaultArt != null)
            {
                if (playerArtImage != null)
                {
                    playerArtImage.sprite = data.defaultArt;
                    playerArtImage.preserveAspect = false;
                    playerArtImage.gameObject.SetActive(true);
                }
                if (placeholderAvatar != null) placeholderAvatar.SetActive(false);
            }
            else
            {
                if (playerArtImage != null) playerArtImage.gameObject.SetActive(false);
                if (placeholderAvatar != null) placeholderAvatar.SetActive(true);
                if (playerInitialsText != null)
                {
                    playerInitialsText.text = GetInitials(data.playerName);
                }
            }

            // 3. Texts
            if (nameText != null)
            {
                nameText.text = data.playerName;
                nameText.fontStyle = FontStyles.Bold;
                nameText.outlineColor = new Color32(0, 0, 0, 180);
                nameText.outlineWidth = 0.08f;

                if (nameText.fontMaterial != null)
                {
                    nameText.fontMaterial.EnableKeyword("OUTLINE_ON");
                    nameText.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.70f));
                    nameText.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);

                    nameText.fontMaterial.EnableKeyword("UNDERLAY_ON");
                    nameText.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                    nameText.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 2.0f);
                    nameText.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -3.0f);
                    nameText.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
                    nameText.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.0f);
                }
            }
            if (teamText != null) teamText.text = data.teamName;
            if (positionText != null) positionText.text = data.position;
            if (rarityText != null) rarityText.text = GetRarityName(data.rarity);

            // 4. Holographic Foil Material only for Card Frame (never for player photo!)
            bool isHolo = (data.rarity == Rarity.Epica || data.rarity == Rarity.Legendaria || data.rarity == Rarity.Mitica || data.rarity == Rarity.FullArt);
            EnsureHolographicMaterial();

            if (playerArtImage != null)
            {
                playerArtImage.material = null; // Always keep photo clean and natural without foil distortion
            }

            if (isHolo && holographicMaterial != null)
            {
                if (holoInstance == null)
                {
                    holoInstance = Instantiate(holographicMaterial);
                }

                if (frameImage != null) frameImage.material = holoInstance;

                HolographicTilt tilt = GetComponent<HolographicTilt>();
                if (tilt != null)
                {
                    tilt.SetTargetMaterial(holoInstance);
                }
            }
            else
            {
                if (frameImage != null) frameImage.material = null;

                HolographicTilt tilt = GetComponent<HolographicTilt>();
                if (tilt != null)
                {
                    tilt.SetTargetMaterial(null);
                }
            }
        }

        private static string GetRarityName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Comun: return "COMUN";
                case Rarity.Especial: return "ESPECIAL";
                case Rarity.Epica: return "EPICA";
                case Rarity.Legendaria: return "LEGENDARIA";
                case Rarity.Mitica: return "MITICA";
                case Rarity.FullArt: return "FULL ART";
                default: return "COMUN";
            }
        }

        private static string GetInitials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "FC";
            string[] parts = fullName.Trim().Split(' ');
            if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
        }
    }
}

