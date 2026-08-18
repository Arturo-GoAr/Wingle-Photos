using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WinglePhotos.Shared;

/// <summary>
/// ObservableCollection that can add many items with a single CollectionChanged
/// notification instead of one per item — needed once a source folder has
/// thousands of photos, where per-item Add would mean one GridView layout
/// pass per photo during the initial scan.
/// </summary>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        var added = items as IList<T> ?? items.ToList();
        if (added.Count == 0)
        {
            return;
        }

        foreach (var item in added)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
