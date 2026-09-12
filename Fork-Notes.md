# Fork Notes — danieltobon21/DesktopFramesPlus

Notas de la revisión del fork y de la rama `fix/border-persistence`.

## 1. El bug del borde (diagnóstico, con evidencia)

Síntoma reportado: los frames quedan sin borde, pero al cerrar y volver a abrir el
programa el borde vuelve con grosor **2**.

Cadena de causas, verificada en el código de la versión instalada (**2.7.7.294**,
`C:\DesktopFrames+\Desktop Frames.dll`) y en los datos reales del perfil
(`C:\DesktopFrames+\Profiles\Default\frames.json`):

1. Al guardar, `FrameDataManager.SaveFrameData()` llama a un helper interno
   `ConsolidateKey(...)` para migrar las claves viejas `Fence*` a `Frame*`.
2. Ese helper se invocaba pasándole **la propia clave oficial como si fuera una clave
   legacy**:

   ```csharp
   ConsolidateKey("FrameBorderThickness", new[] { "FrameBorderThickness", "FrameBorderThickness" });
   ```

   y además, en 2.7.7.294, el valor se consideraba válido solo si
   `ToString() != "" && ToString() != "0"`.

   Resultado: el guardado **borraba** `FrameBorderThickness` del frame y no lo
   rescataba cuando valía `0`. Lo mismo con `FrameBorderColor`.
3. En el arranque siguiente, la clave ya no existía, y `MigrateLegacyJson()` la
   rellenaba con el default **hardcodeado en 2** → borde gris de 2 px otra vez.

Evidencia en los datos en vivo: el `frames.json` del 12/09 15:06 (escrito justo
después de que el usuario pusiera grosor 0) **no contiene** ni `FrameBorderThickness`
ni `FrameBorderColor`, mientras que el resto de propiedades sí están.

El fix real ya existe en `main` upstream (commit `3b55bc6`, 2.7.8.358, 27-jul-2026),
pero **no hay release publicado con él**: el último release es 2.7.7.294 (8-jun-2026),
que es exactamente el que está instalado. Es decir: no se arregla actualizando; hay
que compilar desde el código.

## 2. Ramas y build desplegada

- `fix/border-persistence` — el fix aplicado sobre `main` upstream (2.7.8.358, código dev).
- `dani/stable` — **el fix aplicado sobre el código del release 2.7.7.294**, que es la
  build instalada. Se eligió esta base para no meter código dev sin publicar (~28k líneas
  de diferencia: localización, TabManager, etc.) en la máquina de trabajo diario.
  Versión compilada: **2.7.7.295** (así se distingue de la 2.7.7.294 del autor).
- Cuando upstream publique 2.7.8.x, `fix/border-persistence` ya trae el fix listo.

### Qué cambia exactamente

- `FrameDataManager.ConsolidateKey`: reescrito. Ya no borra la clave oficial (se
  ignoran las entradas legacy que sean iguales a ella), solo migra nombres
  realmente legacy (`FenceBorderThickness`, `frameBorderThickness`, ...) y **acepta
  `0` como valor válido**.
- `FrameAppearanceDefaults.cs` (nuevo): única fuente de verdad para los defaults de
  apariencia de frame. `BorderThickness = 0` → los frames nuevos, los importados y
  los que tengan la clave perdida nacen **sin borde**.
- Se eliminan los `FrameBorderThickness = 2` hardcodeados que había en 5 sitios
  (`FrameManager.InitializeDefaultFrame` x2, `MigrateLegacyJson`,
  `ResetAllCustomizations`, constructor de frames nuevos, `AutoOrganizeManager`).
- `FrameDataManager.ValidateDataTypes`: guardas `ContainsKey` para `Width`/`Height`
  (antes lanzaba `KeyNotFoundException` y abortaba la validación de ese frame).

Efecto para el usuario: el grosor 0 ahora **persiste**; `Customize → Frame Border
Thickness = 0` (con Ctrl + Apply para aplicarlo a todos los frames) se queda.

## 3. Roadmap: convertir esto en tu propia herramienta

### a) Mantener el fork sincronizado (recomendado, primero)

