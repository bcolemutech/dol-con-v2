namespace DolCon.Views;

public class PaginatedList<T>
{
    private IReadOnlyList<T> _items;
    private readonly int _pageSize;

    public PaginatedList(IReadOnlyList<T> items, int pageSize)
    {
        _items = items;
        _pageSize = pageSize;
    }

    public int CurrentPage { get; private set; }
    public int SelectedIndex { get; private set; }
    public int TotalItems => _items.Count;
    public int TotalPages => _items.Count == 0 ? 0 : (int)Math.Ceiling(_items.Count / (double)_pageSize);

    public int CurrentPageSelectedIndex => _items.Count == 0 ? 0 : SelectedIndex % _pageSize;

    public IEnumerable<T> CurrentPageItems => _items
        .Skip(CurrentPage * _pageSize)
        .Take(_pageSize);

    public string PageInfo
    {
        get
        {
            if (_items.Count == 0)
            {
                return "No items";
            }

            var startItem = CurrentPage * _pageSize + 1;
            var endItem = Math.Min((CurrentPage + 1) * _pageSize, _items.Count);
            return $"Page {CurrentPage + 1}/{TotalPages} (Items {startItem}-{endItem} of {_items.Count})";
        }
    }

    public void MoveDown()
    {
        if (_items.Count == 0) return;

        if (SelectedIndex < _items.Count - 1)
        {
            SelectedIndex++;
            CurrentPage = SelectedIndex / _pageSize;
        }
    }

    public void MoveUp()
    {
        if (_items.Count == 0) return;

        if (SelectedIndex > 0)
        {
            SelectedIndex--;
            CurrentPage = SelectedIndex / _pageSize;
        }
    }

    public void NextPage()
    {
        if (_items.Count == 0) return;

        if (CurrentPage < TotalPages - 1)
        {
            CurrentPage++;
            SelectedIndex = CurrentPage * _pageSize;
        }
    }

    public void PreviousPage()
    {
        if (_items.Count == 0) return;

        if (CurrentPage > 0)
        {
            CurrentPage--;
            SelectedIndex = CurrentPage * _pageSize;
        }
    }

    public void Reset()
    {
        CurrentPage = 0;
        SelectedIndex = 0;
    }

    public T? GetSelected()
    {
        if (_items.Count == 0 || SelectedIndex >= _items.Count)
        {
            return default;
        }

        return _items[SelectedIndex];
    }

    public void UpdateItems(IReadOnlyList<T> items)
    {
        _items = items;
        Reset();
    }
}
