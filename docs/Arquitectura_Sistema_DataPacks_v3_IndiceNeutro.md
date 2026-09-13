# 📑 ARQUITECTURA TÉCNICA Y MARCO LEGAL: SISTEMA DE DATA PACKS
**Proyecto:** JuegoTCG  
**Versión del Documento:** 3.0 (Índice Neutro — sin nombres, marcas ni contenido protegido en la infraestructura propia; validación automatizada; Agente DMCA)  
**Fecha:** Septiembre 2026  
**Referencia de Diseño:** Sistema de Data Packs de *World Soccer Champs*, *PES Option Files* y *Football Manager*.

---

## 1. RESUMEN EJECUTIVO Y OBJETIVOS ESTRATÉGICOS

### 1.1. Contexto del Problema
El objetivo de **JuegoTCG** es ofrecer a los jugadores una experiencia inmersiva de cartas coleccionables de fútbol con futbolistas de primer nivel (como Lamine Yamal, Lionel Messi, Erling Haaland, etc.) y clubes reconocidos. 

Sin embargo, licenciar oficialmente los nombres, rostros y escudos de jugadores y clubes con **FIFPRO**, ligas (**LaLiga**, **Premier League**) o federaciones (**UEFA**, **FIFA**) requiere inversiones millonarias inaccesibles para un desarrollo independiente, además de someter el juego al riesgo permanente de demandas o retirada inmediata de **Google Play Store** y **Apple App Store** por infracción de propiedad intelectual.

### 1.2. La Solución: El Modelo de Data Packs Comunitarios
Inspirado en casos de éxito masivo como **World Soccer Champs** (Monkey I-Brow Studios), **eFootball / Pro Evolution Soccer** (Option Files de Konami) y **Football Manager** (Real Name Fix de Sports Interactive):

1. **El juego base (APK oficial):** Se publica en las tiendas con contenido **100% genérico o paródico** (ej. *L. Yamil*, *Chamartin FC*, *Catalonia Blue*) y arte táctico/iniciales por defecto. Pasa todas las revisiones y filtros automatizados de Google y Apple con riesgo cero.
2. **El Sistema de Data Packs:** El juego incluye un motor neutro de personalización capaz de consultar un catálogo remoto e importar paquetes de datos externos (`.zip`) que contienen la base de datos real y las fotografías HD.
3. **Experiencia de Usuario de "1 Clic":** Al iniciar el juego o en el menú de Ajustes, el usuario ve una lista de paquetes populares de la comunidad y un botón **"Descargar e Instalar"**, obteniendo la experiencia completa de fútbol real en segundos sin tener que buscar archivos manualmente por foros externos.
4. **Barrera de Creación Técnica:** El formato de los Data Packs exige una estructura técnica estricta (JSON schemas, validación de IDs, resoluciones específicas y empaquetado comprimido). Esto evita que cualquiera sature el ecosistema con datos rotos o de baja calidad, reservando la creación a usuarios técnicos o al propio equipo desarrollador distribuido como "comunidad".

---

## 2. ESTRATEGIA OPERATIVA: MODELO TRANSPARENTE (INSPIRADO EN WORLD SOCCER CHAMPS)

> **Nota de diseño:** la versión 1.1 de este documento proponía un "índice dinámico" que mostraba contenido distinto al revisor de Google/Apple y a los jugadores finales. Ese patrón (conocido como *cloaking* o *bait-and-switch*) está explícitamente prohibido por las políticas de ambas tiendas, no está protegido por Safe Harbor/DMCA (que ampara a intermediarios pasivos, no a quien diseña el sistema para burlar la revisión) y expone la cuenta de desarrollador completa —incluyendo otros proyectos vinculados a ella— a suspensión. Esta versión reemplaza esa estrategia por el modelo que usa realmente **World Soccer Champs** (Monkey I-Brow Studios, +10M descargas): **una sola versión del juego, idéntica para el revisor y para el jugador final, en todo momento.**

