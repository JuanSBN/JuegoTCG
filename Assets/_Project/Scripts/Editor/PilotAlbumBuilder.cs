#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using JuegoTCG.Cards;
using JuegoTCG.Packs;

namespace JuegoTCG.EditorTools
{
    public static class PilotAlbumBuilder
    {
        private const string FolderPath = "Assets/_Project/ScriptableObjects/PilotAlbum";

        [MenuItem("JuegoTCG/Generar Álbum Piloto")]
        public static void BuildPilotAlbum()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
                AssetDatabase.Refresh();
            }

            // 1. Crear Álbum
            AlbumData album = ScriptableObject.CreateInstance<AlbumData>();
            album.albumId = "album_piloto_liga";
            album.albumName = "Liga Genérica Piloto";
            album.albumType = AlbumType.Liga;
            album.rewardCoins = 500;
            album.active = true;

            string albumPath = $"{FolderPath}/Album_Piloto.asset";
            AssetDatabase.CreateAsset(album, albumPath);

            // 2. Definición de 10 Cartas de Prueba distribuidas por las 6 rarezas
            var pilotCardsData = new (string id, string name, string team, string pos, Rarity rarity)[]
            {
                ("card_01", "Mateo Silva", "FC Piloto", "Portero", Rarity.Comun),
                ("card_02", "Lucas Gómez", "FC Piloto", "Defensor", Rarity.Comun),
                ("card_03", "Carlos Pérez", "FC Piloto", "Defensor", Rarity.Comun),
                ("card_04", "Daniel Torres", "FC Piloto", "Mediocampista", Rarity.Comun),
                ("card_05", "Andrés Ríos", "FC Piloto", "Mediocampista", Rarity.Comun),
                ("card_06", "Gabriel Medina", "FC Piloto", "Delantero", Rarity.Especial),
                ("card_07", "Santiago Benítez", "FC Piloto", "Delantero", Rarity.Especial),
                ("card_08", "Valentín Morales", "FC Piloto", "Mediocampista", Rarity.Epica),
                ("card_09", "Esteban Castro", "FC Piloto", "Delantero", Rarity.Legendaria),
                ("card_10", "Álvaro Leyenda", "FC Piloto", "Delantero Estrella", Rarity.Mitica)
            };

            foreach (var data in pilotCardsData)
            {
                CardData card = ScriptableObject.CreateInstance<CardData>();
                card.cardId = data.id;
                card.playerName = data.name;
                card.teamName = data.team;
                card.position = data.pos;
                card.rarity = data.rarity;
                card.albumId = album.albumId;

                string cardPath = $"{FolderPath}/{data.id}_{data.name.Replace(" ", "_")}.asset";
                AssetDatabase.CreateAsset(card, cardPath);
                album.cards.Add(card);
            }

            EditorUtility.SetDirty(album);

            // 3. Crear Sobre Piloto
            PackData pack = ScriptableObject.CreateInstance<PackData>();
            pack.packId = "pack_piloto_gratis";
            pack.packName = "Sobre Piloto Liga";
            pack.albumId = album.albumId;
            pack.cardsPerPack = 5;
            pack.costType = CostType.GratisTiempo;
            pack.costAmount = 0;

            // Pesos % de rareza según GDD 5.2
            pack.comunWeight = 55f;
            pack.especialWeight = 25f;
            pack.epicaWeight = 12f;
            pack.legendariaWeight = 5f;
            pack.miticaWeight = 2f;
            pack.fullArtWeight = 1f;

            string packPath = $"{FolderPath}/Sobre_Piloto.asset";
            AssetDatabase.CreateAsset(pack, packPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[JuegoTCG] ¡Álbum Piloto, 10 Cartas y Sobre generados con éxito en Assets/_Project/ScriptableObjects/PilotAlbum/!</color>");
        }
    }
}
#endif
