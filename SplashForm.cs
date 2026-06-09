public sealed class SplashForm : Form
{
    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.White;
        DoubleBuffered = true;
        ClientSize = new Size(400, 300);

        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        var gifPath = Path.Combine(AppContext.BaseDirectory, "splash.gif");
        if (File.Exists(gifPath))
        {
            picture.Image = Image.FromFile(gifPath);
        }

        Controls.Add(picture);
    }
}