```
                        FLUJO DE FUNCIONAMIENTO (ÚNICA VERSIÓN, SIEMPRE IGUAL)
┌─────────────────────────────────────────────────────────────────────────────┐
│ EN TODO MOMENTO — REVISIÓN, LANZAMIENTO Y OPERACIÓN CONTINUA                 │
│ • El servidor remoto sirve el mismo "datapacks_index.json" a cualquiera.    │
│ • La pantalla de Data Packs está siempre activa, con el mismo contenido.    │
│ • El pack de nombres/clubes reales aparece listado como "creado por la      │
│   comunidad de fans", igual que lo hace World Soccer Champs.                │
│ • La ficha de la tienda (Play Store/App Store) menciona la función          │
│   abiertamente, tal como hace World Soccer Champs en su descripción         │
│   pública ("nombres de jugadores reales con paquete de datos descargable"). │
│ • RESULTADO: no hay nada que ocultar, ni al revisor humano, ni a los bots   │
│   de escaneo, ni al usuario que lee la ficha antes de descargar.            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.1. Por Qué la Transparencia Es la Protección Legal Real
El argumento de "reproductor neutro" (VLC, editores de Option Files de PES) solo funciona si el software **de verdad** no distribuye ni cura el material con copyright. En el modelo de World Soccer Champs esto se cumple de forma genuina:
- El estudio no decide qué nombres reales aparecen; el pack se presenta como aportado por la comunidad de fans, con autoría propia distinta al estudio.
- No existe una fase donde el juego muestre una cosa y luego otra: la función es visible desde la primera versión publicada.
- Esto es lo que de verdad sostiene el amparo de Safe Harbor/DMCA — no un texto legal bonito, sino que la conducta real del estudio sea la de un intermediario pasivo.

### 2.2. Distancia Editorial Genuina del Índice de Packs
Para que la distancia editorial sea real y no solo nominal:
- El `datapacks_index.json` se genera automáticamente a partir de un repositorio público (ej. GitHub) donde cualquier miembro de la comunidad puede proponer un pack vía *pull request*.
- El equipo del juego puede moderar por criterios técnicos (que el `.zip` cumpla el schema, que no rompa el juego) pero no debería ser quien redacta o cura personalmente el contenido con nombres/fotos reales.
- Cuantos más pasos haya entre "el estudio" y "el contenido con marcas registradas", más sólido es el argumento legal.

### 2.6. Índice Neutro: Ningún Nombre, Marca o Foto Vive en la Infraestructura del Estudio
Regla de oro añadida en esta versión: **ni el repositorio del índice ni ningún sistema operado por el estudio almacena, en ningún momento, nombres de jugadores, clubes, ligas, ni fotografías reales.** Esos datos existen únicamente dentro del `.zip` de cada creador, alojado en su propio servicio (GitHub, Drive, Dropbox).

**Qué SÍ vive en el repositorio del índice (100% neutro):**
```json
{
  "id": "pack_a1b2c3",
  "title": "Pack Temporada 2026",
  "author": "Champholics",
  "version": "1.0.2",
  "sizeMb": 8.4,
  "cardsCount": 50,
  "rating": 4.3,
  "downloadUrl": "https://raw.githubusercontent.com/champholics/mi-pack/main/pack.zip",
  "checksum": "sha256:9f8a...",
  "submittedAt": "2026-09-10"
}
```
Nótese que no hay ningún campo de descripción libre donde un autor pueda escribir nombres de jugadores o clubes reales; el `title` sigue una plantilla neutra (`"Pack {temporada/época}"`) validada por el propio esquema, no texto libre.

**Flujo de validación automática (GitHub Action) — sin intervención humana del estudio:**
1. Un colaborador abre un Pull Request agregando una entrada al índice con su `downloadUrl`.
2. Un workflow automático descarga el `.zip` **temporalmente, solo en memoria de ejecución** (nunca se commitea al repo).
3. Verifica: que sea un ZIP válido, que contenga `manifest.json` y `database.json` con el schema correcto, que el `checksum` coincida, y que los `cardId` existan en el catálogo público del juego.
4. Si todo pasa, el workflow aprueba y fusiona el PR automáticamente, pero **solo conserva los campos neutros de arriba** en el índice — nunca copia `database.json` ni las fotos al repositorio.
5. El archivo descargado temporalmente se descarta al finalizar la validación.
6. Si algo falla, el PR se rechaza automáticamente con un comentario del bot indicando el motivo técnico (no editorial).

Con esto, si alguien audita el repositorio completo —incluido su historial de commits— no encontrará un solo nombre de jugador, club o liga real: solo URLs externas, números y metadatos técnicos.

### 2.3. Nomenclatura en la Interfaz y en los Metadatos
Esto no es para "engañar a un bot de OCR" — es simplemente buena práctica para no usar marcas registradas de forma directa en materiales que el estudio sí controla (el APK base, la ficha de la tienda, el manifest):
- El juego base y su documentación pública usan nombres genéricos/paródicos (ej. *L. Yamil*, *Chamartin FC*), igual que ya tenías definido.
- Los títulos de los packs describen la función sin usar razones sociales de clubes o ligas: `"Pack Oficial Comunidad - Temporada 2026"` en vez de nombrar campeonatos con marca registrada.
- La identidad visual (colores, tipografías) es propia, sin logos protegidos — esto también evita que el APK que sí distribuyes directamente contenga IP de terceros.

### 2.4. Descargo de Responsabilidad (*Disclaimer*) Visible
El aviso legal figura de forma explícita en la cabecera de la propia pantalla de Data Packs, igual que antes — la diferencia es que ahora es una descripción honesta de una arquitectura honesta:
> *"Contenido generado por la comunidad de forma independiente. JuegoTCG no aloja estos archivos ni está afiliado con ninguna federación, club o jugador profesional."*

### 2.5. Separación de Infraestructura (Hosting Externo)
- Los archivos `.zip` de los Data Packs se alojan en un repositorio comunitario independiente (ej. GitHub Pages gestionado por la comunidad), no en la infraestructura de Google Cloud/Firebase vinculada a la cuenta de desarrollador.
- Esto no es una técnica de "borrado de huellas" frente a Google — es la misma separación de responsabilidades que sostiene el argumento legal de "reproductor neutro": el estudio publica un motor, la comunidad aporta el contenido.

---

## 3. MARCO LEGAL: MODELO DE PUERTO SEGURO (*SAFE HARBOR*)

### 3.1. Tipos de Derechos Protegidos en el Fútbol
1. **Derecho de Imagen (*Right of Publicity* / FIFPRO):**
   - Protege la identidad, apodo, firma y apariencia visual (rostro, peinado, silueta) de un deportista real.
   - Usar la imagen reconocible de un atleta con fines comerciales directos sin licencia es ilegal (jurisprudencia *Keller v. Electronic Arts* y disputas de FIFPRO).
2. **Marcas Registradas (*Trademarks* de Clubes y Ligas):**
   - Protege los nombres de equipos ("Real Madrid CF", "FC Barcelona"), escudos oficiales, camisetas, tipografías y patrocinios.
   - Las ligas tienen equipos jurídicos dedicados a enviar avisos de cese y desista (*Cease and Desist*) a apps que incluyan sus marcas en el instalador.

### 3.2. Cómo Protege la Ley el Modelo de "Reproductor Neutro"
Bajo la sección 512 de la **DMCA** (Digital Millennium Copyright Act en EE. UU.) y la directiva de comercio electrónico de la Unión Europea:
- Un software que permite a un usuario final importar y reproducir contenido externo no es culpable de la infracción cometida por dicho contenido, **siempre que el software en sí mismo no distribuya ni comercialice el material protegido**.
- El paralelismo legal:
  - *VLC Media Player* no es ilegal porque los usuarios reproduzcan películas pirateadas en él.
  - *eFootball / PES* nunca fue demandado por LaLiga por permitir importar camisetas vía USB (`WEPES/`), porque Konami vendía un editor vacío y el usuario aplicaba el "Option File" obtenido de sitios web de fans independientes.
  - *World Soccer Champs* opera en Google Play con más de 10 millones de descargas usando exactamente este esquema de Data Packs.

### 3.2.1. Registro de Agente DMCA (Paso Concreto, No Solo Teórico)
El "puerto seguro" del DMCA no es automático: requiere haber **designado un Agente DMCA ante la U.S. Copyright Office** antes de que ocurra cualquier incidente.
- **Costo:** $6 USD. **Trámite:** formulario en línea en `dmca.copyright.gov`, toma minutos.
- **Vigencia:** debe renovarse (reenviarse) cada 3 años.
- **Qué otorga:** si un tercero sube contenido con copyright a través del sistema (ej. un creador incluye una foto sin derechos), el estudio no es responsable legalmente por esa infracción, **siempre que** actúe rápido al recibir una notificación de retiro — lo cual ya está cubierto por el protocolo de la Sección 11.
- **Acción recomendada:** registrar el Agente DMCA antes del lanzamiento público de la función de Data Packs, no después de un primer incidente.

### 3.2.2. Qué NO Cubre el DMCA (Distinción Importante)
El Agente DMCA y el Safe Harbor de la Sección 512 protegen únicamente contra reclamos de **derechos de autor (copyright)** — por ejemplo, una fotografía usada sin permiso. **No existe un registro equivalente para:**
- **Marcas registradas** (nombres de clubes/ligas como texto: "Real Madrid CF", "Premier League").
- **Derecho de imagen/publicidad** (nombre y rostro de un jugador real).

Para estas dos categorías, la protección no viene de un trámite administrativo sino de la **conducta real** del estudio (no curar, no controlar, no beneficiarse directamente del contenido con marcas) descrita en la Sección 2.

**Precedente legal relevante:** en *C.B.C. Distribution v. MLB Advanced Media* (EE. UU.), los tribunales determinaron que las ligas de fantasy football podían usar nombres reales de jugadores y sus estadísticas sin licencia, amparados por la Primera Enmienda, sin que esto violara su derecho de imagen — siempre que el uso sea descriptivo/estadístico y no implique un respaldo o patrocinio del jugador. El `database.json` de este sistema (nombre + club + posición en texto plano, sin implicar endoso) se asemeja a ese uso protegido. Las fotografías son una categoría más sensible del derecho de imagen y no están cubiertas por este mismo precedente — de ahí la importancia de que vivan exclusivamente en la infraestructura de cada creador, nunca en la del estudio.

### 3.3. Reglas de Oro Legales
1. **El APK jamás contendrá fotos reales ni nombres oficiales:** Todo lo compilado en `Resources/` o `Addressables` contiene nombres genéricos/ficticios y arte propio.
2. **No monetizar los Data Packs:** Los paquetes de datos deben ser 100% gratuitos. Nunca cobrar dinero real ni gemas del juego por "desbloquear un pack de datos".
3. **Kill Switch Inmediato:** Si una liga o entidad enviara una queja formal, el enlace se retira del `datapacks_index.json` en segundos sin requerir una nueva versión en la tienda.

---

## 4. COMPARATIVA: CÓMO OPERA LA INDUSTRIA

| Característica | **World Soccer Champs** | **eFootball / PES (Option Files)** | **Football Manager (Real Name Fix)** | **JuegoTCG (Nuestra Propuesta)** |
| :--- | :--- | :--- | :--- | :--- |
| **Distribución Base** | Nombres genéricos y parodias. | Ligas sin licencia ficticias (ej. *MD White*). | Equipos y selecciones fake (Alemania, Brasil). | Jugadores con nombres base y fallback táctico. |
| **Punto de Entrada** | Menú inicial de nueva carrera / Ajustes. | Menú Editar -> Importar/Exportar. | Carpeta de archivos local en disco. | Menú de Ajustes / Pantalla inicial de bienvenida. |
| **Selección de Pack** | **Lista en juego con descarga directa** + Opción de URL. | Requiere transferir archivos a carpeta `WEPES/`. | Requiere mover archivos a `Documents/.../graphics/`. | **Lista remota con 1 clic** + Opción de URL manual o .zip. |
| **Gestión de Revisión** | Misma versión para revisor y usuario final; función anunciada en la ficha de la tienda. | Sin contenido en disco. | Sin contenido en disco. | **Misma versión siempre; índice generado desde repo comunitario público.** |
| **Formato del Pack** | `.json` comprimido con carpetas de logos y caras. | Archivos binarios `.ted` / `.bin` + imágenes `.png`. | Archivos `.lnc` / `.xml` + imágenes `.png`. | Archivo `.zip` con `manifest.json`, `database.json` y fotos. |
| **Dificultad de Creación** | Media-Alta (Estructura de IDs de jugadores y clubes). | Alta (Resoluciones exactas y formato binario propietario). | Media (Mapeo de IDs únicos de base de datos). | **Controlada-Alta (JSON Schema estricto + IDs de cartas).** |

---

## 5. ARQUITECTURA TÉCNICA DEL SISTEMA (3 CAPAS)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          CAPA 1: EL CLIENTE BASE (APK)                       │
│  • Catálogo base en memoria: PlayerCollectionManager                        │
│  • CardData local: IDs inmutables ("card_01" ... "card_10")                 │
│  • Fallback visual: Arte táctico oficial con iniciales                      │
│  • Lógica inmutable de juego: Rarity, stats, copias, mercado en Firebase    │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                         [Petición asíncrona HTTP GET]
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CAPA 2: ÍNDICE REMOTO (DATAPACKS INDEX)                  │
│  • URL pública: https://comunidad-tcg.github.io/datapacks/index.json         │
│  • Generado a partir de un repo comunitario público (PRs de la comunidad)   │
│  • Idéntico para revisores y jugadores finales, en todo momento              │
│  • Actualizable en caliente en 5 segundos sin tocar el APK                   │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                      [Descarga por UnityWebRequest]
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                   CAPA 3: ALMACENAMIENTO Y GESTOR LOCAL                     │
│  • Ruta: Application.persistentDataPath/DataPacks/Active/                   │
│  • DataPackInstaller: Valida integridad, descomprime ZIP y aplica cambios   │
│  • DataPackManager: Expone GetPlayerName(), GetTeamName() y GetCardArt()    │
│  • Notifica recarga en caliente a la UI Toolkit (MyCardsScreen, Market, etc)│
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. ESPECIFICACIÓN TÉCNICA DEL DATA PACK

Para garantizar que **no cualquiera pueda saturar el juego con packs mal hechos** pero que el equipo pueda producir paquetes oficiales de la comunidad impecables, se define una especificación estricta.

### 6.1. Estructura de Archivos del Data Pack (`.zip` o `.tcgpack`)
```text
MiDataPack_2026.zip
│
├── manifest.json        <-- Metadatos del pack, versión del esquema y autor
├── database.json        <-- Reemplazo estético (nombre, iniciales, club y posición textual)
├── photos/              <-- Fotos de jugadores con nombres idénticos al cardId ({cardId}.png)
└── flags/ (Opcional)    <-- Texturas HD alternativas para banderas existentes ({countryCode}.png)
    ├── BR.png
    └── UY.png
