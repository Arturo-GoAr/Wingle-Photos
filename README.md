# Wingle Photos

App de escritorio en **WinUI 3** para explorar tus fotos y videos a partir de las carpetas que tú elijas como fuentes, con las bibliotecas de Imágenes y Videos de Windows incluidas por defecto.

## Funcionalidad

- **Múltiples carpetas fuente** — agrega cualquier carpeta del sistema como fuente; las bibliotecas de Imágenes y Videos de Windows se agregan automáticamente en el primer arranque y no se pueden quitar. Las demás carpetas se pueden quitar desde el kebab (⋯) de cada fila en "Carpetas".
- **Vista tipo Google Fotos** — cuadrícula de miniaturas agrupada por fecha, con carga progresiva y virtualizada para bibliotecas de decenas de miles de elementos.
- **Formatos de foto soportados** — jpg, jpeg, png, gif, bmp, tif/tiff, webp, heic, y RAW (NEF, CR2/CR3, ARW, DNG, ORF, RW2, RAF, PEF, SRW) vía la vista previa embebida del archivo.
- **Formatos de video soportados** — mp4, m4v, mov, mkv, avi, wmv, webm, con miniatura de fotograma y reproducción integrada en el visor.
- **Favoritos** — marca/desmarca fotos y videos, y filtra para ver solo tus favoritos.
- **Etiquetas/categorías** — agrega etiquetas de texto libre a cualquier foto o video desde el menú kebab; se muestran en el modal de detalles.
- **Ver detalles** — modal con metadatos: nombre, ruta, fecha, tamaño, formato, resolución/duración, cámara (si aplica), favorito y etiquetas.
- **Menú contextual (kebab y clic derecho)** — sobre cualquier foto o video: Favorito, Agregar etiqueta, Ver detalles, Eliminar. El clic derecho abre exactamente el mismo menú que el botón kebab.
- **Eliminar** — envía archivos a la papelera de reciclaje, nunca borrado permanente.
- **Visor a pantalla completa** — zoom, navegación con teclado (flechas) y gestos para fotos; reproducción con controles para video.
- **Sidebar de navegación** — Todo, Fotos, Videos, Favoritos, Carpetas (explorar por fuente), Configuración y Ayuda/Documentación.
- **Configuración persistente en SQLite** — carpetas fuente y preferencias (tema claro/oscuro/sistema) se guardan en una base de datos local.
- **Interfaz en español.**

## Arquitectura

El proyecto está organizado por *feature* (screaming architecture) en vez de por capa técnica:

```
WinglePhotos/
├── Features/
│   ├── PhotoLibrary/   # Modelo de fotos/videos, enumeración, miniaturas, grid principal, modal de detalles, diálogo de etiquetas
│   ├── PhotoSources/   # Gestión de carpetas fuente (FolderPicker, FutureAccessList), quitar carpeta
│   ├── Favorites/      # Persistencia de favoritos
│   ├── Tags/           # Persistencia de etiquetas/categorías
│   ├── Viewer/         # Visor a pantalla completa (fotos y video)
│   └── Settings/       # Configuración (SQLite), tema de la app
├── Shared/             # Utilidades reusadas entre features (converters, DB, colecciones, formatos)
├── App.xaml.cs         # Composition root (inyección de dependencias)
└── MainWindow.xaml     # Sidebar (NavigationView) + Frame de navegación
```

Cada servicio se expone detrás de una interfaz (`IPhotoSourceService`, `IPhotoEnumerationService`, `IThumbnailCacheService`, `IFavoritesService`, `ITagsService`, `ISettingsStore`, `IThemeService`) e inyectado vía `Microsoft.Extensions.DependencyInjection`, siguiendo SOLID — los ViewModels dependen de abstracciones, no de implementaciones concretas.

### Decisiones técnicas relevantes

- **App empaquetada (MSIX)**: el acceso a carpetas arbitrarias se maneja con `FolderPicker` + `StorageApplicationPermissions.FutureAccessList`, guardando el token de acceso (no la ruta) para que las fuentes sobrevivan a un reinicio.
- **Enumeración vía Storage API, no `System.IO`**: las bibliotecas de Imágenes y Videos por defecto solo conceden acceso a través del broker de WinRT (capabilities `picturesLibrary`/`videosLibrary`), no acceso NTFS directo — por eso la enumeración usa `StorageFolder`/`StorageFileQueryResult` en vez de `System.IO.Directory`, con `IndexerOption.DoNotUseIndexer` para que extensiones de video poco comunes no se pierdan por depender del índice de Windows Search.
- **Miniaturas cacheadas en disco**, generadas con `StorageFile.GetThumbnailAsync` y cargadas de forma perezosa mediante `ContainerContentChanging` del `GridView` (no `Loaded`, que no se repite cuando WinUI recicla contenedores en listas virtualizadas grandes). El mismo mecanismo genera el fotograma de póster para videos.
- **RAW/NEF**: se muestra la vista previa JPEG embebida en el archivo, no una decodificación completa del RAW.
- **Video**: reproducción con `MediaPlayerElement` en el visor; nunca se decodifica/transcodifica manualmente, se apoya en los códecs instalados en el sistema.
- **Eliminar siempre a la papelera**: todo borrado pasa por `StorageDeleteOption.Default`, nunca `PermanentDelete`.

## Requisitos

- Windows 10 19041+ / Windows 11
- [.NET SDK 9](https://dotnet.microsoft.com/download) o superior
- Modo de desarrollador de Windows activado (Configuración → Privacidad y seguridad → Para desarrolladores) para ejecutar la app empaquetada sin firmar

## Ejecutar

```bash
cd WinglePhotos
dotnet run
```

No requiere Visual Studio — el proyecto usa `Microsoft.Windows.SDK.BuildTools.WinApp`, que agrega soporte de `dotnet run` para apps WinUI 3 empaquetadas directamente desde la CLI.

## Stack

- WinUI 3 / Windows App SDK
- .NET 9
- CommunityToolkit.Mvvm (MVVM, `ObservableObject`, `RelayCommand`)
- Microsoft.Data.Sqlite (configuración persistente)
- Microsoft.Extensions.DependencyInjection (composition root)
