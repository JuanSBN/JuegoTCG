# 📑 ESPECIFICACIÓN Y GUÍA DE PROPIEDADES DE LAS CARTAS (v2.0)
**Proyecto:** JuegoTCG  
**Fecha:** Septiembre 2026  
**Estado:** Implementado y Verificado en Producción  
**Componentes Afectados:** `CardData`, `CardCatalogItem`, UI Toolkit (`MyCardsScreen`, `ProfileScreen`), uGUI Prefabs (`CardPrefab`), Herramientas de Edición (`AlbumWizardWindow`, `PilotAlbumBuilder`).

---

## 1. RESUMEN EJECUTIVO

Con la actualización v2.0, el sistema de cartas de **JuegoTCG** evoluciona de un esquema plano básico (donde la carta únicamente mostraba Nombre, Equipo, Posición y Rareza) a un estándar profesional inspirado en los juegos de referencia mundial (**EA Sports FC / FIFA Ultimate Team**, **eFootball** y colecciones físicas de **Panini/Topps**).

### Principales novedades introducidas:
1. **Nacionalidad con Bandera Oficial:** Cada carta incluye país y código ISO-3166, renderizando la bandera nacional a la izquierda de la carta.
2. **4 Estadísticas Clave por Posición:** Se reemplaza el texto genérico del pie de la carta por 4 métricas numéricas especializadas (diferenciando jugadores de campo vs porteros).
3. **Media Global (OVR) con Algoritmo Ponderado FIFA:** Cálculo matemático automático del rendimiento general según la posición táctica, con posibilidad de sobrescritura manual.
4. **Nuevo Layout Visual Unificado:** La bandera se sitúa fuera de la pestaña inferior a la izquierda, y las 4 estadísticas se integran dentro del cajetín metálico del marco.
5. **Herramientas de Autoría Actualizadas:** El creador visual de álbumes (`AlbumWizardWindow`) y los scripts de generación admiten la edición de estas propiedades y la importación/exportación masiva desde Excel / CSV.

---

## 2. DETALLE DE LAS NUEVAS PROPIEDADES

### 2.1. Nacionalidad y Código de País
Cada futbolista cuenta con dos atributos de país:
- `nationality` (`string`): Nombre formal del país en español (ej. *"España"*, *"Colombia"*, *"Argentina"*).
- `countryCode` (`string`): Código estándar ISO 3166-1 alpha-2 en mayúsculas (ej. `ES`, `CO`, `AR`, `FR`, `NO`, `PT`, `US`, `CI`, `RU`).

#### Servicio de Banderas (`CountryFlagService`):
- **Carga bajo demanda:** Las banderas se cargan dinámicamente desde `Assets/Resources/Flags/{countryCode}.png`.
- **Caché en memoria:** Se almacena un diccionario `Dictionary<string, Sprite>` para evitar recargas constantes y garantizar 60+ FPS al deslizar listas o abrir sobres.
- **Configuración de textura recomendada:**
  - Formato: PNG con canal alfa (o relación de aspecto 3:2).
  - Tamaño recomendado: 96×64 px o 120×80 px (optimizado para pantallas móviles).
  - Import Type: `Sprite (2D and UI)`, Mesh Type: `Full Rect`.
  - El script `FlagSpriteImporter.cs` configura automáticamente estos parámetros en Unity.

---

### 2.2. Las 4 Estadísticas Clave (Campo vs Portero)

A diferencia de los modelos planos donde todos los jugadores tienen las mismas etiquetas, el sistema clasifica la carta en su línea táctica normalizada (`TacticalPositionHelper.Normalize(position)`):

```
                       CLASIFICACIÓN DE ESTADÍSTICAS
       ┌─────────────────────────────────────────────────────────────┐
       │                   LÍNEA TÁCTICA NORMALIZADA                 │
       └──────────────┬───────────────────────────────┬──────────────┘
                      │                               │
                      ▼                               ▼
       ┌─────────────────────────────┐ ┌─────────────────────────────┐
       │   JUGADOR DE CAMPO          │ │   PORTERO                   │
       │   (DEL, MED, DEF)           │ │   (POR)                     │
       ├─────────────────────────────┤ ├─────────────────────────────┤
       │ • TIR (Tiro / shooting)     │ │ • EST (Estirada / diving)   │
       │ • PAS (Pase / passing)      │ │ • REF (Reflejos / reflexes) │
       │ • DEF (Defensa / defending) │ │ • PAR (Parada / handling)   │
       │ • REG (Regate / dribbling)  │ │ • COL (Colocación / posit.) │
       └─────────────────────────────┘ └─────────────────────────────┘
```

#### Rango y Escala:
- Cada valor oscila entre **1 y 99**.
- En código, se accede uniformemente mediante el método `card.GetDisplayStats()`, el cual devuelve la estructura:
```csharp
public struct CardStatsSummary
{
    public string stat1Name; // "TIR" o "EST"
    public int stat1Value;
    public string stat2Name; // "PAS" o "REF"
    public int stat2Value;
    public string stat3Name; // "DEF" o "PAR"
    public int stat3Value;
    public string stat4Name; // "REG" o "COL"
    public int stat4Value;
}
```

