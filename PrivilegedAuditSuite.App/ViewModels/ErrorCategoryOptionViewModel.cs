using PrivilegedAuditSuite.Domain.Models;

namespace PrivilegedAuditSuite.App.ViewModels;

public sealed class ErrorCategoryOptionViewModel : ObservableObject
{
    private bool _isSelected;
    private int _count;

    public ErrorCategoryOptionViewModel(ErrorCategory category, string displayName, string? matchText = null, bool isSelected = true)
    {
        Category = category;
        DisplayName = displayName;
        MatchText = matchText;
        _isSelected = isSelected;
    }

    public event Action? SelectionChanged;

    public ErrorCategory Category { get; }

    public string DisplayName { get; }

    public string? MatchText { get; }

    public bool IsCustom => !string.IsNullOrWhiteSpace(MatchText);

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }

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
