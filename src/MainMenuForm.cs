using System.Drawing.Drawing2D;

namespace NexusCopia;

internal sealed class MainMenuForm : Form
{
    private readonly HunterProgress _profile = ProgressStore.Load();
    private readonly OriginalMusic _music = new();
    private int _selectedDungeon;

    public MainMenuForm()
    {
        Text = "NEXUS COPY | Cazadores del Umbral";
        ClientSize = new Size(1180, 760); MinimumSize = new Size(980, 680);
        StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(8, 8, 25);
        DoubleBuffered = true; KeyPreview = true;
        _selectedDungeon = Math.Clamp(_profile.ClearedDungeons, 0, DungeonCatalog.All.Length - 1);
        Paint += PaintMenu;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.KeyCode == Keys.M) { _music.Toggle(); Invalidate(); }
        };

        var play = Button("ENTRAR A LA MAZMORRA", 450, 470, 280, 52, true);
        play.Click += (_, _) => StartDungeon(); Controls.Add(play);
        var dungeon = Button("CAMBIAR MAZMORRA   ◀   ▶", 450, 535, 280, 42, false);
        dungeon.Click += (_, _) => { _selectedDungeon = (_selectedDungeon + 1) % DungeonCatalog.All.Length; Invalidate(); };
        Controls.Add(dungeon);
        var hunter = Button("CAMBIAR CAZADOR", 450, 589, 280, 42, false);
        hunter.Click += (_, _) =>
        {
            int index = Array.FindIndex(HunterCatalog.All, x => x.Id == _profile.HunterId);
            _profile.HunterId = HunterCatalog.All[(index + 1) % HunterCatalog.All.Length].Id;
            ProgressStore.Save(_profile); Invalidate();
        };
        Controls.Add(hunter);
        var controls = Button("CONTROLES Y OBJETIVO", 450, 643, 280, 42, false);
        controls.Click += (_, _) => MessageBox.Show(this,
            "A / D o flechas: moverse · Espacio: salto · J / X: habilidad\nM: música · Esc: volver al menú\n\n" +
            "Explora la mazmorra, recoge ecos y derrota al jefe para ganar experiencia y créditos. " +
            "Completa fallas de rango creciente para ascender hasta clase S. La partida se guarda automáticamente.",
            "Guía del cazador", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Controls.Add(controls);
        var music = Button("MÚSICA  M", 20, 20, 135, 36, false);
        music.Click += (_, _) => { _music.Toggle(); music.Text = _music.Enabled ? "MÚSICA  M" : "MÚSICA: OFF"; };
        Controls.Add(music);
        _music.PlayMenu();
        FormClosed += (_, _) => _music.Dispose();
    }

    private void StartDungeon()
    {
        using var game = new PlatformerForm(_selectedDungeon, _profile, _music);
        game.ShowDialog(this);
        Invalidate();
    }

    private static Button Button(string label, int x, int y, int w, int h, bool primary)
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
        using var glow = new SolidBrush(Color.FromArgb(28, 89, 71, 255)); g.FillEllipse(glow, 310, 35, 560, 350);
        using var title = new Font("Segoe UI", 48, FontStyle.Bold); using var subtitle = new Font("Segoe UI", 12, FontStyle.Bold);
        using var line = new Font("Segoe UI", 14, FontStyle.Bold); using var small = new Font("Segoe UI", 10);
        using var white = new SolidBrush(Color.White); using var cyan = new SolidBrush(Color.FromArgb(139, 226, 255));
        using var muted = new SolidBrush(Color.FromArgb(184, 184, 214));
        var center = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("N E X U S", title, white, new RectangleF(0, 110, ClientSize.Width, 80), center);
        g.DrawString("CAZADORES DEL UMBRAL", subtitle, cyan, new RectangleF(0, 195, ClientSize.Width, 30), center);
        var dungeon = DungeonCatalog.All[_selectedDungeon]; var hunter = _profile.Hunter;
        g.DrawString(dungeon.Name.ToUpperInvariant(), line, white, new RectangleF(290, 316, 600, 32), center);
        g.DrawString($"{dungeon.Region}   ·   JEFE: {dungeon.BossName}", small, muted, new RectangleF(290, 350, 600, 24), center);
        g.DrawString($"{hunter.Name}  ·  {hunter.ClassName}  ·  {hunter.Title}", line, cyan, new RectangleF(250, 390, 680, 30), center);
        g.DrawString($"RANGO { _profile.Rank }   ·   {_profile.Experience}/{_profile.NextRankExperience} XP   ·   {_profile.Credits} CRÉDITOS   ·   { _profile.ClearedDungeons } MAZMORRAS", small, muted, new RectangleF(220, 428, 740, 24), center);
        g.DrawString("Explora fallas · crea tu rango · vence al guardián", small, muted, new RectangleF(0, 715, ClientSize.Width, 20), center);
    }
}

