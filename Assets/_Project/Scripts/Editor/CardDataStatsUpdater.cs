#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using JuegoTCG.Cards;

namespace JuegoTCG.Editor
{
    [InitializeOnLoad]
    public static class CardDataStatsUpdater
    {
        [InitializeOnLoadMethod]
        [MenuItem("JuegoTCG/⚽ Actualizar Stats y Nacionalidades de Cartas", priority = 50)]
        public static void UpdateAllCardStats()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            int updatedCount = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null || string.IsNullOrEmpty(card.cardId)) continue;

                ApplyStats(card);
                EditorUtility.SetDirty(card);
                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[CardStatsUpdater] ¡{updatedCount} cartas actualizadas exitosamente con nacionalidad, estadísticas FIFA y Media Global!</color>");

            // Reconstruir CardPrefab con bandera y stats
            JuegoTCG.EditorTools.CardPrefabBuilder.BuildCardPrefab();
            JuegoTCG.EditorTools.ResourcesSetupUtility.ExecuteSetup(silent: true);
        }

        private static void ApplyStats(CardData card)
        {
            switch (card.cardId)
            {
                case "card_01": // Vozhina (POR)
                    card.nationality = "Rusia";
                    card.countryCode = "RU";
                    card.diving = 70;
                    card.reflexes = 74;
                    card.handling = 68;
                    card.positioning = 71;
                    card.manualOverall = 0;
                    break;

                case "card_02": // Balogun (DEL)
                    card.nationality = "Estados Unidos";
                    card.countryCode = "US";
                    card.shooting = 78;
                    card.dribbling = 76;
                    card.passing = 66;
                    card.defending = 32;
                    card.manualOverall = 0;
                    break;

                case "card_03": // Diomandé (DEF)
                    card.nationality = "Costa de Marfil";
                    card.countryCode = "CI";
                    card.defending = 79;
                    card.passing = 64;
                    card.dribbling = 66;
                    card.shooting = 35;
                    card.manualOverall = 0;
                    break;

                case "card_04": // James Rodríguez (MED)
                    card.nationality = "Colombia";
                    card.countryCode = "CO";
                    card.passing = 86;
                    card.dribbling = 83;
                    card.shooting = 82;
                    card.defending = 44;
                    card.manualOverall = 0;
                    break;

                case "card_05": // Luis Díaz (DEL)
                    card.nationality = "Colombia";
                    card.countryCode = "CO";
                    card.dribbling = 87;
                    card.shooting = 82;
                    card.passing = 78;
                    card.defending = 40;
                    card.manualOverall = 0;
                    break;

                case "card_06": // Erling Haaland (DEL)
                    card.nationality = "Noruega";
                    card.countryCode = "NO";
                    card.shooting = 93;
                    card.dribbling = 82;
                    card.passing = 70;
                    card.defending = 45;
                    card.manualOverall = 88;
                    break;

                case "card_07": // Cristiano Ronaldo (DEL)
                    card.nationality = "Portugal";
                    card.countryCode = "PT";
                    card.shooting = 90;
                    card.dribbling = 83;
                    card.passing = 76;
                    card.defending = 35;
                    card.manualOverall = 86;
                    break;

                case "card_08": // Lionel Messi (MED/DEL)
                    card.nationality = "Argentina";
                    card.countryCode = "AR";
                    card.dribbling = 94;
                    card.passing = 91;
                    card.shooting = 88;
                    card.defending = 34;
                    card.manualOverall = 91;
                    break;

                case "card_09": // Kylian Mbappé (DEL)
                    card.nationality = "Francia";
                    card.countryCode = "FR";
                    card.shooting = 91;
                    card.dribbling = 92;
                    card.passing = 81;
                    card.defending = 36;
                    card.manualOverall = 91;
                    break;

                case "card_10": // Lamine Yamal (DEL)
                    card.nationality = "España";
                    card.countryCode = "ES";
                    card.dribbling = 89;
                    card.passing = 84;
                    card.shooting = 83;
                    card.defending = 38;
                    card.manualOverall = 85;
                    break;
            }
        }
    }
}
#endif
