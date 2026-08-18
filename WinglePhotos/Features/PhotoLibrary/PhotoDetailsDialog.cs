using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>
/// Builds and shows the "Ver detalles" modal. Metadata is fetched on demand (not eagerly
/// during the scan) since GetImagePropertiesAsync/GetVideoPropertiesAsync are comparatively
/// slow and only a handful of items are ever inspected per session.
/// </summary>
public static class PhotoDetailsDialog
{
    public static async Task ShowAsync(PhotoItem item, XamlRoot xamlRoot)
    {
        var panel = new StackPanel { Spacing = 4 };

        void AddRow(string label, string value) =>
            panel.Children.Add(new TextBlock
            {
                Text = $"{label}: {value}",
                TextWrapping = TextWrapping.Wrap,
            });

        try
        {
            var file = await StorageFile.GetFileFromPathAsync(item.Path);
            var basicProperties = await file.GetBasicPropertiesAsync();

            AddRow("Nombre", file.Name);
            AddRow("Ruta", item.Path);
            AddRow("Fecha", item.DateTaken.LocalDateTime.ToString("dd/MM/yyyy HH:mm"));
            AddRow("Tamaño", FormatSize(basicProperties.Size));
            AddRow("Formato", System.IO.Path.GetExtension(item.Path).TrimStart('.').ToUpperInvariant());

            if (item.IsVideo)
            {
                var videoProperties = await file.Properties.GetVideoPropertiesAsync();
                if (videoProperties.Duration > TimeSpan.Zero)
                {
                    AddRow("Duración", videoProperties.Duration.ToString(@"hh\:mm\:ss"));
                }

                if (videoProperties.Width > 0 && videoProperties.Height > 0)
                {
                    AddRow("Resolución", $"{videoProperties.Width} x {videoProperties.Height}");
                }
            }
            else
            {
                var imageProperties = await file.Properties.GetImagePropertiesAsync();
                if (imageProperties.Width > 0 && imageProperties.Height > 0)
                {
                    AddRow("Resolución", $"{imageProperties.Width} x {imageProperties.Height}");
                }

                if (!string.IsNullOrWhiteSpace(imageProperties.CameraModel))
                {
                    AddRow("Cámara", imageProperties.CameraModel);
                }
            }
        }
        catch (Exception)
        {
            AddRow("Error", "No se pudieron leer los metadatos del archivo.");
        }

        AddRow("Favorito", item.IsFavorite ? "Sí" : "No");
        AddRow("Etiquetas", item.Tags.Count > 0 ? string.Join(", ", item.Tags) : "Ninguna");

        var dialog = new ContentDialog
        {
            Title = "Detalles",
            Content = new ScrollViewer { Content = panel, MaxHeight = 420 },
            CloseButtonText = "Cerrar",
            XamlRoot = xamlRoot,
        };

        await dialog.ShowAsync();
    }

    private static string FormatSize(ulong bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
