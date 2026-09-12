# TobonFrames — notas de mantenimiento

Documento interno del fork: qué se cambió, dónde vive cada cosa y cómo se compila/despliega.
Para la descripción pública del proyecto, ver `README.md`.

## Estado actual

- **Build oficial: 2.7.8.360**, desplegada en `C:\TobonFrames` (Windows 11, Daniel-Work) y en uso
  diario. Rama: **`dani/release`**.
- El bug del borde que motivó el fork está corregido y verificado (ver más abajo), y el fix está
  propuesto upstream como PR.

## Ramas

| Rama | Qué es |
| --- | --- |
| `dani/release` | **La build oficial** (2.7.8.x + fix + marca TobonFrames). Es la que se compila. |
| `dani/beta` | Instantánea previa a la promoción (2.7.8.359) con los arreglos previos. Histórica. |
| `dani/stable` | Línea anterior sobre el código del release 2.7.7.294 (2.7.7.295). Histórica. |
| `fix/border-persistence` | Solo el fix del borde sobre el código dev del upstream. |
| `upstream-fix-border-thickness` | El fix aislado (1 commit, 1 archivo) para el PR upstream. |
| `main` | Espejo del upstream + `ngdfcs/getversion.json`, que es el manifiesto de actualizaciones que lee la app. |

Sincronizar con el upstream cuando publique una versión nueva:

```bash
git remote add upstream https://github.com/limbo666/DesktopFramesPlus.git
git fetch upstream
git rebase upstream/main        # sobre fix/border-persistence, para la versión dev
```

## Rutas (Daniel-Work)

