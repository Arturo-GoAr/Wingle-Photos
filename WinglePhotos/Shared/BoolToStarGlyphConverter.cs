using Microsoft.UI.Xaml.Data;

namespace WinglePhotos.Shared;

public sealed class BoolToStarGlyphConverter : IValueConverter
{
    private static readonly string Filled = "";
    private static readonly string Outline = "";

    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Filled : Outline;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
