using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PokeCollection.Data.Services;

public class MainForm : Form
{
    private readonly WebView2 _webView;
    private readonly string _url;
    private readonly SplashScreen _splash;
    private readonly BackupService _backupService;
    private readonly System.Windows.Forms.Timer _fallbackTimer;
    private bool _revealed;

    public MainForm(string url, WindowService windowService, BackupService backupService, SplashScreen splash)
    {
        windowService.RegisterMainForm(this);

        _url = url;
        _splash = splash;
        _backupService = backupService;
        Text = "PokeCollection TCG";
        Width = 1400;
        Height = 900;
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;

        Opacity = 0;
        ShowInTaskbar = false;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "icon_exe.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }
        else
        {
            Icon = SystemIcons.Application;
        }

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        _fallbackTimer = new System.Windows.Forms.Timer { Interval = 20000 };
        _fallbackTimer.Tick += (_, _) => Reveal();

        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            _backupService.RunAutoBackup();
        }
        catch
        {
        }
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PokeCollection", "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);

        await _webView.EnsureCoreWebView2Async(environment);
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _fallbackTimer.Start();
        _webView.Source = new Uri(_url);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() == "blazorReady")
            Reveal();
    }

    private void Reveal()
    {
        if (_revealed) return;
        _revealed = true;

        _fallbackTimer.Stop();
        Opacity = 1;
        ShowInTaskbar = true;
        Activate();
        BringToFront();
        _splash.Close();
    }
}
