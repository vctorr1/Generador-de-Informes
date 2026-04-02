namespace PrivilegedAuditSuite.App.Services;

public interface IFilePickerService
{
    string? PickFile(string title, string filter);

    string? PickSaveFile(string title, string filter, string defaultFileName);
}
