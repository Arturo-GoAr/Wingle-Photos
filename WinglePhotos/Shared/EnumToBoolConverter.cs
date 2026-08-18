using Microsoft.UI.Xaml.Data;

namespace WinglePhotos.Shared;

/// <summary>Compares an enum value's name to a string ConverterParameter — for binding radio-button groups to an enum property.</summary>
public sealed class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value?.ToString() == parameter as string;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
