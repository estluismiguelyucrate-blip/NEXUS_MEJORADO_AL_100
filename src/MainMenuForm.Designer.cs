#nullable enable

namespace NexusCopia;

partial class MainMenuForm
{
    private System.ComponentModel.IContainer? components = null;
    private Panel panelMenu = null!;
    private Label lblMenuTitle = null!;
    private Button btnPlay = null!;
    private Button btnContinue = null!;
    private Button btnDungeon = null!;
    private Button btnHunter = null!;
    private Button btnDifficulty = null!;
    private Button btnSettings = null!;
    private Button btnControls = null!;
    private Label lblSqlStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        panelMenu = new Panel();
        lblMenuTitle = new Label();
        btnPlay = new Button();
        btnContinue = new Button();
        btnDungeon = new Button();
        btnHunter = new Button();
        btnDifficulty = new Button();
        btnSettings = new Button();
        btnControls = new Button();
        lblSqlStatus = new Label();
        panelMenu.SuspendLayout();
        SuspendLayout();
        // panelMenu
        panelMenu.BackColor = Color.FromArgb(12, 10, 30);
        panelMenu.Controls.Add(lblMenuTitle);
        panelMenu.Controls.Add(btnPlay);
        panelMenu.Controls.Add(btnContinue);
        panelMenu.Controls.Add(btnDungeon);
        panelMenu.Controls.Add(btnHunter);
        panelMenu.Controls.Add(btnDifficulty);
        panelMenu.Controls.Add(btnSettings);
        panelMenu.Controls.Add(btnControls);
        panelMenu.Controls.Add(lblSqlStatus);
        panelMenu.Location = new Point(30, 248);
        panelMenu.Name = "panelMenu";
        panelMenu.Size = new Size(420, 472);
        panelMenu.TabIndex = 0;
        // lblMenuTitle
        lblMenuTitle.Font = new Font("Bahnschrift SemiBold", 15F, FontStyle.Bold, GraphicsUnit.Point);
        lblMenuTitle.ForeColor = Color.FromArgb(224, 211, 255);
        lblMenuTitle.Location = new Point(17, 8);
        lblMenuTitle.Name = "lblMenuTitle";
        lblMenuTitle.Size = new Size(386, 40);
        lblMenuTitle.TabIndex = 0;
        lblMenuTitle.Text = "PORTAL DE EXPEDICIÓN";
        lblMenuTitle.TextAlign = ContentAlignment.MiddleCenter;
        btnPlay.BackColor = Color.FromArgb(0, 139, 204); btnPlay.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnPlay.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 166, 224); btnPlay.FlatStyle = FlatStyle.Flat; btnPlay.Font = new Font("Bahnschrift SemiBold", 11F, FontStyle.Bold); btnPlay.ForeColor = Color.White; btnPlay.Location = new Point(18, 54); btnPlay.Name = "btnPlay"; btnPlay.Size = new Size(384, 46); btnPlay.TabIndex = 1; btnPlay.Text = "ENTRAR A LA MAZMORRA"; btnPlay.UseVisualStyleBackColor = false;
        btnContinue.BackColor = Color.FromArgb(87, 49, 142); btnContinue.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnContinue.FlatAppearance.MouseOverBackColor = Color.FromArgb(112, 70, 175); btnContinue.FlatStyle = FlatStyle.Flat; btnContinue.Font = new Font("Bahnschrift SemiBold", 11F, FontStyle.Bold); btnContinue.ForeColor = Color.White; btnContinue.Location = new Point(18, 108); btnContinue.Name = "btnContinue"; btnContinue.Size = new Size(384, 46); btnContinue.TabIndex = 2; btnContinue.Text = "CONTINUAR PARTIDA · SQL"; btnContinue.UseVisualStyleBackColor = false;
        btnDungeon.BackColor = Color.FromArgb(25, 20, 49); btnDungeon.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnDungeon.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 34, 78); btnDungeon.FlatStyle = FlatStyle.Flat; btnDungeon.Font = new Font("Bahnschrift SemiBold", 10F, FontStyle.Bold); btnDungeon.ForeColor = Color.FromArgb(232, 229, 255); btnDungeon.Location = new Point(18, 162); btnDungeon.Name = "btnDungeon"; btnDungeon.Size = new Size(384, 46); btnDungeon.TabIndex = 3; btnDungeon.Text = "CAMBIAR MAZMORRA"; btnDungeon.UseVisualStyleBackColor = false;
        btnHunter.BackColor = Color.FromArgb(25, 20, 49); btnHunter.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnHunter.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 34, 78); btnHunter.FlatStyle = FlatStyle.Flat; btnHunter.Font = new Font("Bahnschrift SemiBold", 10F, FontStyle.Bold); btnHunter.ForeColor = Color.FromArgb(232, 229, 255); btnHunter.Location = new Point(18, 216); btnHunter.Name = "btnHunter"; btnHunter.Size = new Size(384, 46); btnHunter.TabIndex = 4; btnHunter.Text = "CAMBIAR CAZADOR"; btnHunter.UseVisualStyleBackColor = false;
        btnDifficulty.BackColor = Color.FromArgb(25, 20, 49); btnDifficulty.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnDifficulty.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 34, 78); btnDifficulty.FlatStyle = FlatStyle.Flat; btnDifficulty.Font = new Font("Bahnschrift SemiBold", 10F, FontStyle.Bold); btnDifficulty.ForeColor = Color.FromArgb(232, 229, 255); btnDifficulty.Location = new Point(18, 270); btnDifficulty.Name = "btnDifficulty"; btnDifficulty.Size = new Size(384, 46); btnDifficulty.TabIndex = 5; btnDifficulty.Text = "DIFICULTAD"; btnDifficulty.UseVisualStyleBackColor = false;
        btnSettings.BackColor = Color.FromArgb(25, 20, 49); btnSettings.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 34, 78); btnSettings.FlatStyle = FlatStyle.Flat; btnSettings.Font = new Font("Bahnschrift SemiBold", 10F, FontStyle.Bold); btnSettings.ForeColor = Color.FromArgb(232, 229, 255); btnSettings.Location = new Point(18, 324); btnSettings.Name = "btnSettings"; btnSettings.Size = new Size(384, 46); btnSettings.TabIndex = 6; btnSettings.Text = "CONFIGURACIÓN"; btnSettings.UseVisualStyleBackColor = false;
        btnControls.BackColor = Color.FromArgb(25, 20, 49); btnControls.FlatAppearance.BorderColor = Color.FromArgb(132, 93, 214); btnControls.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 34, 78); btnControls.FlatStyle = FlatStyle.Flat; btnControls.Font = new Font("Bahnschrift SemiBold", 10F, FontStyle.Bold); btnControls.ForeColor = Color.FromArgb(232, 229, 255); btnControls.Location = new Point(18, 378); btnControls.Name = "btnControls"; btnControls.Size = new Size(384, 46); btnControls.TabIndex = 7; btnControls.Text = "CONTROLES Y OBJETIVOS"; btnControls.UseVisualStyleBackColor = false;
        // lblSqlStatus
        lblSqlStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
        lblSqlStatus.ForeColor = Color.FromArgb(156, 222, 255);
        lblSqlStatus.Location = new Point(20, 435);
        lblSqlStatus.Name = "lblSqlStatus";
        lblSqlStatus.Size = new Size(380, 24);
        lblSqlStatus.TabIndex = 8;
        lblSqlStatus.Text = "PROGRESO Y PARTIDAS · SQL SERVER";
        lblSqlStatus.TextAlign = ContentAlignment.MiddleCenter;
        // MainMenuForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(5, 6, 18);
        ClientSize = new Size(1360, 768);
        Controls.Add(panelMenu);
        MinimumSize = new Size(900, 540);
        Name = "MainMenuForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NEXUS COPY | Cazadores del Umbral";
        WindowState = FormWindowState.Maximized;
        panelMenu.ResumeLayout(false);
        ResumeLayout(false);
    }
}
