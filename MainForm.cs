using Microsoft.Web.WebView2.WinForms;

public class MainForm : Form
{
    private readonly WebView2 _webView;
    private readonly string _url;

    public MainForm(string url)
    {
        _url = url;
        Text = "PokeCollection TCG";
        Width = 1400;
        Height = 900;
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = SystemIcons.Application;

        _webView = new WebView2 { Dock = DockStyle.Fill };
        Controls.Add(_webView);

        Load += OnLoad;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.Source = new Uri(_url);
    }
}
