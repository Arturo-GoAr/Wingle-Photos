using System.Globalization;
using WinglePhotos.Shared;

namespace WinglePhotos.Features.PhotoLibrary;

/// <summary>
/// One day's worth of photos. Doubles as the grouped GridView's group object:
/// CollectionViewSource with IsSourceGrouped=true expects each group to itself be
/// an IEnumerable of items, which BulkObservableCollection&lt;PhotoItem&gt; already is.
/// </summary>
public sealed class PhotoDateGroup : BulkObservableCollection<PhotoItem>
{
    private static readonly CultureInfo TitleCulture = new("es-MX");

    public DateOnly Date { get; }

    public string Title => Date.ToDateTime(TimeOnly.MinValue).ToString("D", TitleCulture);

    public PhotoDateGroup(DateOnly date)
    {
        Date = date;
    }
}