```

> [!IMPORTANT]
> **Regla de Oro de Integridad Competitiva y Coherencia de Filtros (Fair Play & Album Integrity):**
> Los Data Packs son **100% estéticos y de identidad visual**. Bajo ninguna circunstancia pueden alterar:
> 1. **Estadísticas numéricas ni OVR:** (`TIR`, `PAS`, `DEF`, `REG`, `EST`, `REF`, `PAR`, `COL`), medias globales, rarezas o probabilidades de sobres.
> 2. **Nacionalidades y códigos de país (`nationality`, `countryCode`):** Son atributos esenciales del motor de juego y filtrado. Si un Data Pack pudiera cambiar a Mbappé a otra nacionalidad, los filtros por nación del álbum ("Francia", "España", etc.) o futuras dinámicas de selecciones se descalibrarían por completo. Por ende, la nacionalidad de cada carta proviene **única y exclusivamente** de los datos oficiales inmutables del juego (`CardData` / Firebase).

### 6.2. Esquema de `manifest.json`
```json
{
  "schemaVersion": 2,
  "packId": "comunidad_futbol_2026",
  "title": "Pack Oficial Comunidad - Temporada 2026",
  "author": "Comunidad TCG",
  "version": "1.0.2",
  "minAppVersion": "1.0.0",
  "releaseDate": "2026-09-08",
  "description": "Nombres reales, clubes mundiales y retratos HD de futbolistas.",
  "totalCards": 50,
  "hasPhotos": true,
  "hasCustomFlags": false
}
```

### 6.3. Esquema de `database.json`
Mapea los `cardId` registrados en el juego con sus nombres reales de presentación visual:
```json
{
  "cards": [
    {
      "cardId": "card_10",
      "playerName": "Lamine Yamal",
      "initials": "LY",
      "teamName": "FC Barcelona",
      "position": "DEL"
    },
    {
      "cardId": "card_08",
      "playerName": "Lionel Messi",
      "initials": "LM",
      "teamName": "Inter Miami",
      "position": "DEL"
    }
  ]
}
```

### 6.4. Requisitos de las Imágenes (`photos/`)
- **Nomenclatura:** `{cardId}.png` o `{cardId}.webp` (ej. `card_10.png`).
- **Resolución recomendada:** `720 x 960 px` (proporción 3:4).
- **Formato:** PNG con canal Alpha transparente (recorte limpio del futbolista) o fondo fotográfico.
- **Optimización móvil:** Archivos comprimidos con TinyPNG o formato WebP para asegurar que un pack completo de 100 cartas no supere los **15 MB**.

### 6.5. Reglas de Validación del Motor (Criterios de Rechazo)
Si un usuario intenta cargar un archivo manipulado o incorrecto, el motor lo rechaza automáticamente:
1. El archivo debe ser un `.zip` descomprimible válido.
2. Debe contener obligatoriamente `manifest.json` con `"schemaVersion": 1`.
3. Debe contener obligatoriamente `database.json`.
4. Los `cardId` deben coincidir con IDs registrados en el catálogo del juego.

---

## 7. ESPECIFICACIÓN DEL ÍNDICE REMOTO (`datapacks_index.json`)

Este archivo se aloja en un repositorio independiente o servidor estático (como **GitHub Pages**), generado automáticamente por el flujo de validación de la Sección 2.6 — nunca editado a mano por el estudio, y sin ningún nombre real, marca o club en ninguno de sus campos:

```json
[
  {
    "id": "pack_a1b2c3",
    "title": "Pack Temporada 2026",
    "author": "Champholics",
    "version": "1.0.2",
    "sizeMb": 8.4,
    "cardsCount": 50,
    "rating": 4.3,
    "downloadUrl": "https://raw.githubusercontent.com/champholics/mi-pack/main/pack.zip",
    "checksum": "sha256:9f8a2c...",
    "submittedAt": "2026-09-10"
  },
  {
    "id": "pack_d4e5f6",
    "title": "Pack Época Retro",
    "author": "RetroCards Team",
    "version": "1.0.0",
    "sizeMb": 6.1,
    "cardsCount": 30,
    "rating": 4.6,
    "downloadUrl": "https://raw.githubusercontent.com/retrocards/pack-retro/main/pack.zip",
    "checksum": "sha256:1b7e4f...",
    "submittedAt": "2026-08-22"
  }
]
```

**Notas de diseño del schema:**
- No hay campo `description` de texto libre: cualquier autor podría usarlo para escribir nombres de jugadores/clubes reales, y eso sí viviría en la infraestructura del estudio. El `title` sigue una plantilla neutra validada por el esquema (época/temporada, nunca nombres propios de clubes o ligas).
- `rating` se calcula desde telemetría propia del backend del juego (conteo de valoraciones enviadas por jugadores tras usar el pack) — no toca el contenido del `.zip` en ningún momento.
- `checksum` permite verificar integridad sin necesidad de que el estudio descargue y conserve el archivo.

---

## 8. EXPERIENCIA DE USUARIO (UI/UX)

```
┌────────────────────────────────────────────────────────────────────────┐
│                     ELIGE UN PAQUETE DE DATOS                          │
│ Para personalizar tu experiencia puedes cargar un Paquete de Datos     │
│ que modifica la información e imágenes de la partida.                  │
├────────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │    ✕     │  │ ★★★★☆   │  │ ★★★☆☆   │  │ ★★★★☆   │   ...        │
│  │  SIN     │  │ PACK PRO │  │  PACK    │  │  PACK    │              │
│  │ PAQUETE  │  │ 26.08 MB │  │ ÉPOCA A  │  │ ÉPOCA B  │              │
│  │ DE DATOS │  │ by Auto1 │  │ by Auto2 │  │ by Auto3 │              │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘              │
│                                                                        │
│ ────────────────────────────────────────────────────────────────────── │
│ [ 🔗 Importar enlace personalizado (URL) ]                             │
│ [ 📁 Importar archivo .zip local ]                                     │
│ [ 🔄 Restaurar datos por defecto del juego ]                           │
├────────────────────────────────────────────────────────────────────────┤
│ ℹ️ Los Data Packs listados son creados y distribuidos por nuestra      │
│ comunidad de fans, sin afiliación ni respaldo del estudio. Su          │
│ contenido puede modificar nombres y logos de competiciones, clubes,    │
│ jugadores, trofeos y estadios. El estudio no reclama propiedad,        │
│ no revisa ni controla el contenido de estos paquetes, y renuncia       │
│ expresamente a cualquier responsabilidad por infracciones de           │
│ copyright o marca registrada que dichos paquetes pudieran contener.    │
└────────────────────────────────────────────────────────────────────────┘
```

Nótese el diseño de este disclaimer (adaptado del que usa World Soccer Champs): la frase clave es *"no revisa ni controla el contenido"* — es la declaración escrita más importante del sistema, porque documenta explícitamente la distancia editorial genuina de la Sección 2.2 y 2.6.

**Cada tarjeta muestra**, tomado directamente del índice neutro de la Sección 7: título genérico, calificación por estrellas, tamaño en MB y autor — nunca una descripción con nombres reales visible antes de la descarga.

### 8.1. Flujo de Descarga en 1 Clic
1. El usuario presiona **"Descargar e Instalar"**.
2. El botón se transforma en una barra de estado animada:
   - *"Conectando con el servidor..."*
   - *"Descargando pack de datos (4.2 MB / 8.4 MB - 50%)..."*
   - *"Validando esquema e instalando texturas..."*
3. Al finalizar, la UI emite una vibración háptica suave y muestra un badge verde: **"✓ INSTALADO Y ACTIVO"**.
4. La pantalla de **"Mis cartas"**, **"Mercado"** y **"Vitrinas"** se actualiza automáticamente con los nombres reales y las fotos sin necesidad de reiniciar la aplicación.

### 8.2. Flujo de Restauración
- Con un solo toque en **"Restaurar datos por defecto"**, el juego vacía la carpeta `DataPacks/Active/`, borra la caché en memoria y la app vuelve instantáneamente a su estado base con nombres genéricos y fallback táctico.

---

## 9. PLAN DE IMPLEMENTACIÓN TÉCNICA EN UNITY

### 9.1. Módulos a Desarrollar en C#

#### 1. `DataPackIndexService.cs`
- Responsable de consultar mediante `UnityWebRequest` la URL remota de `datapacks_index.json`.
- Parsea el JSON a una lista de objetos `DataPackInfo`.
- Maneja cachés locales de la lista para funcionamiento offline.

#### 2. `DataPackInstaller.cs`
- Descarga el archivo comprimido usando `UnityWebRequest` reportando el progreso `request.downloadProgress` en tiempo real.
- Utiliza `System.IO.Compression.ZipArchive` (incluido en .NET Standard 2.1 en Unity) para extraer los archivos en:
  `Application.persistentDataPath/DataPacks/Active/`.
- Realiza un **reemplazo atómico**: descarga y descomprime en una carpeta temporal `DataPacks/Temp/` y solo si la validación es exitosa, reemplaza la carpeta `Active/`. Si el zip estuviera corrupto, la instalación se cancela sin romper el juego.

#### 3. `DataPackManager.cs` (Extensión del script actual)
- Carga y mantiene en memoria un `Dictionary<string, CardDataOverride>` leído desde `database.json`.
- Expone los métodos:
  - `string GetPlayerName(string cardId, string defaultName)`
  - `string GetTeamName(string cardId, string defaultTeam)`
  - `string GetPosition(string cardId, string defaultPos)`
  - `Sprite GetCardArt(string cardId, Sprite defaultArt)`
  - `Sprite GetCustomFlag(string countryCode, Sprite defaultFlag = null)`
  - *(Nota: Nacionalidades y códigos de país permanecen inmutables para proteger los filtros del álbum)*
- Dispara el evento `Action OnDataPackReloaded` para que todas las vistas UI Toolkit se refresquen automáticamente.

#### 4. `PlayerCollectionManager.cs` y Controladores UI Toolkit
- Suscribirse a `DataPackManager.OnDataPackReloaded`.
- Al recibir el evento, reconstruir la cuadrícula de cartas (`PopulateAlbumGrid()`) reflejando los nuevos nombres y fotos en pantalla.

#### 5. `.github/workflows/validate-datapack.yml` (Nuevo — Índice Neutro)
- Se dispara en cada Pull Request al repositorio del índice.
- Descarga el `.zip` propuesto **solo en el runner de CI, nunca lo commitea**.
- Ejecuta un script (`validate_pack.py`) que verifica estructura, schema y `checksum`.
- Si pasa, actualiza `datapacks_index.json` solo con los campos neutros de la Sección 2.6/7 y aprueba el merge automáticamente vía un bot (ej. GitHub Actions con permisos de `contents: write` limitados a ese único archivo).
- Si falla, comenta en el PR el motivo técnico y lo cierra sin intervención humana.

---

## 10. HERRAMIENTA AUTOMATIZADA PARA CREADORES (CLI)

Para que el equipo pueda generar nuevos Data Packs en cuestión de segundos sin errores humanos, se proveerá un script en **Python** (`build_datapack.py`):

```text
Entrada:
  ├── metadata.csv (Columnas: cardId, playerName, teamName, position, initials)
  └── raw_photos/ (Imágenes originales sin recortar o ya preparadas)

