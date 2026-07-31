#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using JuegoTCG.Cards;

namespace JuegoTCG.EditorTools
{
    public static class CardPrefabBuilder
    {
        private const string PrefabFolderPath = "Assets/_Project/Prefabs/Cards";
        private const string FramesFolderPath = "Assets/_Project/Art/CardFrames";

        [MenuItem("JuegoTCG/Generar Prefab de Carta")]
        public static void BuildCardPrefab()
        {
            if (!Directory.Exists(PrefabFolderPath))
            {
                Directory.CreateDirectory(PrefabFolderPath);
                AssetDatabase.Refresh();
            }

            // Create Root GameObject
            GameObject rootGO = new GameObject("CardPrefab");
            RectTransform rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(360, 480);

            CardDisplay display = rootGO.AddComponent<CardDisplay>();

            // 1. Frame Image (Background)
            GameObject frameGO = new GameObject("FrameImage");
            frameGO.transform.SetParent(rootGO.transform, false);
            RectTransform frameRect = frameGO.AddComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.sizeDelta = Vector2.zero;
            Image frameImg = frameGO.AddComponent<Image>();

            // 2. Player Art Image (Center Artwork)
            GameObject artGO = new GameObject("PlayerArtImage");
            artGO.transform.SetParent(rootGO.transform, false);
            RectTransform artRect = artGO.AddComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.08f, 0.18f);
            artRect.anchorMax = new Vector2(0.92f, 0.82f);
            artRect.sizeDelta = Vector2.zero;
            Image artImg = artGO.AddComponent<Image>();

            // 3. Header Container (Player Name)
            GameObject headerGO = new GameObject("HeaderContainer");
            headerGO.transform.SetParent(rootGO.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.05f, 0.84f);
            headerRect.anchorMax = new Vector2(0.95f, 0.96f);
            headerRect.sizeDelta = Vector2.zero;

            // Player Name Text
            GameObject nameGO = new GameObject("PlayerNameText");
            nameGO.transform.SetParent(headerGO.transform, false);
            RectTransform nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = "Nombre Jugador";
            nameTMP.fontSize = 24;
            nameTMP.fontStyle = FontStyles.Bold;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;

            // 4. Footer Container (Ajustado perfectamente dentro de la placa inferior)
            GameObject footerGO = new GameObject("FooterContainer");
            footerGO.transform.SetParent(rootGO.transform, false);
            RectTransform footerRect = footerGO.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.24f, 0.025f);
            footerRect.anchorMax = new Vector2(0.76f, 0.16f);
            footerRect.sizeDelta = Vector2.zero;

            // Team Name Text (Nombre del Equipo Centrado Arriba en la Placa)
            GameObject teamGO = new GameObject("TeamNameText");
            teamGO.transform.SetParent(footerGO.transform, false);
            RectTransform teamRect = teamGO.AddComponent<RectTransform>();
            teamRect.anchorMin = new Vector2(0f, 0.50f);
            teamRect.anchorMax = new Vector2(1f, 0.95f);
            teamRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI teamTMP = teamGO.AddComponent<TextMeshProUGUI>();
            teamTMP.text = "Equipo FC";
            teamTMP.fontSize = 15;
            teamTMP.fontStyle = FontStyles.Bold;
            teamTMP.alignment = TextAlignmentOptions.Center;
            teamTMP.color = new Color(0.1f, 0.1f, 0.1f);

            // Position Text (Posición a la izquierda)
            GameObject posGO = new GameObject("PositionText");
            posGO.transform.SetParent(footerGO.transform, false);
            RectTransform posRect = posGO.AddComponent<RectTransform>();
            posRect.anchorMin = new Vector2(0.02f, 0.08f);
            posRect.anchorMax = new Vector2(0.48f, 0.48f);
            posRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI posTMP = posGO.AddComponent<TextMeshProUGUI>();
            posTMP.text = "Delantero";
            posTMP.fontSize = 11;
            posTMP.fontStyle = FontStyles.Bold;
            posTMP.alignment = TextAlignmentOptions.Center;
            posTMP.color = new Color(0.15f, 0.15f, 0.35f);

            // Rarity Text (Rareza a la derecha)
            GameObject rarityGO = new GameObject("RarityText");
            rarityGO.transform.SetParent(footerGO.transform, false);
            RectTransform rarityRect = rarityGO.AddComponent<RectTransform>();
            rarityRect.anchorMin = new Vector2(0.52f, 0.08f);
            rarityRect.anchorMax = new Vector2(0.98f, 0.48f);
            rarityRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI rarityTMP = rarityGO.AddComponent<TextMeshProUGUI>();
            rarityTMP.text = "RARA";
            rarityTMP.fontSize = 11;
            rarityTMP.fontStyle = FontStyles.Bold;
            rarityTMP.alignment = TextAlignmentOptions.Center;
            rarityTMP.color = new Color(0.45f, 0.1f, 0.0f);

            // Load and assign frame sprites from Assets/_Project/Art/CardFrames
            Sprite[] frames = new Sprite[6];
            string[] frameFileNames = { "Común.png", "Especial.png", "Epica.png", "Legendaria.png", "Mitica.png", "Full Art.png" };
            for (int i = 0; i < frameFileNames.Length; i++) {
                string path = $"{FramesFolderPath}/{frameFileNames[i]}";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            // Assign Fields to SerializedObject of CardDisplay
            SerializedObject so = new SerializedObject(display);
            so.FindProperty("frameImage").objectReferenceValue = frameImg;
            so.FindProperty("playerArtImage").objectReferenceValue = artImg;
            so.FindProperty("nameText").objectReferenceValue = nameTMP;
            so.FindProperty("teamText").objectReferenceValue = teamTMP;
            so.FindProperty("positionText").objectReferenceValue = posTMP;
            so.FindProperty("rarityText").objectReferenceValue = rarityTMP;

            SerializedProperty framesProp = so.FindProperty("rarityFrames");
            framesProp.arraySize = 6;
            for (int i = 0; i < 6; i++) {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefabPath = $"{PrefabFolderPath}/CardPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            Object.DestroyImmediate(rootGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡CardPrefab.prefab creado con éxito en Assets/_Project/Prefabs/Cards/CardPrefab.prefab!</color>");
        }
    }
}
#endif
