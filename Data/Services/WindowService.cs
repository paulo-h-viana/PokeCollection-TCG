namespace PokeCollection.Data.Services;

public class WindowService
{
    private Form? _mainForm;

    public void RegisterMainForm(Form form) => _mainForm = form;

    public string? ShowOpenFileDialog(string filter)
    {
        if (_mainForm is null) return null;
        string? result = null;
        _mainForm.Invoke(() =>
        {
            using var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog() == DialogResult.OK)
                result = dialog.FileName;
        });
        return result;
    }

    public string? ShowSaveFileDialog(string filter, string defaultName)
    {
        if (_mainForm is null) return null;
        string? result = null;
        _mainForm.Invoke(() =>
        {
            using var dialog = new SaveFileDialog { Filter = filter, FileName = defaultName };
            if (dialog.ShowDialog() == DialogResult.OK)
                result = dialog.FileName;
        });
        return result;
    }

    public void RestartApplication()
    {
        _mainForm?.Invoke(Application.Restart);
    }
}
