using System.Drawing.Drawing2D;

namespace NexusCopia;

internal sealed class MainMenuForm : Form
{
    private const float DesignWidth = 1360;
    private const float DesignHeight = 768;
    private readonly HunterProgress _profile = ProgressStore.Load();
    private readonly OriginalMusic _music = new();
    private readonly Image? _logo;
    private readonly NeonMenuButton _play;
    private readonly NeonMenuButton _dungeon;
    private readonly NeonMenuButton _hunter;
    private readonly NeonMenuButton _settings;
    private readonly NeonMenuButton _controls;
    private readonly NeonMenuButton _musicButton;
    private int _selectedDungeon;

    public MainMenuForm()
    {
        Text = "NEXUS COPY | Cazadores del Umbral";
        ClientSize = new Size(1360, 768);
        MinimumSize = new Size(1050, 650);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(5, 6, 18);
        DoubleBuffered = true;
        KeyPreview = true;

        string logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "nexus-logo.png");
        if (File.Exists(logoPath)) _logo = Image.FromFile(logoPath);
        _selectedDungeon = Math.Clamp(_profile.ClearedDungeons, 0, DungeonCatalog.All.Length - 1);

        _play = AddButton("▶", "ENTRAR A LA MAZMORRA", true, StartDungeon);
        _dungeon = AddButton("◀", "CAMBIAR MAZMORRA", false, () =>
        {
            _selectedDungeon = (_selectedDungeon + 1) % DungeonCatalog.All.Length;
            Invalidate();
        });
        _hunter = AddButton("✦", "CAMBIAR CAZADOR", false, () =>
        {
            int index = Array.FindIndex(HunterCatalog.All, x => x.Id == _profile.HunterId);
            _profile.HunterId = HunterCatalog.All[(index + 1) % HunterCatalog.All.Length].Id;
            ProgressStore.Save(_profile);
            Invalidate();
        });
        _settings = AddButton("⚙", "CONFIGURACIÓN", false, ShowSettings);
        _controls = AddButton("⌨", "CONTROLES Y OBJETIVOS", false, ShowControls);
        _musicButton = AddButton("♫", "MÚSICA: SÍ  [M]", false, ToggleMusic);
        _musicButton.Size = new Size(170, 38);
        Resize += (_, _) => PositionControls();
        PositionControls();

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.KeyCode == Keys.M) ToggleMusic();
            if (e.KeyCode == Keys.Enter) StartDungeon();
            if (e.KeyCode == Keys.Left) { _selectedDungeon = (_selectedDungeon + DungeonCatalog.All.Length - 1) % DungeonCatalog.All.Length; Invalidate(); }
            if (e.KeyCode == Keys.Right) { _selectedDungeon = (_selectedDungeon + 1) % DungeonCatalog.All.Length; Invalidate(); }
        };

        _music.PlayMenu();
        FormClosed += (_, _) => { _music.Dispose(); _logo?.Dispose(); };
    }

    private NeonMenuButton AddButton(string icon, string label, bool primary, Action action)
    {
        var button = new NeonMenuButton(icon, label, primary) { Anchor = AnchorStyles.Left | AnchorStyles.Top };
        button.Click += (_, _) => action();
        Controls.Add(button);
        return button;
    }

    private void PositionControls()
    {
        float sx = ClientSize.Width / DesignWidth;
        float sy = ClientSize.Height / DesignHeight;
        int x = (int)(50 * sx);
        int width = Math.Clamp((int)(355 * sx), 300, 390);
        int top = (int)(298 * sy);
        int gap = Math.Max(8, (int)(12 * sy));
        int[] heights = [(int)(54 * sy), (int)(48 * sy), (int)(48 * sy), (int)(48 * sy), (int)(48 * sy)];
        var buttons = new[] { _play, _dungeon, _hunter, _settings, _controls };
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Bounds = new Rectangle(x, top, width, heights[i]);
            top += heights[i] + gap;
        }
        _musicButton.Location = new Point((int)(20 * sx), (int)(20 * sy));
        _musicButton.Size = new Size((int)(170 * sx), (int)(38 * sy));
        Invalidate();
    }

    private void ToggleMusic()
    {
        _music.Toggle();
        _musicButton.Text = _music.Enabled ? "MÚSICA: SÍ  [M]" : "MÚSICA: NO  [M]";
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
        "A / D o flechas: moverse · Espacio: saltar · J / X: habilidad\n" +
        "Flechas izquierda/derecha: cambiar mazmorra · Enter: entrar · M: música · Esc: salir\n\n" +
        "Explora la falla, reúne ecos y derrota al jefe. Cada victoria concede experiencia y créditos " +
        "para subir de rango desde E hasta S. El progreso se guarda automáticamente.",
        "Guía del cazador", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void StartDungeon()
    {
        using var game = new PlatformerForm(_selectedDungeon, _profile, _music);
        game.ShowDialog(this);
        Invalidate();
    }

    private void PaintMenu(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        float sx = ClientSize.Width / DesignWidth, sy = ClientSize.Height / DesignHeight;
        g.ScaleTransform(sx, sy);
        g.SetClip(new RectangleF(0, 0, DesignWidth, DesignHeight));

        using (var sky = new LinearGradientBrush(new Rectangle(0, 0, (int)DesignWidth, (int)DesignHeight), Color.FromArgb(20, 19, 48), Color.FromArgb(4, 6, 18), 90))
            g.FillRectangle(sky, 0, 0, DesignWidth, DesignHeight);

        // Luminous dimensional rift and distant mountains establish depth behind the menu.
        using (var haze = new PathGradientBrush(new[] { new Point(420, 40), new Point(1170, 45), new Point(1340, 630), new Point(310, 690) }))
        {
            haze.CenterColor = Color.FromArgb(115, 80, 90, 191);
            haze.SurroundColors = [Color.FromArgb(0, 8, 9, 25), Color.FromArgb(0, 8, 9, 25), Color.FromArgb(35, 73, 79, 146), Color.FromArgb(0, 8, 9, 25)];
            g.FillRectangle(haze, 280, 0, 1080, 720);
        }
        DrawMountains(g);
        DrawRift(g);
        DrawRuins(g);
        DrawHunter(g);
        DrawParticles(g);
        DrawVignette(g);
        DrawHud(g);
    }

    private static void DrawMountains(Graphics g)
    {
        using var far = new SolidBrush(Color.FromArgb(95, 74, 83, 139));
        using var near = new SolidBrush(Color.FromArgb(190, 20, 23, 49));
        using var ridge = new Pen(Color.FromArgb(125, 167, 178, 226), 2);
        Point[] peaks = [new(345, 495), new(495, 300), new(610, 430), new(770, 245), new(945, 437), new(1080, 285), new(1350, 490), new(1350, 700), new(345, 700)];
        g.FillPolygon(far, peaks);
        g.DrawLines(ridge, peaks[..7]);
        Point[] shadow = [new(285, 600), new(470, 440), new(580, 510), new(740, 390), new(915, 560), new(1095, 405), new(1360, 560), new(1360, 760), new(285, 760)];
        g.FillPolygon(near, shadow);
    }

    private static void DrawRift(Graphics g)
    {
        using var haze = new SolidBrush(Color.FromArgb(32, 172, 174, 255));
        using var beam = new LinearGradientBrush(new Rectangle(620, 95, 600, 470), Color.FromArgb(0, 102, 143, 255), Color.FromArgb(68, 170, 170, 255), 70f);
        g.FillEllipse(haze, 590, 80, 590, 430);
        g.FillEllipse(beam, 710, 155, 410, 330);
        using var arc = new Pen(Color.FromArgb(90, 192, 177, 255), 2);
        g.DrawArc(arc, 655, 160, 485, 355, 195, 151);
    }

    private static void DrawRuins(Graphics g)
    {
        using var stone = new SolidBrush(Color.FromArgb(160, 10, 12, 30));
        using var edge = new Pen(Color.FromArgb(80, 155, 148, 220), 2);
        Point[] leftPillar = [new(362, 475), new(390, 295), new(425, 291), new(413, 483), new(449, 502), new(431, 522)];
        Point[] rightPillar = [new(1272, 440), new(1290, 251), new(1324, 240), new(1332, 470), new(1360, 490), new(1360, 530)];
        g.FillPolygon(stone, leftPillar); g.DrawLines(edge, leftPillar[..4]);
        g.FillPolygon(stone, rightPillar); g.DrawLines(edge, rightPillar[..4]);
        using var chain = new Pen(Color.FromArgb(100, 31, 29, 63), 5);
        g.DrawBezier(chain, 1125, 285, 1175, 315, 1190, 338, 1250, 352);
        g.DrawBezier(chain, 1152, 295, 1202, 321, 1215, 344, 1275, 356);
    }

    private static void DrawHunter(Graphics g)
    {
        // Original stylized hunter silhouette, drawn in code so the menu remains easy to art-direct.
        using var cloak = new SolidBrush(Color.FromArgb(248, 9, 9, 25));
        using var cloakLight = new SolidBrush(Color.FromArgb(255, 20, 20, 47));
        using var trim = new Pen(Color.FromArgb(190, 95, 63, 191), 3);
        using var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        g.FillEllipse(shadow, 770, 653, 530, 54);

        Point[] coat = [new(875, 235), new(840, 280), new(805, 450), new(690, 572), new(645, 645), new(835, 611), new(921, 538), new(993, 649), new(1164, 661), new(1110, 556), new(1060, 444), new(1050, 300), new(1008, 248)];
        g.FillPolygon(cloak, coat);
        g.DrawLines(trim, [new(883, 258), new(855, 355), new(824, 453), new(714, 570)]);
        g.DrawLines(trim, [new(1015, 276), new(1040, 405), new(1098, 538), new(1146, 626)]);
        Point[] cape = [new(868, 290), new(812, 334), new(759, 464), new(672, 530), new(592, 609), new(785, 575), new(886, 470), new(946, 362)];
        g.FillPolygon(cloakLight, cape);
        g.DrawLines(trim, [new(859, 317), new(812, 422), new(730, 512), new(625, 590)]);

        // Neck and head, turned away toward the rift.
        using var skin = new SolidBrush(Color.FromArgb(255, 30, 25, 47));
        g.FillRectangle(skin, 918, 188, 48, 69);
        g.FillEllipse(skin, 888, 112, 110, 133);
        using var hair = new SolidBrush(Color.FromArgb(255, 10, 9, 25));
        g.FillPolygon(hair, [new(889, 174), new(860, 140), new(878, 95), new(862, 63), new(902, 77), new(921, 47), new(942, 80), new(979, 64), new(987, 103), new(1010, 124), new(985, 180), new(965, 136), new(936, 169), new(923, 126), new(901, 172)]);
        using var glint = new Pen(Color.FromArgb(160, 157, 107, 255), 2);
        g.DrawLine(glint, 899, 124, 913, 150);

        // Blade and violet energy trace.
        using var bladeGlow = new Pen(Color.FromArgb(80, 166, 91, 255), 18) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var blade = new Pen(Color.FromArgb(230, 200, 163, 255), 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(bladeGlow, 836, 448, 692, 682);
        g.DrawLine(blade, 836, 448, 692, 682);
        using var core = new Pen(Color.FromArgb(255, 93, 44, 255), 2);
        g.DrawLine(core, 843, 443, 700, 672);
    }

    private static void DrawParticles(Graphics g)
    {
        var random = new Random(27);
        using var glow = new SolidBrush(Color.FromArgb(100, 146, 101, 255));
        using var spark = new SolidBrush(Color.FromArgb(210, 203, 175, 255));
        for (int i = 0; i < 45; i++)
        {
            float x = 410 + random.Next(930), y = random.Next(30, 650), size = random.Next(2, 8);
            g.FillEllipse(i % 5 == 0 ? spark : glow, x, y, size, size);
        }
    }

    private static void DrawVignette(Graphics g)
    {
        using var fade = new LinearGradientBrush(new Rectangle(0, 0, 520, 768), Color.FromArgb(235, 4, 5, 17), Color.FromArgb(0, 4, 5, 17), 0f);
        g.FillRectangle(fade, 0, 0, 520, 768);
        using var bottom = new LinearGradientBrush(new Rectangle(0, 625, 1360, 143), Color.FromArgb(0, 4, 5, 17), Color.FromArgb(225, 4, 5, 17), 90);
        g.FillRectangle(bottom, 0, 625, 1360, 143);
    }

    private void DrawHud(Graphics g)
    {
        using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var left = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        using var logoFallback = new Font("Georgia", 52, FontStyle.Bold | FontStyle.Italic);
        using var tagline = new Font("Segoe UI", 12, FontStyle.Bold);
        using var body = new Font("Segoe UI", 11);
        using var title = new Font("Segoe UI", 19, FontStyle.Bold);
        using var small = new Font("Segoe UI", 10);
        using var white = new SolidBrush(Color.FromArgb(247, 245, 255));
        using var muted = new SolidBrush(Color.FromArgb(199, 202, 224));
        using var cyan = new SolidBrush(Color.FromArgb(140, 229, 255));
        using var darkCard = new SolidBrush(Color.FromArgb(202, 10, 9, 25));
        using var cardEdge = new Pen(Color.FromArgb(150, 152, 94, 237), 1.5f);

        if (_logo is not null)
            g.DrawImage(_logo, new Rectangle(28, 64, 373, 170));
        else
            g.DrawString("NEXUS", logoFallback, white, new RectangleF(40, 85, 360, 85), center);
        g.DrawString("CAZADORES DEL UMBRAL", tagline, white, new RectangleF(45, 216, 365, 30), center);
        g.DrawString("Explora fallas · crea tu rango · vence al guardián", small, muted, new RectangleF(34, 248, 385, 24), center);

        // Hunter card in the top center, showing live local save data.
        g.FillRectangle(darkCard, 443, 28, 394, 82);
        g.DrawRectangle(cardEdge, 443, 28, 394, 82);
        g.DrawString($"{_profile.Hunter.Name}  ·  {_profile.Hunter.ClassName}", body, white, new RectangleF(459, 35, 364, 27), left);
        g.DrawString(_profile.Hunter.Title, small, muted, new RectangleF(459, 59, 364, 20), left);
        g.DrawString($"RANGO {_profile.Rank}  ·  {_profile.Experience}/{_profile.NextRankExperience} XP  ·  {_profile.Credits} CR  ·  {_profile.ClearedDungeons} MAZMORRAS", small, cyan, new RectangleF(459, 82, 370, 20), left);
        DrawXpBar(g, 460, 78, 360, _profile.Experience, _profile.NextRankExperience);

        var dungeon = DungeonCatalog.All[_selectedDungeon];
        using var dungeonCard = new SolidBrush(Color.FromArgb(174, 9, 10, 27));
        g.FillRectangle(dungeonCard, 430, 663, 610, 76);
        using var bottomLine = new Pen(Color.FromArgb(155, 139, 111, 230), 1);
        g.DrawLine(bottomLine, 483, 662, 988, 662);
        g.DrawString(dungeon.Name.ToUpperInvariant(), title, white, new RectangleF(440, 671, 590, 34), center);
        g.DrawString($"{dungeon.Region}  ·  RANGO {_profile.Rank}  ·  JEFE: {dungeon.BossName}", small, muted, new RectangleF(440, 707, 590, 24), center);
        g.DrawString("●  PROGRESO LOCAL · GUARDADO AUTOMÁTICO", small, muted, new RectangleF(20, 731, 420, 22), left);
        g.DrawString(_music.Enabled ? "♫  MÚSICA ORIGINAL: SÍ" : "♫  MÚSICA ORIGINAL: NO", small, muted, new RectangleF(1060, 731, 280, 22), left);
    }

    private static void DrawXpBar(Graphics g, float x, float y, float width, int xp, int target)
    {
        using var back = new SolidBrush(Color.FromArgb(85, 3, 3, 14));
        using var fill = new LinearGradientBrush(new RectangleF(x, y, width, 3), Color.FromArgb(117, 77, 255), Color.FromArgb(83, 225, 255), 0f);
        g.FillRectangle(back, x, y, width, 3);
        g.FillRectangle(fill, x, y, width * Math.Clamp((float)xp / Math.Max(1, target), 0, 1), 3);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintMenu(this, e);
    }
}

internal sealed class NeonMenuButton : Button
{
    private readonly string _icon;
    private readonly bool _primary;
    private bool _hover;

    public NeonMenuButton(string icon, string text, bool primary)
    {
        _icon = icon;
        _primary = primary;
        Text = text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 11, FontStyle.Bold);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Color.Black);
        var outer = new Rectangle(2, 2, Width - 5, Height - 5);
        using var path = CreateCutCorners(outer, 15);
        Color edge = _primary ? Color.FromArgb(77, 222, 255) : Color.FromArgb(177, 105, 255);
        Color fillA = _primary ? Color.FromArgb(42, 185, 239) : Color.FromArgb(214, 17, 14, 40);
        Color fillB = _primary ? Color.FromArgb(18, 87, 199) : Color.FromArgb(221, 18, 12, 34);
        if (_hover) edge = _primary ? Color.FromArgb(183, 247, 255) : Color.FromArgb(221, 162, 255);
        using var glow = new Pen(Color.FromArgb(_hover ? 75 : 30, edge), _hover ? 8 : 5);
        using var fill = new LinearGradientBrush(outer, fillA, fillB, 0f);
        using var outline = new Pen(edge, _primary ? 2.5f : 1.8f);
        g.DrawPath(glow, path);
        g.FillPath(fill, path);
        g.DrawPath(outline, path);
        using var iconFont = new Font("Segoe UI Symbol", _primary ? 19 : 16, FontStyle.Bold);
        using var textFont = new Font("Segoe UI", _primary ? 12 : 11, FontStyle.Bold);
        using var iconBrush = new SolidBrush(_primary ? Color.White : Color.FromArgb(222, 204, 255));
        using var textBrush = new SolidBrush(Color.FromArgb(246, 244, 255));
        g.DrawString(_icon, iconFont, iconBrush, new RectangleF(20, 0, 35, Height), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        g.DrawString(Text, textFont, textBrush, new RectangleF(54, 0, Width - 72, Height), new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
    }

    private static GraphicsPath CreateCutCorners(Rectangle bounds, int cut)
    {
        var path = new GraphicsPath();
        path.AddPolygon([new(bounds.Left + cut, bounds.Top), new(bounds.Right - cut, bounds.Top), new(bounds.Right, bounds.Top + cut), new(bounds.Right, bounds.Bottom - cut), new(bounds.Right - cut, bounds.Bottom), new(bounds.Left + cut, bounds.Bottom), new(bounds.Left, bounds.Bottom - cut), new(bounds.Left, bounds.Top + cut)]);
        path.CloseFigure();
        return path;
    }
}