---

### 2.3. Media Global / Overall Rating (OVR)

El sistema incorpora un cálculo ponderado basado en los algoritmos oficiales de EA Sports FC / FIFA:

- **Porteros (`POR`):**  
  $$\text{OVR} = \text{Reflejos} \times 0.30 + \text{Estirada} \times 0.25 + \text{Parada} \times 0.25 + \text{Colocación} \times 0.20$$
- **Defensores (`DEF`):**  
  $$\text{OVR} = \text{Defensa} \times 0.50 + \text{Pase} \times 0.25 + \text{Regate} \times 0.20 + \text{Tiro} \times 0.05$$
- **Mediocampistas (`MED`):**  
  $$\text{OVR} = \text{Pase} \times 0.35 + \text{Regate} \times 0.30 + \text{Tiro} \times 0.20 + \text{Defensa} \times 0.15$$
- **Delanteros (`DEL`):**  
  $$\text{OVR} = \text{Tiro} \times 0.45 + \text{Regate} \times 0.30 + \text{Pase} \times 0.20 + \text{Defensa} \times 0.05$$

#### Sobrescritura Manual (`manualOverall`):
Si en el ScriptableObject o en la base de datos se especifica un número mayor a `0` (ejemplo: `manualOverall = 88`), el sistema utilizará ese valor exacto en lugar del promedio automático.

> [!NOTE]
> **Estado en la UI:** El valor OVR se calcula, persiste y se muestra en la herramienta del creador de álbumes y en las estructuras de datos. Actualmente **NO se dibuja en la carta física** a petición del equipo de arte, ya que se encuentra en fase de diseño la nueva hornada de marcos con espacio exclusivo para la insignia global.

---

## 3. ESQUEMA DE DISEÑO VISUAL Y MAQUETACIÓN

### 3.1. Anatomía Inferior de la Carta
En versiones anteriores, el pie del marco contenía `Equipo`, `Posición` y `Rareza`, mientras que las estadísticas flotaban por encima. En el diseño moderno:

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                    [ FOTO O AVATAR ]                        │
│                                                             │
│              [ Nombre del Jugador con Sombra ]              │
│  ┌────────┬──────────────────────────────────────────────┐  │
│  │   🏳️   │   TIR: 84    PAS: 86    DEF: 38    REG: 92   │  │
│  │ Bandera│      (Dentro de la pestaña del marco)        │  │
│  └────────┴──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

1. **Bandera (`card-flag-image` / `inspect-flag-image` / `featured-flag-image`):**
   - Se ubica **fuera** de la pestaña del marco, a la izquierda.
   - Posición horizontal calibrada: `left: 5.4%`.
   - Centrada verticalmente con respecto a la pestaña.
2. **Pestaña inferior (`card-frame-footer-box` / `inspect-frame-footer-box`):**
   - Aloja exclusivamente las **4 columnas de estadísticas**.
   - Cada columna presenta el nombre de la estadística arriba en negrita (`#2b2d42`) y el valor numérico abajo (`#0c1822`).
   - Se eliminaron los textos redundantes de equipo, posición y rareza.

---

## 4. TABLA DE REFERENCIA: ÁLBUM PILOTO (10 JUGADORES)

A continuación se detalla la configuración oficial de las 10 cartas que componen el Álbum Piloto:

| ID | Jugador | Posición | Rareza | País | Cód | Stat 1 | Stat 2 | Stat 3 | Stat 4 | OVR |
|:---|:---|:---:|:---:|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| `card_01` | **Vozhina** | Portero | Común | Rusia | `RU` | EST: 70 | REF: 74 | PAR: 68 | COL: 71 | **71** |
| `card_02` | **Balogun** | Delantero | Común | Estados Unidos | `US` | TIR: 78 | PAS: 66 | DEF: 32 | REG: 76 | **72** |
| `card_03` | **Diomandé** | Defensor | Común | Costa de Marfil | `CI` | TIR: 35 | PAS: 64 | DEF: 79 | REG: 66 | **70** |
| `card_04` | **James Rodríguez** | Mediocampista | Común | Colombia | `CO` | TIR: 82 | PAS: 86 | DEF: 44 | REG: 83 | **78** |
| `card_05` | **Luis Díaz** | Extremo Izquierdo | Especial | Colombia | `CO` | TIR: 82 | PAS: 78 | DEF: 40 | REG: 87 | **80** |
| `card_06` | **Erling Haaland** | Delantero | Especial | Noruega | `NO` | TIR: 93 | PAS: 70 | DEF: 45 | REG: 82 | **88** |
| `card_07` | **Cristiano Ronaldo** | Delantero | Épica | Portugal | `PT` | TIR: 91 | PAS: 78 | DEF: 38 | REG: 85 | **87** |
| `card_08` | **Lionel Messi** | Mediocampista | Legendaria | Argentina | `AR` | TIR: 88 | PAS: 92 | DEF: 35 | REG: 93 | **90** |
| `card_09` | **Kylian Mbappé** | Delantero | Legendaria | Francia | `FR` | TIR: 90 | PAS: 80 | DEF: 36 | REG: 92 | **89** |
| `card_10` | **Lamine Yamal** | Extremo Estrella | Mítica | España | `ES` | TIR: 84 | PAS: 86 | DEF: 38 | REG: 92 | **86** |

