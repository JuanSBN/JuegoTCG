using System;
using UnityEngine;
using JuegoTCG.Networking;

namespace JuegoTCG.Core
{
    public class ProfileCustomizationManager : MonoBehaviour
    {
        public static ProfileCustomizationManager Instance { get; private set; }

        public event Action OnCustomizationUpdated;

        [Header("Active Customization")]
        [SerializeField] private string activeFrameId = "frame_gold";
        [SerializeField] private string activePitchTheme = "pitch_night";

        public string ActiveFrameId => activeFrameId;
        public string ActivePitchTheme => activePitchTheme;

        private const string PREF_FRAME = "Profile_AvatarFrame";
        private const string PREF_THEME = "Profile_PitchTheme";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPreferences();
        }

        private void LoadPreferences()
        {
            activeFrameId = PlayerPrefs.GetString(PREF_FRAME, "frame_gold");
            activePitchTheme = PlayerPrefs.GetString(PREF_THEME, "pitch_night");
        }

        public void SetAvatarFrame(string frameId)
        {
            activeFrameId = frameId;
            PlayerPrefs.SetString(PREF_FRAME, frameId);
            PlayerPrefs.Save();

            Debug.Log($"<color=cyan>[Customization] Marco de avatar actualizado: {frameId}</color>");
            OnCustomizationUpdated?.Invoke();
        }

        public void SetPitchTheme(string themeId)
        {
            activePitchTheme = themeId;
            PlayerPrefs.SetString(PREF_THEME, themeId);
            PlayerPrefs.Save();

            Debug.Log($"<color=cyan>[Customization] Tema de cancha actualizado: {themeId}</color>");
            OnCustomizationUpdated?.Invoke();
        }

        public void UpdateUsername(string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername)) return;

            newUsername = newUsername.Trim();
            if (newUsername.Length > 16) newUsername = newUsername.Substring(0, 16);

            PlayerPrefs.SetString("Firebase_DisplayName", newUsername);
            PlayerPrefs.Save();

            Debug.Log($"<color=green>[Customization] Nombre de usuario cambiado a: {newUsername}</color>");
            OnCustomizationUpdated?.Invoke();
        }

        public Color GetFrameColor()
        {
            switch (activeFrameId)
            {
                case "frame_neon": return new Color(0.125f, 0.910f, 0.659f); // #20e8a8 verde neón
                case "frame_classic": return new Color(0.72f, 0.45f, 0.20f); // #b87333 bronce/madera
                case "frame_gold":
                default:
                    return new Color(0.910f, 0.659f, 0.125f); // #e8a820 oro
            }
        }
    }
}
