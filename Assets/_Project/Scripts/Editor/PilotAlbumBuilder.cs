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

        public static void BuildPilotAlbum()
        {
            if (Directory.Exists(FolderPath))
            {
                Directory.Delete(FolderPath, true);
            }
            Directory.CreateDirectory(FolderPath);
            AssetDatabase.Refresh();

            // 1. Crear Álbum
            AlbumData album = ScriptableObject.CreateInstance<AlbumData>();
            album.albumId = "album_piloto_liga";
            album.albumName = "Álbum Estrella Piloto";
            album.albumType = AlbumType.Liga;
            album.rewardCoins = 500;
            album.active = true;

            string albumPath = $"{FolderPath}/Album_Piloto.asset";
            AssetDatabase.CreateAsset(album, albumPath);

            // 2. 10 Cartas de Prueba según la selección del usuario
            var pilotCardsData = new (string id, string name, string team, string pos, Rarity rarity, string nat, string code, int s1, int s2, int s3, int s4, int ovr)[]
            {
                ("card_01", "Vozhina", "FC Piloto", "Portero", Rarity.Comun, "Rusia", "RU", 70, 74, 68, 71, 0),
                ("card_02", "Balogun", "FC Piloto", "Delantero", Rarity.Comun, "Estados Unidos", "US", 78, 66, 32, 76, 0),
                ("card_03", "Diomandé", "FC Piloto", "Defensor", Rarity.Comun, "Costa de Marfil", "CI", 35, 64, 79, 66, 0),
                ("card_04", "James Rodríguez", "FC Piloto", "Mediocampista", Rarity.Comun, "Colombia", "CO", 82, 86, 44, 83, 0),
                ("card_05", "Luis Díaz", "FC Piloto", "Extremo Izquierdo", Rarity.Especial, "Colombia", "CO", 82, 78, 40, 87, 0),
                ("card_06", "Erling Haaland", "FC Piloto", "Delantero", Rarity.Especial, "Noruega", "NO", 93, 70, 45, 82, 88),
                ("card_07", "Cristiano Ronaldo", "FC Piloto", "Delantero", Rarity.Epica, "Portugal", "PT", 91, 78, 38, 85, 87),
                ("card_08", "Lionel Messi", "FC Piloto", "Mediocampista", Rarity.Legendaria, "Argentina", "AR", 88, 92, 35, 93, 90),
                ("card_09", "Kylian Mbappé", "FC Piloto", "Delantero", Rarity.Legendaria, "Francia", "FR", 90, 80, 36, 92, 89),
                ("card_10", "Lamine Yamal", "FC Piloto", "Extremo Estrella", Rarity.Mitica, "España", "ES", 84, 86, 38, 92, 86)
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
                card.nationality = data.nat;
                card.countryCode = data.code;
                card.manualOverall = data.ovr;

                if (card.TacticalLine == TacticalPosition.POR)
                {
                    card.diving = data.s1;
                    card.reflexes = data.s2;
                    card.handling = data.s3;
                    card.positioning = data.s4;
                }
                else
                {
                    card.shooting = data.s1;
                    card.passing = data.s2;
                    card.defending = data.s3;
                    card.dribbling = data.s4;
                }

                string cardPath = $"{FolderPath}/{data.id}_{data.name.Replace(" ", "_")}.asset";
                AssetDatabase.CreateAsset(card, cardPath);
                album.cards.Add(card);
            }

            EditorUtility.SetDirty(album);

            // 3. Crear Sobre Piloto
            PackData pack = ScriptableObject.CreateInstance<PackData>();
            pack.packId = "pack_piloto_gratis";
            pack.packName = "Sobre Estrella Piloto";
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

            Debug.Log("<color=green>[JuegoTCG] ¡Álbum Piloto actualizado con los jugadores solicitados (Lamine Yamal, Messi, Mbappé, Cristiano, Haaland, Luis Díaz, James, Vozhina, Balogun, Diomandé) en Assets/_Project/ScriptableObjects/PilotAlbum/!</color>");
        }
    }
}
#endif
