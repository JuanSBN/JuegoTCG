using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Servicio centralizado para la carga y gestión de los nuevos marcos de cartas divididos en 3 componentes:
    /// Componente 1: Pestaña superior derecha (Media global / OVR).
    /// Componente 2: Marco exterior completo.
    /// Componente 3: Pestaña inferior (Estadísticas y bandera).
    /// </summary>
    public static class CardFrameService
    {
        public struct FrameSet
        {
            public Sprite ovrTab;    // Componente 1
            public Sprite mainFrame; // Componente 2
            public Sprite statsTab;  // Componente 3
        }

        private static readonly Dictionary<Rarity, FrameSet> cache = new Dictionary<Rarity, FrameSet>();

        private static readonly string[] FolderNames = new string[]
        {
            "Común",      // Rarity.Comun = 0
            "Especial",   // Rarity.Especial = 1
            "Epica",      // Rarity.Epica = 2
            "Legendaria", // Rarity.Legendaria = 3
            "Mitica",     // Rarity.Mitica = 4
            "Full Art"    // Rarity.FullArt = 5
        };

        public static FrameSet GetFrames(Rarity rarity)
        {
            if (cache.TryGetValue(rarity, out FrameSet set) && set.mainFrame != null)
            {
                return set;
            }

            int index = Mathf.Clamp((int)rarity, 0, FolderNames.Length - 1);
            string folder = FolderNames[index];

            Sprite s1 = LoadSprite(folder, 1);
            Sprite s2 = LoadSprite(folder, 2);
            Sprite s3 = LoadSprite(folder, 3);

            if (s2 == null && folder == "Común")
            {
                s1 = LoadSprite("Comun", 1);
                s2 = LoadSprite("Comun", 2);
                s3 = LoadSprite("Comun", 3);
            }

            set = new FrameSet { ovrTab = s1, mainFrame = s2, statsTab = s3 };
            cache[rarity] = set;
            return set;
        }

        private static Sprite LoadSprite(string folder, int componentNumber)
        {
            Sprite sp = Resources.Load<Sprite>($"CardFrames/{folder}/{folder} {componentNumber}");
            if (sp != null) return sp;

            if (folder == "Mitica")
            {
                sp = Resources.Load<Sprite>($"CardFrames/Mitica/Mitico {componentNumber}");
                if (sp != null) return sp;
            }

            Sprite[] all = Resources.LoadAll<Sprite>($"CardFrames/{folder}");
            if (all != null)
            {
                foreach (var s in all)
                {
                    if (s.name.EndsWith($" {componentNumber}") || s.name.Contains($"{componentNumber}"))
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        public static Sprite GetMainFrame(Rarity rarity) => GetFrames(rarity).mainFrame;
        public static Sprite GetOvrTab(Rarity rarity) => GetFrames(rarity).ovrTab;
        public static Sprite GetStatsTab(Rarity rarity) => GetFrames(rarity).statsTab;
    }
}
