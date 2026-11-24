using System.ComponentModel;

namespace NexusCopia;

/// <summary>Menú interactivo compuesto sobre el arte de referencia de Nexus Copy.</summary>
internal sealed partial class MainMenuForm : Form
{
    private const float DesignWidth = 1360;
    private const float DesignHeight = 768;
    private HunterProgress _profile = new();
    private OriginalMusic _music = null!;
    private Image _menuBackground = null!;
    private ExpeditionSave? _savedExpedition;
    private int _selectedDungeon;
    private int _selectedDifficulty = 1;

    public MainMenuForm()
    {
        InitializeComponent();
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        _profile = ProgressStore.Load();
        _music = new OriginalMusic();
        Text = "NEXUS COPY | Cazadores del Umbral";
        ClientSize = new Size(1360, 768);
        MinimumSize = new Size(900, 540);
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(5, 6, 18);
        DoubleBuffered = true;
        KeyPreview = true;
        Cursor = Cursors.Default;
        Resize += (_, _) => LayoutMenuButtons();
        LayoutMenuButtons();

        string artPath = Path.Combine(AppContext.BaseDirectory, "assets", "menu-background.jpeg");
        if (!File.Exists(artPath))
            throw new FileNotFoundException("Falta el fondo del menú. Vuelve a extraer el repositorio completo y compila NexusCopy.sln.", artPath);
        _menuBackground = Image.FromFile(artPath);
        _selectedDungeon = Math.Clamp(_profile.ClearedDungeons, 0, DungeonCatalog.All.Length - 1);
        _savedExpedition = ProgressStore.LoadExpedition();
        UpdateMenuControls();
        btnPlay.Click += (_, _) => StartDungeon();
        btnContinue.Click += (_, _) => ContinueExpedition();
        btnDungeon.Click += (_, _) => CycleDungeon();
        btnHunter.Click += (_, _) => ChangeHunter();
        btnDifficulty.Click += (_, _) => CycleDifficulty();
        btnSettings.Click += (_, _) => ShowSettings();
        btnControls.Click += (_, _) => ShowControls();
        KeyDown += HandleKeyDown;
        _music.PlayMenu();
        FormClosed += (_, _) =>
        {
            _music.Dispose(); _menuBackground?.Dispose();
        };
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
        else if (e.KeyCode == Keys.M) { ToggleMusic(); e.Handled = true; }
        else if (e.KeyCode == Keys.Enter) { StartDungeon(); e.Handled = true; }
        else if (e.KeyCode is Keys.Left or Keys.Right)
        {
            CycleDungeon(); e.Handled = true;
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

    private void CycleDungeon()
    {
        _selectedDungeon = (_selectedDungeon + 1) % DungeonCatalog.All.Length;
        UpdateMenuControls();
        Invalidate();
    }

    private void UpdateMenuControls()
    {
        btnDungeon.Text = $"◀  {DungeonCatalog.All[_selectedDungeon].Name.ToUpperInvariant()}  ▶";
        btnHunter.Text = $"✦  {_profile.Hunter.Name.ToUpperInvariant()}";
        DifficultyDefinition selected = DifficultyCatalog.All[_selectedDifficulty];
        btnDifficulty.Text = $"DIFICULTAD · {selected.Name}";
        btnContinue.Visible = true;
        btnContinue.Enabled = _savedExpedition is not null;
        btnContinue.Text = _savedExpedition is null ? "SIN PARTIDA" : "CONTINUAR";
    }

    private void LayoutMenuButtons()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        float sx = ClientSize.Width / DesignWidth, sy = ClientSize.Height / DesignHeight;
        Place(btnPlay, 64, 303, 328, 38, sx, sy);
        Place(btnDungeon, 64, 378, 328, 38, sx, sy);
        Place(btnHunter, 64, 447, 328, 38, sx, sy);
        Place(btnSettings, 64, 517, 328, 38, sx, sy);
        Place(btnControls, 64, 587, 328, 38, sx, sy);
        Place(btnDifficulty, 52, 650, 174, 38, sx, sy);
        Place(btnContinue, 232, 650, 174, 38, sx, sy);
    }

    private static void Place(Control control, int x, int y, int width, int height, float sx, float sy) =>
        control.Bounds = new Rectangle((int)MathF.Round(x * sx), (int)MathF.Round(y * sy),
            (int)MathF.Round(width * sx), (int)MathF.Round(height * sy));

    private void ToggleMusic()
    {
        _music.Toggle();
        Invalidate();
    }

    private void CycleDifficulty()
    {
        _selectedDifficulty = (_selectedDifficulty + 1) % DifficultyCatalog.All.Length;
        UpdateMenuControls();
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
        UpdateMenuControls();
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
        "Flechas izquierda/derecha: cambiar mazmorra · Enter: entrar · V: dificultad\nF5: guardar en SQL · Esc: guardar y volver al menú\n\n" +
        "La partida guarda posición, checkpoints, fragmentos, enemigos, trampas, jefe, vidas y dificultad. " +
        "Usa CONTINUAR PARTIDA para reanudarla.",
        "Guía del cazador", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void StartDungeon()
    {
        using var game = new PlatformerForm(_selectedDungeon, _selectedDifficulty, _profile, _music);
        game.ShowDialog(this);
        RefreshSavedExpedition();
        Invalidate();
    }

    private void ContinueExpedition()
    {
        if (_savedExpedition is null) return;
        ExpeditionSave save = _savedExpedition;
        using var game = new PlatformerForm(save.DungeonIndex, save.DifficultyIndex, _profile, _music, save);
        game.ShowDialog(this);
        RefreshSavedExpedition();
        Invalidate();
    }

    private void RefreshSavedExpedition()
    {
        try { _savedExpedition = ProgressStore.LoadExpedition(); }
        catch (InvalidOperationException ex) { MessageBox.Show(this, ex.Message, "SQL Server no disponible", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        UpdateMenuControls();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_menuBackground is null) return;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        e.Graphics.DrawImage(_menuBackground, ClientRectangle);
    }
}
