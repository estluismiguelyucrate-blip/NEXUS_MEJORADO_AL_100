using System.Drawing.Drawing2D;
using System.Diagnostics;

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
    private readonly Stopwatch _animationClock = Stopwatch.StartNew();
    private readonly Pen _auraOuter = new(Color.FromArgb(24, 210, 111, 255), 25);
    private readonly Pen _auraInner = new(Color.FromArgb(34, 171, 82, 255), 13);
    private readonly Pen _swordGlow = new(Color.FromArgb(60, 155, 68, 255), 16) { StartCap = LineCap.Round, EndCap = LineCap.Round };
    private readonly Pen _swordEdge = new(Color.FromArgb(180, 206, 142, 255), 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };
    private readonly Pen _swordCore = new(Color.FromArgb(220, 238, 226, 255), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
    private readonly SolidBrush[] _moteBrushes = Enumerable.Range(0, 4)
        .Select(i => new SolidBrush(Color.FromArgb(110 + i * 35, 190, 126, 255))).ToArray();
    private int _selectedDungeon;
    private int _selectedDifficulty = 1;
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

        string artPath = Path.Combine(AppContext.BaseDirectory, "assets", "menu-background.jpeg");
        string spritePath = Path.Combine(AppContext.BaseDirectory, "assets", "characters", "menu-hunter.png");
        if (!File.Exists(artPath) || !File.Exists(spritePath))
            throw new FileNotFoundException("Faltan los recursos del menú. Vuelve a extraer el repositorio completo y compila NexusCopy.sln.", !File.Exists(artPath) ? artPath : spritePath);
        _menuBackground = Image.FromFile(artPath);
        _hunterSprite = Image.FromFile(spritePath);
        _idleTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _idleTimer.Tick += (_, _) =>
        {
            // Limit redraw work to the hunter scene and use elapsed time so the animation
            // keeps the same pace when Windows skips or delays a timer tick.
            float sx = ClientSize.Width / DesignWidth, sy = ClientSize.Height / DesignHeight;
            Invalidate(Rectangle.Round(new RectangleF(565 * sx, 75 * sy, 795 * sx, 685 * sy)));
        };
        _idleTimer.Start();
        _selectedDungeon = Math.Clamp(_profile.ClearedDungeons, 0, DungeonCatalog.All.Length - 1);

        MouseMove += HandleMouseMove;
        MouseLeave += (_, _) => { _hovered = ""; Cursor = Cursors.Default; Invalidate(); };
        MouseClick += HandleMenuClick;
        KeyDown += HandleKeyDown;
        _music.PlayMenu();
        FormClosed += (_, _) =>
        {
            _idleTimer.Stop(); _idleTimer.Dispose(); _music.Dispose(); _menuBackground.Dispose(); _hunterSprite.Dispose();
            _auraOuter.Dispose(); _auraInner.Dispose(); _swordGlow.Dispose(); _swordEdge.Dispose(); _swordCore.Dispose();
            foreach (var brush in _moteBrushes) brush.Dispose();
        };
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
        else if (e.KeyCode == Keys.V)
        {
            CycleDifficulty(); e.Handled = true;
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
            case "difficulty": CycleDifficulty(); break;
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
        if (new RectangleF(42, 650, 370, 65).Contains(p)) return "difficulty";
        return "";
    }

    private void ToggleMusic()
    {
        _music.Toggle();
        Invalidate();
    }

    private void CycleDifficulty()
    {
        _selectedDifficulty = (_selectedDifficulty + 1) % DifficultyCatalog.All.Length;
        Invalidate();
    }

    private void ChangeHunter()
    {
        string previous = _profile.HunterId;
        int index = Array.FindIndex(HunterCatalog.All, x => x.Id == _profile.HunterId);
        _profile.HunterId = HunterCatalog.All[(index + 1) % HunterCatalog.All.Length].Id;
        try { ProgressStore.Save(_profile); }
        catch (InvalidOperationException ex)
        {
            _profile.HunterId = previous;
            MessageBox.Show(this, ex.Message, "No se pudo guardar en SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        Invalidate();
    }

    private void ShowSettings()
    {
        using var dialog = new Form
        {
            Text = "Configuración del cazador", StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
            ClientSize = new Size(490, 300), BackColor = Color.FromArgb(15, 13, 34), ForeColor = Color.White
        };
        var heading = new Label { Text = "AJUSTES DE NEXUS", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(213, 190, 255), AutoSize = true, Location = new Point(28, 24) };
        var sound = new CheckBox { Text = "Música original", Checked = _music.Enabled, ForeColor = Color.White, AutoSize = true, Location = new Point(34, 82), Font = new Font("Segoe UI", 12) };
        sound.CheckedChanged += (_, _) => { if (sound.Checked != _music.Enabled) ToggleMusic(); };
        var info = new Label { Text = $"Progreso guardado solo en SQL Server: {ProgressStore.StorageStatus}.\nLocalDB se configura automáticamente. Para otra instancia, configura NEXUS_SQLSERVER_CONNECTION.\nPulsa M para alternar la música.", AutoSize = true, MaximumSize = new Size(425, 0), ForeColor = Color.FromArgb(180, 180, 205), Location = new Point(34, 128), Font = new Font("Segoe UI", 9) };
        var close = new Button { Text = "LISTO", Location = new Point(360, 242), Size = new Size(90, 34), DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 40, 105), ForeColor = Color.White };
        dialog.Controls.AddRange([heading, sound, info, close]);
        dialog.AcceptButton = close;
        dialog.ShowDialog(this);
    }

    private void ShowControls() => MessageBox.Show(this,
        "A / D o flechas: moverse · Espacio / W / ↑: saltar · J / X: atacar\n" +
        "Flechas izquierda/derecha en el menú: cambiar mazmorra · Enter: entrar\nM: música · C: cazador · V: dificultad · Esc: volver al menú\n\n" +
        "Explora la falla, reúne ecos y derrota al jefe. Cada victoria concede experiencia y créditos " +
        "para subir de rango desde E hasta S. El progreso se guarda automáticamente.",
        "Guía del cazador", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void StartDungeon()
    {
        using var game = new PlatformerForm(_selectedDungeon, _selectedDifficulty, _profile, _music);
        game.ShowDialog(this);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        float sx = ClientSize.Width / DesignWidth, sy = ClientSize.Height / DesignHeight;
        g.ScaleTransform(sx, sy);
        g.DrawImage(_menuBackground, new RectangleF(0, 0, DesignWidth, DesignHeight));
        DrawHunterIdle(g);
        DrawHover(g);
        DrawDynamicPanels(g);
    }

    private void DrawHunterIdle(Graphics g)
    {
        float time = (float)_animationClock.Elapsed.TotalSeconds;
        float sway = MathF.Sin(time * 1.25f) * 2.8f;
        float bob = MathF.Sin(time * 2.45f) * 2.2f;
        float breath = 1f + .0015f * MathF.Sin(time * 2.45f + .6f);
        RectangleF body = new(637 + sway - 327.5f * (breath - 1), 90 + bob - 331 * (breath - 1), 655 * breath, 662 * breath);
        // A larger pulse behind the cutout plus a visible shoulder-to-shoulder sway.
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 3.1f);
        _auraOuter.Width = 22 + 7 * pulse;
        _auraInner.Width = 11 + 4 * pulse;
        g.DrawEllipse(_auraOuter, 810 + sway, 122 + bob, 320 + pulse * 18, 440 + pulse * 12);
        g.DrawEllipse(_auraInner, 835 + sway, 145 + bob, 270 + pulse * 12, 385 + pulse * 8);
        g.DrawImage(_hunterSprite, body);

        PointF sword = new(785 + sway, 588 + bob);
        PointF tip = new(435 + sway, 655 + bob + MathF.Sin(time * 2.2f) * 3.5f);
        _swordGlow.Width = 13 + 7 * pulse;
        _swordGlow.Color = Color.FromArgb((int)(42 + 75 * pulse), 155, 68, 255);
        _swordEdge.Color = Color.FromArgb((int)(120 + 110 * pulse), 206, 142, 255);
        g.DrawLine(_swordGlow, sword, tip);
        g.DrawLine(_swordEdge, sword, tip);
        g.DrawLine(_swordCore, sword, tip);

        for (int i = 0; i < 14; i++)
        {
            float phase = time * (0.75f + i * 0.025f) + i * 0.91f;
            float x = 654 + i * 47 + MathF.Sin(phase) * 18 + sway * .4f;
            float y = 145 + (time * (33 + i * 4) + i * 61) % 510;
            float size = 3 + (i % 3) * 1.7f;
            g.FillEllipse(_moteBrushes[i % _moteBrushes.Length], x, y, size, size * 2.1f);
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
            "difficulty" => new(48, 650, 358, 58),
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
        g.DrawString($"●  {ProgressStore.StorageStatus}", statsFont, muted, 20, 741);
        g.FillRectangle(footerBg, 1200, 735, 160, 33);
        g.DrawString(_music.Enabled ? "♫ MÚSICA: SÍ" : "♫ MÚSICA: NO", statsFont, muted, new RectangleF(1208, 741, 148, 20));

        RectangleF difficultyBox = new(42, 650, 370, 58);
        using var difficultyBack = new SolidBrush(Color.FromArgb(238, 9, 8, 25));
        using var difficultyEdge = new Pen(_hovered == "difficulty" ? Color.FromArgb(255, 208, 173, 255) : Color.FromArgb(185, 159, 111, 244), _hovered == "difficulty" ? 2.4f : 1.4f);
        using var difficultyLabel = new Font("Segoe UI", 8, FontStyle.Bold);
        using var difficultyValue = new Font("Segoe UI", 12, FontStyle.Bold);
        using var difficultyHint = new Font("Segoe UI", 7, FontStyle.Regular);
        g.FillRectangle(difficultyBack, difficultyBox);
        g.DrawRectangle(difficultyEdge, Rectangle.Round(difficultyBox));
        DifficultyDefinition selected = DifficultyCatalog.All[_selectedDifficulty];
        Color difficultyColor = _selectedDifficulty switch { 0 => Color.FromArgb(103, 241, 196), 2 => Color.FromArgb(255, 112, 150), _ => Color.FromArgb(152, 199, 255) };
        using var difficultyBrush = new SolidBrush(difficultyColor);
        var alignCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("DIFICULTAD · V / CLIC PARA CAMBIAR", difficultyLabel, muted, new RectangleF(52, 653, 350, 18), alignCenter);
        g.DrawString(selected.Name, difficultyValue, difficultyBrush, new RectangleF(52, 672, 350, 25), alignCenter);
        using var hintBrush = new SolidBrush(Color.FromArgb(190, 196, 192, 219));
        string difficultyDescription = _selectedDifficulty switch
        {
            0 => "5 vidas · enemigos y jefes más accesibles",
            2 => "2 vidas · enemigos resistentes · jefes feroces",
            _ => "3 vidas · desafío equilibrado"
        };
        g.DrawString(difficultyDescription, difficultyHint, hintBrush, new RectangleF(54, 694, 346, 12), alignCenter);
    }
}
