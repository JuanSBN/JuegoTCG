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

            // Nacionalidad
            public string nationality = "España";
            public string countryCode = "ES";

            // Estadísticas Campo (1-99)
            public int shooting = 50;
            public int passing = 50;
            public int defending = 50;
            public int dribbling = 50;

            // Estadísticas Portero (1-99)
            public int diving = 50;
            public int reflexes = 50;
            public int handling = 50;
            public int positioning = 50;

            // Media Global manual (0 = auto)
            public int manualOverall = 0;

            public bool isExpanded = true;

            public TacticalPosition TacticalLine => TacticalPositionHelper.Normalize(position);

            public int CalculateOverall()
            {
                if (manualOverall > 0) return manualOverall;
                switch (TacticalLine)
                {
                    case TacticalPosition.POR:
                        return Mathf.Clamp(Mathf.RoundToInt(reflexes * 0.30f + diving * 0.25f + handling * 0.25f + positioning * 0.20f), 1, 99);
                    case TacticalPosition.DEF:
                        return Mathf.Clamp(Mathf.RoundToInt(defending * 0.50f + passing * 0.25f + dribbling * 0.20f + shooting * 0.05f), 1, 99);
                    case TacticalPosition.MED:
                        return Mathf.Clamp(Mathf.RoundToInt(passing * 0.35f + dribbling * 0.30f + shooting * 0.20f + defending * 0.15f), 1, 99);
                    case TacticalPosition.DEL:
                    default:
                        return Mathf.Clamp(Mathf.RoundToInt(shooting * 0.45f + dribbling * 0.30f + passing * 0.20f + defending * 0.05f), 1, 99);
                }
            }

            public void ApplyPresetStats()
            {
                int baseVal;
                switch (rarity)
                {
                    case Rarity.Comun: baseVal = 64; break;
                    case Rarity.Especial: baseVal = 75; break;
                    case Rarity.Epica: baseVal = 83; break;
                    case Rarity.Legendaria: baseVal = 89; break;
                    case Rarity.Mitica: baseVal = 94; break;
                    case Rarity.FullArt: baseVal = 97; break;
                    default: baseVal = 64; break;
                }

                if (TacticalLine == TacticalPosition.POR)
                {
                    reflexes = Mathf.Clamp(baseVal + 1, 1, 99);
                    diving = Mathf.Clamp(baseVal, 1, 99);
                    handling = Mathf.Clamp(baseVal - 2, 1, 99);
                    positioning = Mathf.Clamp(baseVal - 1, 1, 99);
                }
                else if (TacticalLine == TacticalPosition.DEF)
                {
                    defending = Mathf.Clamp(baseVal + 3, 1, 99);
                    passing = Mathf.Clamp(baseVal - 12, 1, 99);
                    dribbling = Mathf.Clamp(baseVal - 14, 1, 99);
                    shooting = Mathf.Clamp(baseVal - 35, 1, 99);
                }
                else if (TacticalLine == TacticalPosition.MED)
                {
                    passing = Mathf.Clamp(baseVal + 2, 1, 99);
                    dribbling = Mathf.Clamp(baseVal, 1, 99);
                    shooting = Mathf.Clamp(baseVal - 6, 1, 99);
                    defending = Mathf.Clamp(baseVal - 16, 1, 99);
                }
                else // DEL
                {
                    shooting = Mathf.Clamp(baseVal + 3, 1, 99);
                    dribbling = Mathf.Clamp(baseVal + 1, 1, 99);
                    passing = Mathf.Clamp(baseVal - 8, 1, 99);
                    defending = Mathf.Clamp(baseVal - 40, 1, 99);
                }
            }
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
            window.minSize = new Vector2(820, 720);
            window.Show();
        }

        private void OnEnable()
        {
            if (cardEntries.Count == 0)
            {
                // Ejemplo inicial para orientar al creador
                var c1 = new CardWizardEntry { cardId = "champ_01", playerName = "Jugador Estrella 1", teamName = "Equipo A", position = "DEL", rarity = Rarity.Legendaria, nationality = "Brasil", countryCode = "BR" };
                c1.ApplyPresetStats();
                var c2 = new CardWizardEntry { cardId = "champ_02", playerName = "Mediocampista Top", teamName = "Equipo A", position = "MED", rarity = Rarity.Epica, nationality = "España", countryCode = "ES" };
                c2.ApplyPresetStats();
                var c3 = new CardWizardEntry { cardId = "champ_03", playerName = "Portero Titular", teamName = "Equipo B", position = "POR", rarity = Rarity.Comun, nationality = "Argentina", countryCode = "AR" };
                c3.ApplyPresetStats();

                cardEntries.Add(c1);
                cardEntries.Add(c2);
                cardEntries.Add(c3);
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
                var newC = new CardWizardEntry { cardId = nextId, playerName = "Nuevo Jugador", position = "DEL", rarity = Rarity.Comun, nationality = "España", countryCode = "ES" };
                newC.ApplyPresetStats();
                cardEntries.Add(newC);
            }
            if (GUILayout.Button("⚡ Auto-Stats Todas por Rareza", GUILayout.Height(26), GUILayout.Width(190)))
            {
                foreach (var c in cardEntries) c.ApplyPresetStats();
            }
            if (GUILayout.Button("🔄 Re-numerar IDs", GUILayout.Height(26), GUILayout.Width(115)))
            {
                RenumberCardIds();
            }
            if (GUILayout.Button("📂 Expandir / Colapsar", GUILayout.Height(26), GUILayout.Width(140)))
            {
                bool anyExpanded = cardEntries.Exists(c => c.isExpanded);
                foreach (var c in cardEntries) c.isExpanded = !anyExpanded;
            }
            if (GUILayout.Button("🗑️ Limpiar", GUILayout.Height(26), GUILayout.Width(75)))
            {
                if (EditorUtility.DisplayDialog("Limpiar Cartas", "¿Seguro que deseas vaciar la lista?", "Sí, vaciar", "Cancelar"))
                {
                    cardEntries.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);

            for (int i = 0; i < cardEntries.Count; i++)
            {
                var entry = cardEntries[i];
                EditorGUILayout.BeginVertical("box");

                // Fila 1: Datos Principales de la Carta
                EditorGUILayout.BeginHorizontal();
                entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, $"#{i + 1}", true, EditorStyles.foldout);

                GUILayout.Label("ID:", GUILayout.Width(20));
                entry.cardId = EditorGUILayout.TextField(entry.cardId, GUILayout.Width(75));

                GUILayout.Label("Nombre:", GUILayout.Width(50));
                entry.playerName = EditorGUILayout.TextField(entry.playerName, GUILayout.Width(125));

                GUILayout.Label("Pos:", GUILayout.Width(30));
                string oldPos = entry.position;
                entry.position = EditorGUILayout.TextField(entry.position, GUILayout.Width(45));
                if (oldPos != entry.position)
                {
                    entry.ApplyPresetStats();
                }

                GUILayout.Label("País:", GUILayout.Width(32));
                entry.nationality = EditorGUILayout.TextField(entry.nationality, GUILayout.Width(80));

                GUILayout.Label("Cód:", GUILayout.Width(30));
                entry.countryCode = EditorGUILayout.TextField(entry.countryCode, GUILayout.Width(32)).ToUpperInvariant();

                GUILayout.Label("Equipo:", GUILayout.Width(48));
                entry.teamName = EditorGUILayout.TextField(entry.teamName, GUILayout.Width(90));

                Rarity oldRarity = entry.rarity;
                entry.rarity = (Rarity)EditorGUILayout.EnumPopup(entry.rarity, GUILayout.Width(90));
                if (oldRarity != entry.rarity)
                {
                    entry.ApplyPresetStats();
                }

                // Badge de OVR
                int ovr = entry.CalculateOverall();
                GUIStyle ovrStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = ovr >= 90 ? new Color(1f, 0.85f, 0.2f) : (ovr >= 80 ? new Color(0.3f, 0.8f, 1f) : Color.white) }
                };
                GUILayout.Label($"OVR:{ovr}", ovrStyle, GUILayout.Width(50));

                entry.customArt = (Sprite)EditorGUILayout.ObjectField(entry.customArt, typeof(Sprite), false, GUILayout.Width(70));

                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    cardEntries.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // Fila 2 (Plegable): 4 Estadísticas de Juego (Campo vs Portero)
                if (entry.isExpanded)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Space(15);

                    if (entry.TacticalLine == TacticalPosition.POR)
                    {
                        GUILayout.Label("🧤 PORTERO:", EditorStyles.miniBoldLabel, GUILayout.Width(75));
                        GUILayout.Label("EST:", GUILayout.Width(28));
                        entry.diving = EditorGUILayout.IntField(entry.diving, GUILayout.Width(36));
                        GUILayout.Label("REF:", GUILayout.Width(28));
                        entry.reflexes = EditorGUILayout.IntField(entry.reflexes, GUILayout.Width(36));
                        GUILayout.Label("PAR:", GUILayout.Width(28));
                        entry.handling = EditorGUILayout.IntField(entry.handling, GUILayout.Width(36));
                        GUILayout.Label("COL:", GUILayout.Width(28));
                        entry.positioning = EditorGUILayout.IntField(entry.positioning, GUILayout.Width(36));
                    }
                    else
                    {
                        GUILayout.Label("⚽ CAMPO:", EditorStyles.miniBoldLabel, GUILayout.Width(75));
                        GUILayout.Label("TIR:", GUILayout.Width(28));
                        entry.shooting = EditorGUILayout.IntField(entry.shooting, GUILayout.Width(36));
                        GUILayout.Label("PAS:", GUILayout.Width(28));
                        entry.passing = EditorGUILayout.IntField(entry.passing, GUILayout.Width(36));
                        GUILayout.Label("DEF:", GUILayout.Width(28));
                        entry.defending = EditorGUILayout.IntField(entry.defending, GUILayout.Width(36));
                        GUILayout.Label("REG:", GUILayout.Width(28));
                        entry.dribbling = EditorGUILayout.IntField(entry.dribbling, GUILayout.Width(36));
                    }

                    GUILayout.Space(10);
                    GUILayout.Label("OVR Manual (0=Auto):", GUILayout.Width(130));
                    entry.manualOverall = EditorGUILayout.IntField(entry.manualOverall, GUILayout.Width(36));

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("⚡ Preset Stats", EditorStyles.miniButton, GUILayout.Width(90)))
                    {
                        entry.ApplyPresetStats();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawCsvImportTab()
        {
            EditorGUILayout.HelpBox(
                "Pega aquí una lista copiada de Excel o Google Sheets con las siguientes columnas:\n\n" +
                "[ID] \\t [Nombre Jugador] \\t [Posición] \\t [Equipo] \\t [Rareza] \\t [País] \\t [CódPaís] \\t [Stat1] \\t [Stat2] \\t [Stat3] \\t [Stat4] \\t [OVR]\n\n" +
                "• Si solo pegas las 5 columnas tradicionales ([ID] [Nombre] [Pos] [Equipo] [Rareza]), el sistema asignará país por defecto y auto-calculará las estadísticas según posición y rareza.\n" +
                "• Para Porteros: Stat1=EST, Stat2=REF, Stat3=PAR, Stat4=COL.\n" +
                "• Para Jugadores de Campo: Stat1=TIR, Stat2=PAS, Stat3=DEF, Stat4=REG.\n\n" +
                "Ejemplo con atributos modernos:\n" +
                "champ_01\\tLamine Yamal\\tDEL\\tFC Barcelona\\tMitica\\tEspaña\\tES\\t84\\t86\\t38\\t92\\t86\n" +
                "champ_02\\tLionel Messi\\tMED\\tInter Miami\\tLegendaria\\tArgentina\\tAR\\t88\\t92\\t35\\t93\\t90\n" +
                "champ_03\\tThibaut Courtois\\tPOR\\tReal Madrid\\tLegendaria\\tBélgica\\tBE\\t85\\t90\\t88\\t86\\t89",
                MessageType.Info
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

                    // Nacionalidad y Bandera
                    card.nationality = !string.IsNullOrWhiteSpace(entry.nationality) ? entry.nationality.Trim() : "España";
                    card.countryCode = !string.IsNullOrWhiteSpace(entry.countryCode) ? entry.countryCode.Trim().ToUpperInvariant() : "ES";

                    // Estadísticas de Campo
                    card.shooting = Mathf.Clamp(entry.shooting, 1, 99);
                    card.passing = Mathf.Clamp(entry.passing, 1, 99);
                    card.defending = Mathf.Clamp(entry.defending, 1, 99);
                    card.dribbling = Mathf.Clamp(entry.dribbling, 1, 99);

                    // Estadísticas de Portero
                    card.diving = Mathf.Clamp(entry.diving, 1, 99);
                    card.reflexes = Mathf.Clamp(entry.reflexes, 1, 99);
                    card.handling = Mathf.Clamp(entry.handling, 1, 99);
                    card.positioning = Mathf.Clamp(entry.positioning, 1, 99);

                    // Media Global (0 = cálculo ponderado FIFA)
                    card.manualOverall = Mathf.Clamp(entry.manualOverall, 0, 99);

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
                $"El álbum '{albumName}' con {cardEntries.Count} cartas con nacionalidad y estadísticas, junto a su sobre para la tienda, " +
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
                sb.AppendLine($"      \"position\": \"{c.position}\",");
                sb.AppendLine($"      \"rarity\": \"{c.rarity}\",");
                sb.AppendLine($"      \"nationality\": \"{c.nationality}\",");
                sb.AppendLine($"      \"countryCode\": \"{c.countryCode}\",");
                sb.AppendLine($"      \"overall\": {c.CalculateOverall()},");
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
            sb.AppendLine("CardId,PlayerName,Position,TeamName,Rarity,Nationality,CountryCode,Stat1,Stat2,Stat3,Stat4,Overall");
            foreach (var c in cardEntries)
            {
                int s1 = c.TacticalLine == TacticalPosition.POR ? c.diving : c.shooting;
                int s2 = c.TacticalLine == TacticalPosition.POR ? c.reflexes : c.passing;
                int s3 = c.TacticalLine == TacticalPosition.POR ? c.handling : c.defending;
                int s4 = c.TacticalLine == TacticalPosition.POR ? c.positioning : c.dribbling;
                sb.AppendLine($"{c.cardId},{c.playerName},{c.position},{c.teamName},{c.rarity},{c.nationality},{c.countryCode},{s1},{s2},{s3},{s4},{c.CalculateOverall()}");
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

                    string nat = tokens.Length > 5 && !string.IsNullOrWhiteSpace(tokens[5]) ? tokens[5].Trim() : "España";
                    string code = tokens.Length > 6 && !string.IsNullOrWhiteSpace(tokens[6]) ? tokens[6].Trim().ToUpperInvariant() : "ES";

                    var newEntry = new CardWizardEntry
                    {
                        cardId = id,
                        playerName = name,
                        position = pos,
                        teamName = team,
                        rarity = rar,
                        nationality = nat,
                        countryCode = code
                    };

                    newEntry.ApplyPresetStats();

                    if (tokens.Length > 7 && int.TryParse(tokens[7].Trim(), out int s1))
                    {
                        if (newEntry.TacticalLine == TacticalPosition.POR) newEntry.diving = s1;
                        else newEntry.shooting = s1;
                    }
                    if (tokens.Length > 8 && int.TryParse(tokens[8].Trim(), out int s2))
                    {
                        if (newEntry.TacticalLine == TacticalPosition.POR) newEntry.reflexes = s2;
                        else newEntry.passing = s2;
                    }
                    if (tokens.Length > 9 && int.TryParse(tokens[9].Trim(), out int s3))
                    {
                        if (newEntry.TacticalLine == TacticalPosition.POR) newEntry.handling = s3;
                        else newEntry.defending = s3;
                    }
                    if (tokens.Length > 10 && int.TryParse(tokens[10].Trim(), out int s4))
                    {
                        if (newEntry.TacticalLine == TacticalPosition.POR) newEntry.positioning = s4;
                        else newEntry.dribbling = s4;
                    }
                    if (tokens.Length > 11 && int.TryParse(tokens[11].Trim(), out int ovr))
                    {
                        newEntry.manualOverall = ovr;
                    }

                    cardEntries.Add(newEntry);
                    imported++;
                }
            }

            selectedTab = 0; // Regresar a la tabla visual
            EditorUtility.DisplayDialog("Importación Completada", $"Se importaron exitosamente {imported} cartas con sus nacionalidades y estadísticas.", "Aceptar");
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
