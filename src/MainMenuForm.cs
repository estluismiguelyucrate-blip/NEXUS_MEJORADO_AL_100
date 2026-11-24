using System.Drawing.Drawing2D;

namespace NexusCopia;

/// <summary>Menú interactivo compuesto sobre el arte de referencia de Nexus Copy.</summary>
internal sealed class MainMenuForm : Form
{
    private const float DesignWidth = 1360;
    private const float DesignHeight = 768;
    private readonly HunterProgress _profile = ProgressStore.Load();
    private readonly OriginalMusic _music = new();
    private readonly Image _menuBackground;
    private readonly Image _hunterSprite;
    private readonly System.Windows.Forms.Timer _idleTimer;
    private double _idleTime;
    private int _selectedDungeon;
    private string _hovered = "";

    public MainMenuForm()
    {
        Text = "NEXUS COPY | Cazadores del Umbral";
        ClientSize = new Size(1360, 768);
        MinimumSize = new Size(900, 540);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(5, 6, 18);
        DoubleBuffered = true;
        KeyPreview = true;
        Cursor = Cursors.Default;

        string artPath = Path.Combine(AppContext.BaseDirectory, "assets", "backgrounds", "menu-background-clean.jpg");
        string spritePath = Path.Combine(AppContext.BaseDirectory, "assets", "characters", "menu-hunter.png");
        if (!File.Exists(artPath) || !File.Exists(spritePath))
            throw new FileNotFoundException("Faltan las capas animadas del menú. Vuelve a extraer el repositorio completo y compila NexusCopy.sln.", !File.Exists(artPath) ? artPath : spritePath);
        _menuBackground = Image.FromFile(artPath);
        _hunterSprite = Image.FromFile(spritePath);
        _idleTimer = new System.Windows.Forms.Timer { Interval = 33 };
        _idleTimer.Tick += (_, _) => { _idleTime += 0.033; Invalidate(); };
        _idleTimer.Start();
        _selectedDungeon = Math.Clamp(_profile.ClearedDungeons, 0, DungeonCatalog.All.Length - 1);

        MouseMove += HandleMouseMove;
        MouseLeave += (_, _) => { _hovered = ""; Cursor = Cursors.Default; Invalidate(); };
        MouseClick += HandleMenuClick;
        KeyDown += HandleKeyDown;
        _music.PlayMenu();
        FormClosed += (_, _) => { _idleTimer.Stop(); _idleTimer.Dispose(); _music.Dispose(); _menuBackground.Dispose(); _hunterSprite.Dispose(); };
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
        else if (e.KeyCode == Keys.M) { ToggleMusic(); e.Handled = true; }
        else if (e.KeyCode == Keys.Enter) { StartDungeon(); e.Handled = true; }
        else if (e.KeyCode is Keys.Left or Keys.Right)
        {
            int direction = e.KeyCode == Keys.Right ? 1 : -1;
            _selectedDungeon = (_selectedDungeon + direction + DungeonCatalog.All.Length) % DungeonCatalog.All.Length;
            Invalidate(); e.Handled = true;
        }
        else if (e.KeyCode == Keys.C)
        {
            ChangeHunter(); e.Handled = true;
        }
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        PointF p = ToDesignPoint(e.Location);
        string current = HitTest(p);
        if (current != _hovered)
        {
            _hovered = current;
            Cursor = current.Length > 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    private void HandleMenuClick(object? sender, MouseEventArgs e)
    {
        string action = HitTest(ToDesignPoint(e.Location));
        switch (action)
        {
            case "music": ToggleMusic(); break;
            case "play": StartDungeon(); break;
            case "dungeon":
                _selectedDungeon = (_selectedDungeon + 1) % DungeonCatalog.All.Length;
                Invalidate();
                break;
            case "hunter": ChangeHunter(); break;
            case "settings": ShowSettings(); break;
            case "controls": ShowControls(); break;
        }
    }

    private PointF ToDesignPoint(Point point) => new(
        point.X * DesignWidth / Math.Max(1, ClientSize.Width),
        point.Y * DesignHeight / Math.Max(1, ClientSize.Height));

    private string HitTest(PointF p)
    {
        // The reference artwork is 1360×768. Coordinates match the visible artwork buttons.
        if (new RectangleF(10, 7, 135, 42).Contains(p)) return "music";
        if (new RectangleF(42, 290, 370, 69).Contains(p)) return "play";
        if (new RectangleF(42, 365, 370, 66).Contains(p)) return "dungeon";
        if (new RectangleF(42, 435, 370, 64).Contains(p)) return "hunter";
        if (new RectangleF(42, 505, 370, 65).Contains(p)) return "settings";
        if (new RectangleF(42, 575, 370, 69).Contains(p)) return "controls";
        return "";
    }

    private void ToggleMusic()
    {
        _music.Toggle();
        Invalidate();
    }

    private void ChangeHunter()
    {
        int index = Array.FindIndex(HunterCatalog.All, x => x.Id == _profile.HunterId);
        _profile.HunterId = HunterCatalog.All[(index + 1) % HunterCatalog.All.Length].Id;
        ProgressStore.Save(_profile);
        Invalidate();
    }

    private void ShowSettings()
    {
        using var dialog = new Form
        {
            Text = "Configuración del cazador", StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
            ClientSize = new Size(440, 250), BackColor = Color.FromArgb(15, 13, 34), ForeColor = Color.White
        };
        var heading = new Label { Text = "AJUSTES DE NEXUS", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(213, 190, 255), AutoSize = true, Location = new Point(28, 24) };
        var sound = new CheckBox { Text = "Música original", Checked = _music.Enabled, ForeColor = Color.White, AutoSize = true, Location = new Point(34, 91), Font = new Font("Segoe UI", 12) };
        sound.CheckedChanged += (_, _) => { if (sound.Checked != _music.Enabled) ToggleMusic(); };
        var info = new Label { Text = "El progreso se guarda automáticamente en este equipo.\nPulsa M en cualquier momento para alternar la música.", AutoSize = true, ForeColor = Color.FromArgb(180, 180, 205), Location = new Point(34, 137), Font = new Font("Segoe UI", 9) };
        var close = new Button { Text = "LISTO", Location = new Point(318, 194), Size = new Size(90, 34), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 40, 105), ForeColor = Color.White };
        dialog.Controls.AddRange([heading, sound, info, close]);
        dialog.AcceptButton = close;
        dialog.ShowDialog(this);
    }

    private void ShowControls() => MessageBox.Show(this,
        "A / D o flechas: moverse · Espacio / W / ↑: saltar · J / X: atacar\n" +
        "Flechas izquierda/derecha en el menú: cambiar mazmorra · Enter: entrar\nM: música · C: cambiar cazador · Esc: volver al menú\n\n" +
        "Explora la falla, reúne ecos y derrota al jefe. Cada victoria concede experiencia y créditos " +
        "para subir de rango desde E hasta S. El progreso se guarda automáticamente.",
        "Guía del cazador", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void StartDungeon()
    {
        using var game = new PlatformerForm(_selectedDungeon, _profile, _music);
        game.ShowDialog(this);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        float sx = ClientSize.Width / DesignWidth, sy = ClientSize.Height / DesignHeight;
        g.ScaleTransform(sx, sy);
        g.DrawImage(_menuBackground, new RectangleF(0, 0, DesignWidth, DesignHeight));
        DrawHunterIdle(g);
        DrawHover(g);
        DrawDynamicPanels(g);
    }

    private void DrawHunterIdle(Graphics g)
    {
        float sway = MathF.Sin((float)_idleTime * 1.15f) * 3.0f;
        float bob = MathF.Sin((float)_idleTime * 2.3f) * 2.1f;
        RectangleF body = new(578 + sway, 91 + bob, 714, 662);
        // The original hunter cutout gets a living violet aura and a gentle cloak-like sway.
        using (var aura = new Pen(Color.FromArgb(34, 171, 82, 255), 13))
        using (var glow = new Pen(Color.FromArgb(24, 210, 111, 255), 25))
        {
            g.DrawEllipse(glow, 810 + sway, 122 + bob, 320, 440);
            g.DrawEllipse(aura, 835 + sway, 145 + bob, 270, 385);
        }
        g.DrawImage(_hunterSprite, body);

        float pulse = 0.5f + 0.5f * MathF.Sin((float)_idleTime * 4.4f);
        PointF sword = new(752 + sway, 637 + bob);
        PointF tip = new(805 + sway, 574 + bob);
        using (var glow = new Pen(Color.FromArgb((int)(35 + 50 * pulse), 155, 68, 255), 16))
        using (var edge = new Pen(Color.FromArgb((int)(150 + 80 * pulse), 206, 142, 255), 4))
        using (var core = new Pen(Color.FromArgb(220, 238, 226, 255), 1.6f))
        {
            glow.StartCap = glow.EndCap = LineCap.Round;
            edge.StartCap = edge.EndCap = LineCap.Round;
            core.StartCap = core.EndCap = LineCap.Round;
            g.DrawLine(glow, sword, tip);
            g.DrawLine(edge, sword, tip);
            g.DrawLine(core, sword, tip);
        }

        for (int i = 0; i < 8; i++)
        {
            float phase = (float)_idleTime * (0.65f + i * 0.035f) + i * 0.91f;
            float x = 670 + i * 68 + MathF.Sin(phase) * 10;
            float y = 185 + (float)((_idleTime * (25 + i * 3) + i * 97) % 470);
            float size = 2.2f + (i % 3) * 1.2f;
            using var mote = new SolidBrush(Color.FromArgb(100 + (int)(100 * pulse), 190, 126, 255));
            g.FillEllipse(mote, x, y, size, size * 1.8f);
        }
    }

    private void DrawHover(Graphics g)
    {
        if (_hovered.Length == 0 || _hovered == "music") return;
        RectangleF rect = _hovered switch
        {
            "play" => new(50, 296, 356, 54),
            "dungeon" => new(50, 369, 356, 54),
            "hunter" => new(50, 439, 356, 53),
            "settings" => new(50, 509, 356, 53),
            "controls" => new(50, 579, 356, 53),
            _ => RectangleF.Empty
        };
        if (rect.IsEmpty) return;
        PointF[] corners = [new(rect.Left + 15, rect.Top), new(rect.Right - 15, rect.Top), new(rect.Right, rect.Top + 15), new(rect.Right, rect.Bottom - 15), new(rect.Right - 15, rect.Bottom), new(rect.Left + 15, rect.Bottom), new(rect.Left, rect.Bottom - 15), new(rect.Left, rect.Top + 15)];
        using var path = new GraphicsPath();
        path.AddPolygon(corners);
        using var fill = new SolidBrush(Color.FromArgb(_hovered == "play" ? 50 : 32, _hovered == "play" ? 46 : 159, _hovered == "play" ? 201 : 116, 255));
        using var edge = new Pen(Color.FromArgb(215, _hovered == "play" ? 91 : 199, _hovered == "play" ? 225 : 255, 255), 2.5f);
        g.FillPath(fill, path);
        g.DrawPath(edge, path);
    }

    private void DrawDynamicPanels(Graphics g)
    {
        using var card = new SolidBrush(Color.FromArgb(255, 9, 8, 25));
        using var edge = new Pen(Color.FromArgb(180, 159, 111, 244), 1.4f);
        using var white = new SolidBrush(Color.FromArgb(246, 243, 255));
        using var muted = new SolidBrush(Color.FromArgb(195, 203, 224));
        using var cyan = new SolidBrush(Color.FromArgb(147, 226, 255));
        using var normal = new Font("Segoe UI", 11, FontStyle.Regular);
        using var statsFont = new Font("Segoe UI", 9, FontStyle.Regular);
        using var dungeonFont = new Font("Segoe UI", 18, FontStyle.Bold);

        // Replace the reference's example hunter/save data with the current local profile.
        g.FillRectangle(card, 443, 29, 391, 78);
        g.DrawRectangle(edge, 443, 29, 391, 78);
        g.DrawString($"{_profile.Hunter.Name}  •  {_profile.Hunter.ClassName}  •  {_profile.Hunter.Title}", normal, white, 459, 35);
        g.DrawString($"RANGO {_profile.Rank}  •  {_profile.Experience}/{_profile.NextRankExperience} XP  •  {_profile.Credits} CR  •  {_profile.ClearedDungeons} MAZMORRAS", statsFont, muted, new RectangleF(459, 78, 367, 20));
        using (var barBack = new SolidBrush(Color.FromArgb(255, 30, 29, 53))) g.FillRectangle(barBack, 459, 68, 360, 2);
        using (var bar = new LinearGradientBrush(new Rectangle(459, 68, 360, 2), Color.FromArgb(151, 74, 255), Color.FromArgb(67, 217, 255), LinearGradientMode.Horizontal))
            g.FillRectangle(bar, 459, 68, 360 * Math.Clamp((float)_profile.Experience / Math.Max(1, _profile.NextRankExperience), 0, 1), 2);

        // Keep the stage plaque readable while the dungeon changes.
        g.FillRectangle(card, 418, 660, 545, 80);
        using (var line = new Pen(Color.FromArgb(155, 148, 119, 237), 1)) g.DrawLine(line, 469, 662, 914, 662);
        var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        DungeonDefinition dungeon = DungeonCatalog.All[_selectedDungeon];
        g.DrawString(dungeon.Name.ToUpperInvariant(), dungeonFont, white, new RectangleF(423, 666, 535, 32), center);
        using var plaqueCaption = new Font("Segoe UI", 9, FontStyle.Regular);
        g.DrawString($"{dungeon.Region}  ·  JEFE: {dungeon.BossName}", plaqueCaption, muted, new RectangleF(423, 704, 535, 21), center);

        // The artwork's status captions are replaced with accurate local state.
        using var footerBg = new SolidBrush(Color.FromArgb(255, 3, 5, 15));
        g.FillRectangle(footerBg, 0, 735, 470, 33);
        g.DrawString("●  PROGRESO LOCAL · GUARDADO AUTOMÁTICO", statsFont, muted, 20, 741);
        g.FillRectangle(footerBg, 1200, 735, 160, 33);
        g.DrawString(_music.Enabled ? "♫ MÚSICA: SÍ" : "♫ MÚSICA: NO", statsFont, muted, new RectangleF(1208, 741, 148, 20));
    }
}
