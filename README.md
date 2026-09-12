# TobonFrames

> **Build personal de [Desktop Frames +](https://github.com/limbo666/DesktopFramesPlus)** para Windows:
> organiza los iconos del escritorio en marcos («frames») que se comportan como paneles
> translúcidos, con atajos, portales a carpetas, notas y plugins.
> Portable, *framework-dependent* (.NET 8) y **sin borde por defecto**.

Este repositorio es un fork de [`limbo666/DesktopFramesPlus`](https://github.com/limbo666/DesktopFramesPlus)
(MIT), mantenido para uso propio. La línea que se compila y se instala es la rama
**`dani/release`**, basada en el código 2.7.8.x del upstream más los arreglos y la marca
TobonFrames descritos abajo.

## Qué cambia respecto al upstream

| Tema | Detalle |
| --- | --- |
| **Fix del borde** | El upstream borraba `FrameBorderThickness` de `frames.json` en cada guardado cuando valía `0` (el helper `ConsolidateKey` se llamaba con la propia clave oficial como si fuera legacy y trataba `0` como vacío). Al reabrir, la clave faltante se rellenaba con el default `2`: el borde «volvía solo» ([issue #120](https://github.com/limbo666/DesktopFramesPlus/issues/120)). Corregido, y el PR está propuesto upstream. |
| **Sin borde por defecto** | `FrameAppearanceDefaults` concentra los valores de apariencia (`BorderThickness = 0`): los frames nuevos, los importados y los que tengan la clave perdida nacen sin borde. |
| **Compila con solo el .NET SDK** | Se eliminaron los `COMReference` de `IWshRuntimeLibrary` (obligaban a TlbImp/AxImp del Windows SDK: `dotnet build` fallaba con MSB4803). `WScript.Shell` se usa por IDispatch en `Interop/WshLateBound.cs`. Compila y publica con `dotnet publish`, y hay CI en `.github/workflows/build.yml`. |
| **UI en español** | 8 packs de idioma (es, de, fr, it, pl, pt, ru, zh-Hans). El idioma se elige en Opciones → General. |
| **Marca propia** | Nombre, icono (multi-tamaño, monocromo + un acento), banner del menú de los frames, About, log, mutex y registros propios. About sin botón de PayPal ni footer «Hand Water Pump»; los créditos originales y la licencia MIT se conservan. |
| **Actualizaciones** | El chequeo de versión lee `ngdfcs/getversion.json` de este fork, no del upstream. |

## Compilar

Requiere el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) en Windows.

```powershell
git clone -b dani/release https://github.com/danieltobon21/DesktopFramesPlus.git
cd DesktopFramesPlus\Code
dotnet publish "Desktop Frames\Desktop Frames.csproj" -c Release -o ..\dist
```

La salida (`dist\TobonFrames.exe` + `TobonFrames.dll` + dependencias) es framework-dependent:
el equipo necesita el runtime de .NET 8 (el escritorio de Windows ya lo trae).

## Instalar y actualizar

1. Extrae el contenido del paquete en una carpeta de usuario (por ejemplo `C:\TobonFrames`).
2. Ejecuta `TobonFrames.exe`. Al primer arranque se crean `Profiles\Default\` con los datos.
3. Para actualizar: cierra la aplicación y sobrescribe los archivos **sin tocar `Profiles\`**.

Los datos y la configuración viven en `Profiles\<perfil>\`:

| Archivo | Contenido |
| --- | --- |
| `frames.json` | Frames, posiciones, tamaños, apariencia e iconos |
| `options.json` | Ajustes globales (`Language`, `TintValue`, `AutoHideFrames`, …) |
| `Shortcuts\` | Los accesos directos propios de los frames |
| `Backups\` | Copias automáticas diarias |

## Ramas

| Rama | Qué es |
| --- | --- |
| `dani/release` | **La build oficial** (2.7.8.x + arreglos + marca). Se compila y se despliega. |
| `dani/beta` | Instantánea previa a la promoción (2.7.8.359). Histórica. |
| `dani/stable` | La línea anterior, sobre el código del release 2.7.7.294 (2.7.7.295). Histórica. |
| `fix/border-persistence` | Solo el fix del borde sobre el código dev del upstream. |
| `upstream-fix-border-thickness` | El fix del borde aislado, listo para el PR upstream. |
| `main` | Espejo del upstream + el manifiesto de actualizaciones de este fork. |

## Notas

- Los ajustes sin interfaz (por ejemplo `MenuTintValue`, `AllowAutoReposition`) se pueden editar
  en `options.json`; el upstream los documenta en [`tweaks.md`](tweaks.md).
- Antes de desplegar una build nueva conviene respaldar `Profiles\`: la aplicación reescribe
  `frames.json` ante cualquier cambio.

## Licencia y créditos

MIT, igual que el proyecto original. TobonFrames es una build personal de **Desktop Frames +**,
la utilidad de código abierto para Windows creada originalmente por **HakanKokcu** con el nombre
**BirdyFences** y mantenida por **Nikos Georgousis**. Se conservan intactos los avisos de
copyright, la autoría y los créditos originales.
