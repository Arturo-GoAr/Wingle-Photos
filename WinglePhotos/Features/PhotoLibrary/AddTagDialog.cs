using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>Prompts for a tag name and adds it to the item via the view model.</summary>
public static class AddTagDialog
{
    public static async Task ShowAsync(PhotoItem item, MainViewModel viewModel, XamlRoot xamlRoot)
    {
        var textBox = new TextBox { PlaceholderText = "Nombre de la etiqueta" };
        var dialog = new ContentDialog
        {
            Title = "Agregar etiqueta",
            Content = textBox,
            PrimaryButtonText = "Agregar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            await viewModel.AddTagAsync(item, textBox.Text);
        }
    }
}