| Cosa | Ruta |
| --- | --- |
| Build instalada | `C:\TobonFrames\TobonFrames.exe` (framework-dependent: `.exe` + `.dll`) |
| Datos de usuario | `C:\TobonFrames\Profiles\Default\` (`frames.json`, `options.json`, `Shortcuts\`) |
| Clone para compilar | `C:\working-files\DesktopFramesPlus` (rama `dani/release`), salida en `...\dist` |
| Instalación anterior | `C:\DesktopFrames+` (2.7.7.294, sin usar; se puede borrar) |
| Backups previos a desplegar | `C:\build\backup-promote-20260912-1617\` (build 2.7.7.295 + `Profiles.zip`) y `C:\build\backup-20260912-1539\` |
| Arranque automático | `HKCU\...\CurrentVersion\Run` → valor `TobonFrames` |
| Registro propio | `HKCU\SOFTWARE\Desktop_Frames_Plus\*` (mismo root que la versión anterior, por continuidad) |

## Compilar y desplegar

```powershell
# en Daniel-Work (ya tiene el SDK .NET 8)
cd C:\working-files\DesktopFramesPlus
git pull
cd Code
dotnet publish "Desktop Frames\Desktop Frames.csproj" -c Release -o ..\dist
```

Para desplegar: cerrar `TobonFrames`, respaldar `Profiles\`, copiar el contenido de `dist\` sobre
`C:\TobonFrames` (sin tocar `Profiles\` ni `ProfileOptions.json`) y volver a arrancar. El `.exe`
y el `.dll` conservan el nombre, así que accesos directos, pin de la barra y arranque automático
siguen funcionando.

Desde SSH la app se lanza en la **sesión del usuario** con una tarea interactiva:
`schtasks /create /tn "X" /tr "C:\TobonFrames\TobonFrames.exe" /sc once /st 23:59 /it /f` y
`schtasks /run /tn "X"` (ver la skill `windows-remote-ops`).

## Qué se cambió respecto al upstream

1. **Fix de persistencia del borde** (`FrameDataManager.ConsolidateKey`): se llamaba con la propia
   clave oficial como si fuera legacy y, en 2.7.7.294, el valor `0` se trataba como vacío. Cada
   guardado borraba `FrameBorderThickness` y al arrancar `MigrateLegacyJson()` lo rellenaba con el
   default `2` → el borde «volvía solo» (issue #120). Ahora la clave oficial no se toca, solo se
   migran los nombres legacy reales y `0` es un valor válido. Reproducido y verificado en la
   instalación real: poner 0, guardar, cerrar y reabrir → sigue en 0.
2. **`FrameAppearanceDefaults.cs`**: única fuente de verdad de la apariencia (`BorderThickness = 0`),
   en lugar de cinco `2` hardcodeados repartidos.
3. **`ValidateDataTypes`**: guardas `ContainsKey` para `Width`/`Height` (antes lanzaba
   `KeyNotFoundException` y abortaba la validación de ese frame).
4. **Sin `COMReference`**: `WScript.Shell` por IDispatch en `Interop/WshLateBound.cs`, así el
   proyecto compila con solo el .NET SDK (antes exigía TlbImp/AxImp del Windows SDK) y hay CI en
   `.github/workflows/build.yml`.
5. **Rebranding**: nombre/producto/icono/banner de menú propios, UI en español (los 8 packs de
   `Localization/`), mutex, log y nombre de arranque propios, About sin PayPal ni footer «Hand
   Water Pump», y el chequeo de actualizaciones leyendo el manifiesto de este fork.
6. **Opciones**: ventana redimensionable (`CanResizeWithGrip` + `MinWidth/MinHeight`) y controles de
   idioma en un `WrapPanel`, para que el botón de importar paquete no se recorte.

## Verificación hecha

- `dotnet publish`: 0 errores; `TobonFrames.exe` reporta 2.7.8.360 / ProductName TobonFrames.
- Ciclo **cerrar y reabrir** con la app real: `FrameBorderThickness` sigue en `0` y el archivo se
  reescribe al arrancar sin perderlo (ese era exactamente el bug).
- Los packs de idioma: cargando el assembly principal y leyendo la cultura `es`
  (`AboutTitle` = «Acerca de TobonFrames», etc.).
- Escaneo de píxeles del escritorio: sin línea de borde alrededor de los frames.
- CI: workflow `build` en verde.

## Pendientes / ideas

1. `C:\DesktopFrames+` (instalación vieja) se puede borrar cuando no haga falta el rollback.
2. Opción global «sin borde / sin fondo»: `TintValue = 0` ya deja el fondo transparente, así que
   la combinación son solo iconos flotando sobre el wallpaper.
3. Unificar la lógica de apariencia: hoy se decide en cuatro sitios (creación del frame,
   `UpdateFrameProperty`, `ApplyFrameBorderSettings`, `ResetAllCustomizations`).
4. Una sola tubería de migración: conviven `FrameDataManager.Initialize`, `ApplySimpleMigrations`,
   `MigrateLegacyJson` y `ConsolidateKey`.
5. `FrameManager.cs` (~12.000 líneas) partido por responsabilidades.
6. Ajustes que existen sin interfaz (`AllowAutoReposition`, `EnableDimensionSnap`, `AutoRollTime`,
   `FramesFadeOutFx` + tiempos, `PortalBackgroundOpacity`, `MenuTintValue`, `MaxDisplayNameLength`,
   `MinLogLevel`, `EnabledLogCategories`): exponerlos es de los mejores ratios valor/esfuerzo.
7. Bugs de upstream sin arreglar que valen la pena: #135 (los presets de Auto-Organize nunca
   coinciden con `.docx/.xlsx/.pptx` porque `*.doc*` se normaliza a `.doc`), #134 («Show Desktop»
   esconde los frames), #121 (opacidad de iconos vs frame), #128 (hover expand en frames nuevos).
8. `FilePathUtilities.RemoveDeadItemsFromArray` **borra el `.lnk`** cuando el target no existe,
   tras un `File.Exists` sincrónico: con un disco externo desconectado o una ruta de red caída se
   pierde el ítem. Conviene no borrar cuando la unidad/target no está accesible y delegar la
   validación al `TargetChecker`.