```bash
git remote add upstream https://github.com/limbo666/DesktopFramesPlus.git
git fetch upstream
git checkout -b dani/main
git rebase upstream/main        # trae fixes como el de arriba
```

Trabajar siempre en `dani/main` (o ramas por tema) evita pelearse con `main`, que es
la rama que sigue al upstream.

### b) Compilar tu propia build

La instalación es **framework-dependent** (`.exe` + `Desktop Frames.dll` +
`runtimeconfig.json`), no self-contained: se puede reemplazar solo el DLL.

```powershell
winget install Microsoft.DotNet.SDK.8
dotnet publish "Code\Desktop Frames\Desktop Frames.csproj" -c Release -r win-x64 --self-contained false
```

El SDK de .NET 8 **no está instalado** hoy en Daniel-Work (solo el runtime 8.0.31),
por eso hay que instalarlo antes de compilar.

### c) Rebranding (que sea tu copia, no el proyecto del autor)

Puntos concretos donde vive la identidad:

| Qué | Dónde |
| --- | --- |
| Nombre del producto, versión, icono | `Desktop Frames.csproj` (`AssemblyName`, `Version`, `ApplicationIcon`) |
| Iconos de bandeja / ventana | `Resources/logo*.ico` |
| Enlace a GitHub y botón PayPal | `AboutFormManager.cs` (líneas ~483, ~516, ~671, ~857), `OptionsFormManager.cs` (~1336) |
| Chequeo de actualizaciones | `RemoteInfoManager.cs` (`MANIFEST_URL` apunta a `limbo666/.../ngdfcs/getversion.json`) |
| Ruta de registro | `RegistryHelper.cs` → `HKCU\SOFTWARE\Desktop_Frames_Plus\*` (un solo sitio) |
| Notas legales | `License.md` (MIT: hay que **conservar** el copyright y los créditos a BirdyFences / Nikos Georgousis) |

Truco sin recompilar: `RemoteInfoManager` ya soporta un archivo `developer_repo.txt`
junto al `.exe` con una URL propia; sirve para apuntar el aviso de actualización a tu
fork en lugar del repo del autor.

### d) Ajustes que valen la pena en el código

1. **Unificar la lógica de apariencia.** El borde se decide en 4 sitios distintos
   (creación del frame, `UpdateFrameProperty`, `ApplyFrameBorderSettings`,
   `ResetAllCustomizations`). Extraer un `FrameAppearance.Apply(win, frame)` único
   mata toda esta familia de bugs.
2. **Una sola tubería de migración.** Hoy conviven `FrameDataManager.Initialize`,
   `ApplySimpleMigrations`, `MigrateLegacyJson` y el `ConsolidateKey`: tres
   normalizadores de arranque que pueden contradecirse (es lo que pasó aquí).
3. **`FrameManager.cs` tiene ~12.000 líneas / 450 KB.** Partirlo (frames, menús,
   drag&drop, portales, notas) hace que cada cambio deje de ser arqueología.
4. **Opción global "sin borde" / "sin fondo"** (patrón ya usado por
   `FramesWithNoRoundCorners`, documentado en `tweaks.md`): un ajuste oculto en
   `options.json` para forzar grosor 0 y tinte 0 a todos los frames sin tocar frame
   por frame. `TintValue = 0` ya deja el fondo transparente
   (`Utility.ApplyTintAndColorToFrame`), así que "sin borde + sin fondo" =
   solo iconos flotando sobre el wallpaper.
5. **Contexto de menú / instancia única**: nombres de registro y mutex
   (`Global\DesktopFramesPlus_Mutex_UniqueId_v2`) → cambiarlos si quieres que tu
   build y la del autor convivan instaladas.

### e) Empaquetado y distribución

- `.github/workflows/release.yml` con `runs-on: windows-latest`, `dotnet publish` y
  `actions/upload-artifact` o release en tag. Con eso cada push produce un zip
  instalable propio.
- Antes de desplegar una build parcheada sobre `C:\DesktopFrames+`: **cerrar el
  programa** y hacer backup de `Profiles\Default\` (el app reescribe `frames.json`
  ante cualquier cambio, y una build vieja que guarde encima puede volver a borrar
  claves — por ejemplo el grosor 0 antes de este fix).
