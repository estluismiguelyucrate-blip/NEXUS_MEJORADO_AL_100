#nullable enable

namespace NexusCopia;

partial class MainMenuForm
{
    private System.ComponentModel.IContainer? components = null;
    private Button btnPlay = null!;
    private Button btnContinue = null!;
    private Button btnDungeon = null!;
    private Button btnHunter = null!;
    private Button btnDifficulty = null!;
    private Button btnSettings = null!;
    private Button btnControls = null!;
    private Button btnProgress = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        btnPlay = new Button();
        btnContinue = new Button();
        btnDungeon = new Button();
        btnHunter = new Button();
        btnDifficulty = new Button();
        btnSettings = new Button();
        btnControls = new Button();
        btnProgress = new Button();
        SuspendLayout();
        ConfigureMenuButton(btnPlay, "btnPlay", "▶  ENTRAR A LA MAZMORRA  ✦", Color.FromArgb(0, 146, 205), 11F);
        ConfigureMenuButton(btnContinue, "btnContinue", "CONTINUAR PARTIDA", Color.FromArgb(45, 27, 77), 9F);
        ConfigureMenuButton(btnDungeon, "btnDungeon", "◀  CAMBIAR MAZMORRA  ▶", Color.FromArgb(16, 12, 34), 10F);
        ConfigureMenuButton(btnHunter, "btnHunter", "✦  CAMBIAR CAZADOR", Color.FromArgb(16, 12, 34), 10F);
        ConfigureMenuButton(btnDifficulty, "btnDifficulty", "DIFICULTAD", Color.FromArgb(45, 27, 77), 9F);
        ConfigureMenuButton(btnSettings, "btnSettings", "⚙  CONFIGURACIÓN", Color.FromArgb(16, 12, 34), 10F);
        ConfigureMenuButton(btnControls, "btnControls", "⌨  CONTROLES Y OBJETIVOS", Color.FromArgb(16, 12, 34), 10F);
        ConfigureMenuButton(btnProgress, "btnProgress", "PROGRESO", Color.FromArgb(45, 27, 77), 8.5F);
        btnContinue.Visible = true;
        btnContinue.Enabled = false;
        btnContinue.AccessibleName = "Continuar partida guardada en SQL";
        btnDifficulty.AccessibleName = "Cambiar dificultad";
        btnPlay.AccessibleName = "Entrar a la mazmorra";
        btnDungeon.AccessibleName = "Cambiar mazmorra";
        btnHunter.AccessibleName = "Cambiar cazador";
        btnSettings.AccessibleName = "Configuración";
        btnControls.AccessibleName = "Controles y objetivos";
        btnProgress.AccessibleName = "Consultar rango y progreso";
        Controls.AddRange([btnPlay, btnContinue, btnDungeon, btnHunter, btnDifficulty, btnSettings, btnControls, btnProgress]);
        // MainMenuForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(5, 6, 18);
        ClientSize = new Size(1360, 768);
        MinimumSize = new Size(900, 540);
        Name = "MainMenuForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NEXUS COPY | Cazadores del Umbral";
        WindowState = FormWindowState.Maximized;
        ResumeLayout(false);
    }

    private static void ConfigureMenuButton(Button button, string name, string text, Color color, float fontSize)
    {
        button.BackColor = color;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        if (name is "btnDifficulty" or "btnContinue" or "btnProgress")
        {
            button.FlatAppearance.BorderColor = Color.FromArgb(160, 107, 242);
            button.FlatAppearance.BorderSize = 1;
        }
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 40, 105);
        button.FlatAppearance.MouseOverBackColor = color == Color.FromArgb(0, 146, 205)
            ? Color.FromArgb(18, 170, 224) : Color.FromArgb(43, 31, 73);
        button.Font = new Font("Bahnschrift SemiBold", fontSize, FontStyle.Bold, GraphicsUnit.Point);
        button.ForeColor = Color.White;
        button.Name = name;
        button.Location = name switch
        {
            "btnPlay" => new Point(64, 303), "btnDungeon" => new Point(64, 378),
            "btnHunter" => new Point(64, 447), "btnSettings" => new Point(64, 517),
            "btnControls" => new Point(64, 587), "btnDifficulty" => new Point(52, 650),
            "btnContinue" => new Point(168, 650), "btnProgress" => new Point(284, 650), _ => Point.Empty
        };
        button.Size = name is "btnDifficulty" or "btnContinue" or "btnProgress" ? new Size(108, 38) : new Size(328, 38);
        button.TabIndex = name switch { "btnPlay" => 0, "btnDungeon" => 1, "btnHunter" => 2, "btnSettings" => 3, "btnControls" => 4, "btnDifficulty" => 5, "btnContinue" => 6, _ => 7 };
        button.Text = text;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
        button.TextAlign = ContentAlignment.MiddleCenter;
    }
}