---

## 5. HERRAMIENTAS Y FLUJO DE CREACIÓN DE ÁLBUMES

### 5.1. Asistente Visual (`AlbumWizardWindow.cs`)
Accesible desde la barra de Unity: **`JuegoTCG > 🛠️ Creador de Álbumes (Album Wizard)`**.

- **Tabla Visual Interactiva:** Permite crear y ordenar cartas visualmente, asignando país, código ISO y ajustando las 4 estadísticas mediante controles numéricos.
- **Botón `⚡ Preset Stats`:** Genera estadísticas automáticas calibradas según la rareza y posición:
  - *Común:* ~64 base
  - *Especial:* ~75 base
  - *Épica:* ~83 base
  - *Legendaria:* ~89 base
  - *Mítica:* ~94 base
- **Botón `⚡ Auto-Stats Todas por Rareza`:** Calcula las estadísticas de todo el álbum con 1 solo clic.
- **Botón `📂 Expandir / Colapsar`:** Despliega u oculta las estadísticas de todas las cartas para una edición cómoda.

### 5.2. Importación y Exportación Masiva (Excel / Google Sheets / CSV)
El asistente soporta importar listas copiadas directamente desde hojas de cálculo con las siguientes columnas separadas por tabulador (`\t`) o coma (`, `):

```text
[ID]	[Nombre Jugador]	[Posición]	[Equipo]	[Rareza]	[País]	[CódPaís]	[Stat1]	[Stat2]	[Stat3]	[Stat4]	[OVR]
```

#### Ejemplo listo para copiar y pegar:
```tsv
champ_01	Vinícius Jr.	DEL	Real Madrid	Legendaria	Brasil	BR	89	81	32	92	89
champ_02	Jude Bellingham	MED	Real Madrid	Legendaria	Inglaterra	GB	84	86	78	85	84
champ_03	Thibaut Courtois	POR	Real Madrid	Legendaria	Bélgica	BE	85	90	88	86	89
```

> [!TIP]
> **Retrocompatibilidad:** Si se pega una lista antigua con solo las primeras 5 columnas (`ID`, `Nombre`, `Posición`, `Equipo`, `Rareza`), el sistema automáticamente le asignará país por defecto (`España` / `ES`) y calculará las estadísticas óptimas según su rareza y posición.

---

## 6. GUÍA RÁPIDA: CÓMO AGREGAR UN NUEVO PAÍS / BANDERA

1. **Obtener la bandera:** Descarga o crea la imagen en formato PNG (ejemplo: `BR.png` para Brasil, `UY.png` para Uruguay, `DE.png` para Alemania).
2. **Ubicación:** Guarda el archivo en la carpeta:  
   `Assets/Resources/Flags/{CODIGO_ISO}.png`
3. **Importación:** Al guardar el archivo, Unity ejecuta automáticamente `FlagSpriteImporter.cs`, que lo convierte en un `Sprite (2D and UI)` listo para usar.
4. **Asignación:** En `CardData` o en el `AlbumWizard`, asigna en el campo `countryCode` el mismo código de 2 letras (ej: `BR`). El juego vinculará la bandera automáticamente tanto en Mis Cartas como en Cartas Destacadas.

---

## 7. ARCHIVOS TÉCNICOS INVOLUCRADOS

- **Modelos:**
  - `Assets/_Project/Scripts/Cards/CardData.cs`: ScriptableObject con variables de nacionalidad, stats y cálculo de OVR.
  - `Assets/_Project/Scripts/Cards/PlayerCollectionManager.cs`: `CardCatalogItem` con propiedades en memoria y normalización táctica.
  - `Assets/_Project/Scripts/Cards/CountryFlagService.cs`: Servicio singleton en memoria para carga ágil de banderas.
- **Vistas y Estilos UI Toolkit:**
  - `Assets/_Project/UI/Views/MyCardsScreen.uxml` y `Assets/_Project/UI/Styles/MyCardsScreen.uss`
  - `Assets/_Project/UI/Views/ProfileScreen.uxml` y `Assets/_Project/UI/Styles/ProfileScreen.uss`
  - `Assets/_Project/Scripts/UI/UIToolkitMyCardsController.cs`
  - `Assets/_Project/Scripts/UI/UIToolkitProfileController.cs`
- **Prefabs uGUI y Editores:**
  - `Assets/Resources/CardPrefab.prefab` y `Assets/_Project/Prefabs/Cards/CardPrefab.prefab`
  - `Assets/_Project/Scripts/Editor/CardPrefabBuilder.cs`
  - `Assets/_Project/Scripts/Editor/AlbumWizardWindow.cs`
  - `Assets/_Project/Scripts/Editor/PilotAlbumBuilder.cs`
  - `Assets/_Project/Scripts/Editor/CardDataStatsUpdater.cs`
  - `Assets/_Project/Scripts/Editor/FlagSpriteImporter.cs`
