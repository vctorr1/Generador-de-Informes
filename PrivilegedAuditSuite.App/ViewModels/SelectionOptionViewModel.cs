namespace PrivilegedAuditSuite.App.ViewModels;

public sealed class SelectionOptionViewModel : ObservableObject
{
    private bool _isSelected;

    public SelectionOptionViewModel(string name, int count, bool isSelected = true)
    {
        Name = name;
        Count = count;
        _isSelected = isSelected;
    }

    public event Action? SelectionChanged;

    public string Name { get; }

    public int Count { get; }

    public string DisplayName => $"{Name} ({Count})";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                SelectionChanged?.Invoke();
            }
        }
    }
}
