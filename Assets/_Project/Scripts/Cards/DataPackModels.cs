using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuegoTCG.Cards
{
    /// <summary>
    /// Metadatos generales del Data Pack (leído desde manifest.json).
    /// </summary>
    [Serializable]
    public class DataPackManifest
    {
        public int schemaVersion = 2;
        public string packId = "";
        public string title = "";
        public string author = "";
        public string version = "1.0.0";
        public string minAppVersion = "1.0.0";
        public string releaseDate = "";
        public string description = "";
        public int totalCards = 0;
        public bool hasPhotos = true;
        public bool hasCustomFlags = false;
    }

    /// <summary>
    /// Entrada de sustitución visual de un futbolista dentro de database.json.
    /// Importante: Por integridad competitiva (Fair Play) y consistencia de filtros del álbum,
    /// los Data Packs NO modifican estadísticas, OVR ni nacionalidades/códigos de país.
    /// </summary>
    [Serializable]
    public class DataPackCardEntry
    {
        public string cardId = "";
        public string playerName = "";
        public string initials = "";
        public string teamName = "";
        public string position = "";
    }

    /// <summary>
    /// Contenedor raíz de la base de datos de sustituciones cosméticas de futbolistas.
    /// </summary>
    [Serializable]
    public class DataPackDatabase
    {
        public List<DataPackCardEntry> cards = new List<DataPackCardEntry>();
    }

    /// <summary>
    /// Elemento de catálogo remoto para listar paquetes descargables (leído desde datapacks_index.json).
    /// </summary>
    [Serializable]
    public class DataPackIndexItem
    {
        public string id = "";
        public string title = "";
        public string author = "";
        public string version = "";
        public string description = "";
        public string downloadUrl = "";
        public string sizeMb = "";
        public int cardsCount = 0;
        public bool isRecommended = false;
        public string bannerColor = "#E8A820";
    }

    /// <summary>
    /// Contenedor de lista para deserialización JSON de catálogo remoto.
    /// </summary>
    [Serializable]
    public class DataPackIndexRoot
    {
        public List<DataPackIndexItem> packs = new List<DataPackIndexItem>();
    }
}
