# Guía Paso a Paso: Creación, Alojamiento y Publicación de Data Packs

Esta guía explica detalladamente el proceso completo para crear, empaquetar, alojar en la nube y publicar un **Data Pack de la Comunidad** para **JuegoTCG Football**, permitiendo que los jugadores lo descarguen en **1 clic** desde la pantalla de Ajustes.

---

## Índice
1. [Reglas de Oro (Fair Play y Filtros de Álbum)](#1-reglas-de-oro-fair-play-y-filtros-de-álbum)
2. [Estructura de Carpetas del Data Pack](#2-estructura-de-carpetas-del-data-pack)
3. [Paso 1: Creación de Archivos Base](#paso-1-creación-de-archivos-base)
   - [A. El archivo `manifest.json`](#a-el-archivo-manifestjson)
   - [B. El archivo `database.json`](#b-el-archivo-databasejson)
   - [C. La carpeta de fotos `photos/`](#c-la-carpeta-de-fotos-photos)
   - [D. La carpeta de banderas `flags/` (Opcional)](#d-la-carpeta-de-banderas-flags-opcional)
4. [Paso 2: Comprimir en archivo `.zip`](#paso-2-comprimir-en-archivo-zip)
5. [Paso 3: Alojamiento Externo (Subir a Internet)](#paso-3-alojamiento-externo-subir-a-internet)
6. [Paso 4: Publicar en la Lista de "Recomendados" (Consola de Firebase)](#paso-4-publicar-en-la-lista-de-recomendados-consola-de-firebase)
7. [Paso 5: Cómo lo Experimenta el Jugador en el Móvil](#paso-5-cómo-lo-experimenta-el-jugador-en-el-móvil)
8. [Checklist Final antes de Publicar](#checklist-final-antes-de-publicar)

---

## 1. Reglas de Oro (Fair Play y Filtros de Álbum)

> [!IMPORTANT]
> Los Data Packs en JuegoTCG son **estrictamente cosméticos y de presentación visual**.
> 
> - **QUÉ SÍ PUEDE MODIFICAR UN DATA PACK:**
>   - Nombre del jugador (`playerName`).
>   - Iniciales para el avatar por defecto (`initials`).
>   - Nombre del equipo o club (`teamName`).
>   - Posición textual (`position`, ej: `DEL`, `MED`, `DEF`, `POR`).
>   - Retratos o fotos en alta definición (`photos/{cardId}.png`).
>   - Texturas HD opcionales para banderas (`flags/{countryCode}.png`).
> 
> - **QUÉ NUNCA PUEDE MODIFICAR UN DATA PACK (INMUTABLES POR MOTOR):**
>   - **Estadísticas y OVR:** Tiro, Pase, Defensa, Regate, Portería y Media Global no pueden ser alteradas.
>   - **Nacionalidades y Códigos de País (`nationality`, `countryCode`):** No se alteran para evitar descalibrar los filtros por nación del álbum (ej: buscar jugadores de Francia, España, etc.).
>   - **Rarezas y Probabilidades de Sobres:** Controladas exclusivamente por el backend oficial.

---

## 2. Estructura de Carpetas del Data Pack

Crea una carpeta en tu computadora (por ejemplo, `MiPrimerDataPack`). La estructura interna debe ser exactamente la siguiente:

```text
MiPrimerDataPack/
├── manifest.json         <-- Metadatos del paquete y versión
├── database.json         <-- Sustituciones de texto (nombres y clubes)
├── photos/               <-- Fotos de los futbolistas
│   ├── card_01.png
│   ├── card_02.png
│   └── card_10.png
└── flags/ (Opcional)     <-- Banderas personalizadas
    ├── BR.png
    └── ES.png
```

---

## Paso 1: Creación de Archivos Base

### A. El archivo `manifest.json`
Crea un archivo de texto llamado `manifest.json` en la raíz de tu carpeta con la siguiente estructura:

```json
{
  "schemaVersion": 2,
  "packId": "comunidad_futbol_real_v1",
  "title": "Real Names & Photos Community Pack",
  "author": "Comunidad TCG",
  "version": "1.0.0",
  "minAppVersion": "0.1.0",
  "releaseDate": "2026-09-11",
  "description": "Sustituye nombres ficticios por futbolistas reales con sus clubes oficiales y fotos HD.",
  "totalCards": 18,
  "hasPhotos": true,
  "hasCustomFlags": false
}
```

#### Descripción de campos:
| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| `schemaVersion` | Número | Siempre debe ser `2`. |
| `packId` | Texto | Identificador único sin espacios (ej: `liga_espanola_2026`). |
| `title` | Texto | Nombre visible que verá el jugador en la lista. |
| `author` | Texto | Nombre del creador o comunidad modder. |
| `version` | Texto | Versión semántica del paquete (ej: `1.0.0`, `1.1.0`). |
| `minAppVersion` | Texto | Versión mínima compatible del juego (`0.1.0`). |
| `description` | Texto | Breve resumen de lo que incluye el pack. |
| `totalCards` | Número | Cantidad de cartas que incluye el pack. |
| `hasPhotos` | Booleano | `true` si incluye imágenes en la carpeta `photos/`. |
| `hasCustomFlags` | Booleano | `true` si incluye banderas en la carpeta `flags/`. |

---

### B. El archivo `database.json`
Crea un archivo de texto llamado `database.json` en la raíz de tu carpeta. Mapea cada `cardId` con su nombre y club real:

```json
{
  "cards": [
    {
      "cardId": "card_01",
      "playerName": "Lev Yashin",
      "initials": "LY",
      "teamName": "Dinamo Moscú",
      "position": "POR"
    },
    {
      "cardId": "card_10",
      "playerName": "Lamine Yamal",
      "initials": "LY",
      "teamName": "FC Barcelona",
      "position": "DEL"
    },
    {
      "cardId": "card_13",
      "playerName": "Kylian Mbappé",
      "initials": "KM",
      "teamName": "Real Madrid",
      "position": "DEL"
    },
    {
      "cardId": "card_16",
      "playerName": "Lionel Messi",
      "initials": "LM",
      "teamName": "Inter Miami",
      "position": "DEL"
    }
  ]
}
```

> [!TIP]
> Solo necesitas incluir las cartas que deseas sustituir. Si una carta no está en el `database.json`, el juego mantendrá su nombre y club por defecto de forma automática sin errores.

---

### C. La carpeta de fotos `photos/`
Crea una subcarpeta llamada `photos`.
1. Cada foto debe nombrarse **exactamente igual al `cardId`** del futbolista:
   - `card_01.png`
   - `card_10.png`
   - `card_13.png`
2. **Formato:** PNG con fondo transparente (recorte del jugador) o JPG/WebP con fondo.
3. **Resolución recomendada:** `720 x 960 px` (proporción 3:4).
4. **Optimización móvil:** Se recomienda pasar las imágenes por herramientas gratuitas como [TinyPNG](https://tinypng.com/) para que el paquete pese poco y descargue en segundos.

---

### D. La carpeta de banderas `flags/` (Opcional)
Si deseas suministrar banderas personalizadas o en mayor definición para naciones específicas:
1. Crea una subcarpeta llamada `flags`.
2. Guarda los archivos usando el código ISO-2 en mayúsculas:
   - `AR.png` (Argentina)
   - `ES.png` (España)
   - `FR.png` (Francia)
   - `CO.png` (Colombia)

---

## Paso 2: Comprimir en archivo `.zip`

Para que el instalador de Unity lo procese correctamente:

1. Entra a la carpeta de tu Data Pack.
2. Selecciona directamente los archivos (`manifest.json`, `database.json`, la carpeta `photos/` y opcionalmente `flags/`).
3. Haz clic derecho:
   - **En Windows:** *Enviar a -> Carpeta comprimida (en zip)* o *Comprimir en archivo ZIP*.
   - **En Mac:** *Comprimir elementos*.
4. Nombra el archivo (ejemplo: `datapack_real_names_v1.zip`).

> [!CAUTION]
> **Error común:** No comprimas la carpeta contenedora exterior. Al abrir el archivo `.zip`, los archivos `manifest.json` y `database.json` deben verse inmediatamente en la raíz del archivo comprimido.

---

## Paso 3: Alojamiento Externo (Subir a Internet)

Para proteger legalmente el repositorio del juego y mantenerlo liviano, sube tu `.zip` a un servidor de archivos en la nube que te entregue un **enlace directo de descarga**:

### Opciones recomendadas:

#### Opción A: Releases de GitHub (Gratis y muy rápido)
1. Puedes crear un repositorio público en GitHub dedicado a mods (ejemplo: `MiComunidad-TCG-Packs`).
2. Ve a la pestaña **Releases** -> **Draft a new release**.
3. Arrastra tu archivo `datapack_real_names_v1.zip`.
4. Publica el Release y copia el enlace directo del archivo zip (ejemplo: `https://github.com/MiComunidad/Packs/releases/download/v1.0/datapack_real_names_v1.zip`).

#### Opción B: Servidores de Archivos en la Nube
- **Google Cloud Storage / Firebase Storage / AWS S3 / Cloudflare R2:** Te dan un enlace directo HTTPS seguro.
- **Mediafire / Dropbox:** Asegúrate de copiar el enlace de descarga directa (*Direct Download URL*).

---

## Paso 4: Publicar en la Lista de "Recomendados" (Consola de Firebase)

El juego consulta la colección pública `datapacks_catalog` en Cloud Firestore. Para que tu pack aparezca en los teléfonos de todos los jugadores:

1. Abre tu navegador e ingresa a la [Consola de Firebase](https://console.firebase.google.com/).
2. Selecciona tu proyecto (`juegotcg-dev`).
3. En el menú lateral izquierdo, ve a **Firestore Database**.
4. Haz clic en **Iniciar colección** (o selecciona `datapacks_catalog` si ya existe):
   - **ID de la colección:** `datapacks_catalog`
5. Añade un nuevo documento para tu pack:
   - **ID del documento:** (Puedes usar el `packId`, por ejemplo: `pack_real_names_2026`).

6. Agrega los siguientes campos al documento:

| Nombre del Campo | Tipo | Ejemplo de Valor |
| :--- | :--- | :--- |
| `id` | `string` | `official_real_names_pack` |
| `title` | `string` | `Real Names & Photos Community Pack` |
| `author` | `string` | `Comunidad TCG` |
| `version` | `string` | `1.0.0` |
| `description` | `string` | `Nombres y clubes reales con fotos oficiales.` |
| `downloadUrl` | `string` | `https://tu-servidor.com/datapack_real_names_v1.zip` |
| `sizeMb` | `string` | `12.4 MB` |
| `cardsCount` | `number` | `18` |
| `isRecommended` | `boolean` | `true` |
| `bannerColor` | `string` | `#FFB300` |

7. Presiona **Guardar**.

> [!TIP]
> **¿Quieres actualizar a la versión 1.1?**
> Solo editas ese mismo documento en Firebase: cambias `version` a `1.1.0` y actualizas el `downloadUrl`. ¡A todos los usuarios les aparecerá la nueva versión de inmediato!

---

## Paso 5: Cómo lo Experimenta el Jugador en el Móvil

Una vez guardado en Firebase:

1. El usuario abre el juego en su celular o PC.
2. Va a **Ajustes** -> **Data Packs (Comunidad)**.
3. El juego consulta Firestore y muestra tu Data Pack con su título, autor, tamaño y descripción.
4. El jugador presiona **"INSTALAR (1 CLIC)"**:
   - Se muestra la barra de progreso (descargando, verificando y extrayendo).
   - En segundos, el botón cambia a **"✓ ACTIVO"**.
5. Al volver a **Mis Cartas**, **Perfil**, **11 Ideal** o **Partidos**, todas las cartas se transforman instantáneamente con las fotos y nombres reales sin necesidad de reiniciar el juego.
6. Si en algún momento el usuario desea volver a las cartas genéricas, simplemente presiona **"RESTAURAR ORIGINALES"**.

---

## Checklist Final antes de Publicar

- [ ] `manifest.json` tiene `"schemaVersion": 2`.
- [ ] `database.json` contiene únicamente `cardId`, `playerName`, `initials`, `teamName` y `position` (sin estadísticas ni nacionalidades).
- [ ] Todas las fotos en `photos/` tienen exactamente el mismo nombre que su `cardId` (ej. `card_10.png`).
- [ ] El archivo `.zip` tiene los archivos directamente en la raíz (no dentro de una subcarpeta doble).
- [ ] El enlace `downloadUrl` es de descarga directa HTTP/HTTPS.
- [ ] El documento fue creado en la colección `datapacks_catalog` de Firestore.
