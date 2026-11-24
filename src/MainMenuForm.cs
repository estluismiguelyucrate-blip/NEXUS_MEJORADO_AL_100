using System.Drawing.Drawing2D;

namespace NexusCopia;

internal sealed class MainMenuForm : Form
{
    private readonly string[] _levels = ["01 · RUINAS DEL UMBRAL", "02 · BOSQUE CARMESÍ · PRÓXIMAMENTE", "03 · CIUDAD SUMERGIDA · PRÓXIMAMENTE"];
    private int _selected;

    public MainMenuForm()
    {
        Text = "NEXUS COPIA | Ecos del Umbral";
        ClientSize = new Size(1100, 680); MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(8, 8, 25);
        DoubleBuffered = true; KeyPreview = true;
        Paint += PaintMenu;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        var play = Button("INICIAR AVENTURA", 440, 350, 230, 48, true);
        play.Click += (_, _) => { using var game = new PlatformerForm(); game.ShowDialog(this); };
        Controls.Add(play);
        var choose = Button("CAMBIAR MAPA   ◀   ▶", 440, 410, 230, 42, false);
        choose.Click += (_, _) => { _selected = (_selected + 1) % _levels.Length; Invalidate(); };
        Controls.Add(choose);
        var controls = Button("CONTROLES", 440, 464, 230, 42, false);
        controls.Click += (_, _) => MessageBox.Show(this, "A / D o flechas: mover\nEspacio / W: saltar\nJ / X: atacar\nEsc: salir", "Controles", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Controls.Add(controls);
        var quit = Button("SALIR", 440, 518, 230, 42, false); quit.Click += (_, _) => Close(); Controls.Add(quit);
    }

    private Button Button(string label, int x, int y, int w, int h, bool primary)
    {
        var b = new Button { Text = label, Location = new Point(x, y), Size = new Size(w, h), FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(55, 169, 240) : Color.FromArgb(22, 20, 48),
            ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(98, 106, 215); b.FlatAppearance.BorderSize = 1;
        return b;
    }

    private void PaintMenu(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(32, 24, 78), Color.FromArgb(5, 7, 22), 90); g.FillRectangle(bg, ClientRectangle);
        using var glow = new SolidBrush(Color.FromArgb(26, 89, 71, 255)); g.FillEllipse(glow, 325, 38, 460, 300);
        using var title = new Font("Segoe UI", 46, FontStyle.Bold); using var sub = new Font("Segoe UI", 12, FontStyle.Bold);
        using var white = new SolidBrush(Color.White); using var cyan = new SolidBrush(Color.FromArgb(139, 226, 255));
        var center = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("N E X U S", title, white, new RectangleF(0, 105, ClientSize.Width, 75), center);
        g.DrawString("ECOS DEL UMBRAL", sub, cyan, new RectangleF(0, 186, ClientSize.Width, 30), center);
        g.DrawString(_levels[_selected], sub, white, new RectangleF(300, 300, 500, 30), center);
        using var small = new Font("Segoe UI", 9); using var muted = new SolidBrush(Color.FromArgb(184, 184, 214));
        g.DrawString("Una aventura de plataformas · combate · exploración · jefes", small, muted, new RectangleF(0, 594, ClientSize.Width, 24), center);
    }
}

