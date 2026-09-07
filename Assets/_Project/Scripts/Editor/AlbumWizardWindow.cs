#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using JuegoTCG.Cards;
using JuegoTCG.Packs;

namespace JuegoTCG.EditorTools
{
    /// <summary>
    /// Asistente Visual (Album Wizard) para diseñar y generar álbumes completos,
    /// cartas individuales, sobres para la tienda y plantillas para Data Packs.
    /// Accesible desde la barra superior de Unity: JuegoTCG > 🛠️ Creador de Álbumes (Album Wizard).
    /// </summary>
    public class AlbumWizardWindow : EditorWindow
    {
        [Serializable]
        public class CardWizardEntry
        {
            public string cardId = "";
            public string playerName = "";
            public string teamName = "FC Genérico";
            public string position = "DEL";
            public Rarity rarity = Rarity.Comun;
            public Sprite customArt = null;
        }

        // Datos Generales del Álbum
        private string albumName = "Copa de Campeones 2026";
        private string albumId = "album_campeones_2026";
        private AlbumType albumType = AlbumType.Torneo;
        private int rewardCoins = 500;
        private bool autoGenerateId = true;

        // Configuración del Sobre de Tienda
        private bool createPack = true;
        private string packName = "Sobre Copa de Campeones";
        private string packId = "pack_campeones_2026";
        private int packCostCoins = 150;
        private int cardsPerPack = 5;
        private float weightComun = 55f;
        private float weightEspecial = 25f;
        private float weightEpica = 12f;
        private float weightLegendaria = 5f;
        private float weightMitica = 2f;
        private float weightFullArt = 1f;

        // Lista de Cartas
        private List<CardWizardEntry> cardEntries = new List<CardWizardEntry>();
        private Vector2 scrollPos;
        private int selectedTab = 0; // 0 = Editor Visual de Cartas, 1 = Importación Masiva CSV / Excel

        // Importación CSV / Texto
        private string csvImportText = "";

        // Plegables de UI
        private bool foldoutAlbumInfo = true;
        private bool foldoutPackInfo = true;
        private bool foldoutCards = true;