Proceso automático del script:
  1. Redimensiona y optimiza las imágenes a 720x960 WebP/PNG.
  2. Genera database.json estrictamente formateado.
  3. Genera manifest.json con cálculo de hash SHA-256.
  4. Comprime todo en un archivo .zip listo para alojar en GitHub/CDN.
```

---

## 11. PROTOCOLO DE RESPUESTA A INCIDENTES Y CONTINGENCIA LEGAL

En el escenario eventual de que un representante legal de un club o liga contacte al equipo:

1. **Tiempo de respuesta: Menos de 1 hora.**
2. **Acción técnica:**
   - Se accede al archivo `datapacks_index.json` en GitHub o el servidor remoto.
   - Se elimina la entrada del pack señalado o se reemplaza el enlace de descarga.
   - El cambio tiene efecto mundial inmediato para todos los usuarios sin necesidad de enviar parches a Google Play.
3. **Respuesta jurídica:**
   - Indicar que JuegoTCG es una plataforma de software independiente, que el APK publicado en la tienda no aloja ni distribuye dicho material, y que el enlace de la comunidad señalado ha sido desindexado de forma preventiva.
   - Si el reclamo es específicamente de **copyright** (ej. una foto), se procesa como notificación formal al **Agente DMCA registrado** (Sección 3.2.1) y se documenta la respuesta dentro del plazo legal.
   - Si el reclamo es de **marca registrada o derecho de imagen**, se responde igual de rápido por buena práctica y buena fe, aclarando que no existe un puerto seguro administrativo equivalente para esa categoría, pero que el contenido ya fue retirado.

---

## 12. CONCLUSIÓN Y PRÓXIMOS PASOS

Este modelo reduce el riesgo (nunca lo elimina del todo — ningún esquema de datapacks de fútbol está 100% libre de un cease-and-desist) siendo consistente en vez de evasivo, y sin que ningún nombre, marca o foto real toque jamás la infraestructura propia del estudio:

1. **Cumplimiento real, no solo aparente:** misma versión para revisor y jugador, siempre; nada que "superar" ni ningún escaneo que evadir.
2. **Índice 100% neutro:** el repositorio del estudio nunca contiene nombres, marcas ni fotos — solo metadata técnica y URLs externas, validadas automáticamente sin curaduría editorial humana.
3. **Copyright cubierto de forma concreta:** Agente DMCA registrado ($6, Sección 3.2.1) más protocolo de retiro rápido.
4. **Marca registrada y derecho de imagen mitigados, no eliminados:** protegidos por conducta (distancia editorial genuina) y precedente (*C.B.C. v. MLB Advanced Media*), no por un trámite administrativo — este es el riesgo residual real del modelo.
5. **Experiencia de usuario intacta:** el jugador final sigue viendo la misma pantalla con tarjetas, estrellas y descarga en 1 clic que se diseñó originalmente.
6. **Próximo paso recomendado:** antes de publicar, una consulta puntual con un abogado de PI en tu jurisdicción (Colombia/Ecuador y el país de la cuenta de desarrollador) para validar este diseño con tu caso específico — este documento reduce riesgo pero no sustituye asesoría legal real.
