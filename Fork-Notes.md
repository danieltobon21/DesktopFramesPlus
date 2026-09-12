# TobonFrames — notas del fork

Fork de `limbo666/DesktopFramesPlus` convertido en build propia (**TobonFrames**), con el
bug del borde corregido, compilable solo con el .NET SDK y desplegado en `C:\TobonFrames`.

## 1. El bug del borde (diagnóstico con evidencia)

Síntoma: los frames se ponían sin borde, pero al cerrar y reabrir el programa el borde
volvía con grosor **2**.

Cadena de causas, verificada contra la versión instalada (**2.7.7.294**) y sus datos reales:

1. Al guardar, `FrameDataManager.SaveFrameData()` llama a un helper `ConsolidateKey(...)`
   que migra las claves viejas `Fence*` a `Frame*`.
2. Ese helper se invocaba pasándole **la propia clave oficial como si fuera legacy**
   (`ConsolidateKey("FrameBorderThickness", new[] { "FrameBorderThickness", ... })`) y, en
   2.7.7.294, solo aceptaba el valor si `ToString() != "" && ToString() != "0"`.
   → cada guardado **borraba** `FrameBorderThickness` del frame, y no lo rescataba cuando
   valía `0`. Igual con `FrameBorderColor`.
3. Al arrancar, la clave ya no existía y `MigrateLegacyJson()` la rellenaba con el default
   **hardcodeado en 2** → borde gris de 2 px otra vez.

Evidencia: el `frames.json` en uso (12/09 15:06, recién guardado) **no contenía** ni
`FrameBorderThickness` ni `FrameBorderColor`, mientras el resto de propiedades sí.

El fix de upstream (commit `3b55bc6`, 2.7.8.358) existe solo en `main`, **sin release
publicado** (el último release es 2.7.7.294, justo el instalado). Por eso hubo que compilar.

## 2. Ramas

- `main` — upstream, sin tocar.
- `fix/border-persistence` — el fix sobre `main` dev (2.7.8.358), listo para el día que se
  quiera saltar a esa versión.
- `dani/stable` — **la rama que se compila y despliega**: fix + rebranding sobre el código
  del release 2.7.7.294 (no se metió código dev de 2.7.8.x, que trae ~28k líneas de
  diferencia, en una máquina de trabajo diario). Versión compilada: **2.7.7.295**.

## 3. Qué cambió respecto al release 2.7.7.294

- `FrameDataManager.ConsolidateKey`: reescrito. No borra la clave oficial, solo migra
  nombres legacy reales y **acepta `0` como valor válido**.
- `FrameAppearanceDefaults.cs` (nuevo): una sola fuente de verdad para la apariencia
  (`BorderThickness = 0`), en lugar de cinco `2` hardcodeados repartidos.
- `FrameDataManager.ValidateDataTypes`: guardas `ContainsKey` para `Width`/`Height`.
- `Interop/WshLateBound.cs` (nuevo): `WScript.Shell` por IDispatch. Se eliminaron los tres
  `COMReference` (`IWshRuntimeLibrary`), que exigían TlbImp.exe/AxImp.exe del Windows SDK:
  `dotnet build` moría con MSB4803 y MSBuild puro con MSB3091. Ahora compila con solo el
  .NET 8 SDK — verificado también en GitHub Actions (`.github/workflows/build.yml`).
- Rebranding: `AssemblyName`/`Product` = TobonFrames, icono propio multi-tamaño
  (`Resources/tobonframes.ico`, monocromo + un acento, con variante simplificada 16/24/32 px),
  títulos de ventanas/bandeja, About con el botón de PayPal eliminado y el botón de GitHub
  apuntando al fork (créditos originales conservados: BirdyFences/HakanKokcu, Nikos
  Georgousis, MIT), mutex propio, `TobonFrames.log`, y el update-check leyendo
  `ngdfcs/getversion.json` del fork.
- El valor de inicio automático pasa a llamarse `TobonFrames` (y el viejo `Desktop Frames +`
  se limpia solo al arrancar).

## 4. Dónde quedó instalado

| Cosa | Ruta |
| --- | --- |
| Build desplegada | `C:\TobonFrames` (framework-dependent: `TobonFrames.exe` + `.dll`) |
| Datos de usuario | `C:\TobonFrames\Profiles\Default\` (migrados desde la instalación vieja) |
| Instalación vieja (rollback) | `C:\DesktopFrames+` (intacta, sin usar) |
| Backups hechos antes de desplegar | `C:\build\backup-20260912-1539\Profiles.zip` y `old-build.zip` |
| Fuente para reconstruir | `C:\build\DesktopFramesPlus` (clone de `dani/stable`) y `C:\build\out5` (salida del publish) |
| Inicio automático | `HKCU\...\CurrentVersion\Run` → `TobonFrames` |
| Accesos directos | `TobonFrames.lnk` en el Escritorio y en el menú Inicio |

Compilar de nuevo (en Daniel-Work, ya tiene el SDK 8.0.425):

```powershell
cd C:\build\DesktopFramesPlus
git pull
cd Code
dotnet publish "Desktop Frames\Desktop Frames.csproj" -c Release -o C:\build\out5
```

## 5. Verificación hecha

- `dotnet publish`: OK, 0 errores; `TobonFrames.exe` reporta 2.7.7.295 / ProductName TobonFrames.
- Wrapper COM probado aislado: crea un `.lnk`, lo relee y PowerShell lee los mismos valores.
- Tras desplegar, `frames.json` contiene `"FrameBorderThickness": 0` en los dos frames.
- **Ciclo cerrar/abrir**: con la app cerrada y reabierta, los dos frames siguen en 0 (el
  archivo se reescribe al arrancar y el valor sobrevive). Ese era exactamente el bug.
- Escaneo de píxeles del escritorio: no hay línea de borde alrededor de los frames.
- CI en GitHub Actions: `build` en `dani/stable` → success.

## 6. Pendientes / ideas siguientes

1. **Pin de la barra de tareas**: los `.lnk` fijados se repuntaron al nuevo exe, pero el
   icono en caché puede tardar; si no se actualiza, desanclar y volver a fijar.
2. `C:\DesktopFrames+` se puede borrar cuando no se necesite el rollback.
3. **Opción global “sin borde / sin fondo”**: patrón ya usado por `FramesWithNoRoundCorners`
   (documentado en `tweaks.md`). `TintValue = 0` ya deja el fondo transparente
   (`Utility.ApplyTintAndColorToFrame`), así que la combinación son solo iconos flotando.
4. **Unificar la apariencia del frame**: hoy se decide en cuatro sitios (creación,
   `UpdateFrameProperty`, `ApplyFrameBorderSettings`, `ResetAllCustomizations`). Un
   `FrameAppearance.Apply(win, frame)` único elimina esta familia entera de bugs.
5. **Una sola tubería de migración**: conviven `FrameDataManager.Initialize`,
   `ApplySimpleMigrations`, `MigrateLegacyJson` y `ConsolidateKey`.
6. **`FrameManager.cs` tiene ~12.000 líneas / 450 KB**; partirlo por responsabilidades.
7. Mantener el fork sincronizado:
   ```bash
   git remote add upstream https://github.com/limbo666/DesktopFramesPlus.git
   git fetch upstream && git rebase upstream/main   # sobre fix/border-persistence
   ```