        [MenuItem("JuegoTCG/🛠️ Creador de Álbumes (Album Wizard)", priority = 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<AlbumWizardWindow>("Creador de Álbumes");
            window.minSize = new Vector2(650, 700);
            window.Show();
        }

        private void OnEnable()
        {
            if (cardEntries.Count == 0)
            {
                // Ejemplo inicial para orientar al creador
                cardEntries.Add(new CardWizardEntry { cardId = "champ_01", playerName = "Jugador Estrella 1", teamName = "Equipo A", position = "DEL", rarity = Rarity.Legendaria });
                cardEntries.Add(new CardWizardEntry { cardId = "champ_02", playerName = "Mediocampista Top", teamName = "Equipo A", position = "MED", rarity = Rarity.Epica });
                cardEntries.Add(new CardWizardEntry { cardId = "champ_03", playerName = "Defensa Central", teamName = "Equipo B", position = "DEF", rarity = Rarity.Comun });
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            DrawHeader();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawAlbumSection();
            DrawPackSection();
            DrawCardsSection();
            DrawValidationAndActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.2f, 0.75f, 1f);

            EditorGUILayout.LabelField("⚽ CREADOR DE ÁLBUMES - JUEGOTCG", titleStyle);
            EditorGUILayout.HelpBox(
                "Diseña y genera álbumes completos con sus cartas y sobres en 1 clic. " +
                "Los assets se crean listos para Android y se integran al sistema de colecciones.",
                MessageType.Info
            );
            EditorGUILayout.Space(8);
        }

        private void DrawAlbumSection()
        {
            foldoutAlbumInfo = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutAlbumInfo, "1. INFORMACIÓN DEL ÁLBUM");
            if (foldoutAlbumInfo)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUI.BeginChangeCheck();
                albumName = EditorGUILayout.TextField("Nombre del Álbum", albumName);
                if (EditorGUI.EndChangeCheck() && autoGenerateId)
                {
                    albumId = "album_" + SanitizeId(albumName);
                    packName = "Sobre " + albumName;
                    packId = "pack_" + SanitizeId(albumName);
                }

                EditorGUILayout.BeginHorizontal();
                albumId = EditorGUILayout.TextField("ID Interno Único", albumId);
                autoGenerateId = EditorGUILayout.ToggleLeft("Auto-generar", autoGenerateId, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                albumType = (AlbumType)EditorGUILayout.EnumPopup("Tipo de Álbum", albumType);
                rewardCoins = EditorGUILayout.IntField("Monedas de Premio (100%)", rewardCoins);

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(8);
        }

        private void DrawPackSection()
        {
            foldoutPackInfo = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutPackInfo, "2. SOBRE PARA LA TIENDA");
            if (foldoutPackInfo)
            {
                EditorGUILayout.BeginVertical("box");
                createPack = EditorGUILayout.ToggleLeft(" Generar Sobre automáticamente para este Álbum", createPack, EditorStyles.boldLabel);

                if (createPack)
                {
                    EditorGUI.indentLevel++;
                    packName = EditorGUILayout.TextField("Nombre del Sobre", packName);
                    packId = EditorGUILayout.TextField("ID del Sobre", packId);
                    packCostCoins = EditorGUILayout.IntField("Precio en Monedas", packCostCoins);
                    cardsPerPack = EditorGUILayout.IntSlider("Cartas por Sobre", cardsPerPack, 1, 10);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Pesos de Probabilidad RNG (% Ponderado):", EditorStyles.boldLabel);
                    weightComun = EditorGUILayout.Slider("Común", weightComun, 0f, 100f);
                    weightEspecial = EditorGUILayout.Slider("Especial", weightEspecial, 0f, 100f);
                    weightEpica = EditorGUILayout.Slider("Épica", weightEpica, 0f, 100f);
                    weightLegendaria = EditorGUILayout.Slider("Legendaria", weightLegendaria, 0f, 100f);
                    weightMitica = EditorGUILayout.Slider("Mítica (Holo)", weightMitica, 0f, 100f);
                    weightFullArt = EditorGUILayout.Slider("Full Art (Holo)", weightFullArt, 0f, 100f);

                    float totalWeight = weightComun + weightEspecial + weightEpica + weightLegendaria + weightMitica + weightFullArt;
                    EditorGUILayout.LabelField($"Suma Total de Pesos: {totalWeight:F1}%", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(8);
        }

        private void DrawCardsSection()
        {
            foldoutCards = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutCards, $"3. CATÁLOGO DE CARTAS ({cardEntries.Count} cartas)");
            if (foldoutCards)
            {
                EditorGUILayout.BeginVertical("box");

                selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "📋 Tabla Visual Interactiva", "📥 Importar desde Excel / CSV" });
                EditorGUILayout.Space(6);

                if (selectedTab == 0)
                {
                    DrawCardsTable();
                }
                else
                {
                    DrawCsvImportTab();
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(8);
        }

        private void DrawCardsTable()
        {
            // Barra superior de acciones de cartas
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Agregar Nueva Carta", GUILayout.Height(26)))
            {
                string nextId = $"{GetPrefixFromAlbumId()}_{cardEntries.Count + 1:D2}";
                cardEntries.Add(new CardWizardEntry { cardId = nextId, playerName = "Nuevo Jugador", position = "DEL", rarity = Rarity.Comun });
            }
            if (GUILayout.Button("🔄 Re-numerar IDs", GUILayout.Height(26), GUILayout.Width(130)))
            {
                RenumberCardIds();
            }
            if (GUILayout.Button("🗑️ Limpiar Todo", GUILayout.Height(26), GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Limpiar Cartas", "¿Seguro que deseas vaciar la lista?", "Sí, vaciar", "Cancelar"))
                {
                    cardEntries.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            // Cabecera de tabla
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#", GUILayout.Width(25));
            GUILayout.Label("ID Carta", GUILayout.Width(90));
            GUILayout.Label("Nombre Jugador", GUILayout.Width(140));
            GUILayout.Label("Posición", GUILayout.Width(60));
            GUILayout.Label("Equipo", GUILayout.Width(100));
            GUILayout.Label("Rareza", GUILayout.Width(95));
            GUILayout.Label("Sprite (Opcional)", GUILayout.Width(90));
            GUILayout.Label("", GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < cardEntries.Count; i++)
            {
                var entry = cardEntries[i];
                EditorGUILayout.BeginHorizontal("box");

                GUILayout.Label((i + 1).ToString(), GUILayout.Width(25));
                entry.cardId = EditorGUILayout.TextField(entry.cardId, GUILayout.Width(90));
                entry.playerName = EditorGUILayout.TextField(entry.playerName, GUILayout.Width(140));
                entry.position = EditorGUILayout.TextField(entry.position, GUILayout.Width(60));
                entry.teamName = EditorGUILayout.TextField(entry.teamName, GUILayout.Width(100));
                entry.rarity = (Rarity)EditorGUILayout.EnumPopup(entry.rarity, GUILayout.Width(95));
                entry.customArt = (Sprite)EditorGUILayout.ObjectField(entry.customArt, typeof(Sprite), false, GUILayout.Width(90));

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    cardEntries.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCsvImportTab()
        {
            EditorGUILayout.HelpBox(
                "Pega aquí una lista copiada de Excel o Google Sheets con las columnas:\n" +
                "[ID] [Nombre Jugador] [Posición] [Equipo] [Rareza]\n\n" +
                "Valores aceptados de Rareza: Comun, Especial, Epica, Legendaria, Mitica, FullArt\n" +
                "Ejemplo:\n" +
                "champ_01\tVinícius Jr.\tDEL\tReal Madrid\tLegendaria\n" +
                "champ_02\tJude Bellingham\tMED\tReal Madrid\tLegendaria",
                MessageType.None
            );

            csvImportText = EditorGUILayout.TextArea(csvImportText, GUILayout.Height(130));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📥 Procesar e Importar Tabla", GUILayout.Height(30)))
            {
                ImportFromText(csvImportText);
            }
            if (GUILayout.Button("📂 Cargar archivo .CSV", GUILayout.Height(30)))
            {
                string path = EditorUtility.OpenFilePanel("Seleccionar archivo CSV", "", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    csvImportText = File.ReadAllText(path);
                    ImportFromText(csvImportText);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationAndActions()
        {
            EditorGUILayout.Space(12);

            // Validaciones
            List<string> errors = ValidateAlbumData();
            if (errors.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                GUIStyle errStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
                EditorGUILayout.LabelField("⚠️ Errores encontrados antes de generar:", errStyle);
                foreach (var err in errors)
                {
                    EditorGUILayout.LabelField("• " + err, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6);
            }

            GUI.enabled = errors.Count == 0;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("🚀 CREAR ÁLBUM COMPLETO CON UN CLIC", btnStyle, GUILayout.Height(45)))
            {
                ExecuteCreateAlbum();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📦 Exportar Plantilla para Data Pack (JSON)", GUILayout.Height(28)))
            {
                ExportDataPackTemplate();
            }
            if (GUILayout.Button("📑 Exportar Catálogo a CSV", GUILayout.Height(28)))
            {
                ExportCatalogCsv();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private List<string> ValidateAlbumData()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(albumName)) errors.Add("El nombre del álbum no puede estar vacío.");
            if (string.IsNullOrWhiteSpace(albumId)) errors.Add("El ID del álbum no puede estar vacío.");
            if (cardEntries.Count == 0) errors.Add("El álbum debe contener al menos 1 carta.");

            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < cardEntries.Count; i++)
            {
                var c = cardEntries[i];
                if (string.IsNullOrWhiteSpace(c.cardId))
                {
                    errors.Add($"La carta #{i + 1} no tiene ID.");
                }
                else if (seenIds.Contains(c.cardId))
                {
                    errors.Add($"El ID '{c.cardId}' está duplicado (carta #{i + 1}).");
                }
                else
                {
                    seenIds.Add(c.cardId);
                }

                if (string.IsNullOrWhiteSpace(c.playerName))
                {
                    errors.Add($"La carta '{c.cardId}' no tiene nombre de jugador.");
                }
            }

            if (createPack)
            {
                if (string.IsNullOrWhiteSpace(packId)) errors.Add("El sobre debe tener un ID único.");
                if (string.IsNullOrWhiteSpace(packName)) errors.Add("El sobre debe tener un nombre.");
            }

            return errors;
        }

        private void ExecuteCreateAlbum()
        {
            string cleanId = SanitizeId(albumId);
            string resourcesDir = $"Assets/Resources/Albums/{cleanId}";
            string scriptableObjectsDir = $"Assets/_Project/ScriptableObjects/{cleanId}";

            // Crear carpetas
            EnsureDirectory(resourcesDir);
            EnsureDirectory(scriptableObjectsDir);

            AssetDatabase.StartAssetEditing();
            try
            {
                // 1. Crear AlbumData
                AlbumData album = ScriptableObject.CreateInstance<AlbumData>();
                album.albumId = albumId.Trim();
                album.albumName = albumName.Trim();
                album.albumType = albumType;
                album.rewardCoins = rewardCoins;
                album.active = true;

                // 2. Crear Cartas individuales (CardData)
                foreach (var entry in cardEntries)
                {
                    CardData card = ScriptableObject.CreateInstance<CardData>();
                    card.cardId = entry.cardId.Trim();
                    card.playerName = entry.playerName.Trim();
                    card.teamName = entry.teamName.Trim();
                    card.position = entry.position.Trim();
                    card.rarity = entry.rarity;
                    card.albumId = album.albumId;
                    card.defaultArt = entry.customArt;

                    string safeName = SanitizeFileName(entry.playerName);
                    string cardPathSO = $"{scriptableObjectsDir}/{card.cardId}_{safeName}.asset";
                    string cardPathRes = $"{resourcesDir}/{card.cardId}_{safeName}.asset";

                    AssetDatabase.CreateAsset(card, cardPathSO);
                    // Copia para Resources (mobile APK)
                    AssetDatabase.CopyAsset(cardPathSO, cardPathRes);

                    album.cards.Add(card);
                }

                // Guardar AlbumData en ambas carpetas
                string albumPathSO = $"{scriptableObjectsDir}/Album_{cleanId}.asset";
                string albumPathRes = $"{resourcesDir}/Album_{cleanId}.asset";
                AssetDatabase.CreateAsset(album, albumPathSO);
                AssetDatabase.CopyAsset(albumPathSO, albumPathRes);

                // 3. Crear PackData si está habilitado
                if (createPack)
                {
                    PackData pack = ScriptableObject.CreateInstance<PackData>();
                    pack.packId = packId.Trim();
                    pack.packName = packName.Trim();
                    pack.albumId = album.albumId;
                    pack.cardsPerPack = cardsPerPack;
                    pack.costType = CostType.Moneda;
                    pack.costAmount = packCostCoins;

                    pack.comunWeight = weightComun;
                    pack.especialWeight = weightEspecial;
                    pack.epicaWeight = weightEpica;
                    pack.legendariaWeight = weightLegendaria;
                    pack.miticaWeight = weightMitica;
                    pack.fullArtWeight = weightFullArt;

                    string packPathSO = $"{scriptableObjectsDir}/Pack_{cleanId}.asset";
                    string packPathRes = $"{resourcesDir}/Pack_{cleanId}.asset";
                    AssetDatabase.CreateAsset(pack, packPathSO);
                    AssetDatabase.CopyAsset(packPathSO, packPathRes);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Registrar inmediatamente en PlayerCollectionManager si está en memoria
            PlayerCollectionManager.EnsureExists();
            if (PlayerCollectionManager.Instance != null)
            {
                PlayerCollectionManager.Instance.LoadAllAlbums();
            }

            EditorUtility.DisplayDialog(
                "¡Álbum Creado Exitosamente!",
                $"El álbum '{albumName}' con {cardEntries.Count} cartas y su sobre correspondiente " +
                $"fueron generados e integrados correctamente en:\n\n{resourcesDir}\n\n¡Listo para jugar y compilar!",
                "¡Excelente!"
            );

            Debug.Log($"<color=green>[AlbumWizard] ¡Álbum '{albumName}' ({albumId}) generado con {cardEntries.Count} cartas en Resources/Albums/{cleanId}!</color>");
        }

        private void ExportDataPackTemplate()
        {
            string savePath = EditorUtility.SaveFilePanel("Guardar Plantilla Data Pack", "", $"DataPack_{SanitizeId(albumName)}_Template.json", "json");
            if (string.IsNullOrEmpty(savePath)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"targetAlbumId\": \"{albumId}\",");
            sb.AppendLine($"  \"packTitle\": \"Data Pack {albumName}\",");
            sb.AppendLine("  \"author\": \"Comunidad\",");
            sb.AppendLine("  \"version\": \"1.0\",");
            sb.AppendLine("  \"players\": [");

            for (int i = 0; i < cardEntries.Count; i++)
            {
                var c = cardEntries[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"cardId\": \"{c.cardId}\",");
                sb.AppendLine($"      \"realName\": \"{c.playerName}\",");
                sb.AppendLine($"      \"realTeam\": \"{c.teamName}\",");
                sb.AppendLine($"      \"photoFileName\": \"{c.cardId}.png\"");
                sb.Append("    }");
                if (i < cardEntries.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            EditorUtility.DisplayDialog("Plantilla Generada", $"Plantilla JSON exportada en:\n{savePath}\n\nLa comunidad puede usar esta estructura para crear Data Packs.", "Entendido");
        }

        private void ExportCatalogCsv()
        {
            string savePath = EditorUtility.SaveFilePanel("Guardar Catálogo CSV", "", $"{albumId}_catalogo.csv", "csv");
            if (string.IsNullOrEmpty(savePath)) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CardId,PlayerName,Position,TeamName,Rarity");
            foreach (var c in cardEntries)
            {
                sb.AppendLine($"{c.cardId},{c.playerName},{c.position},{c.teamName},{c.rarity}");
            }

            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            EditorUtility.DisplayDialog("CSV Exportado", $"Catálogo guardado en:\n{savePath}", "Aceptar");
        }

        private void ImportFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int imported = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("CardId", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Soporta delimitadores por tabulaciones (Excel directo) o comas (CSV)
                char delimiter = line.Contains("\t") ? '\t' : ',';
                string[] tokens = line.Split(delimiter);

                if (tokens.Length >= 2)
                {
                    string id = tokens[0].Trim();
                    string name = tokens[1].Trim();
                    string pos = tokens.Length > 2 ? tokens[2].Trim() : "DEL";
                    string team = tokens.Length > 3 ? tokens[3].Trim() : "FC Genérico";
                    Rarity rar = Rarity.Comun;

                    if (tokens.Length > 4)
                    {
                        Enum.TryParse(tokens[4].Trim(), true, out rar);
                    }

                    cardEntries.Add(new CardWizardEntry
                    {
                        cardId = id,
                        playerName = name,
                        position = pos,
                        teamName = team,
                        rarity = rar
                    });
                    imported++;
                }
            }

            selectedTab = 0; // Regresar a la tabla visual
            EditorUtility.DisplayDialog("Importación Completada", $"Se importaron exitosamente {imported} cartas a la tabla.", "Aceptar");
        }

        private void RenumberCardIds()
        {
            string prefix = GetPrefixFromAlbumId();
            for (int i = 0; i < cardEntries.Count; i++)
            {
                cardEntries[i].cardId = $"{prefix}_{i + 1:D2}";
            }
        }

        private string GetPrefixFromAlbumId()
        {
            string clean = SanitizeId(albumId).Replace("album_", "");
            return clean.Length > 5 ? clean.Substring(0, 5) : clean;
        }

        private string SanitizeId(string input)
        {
            if (string.IsNullOrEmpty(input)) return "nuevo_album";
            StringBuilder sb = new StringBuilder();
            foreach (char c in input.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            string result = sb.ToString();
            while (result.Contains("__")) result = result.Replace("__", "_");
            return result.Trim('_');
        }

        private string SanitizeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Carta";
            char[] invalids = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if (Array.IndexOf(invalids, c) < 0 && c != ' ') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif
