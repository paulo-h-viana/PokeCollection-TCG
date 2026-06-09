public sealed class SplashScreen
{
    private SplashForm? _form;
    private Thread? _thread;
    private readonly ManualResetEventSlim _shown = new(false);

    public void Show()
    {
        _thread = new Thread(() =>
        {
            _form = new SplashForm();
            _form.Shown += (_, _) => _shown.Set();
            Application.Run(_form);
        })
        {
            IsBackground = true
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _shown.Wait(TimeSpan.FromSeconds(5));
    }

    public void Close()
    {
        var form = _form;
        if (form is null || !form.IsHandleCreated) return;
        form.BeginInvoke(() => form.Close());
    }
}
