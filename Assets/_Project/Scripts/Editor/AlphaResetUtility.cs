#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using JuegoTCG.Cards;
using JuegoTCG.Networking;

namespace JuegoTCG.EditorTools
{
    public static class AlphaResetUtility
    {
        [MenuItem("JuegoTCG/🎮 Reiniciar Cuentas a 0 (Alpha Reset)", priority = 1)]
        public static void ResetAlphaAccount()
        {
            const int initialCoins = 400;

            PlayerCollectionManager.EnsureExists();
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.ResetAlphaAccountData(initialCoins);
            }
            else
            {
                // Limpieza directa de PlayerPrefs
                string[] pilotIds = { 
                    "card_01", "card_02", "card_03", "card_04", "card_05", 
                    "card_06", "card_07", "card_08", "card_09", "card_10",
                    "EH", "RO", "LD", "VJ", "KM", "PE", "LY", "JB", "MS", "KDB" 
                };

                foreach (var id in pilotIds)
                {
                    PlayerPrefs.DeleteKey("Collection_Card_" + id);
                }

                PlayerPrefs.SetInt("Collection_TotalUnique", 0);
                PlayerPrefs.SetInt("Player_CollectionPower", 0);
                PlayerPrefs.SetInt("Firebase_Coins", initialCoins);
                PlayerPrefs.Save();
            }

            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.SetCoins(initialCoins);
            }

            EditorUtility.DisplayDialog(
                "Alpha Reset Completado",
                $"¡Las cuentas se han reiniciado exitosamente a 0!\n\n" +
                $"• Cartas en posesión: 0/10 (Álbum Piloto limpio)\n" +
                $"• Monedas iniciales otorgadas: {initialCoins} monedas\n" +
                $"• Poder de colección: 0 puntos\n\n" +
                $"Ahora puedes abrir la Tienda, comprar sobres y ver cómo se van desbloqueando las cartas en el Álbum.",
                "Entendido"
            );

            Debug.Log($"<color=cyan>[JuegoTCG:Alpha] ¡Reinicio de cuenta completado! Monedas: {initialCoins}, Cartas: 0/10.</color>");
        }
    }
}
#endif
