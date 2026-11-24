using System.Drawing.Drawing2D;

namespace NexusCopia;

/// <summary>Modo arcade de plataformas de NEXUS, independiente de la expedición por sectores.</summary>
internal sealed class PlatformerForm : Form
{
    private const float WorldWidth = 2600, GroundY = 535, PlayerWidth = 46, PlayerHeight = 62;
    private readonly Panel _canvas;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Image? _knightSprite;
    private readonly HashSet<Keys> _held = new();
    private readonly List<RectangleF> _platforms = new()
    {
        new(0, GroundY, WorldWidth, 180), new(250,440,150,20), new(500,365,150,20),
        new(770,430,180,20), new(1050,350,160,20), new(1350,420,180,20),
        new(1635,340,170,20), new(1930,415,180,20), new(2210,345,160,20)
    };
    private readonly List<Shard> _shards = new();
    private readonly List<Patroller> _enemies = new();
    private float _x = 65, _y = GroundY - PlayerHeight, _vx, _vy, _camera;
    private float _checkpoint = 65;
    private bool _grounded, _facingRight = true, _won, _gameOver;
    private int _attackTicks, _attackCooldown;
    private int _lives = 3, _score;
    private int _bossHealth = 6, _bossAttackCooldown;
    private bool _bossAwakened, _bossDefeated;
    private string _notice = "Corre, salta y recoge los fragmentos de energía.";
    private int _noticeTicks = 220;

    public PlatformerForm()
    {
        Text = "NEXUS | Salto al Umbral";
        ClientSize = new Size(1100, 680);
        MinimumSize = new Size(850, 570);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(7, 8, 22);
        KeyPreview = true;
        string spritePath = Path.Combine(AppContext.BaseDirectory, "assets", "umbral-caballero.png");
        if (File.Exists(spritePath)) _knightSprite = Image.FromFile(spritePath);
        _canvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 12, 34) };
        _canvas.Paint += PaintWorld;
        _canvas.Resize += (_, _) => _canvas.Invalidate();
        Controls.Add(_canvas);
        SeedLevel();
        KeyDown += OnKeyDown;
        KeyUp += (_, e) => _held.Remove(e.KeyCode);
        Deactivate += (_, _) => _held.Clear();
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => { UpdateGame(); _canvas.Invalidate(); };
        _timer.Start();
        FormClosed += (_, _) => { _timer.Dispose(); _knightSprite?.Dispose(); };
    }

    private void SeedLevel()
    {
        foreach (float x in new float[] { 180,290,350,535,590,810,875,1090,1150,1395,1465,1675,1740,1970,2040,2250,2310,2445 })
        {
            float y = _platforms.Where(p => x >= p.Left - 25 && x <= p.Right + 25).Select(p => p.Top - 24).DefaultIfEmpty(GroundY - 24).Min();
            _shards.Add(new Shard(x, y));
        }
        _enemies.Add(new Patroller(430, GroundY - 30, 405, 490));
        _enemies.Add(new Patroller(900, GroundY - 30, 850, 1010));
        _enemies.Add(new Patroller(1240, GroundY - 30, 1220, 1320));
        _enemies.Add(new Patroller(1800, GroundY - 30, 1830, 1900));
        _enemies.Add(new Patroller(2130, GroundY - 30, 2110, 2180));
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Close(); return; }
        if (e.KeyCode == Keys.R && (_won || _gameOver)) { ResetGame(); return; }
        if (e.KeyCode is Keys.J or Keys.X)
        {
            if (!_held.Contains(e.KeyCode)) SwingAxe();
            _held.Add(e.KeyCode); e.Handled = true; return;
        }
        if (e.KeyCode is Keys.Space or Keys.W or Keys.Up)
        {
            if (!_held.Contains(e.KeyCode) && _grounded && !_won && !_gameOver) { _vy = -12.5f; _grounded = false; }
        }
        _held.Add(e.KeyCode);
        e.Handled = true;
    }

    private void UpdateGame()
    {
        if (_won || _gameOver) return;
        if (_noticeTicks > 0) _noticeTicks--;
        if (_bossAttackCooldown > 0) _bossAttackCooldown--;
        if (_attackTicks > 0) _attackTicks--;
        if (_attackCooldown > 0) _attackCooldown--;
        float input = (_held.Contains(Keys.Right) || _held.Contains(Keys.D) ? 1 : 0) - (_held.Contains(Keys.Left) || _held.Contains(Keys.A) ? 1 : 0);
        _vx += input * .8f;
        _vx *= input == 0 ? .78f : .88f;
        _vx = Math.Clamp(_vx, -5.2f, 5.2f);
        if (input != 0) _facingRight = input > 0;
        float oldBottom = _y + PlayerHeight;
        _x = Math.Clamp(_x + _vx, 0, WorldWidth - PlayerWidth);
        _vy = Math.Min(_vy + .58f, 14);
        _y += _vy;
        _grounded = false;
        var player = new RectangleF(_x, _y, PlayerWidth, PlayerHeight);
        foreach (var platform in _platforms)
        {
            if (_vy >= 0 && oldBottom <= platform.Top + 5 && player.Right > platform.Left && player.Left < platform.Right && player.Bottom >= platform.Top)
            {
                _y = platform.Top - PlayerHeight; _vy = 0; _grounded = true;
            }
        }
        player = new RectangleF(_x, _y, PlayerWidth, PlayerHeight);
        if (_x > 2240 && !_bossAwakened) { _bossAwakened = true; SetNotice("¡El Guardián del Umbral despierta! Ataca cuando se acerque."); }
        if (_bossAwakened && !_bossDefeated)
        {
            var boss = new RectangleF(2460, GroundY - 118, 104, 118);
            if (player.IntersectsWith(boss) && _bossAttackCooldown == 0)
            { _bossAttackCooldown = 55; HitPlayer(); }
        }
        foreach (var shard in _shards.Where(s => !s.Collected))
        {
            if (player.IntersectsWith(new RectangleF(shard.X - 10, shard.Y - 10, 20, 20)))
            { shard.Collected = true; _score += 10; SetNotice("¡Fragmento conseguido! +10 puntos"); }
        }
        foreach (var enemy in _enemies.Where(en => en.Alive))
        {
            enemy.X += enemy.Speed;
            if (enemy.X < enemy.Min || enemy.X > enemy.Max) enemy.Speed *= -1;
            var bounds = new RectangleF(enemy.X, enemy.Y, 34, 30);
            if (!player.IntersectsWith(bounds)) continue;
            if (_vy > 1 && oldBottom <= enemy.Y + 13)
            { enemy.Alive = false; _vy = -8; _score += 25; SetNotice("¡Buen salto! Centinela superado. +25"); }
            else HitPlayer();
        }
        if (_x > 1280 && _checkpoint < 1300) { _checkpoint = 1320; SetNotice("Punto de control activado."); }
        if (_y > 720) HitPlayer();
        if (_x > 2500 && _bossDefeated) { _won = true; SetNotice("¡Mazmorra superada! Presiona R para volver a jugar."); }
        _camera = Math.Clamp(_x - 330, 0, WorldWidth - 1100);
    }

    private void SwingAxe()
    {
        if (_won || _gameOver || _attackCooldown > 0) return;
        _attackTicks = 16; _attackCooldown = 24;
        float playerCenter = _x + PlayerWidth / 2;
        foreach (var enemy in _enemies.Where(en => en.Alive))
        {
            float enemyCenter = enemy.X + 17;
            float forwardDistance = (_facingRight ? enemyCenter - playerCenter : playerCenter - enemyCenter);
            if (forwardDistance is > 0 and < 88 && Math.Abs((enemy.Y + 15) - (_y + PlayerHeight / 2)) < 52)
            {
                enemy.Alive = false; _score += 35; SetNotice("¡Hachazo certero! Centinela derrotado. +35"); return;
            }
        }
        if (_bossAwakened && !_bossDefeated)
        {
            float bossCenter = 2512, playerCenterX = _x + PlayerWidth / 2;
            float forward = _facingRight ? bossCenter - playerCenterX : playerCenterX - bossCenter;
            if (forward is > 0 and < 112 && Math.Abs((GroundY - 59) - (_y + PlayerHeight / 2)) < 86)
            {
                _bossHealth--; _score += 50;
                if (_bossHealth <= 0) { _bossDefeated = true; _score += 250; SetNotice("¡Guardián derrotado! La salida de la mazmorra está abierta."); }
                else SetNotice($"¡Impacto al Guardián! Integridad: {_bossHealth}/6");
                return;
            }
        }
        SetNotice("¡Hachazo!");
    }

    private void HitPlayer()
    {
        _lives--;
        if (_lives <= 0) { _gameOver = true; SetNotice("Fin de la partida. Presiona R para intentarlo otra vez."); return; }
        _x = _checkpoint; _y = GroundY - PlayerHeight - 5; _vx = _vy = 0;
        SetNotice($"¡Cuidado! Te queda{(_lives == 1 ? "" : "n")} {_lives} vida{(_lives == 1 ? "" : "s")}. Regresas al punto de control.");
    }

    private void ResetGame()
    {
        _x = 65; _y = GroundY - PlayerHeight; _vx = _vy = _camera = 0; _checkpoint = 65; _attackTicks = _attackCooldown = 0;
        _lives = 3; _score = 0; _won = _gameOver = false; _held.Clear();
        _bossHealth = 6; _bossAttackCooldown = 0; _bossAwakened = _bossDefeated = false;
        foreach (var shard in _shards) shard.Collected = false;
        foreach (var enemy in _enemies) enemy.Alive = true;
        SetNotice("Corre, salta y recoge los fragmentos de energía.");
    }

    private void SetNotice(string message) { _notice = message; _noticeTicks = 180; }

    private void PaintWorld(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        float sx = _canvas.ClientSize.Width / 1100f, sy = _canvas.ClientSize.Height / 620f;
        g.ScaleTransform(sx, sy);
        var viewport = new Rectangle(0, 0, 1100, 620);
        using (var sky = new LinearGradientBrush(viewport, Color.FromArgb(58, 46, 122), Color.FromArgb(9, 10, 30), LinearGradientMode.Vertical)) g.FillRectangle(sky, viewport);
        DrawBackdrop(g);
        g.TranslateTransform(-_camera, 0);
        DrawLevel(g);
        DrawPlayer(g);
        g.ResetTransform(); g.ScaleTransform(sx, sy);
        DrawHud(g);
    }

    private void DrawBackdrop(Graphics g)
    {
        using var stars = new SolidBrush(Color.FromArgb(170, 222, 212, 255));
        for (int i = 0; i < 95; i++) { float x = (i * 137 % 1100 + _camera * .14f) % 1100; float y = 28 + i * 47 % 290; g.FillEllipse(stars, x, y, i % 3 == 0 ? 2.5f : 1.5f, i % 3 == 0 ? 2.5f : 1.5f); }
        using var moonGlow = new SolidBrush(Color.FromArgb(28, 198, 143, 255)); g.FillEllipse(moonGlow, 765, 65, 200, 200);
        using var moon = new LinearGradientBrush(new Rectangle(800, 100, 130, 130), Color.FromArgb(233, 226, 255), Color.FromArgb(112, 94, 174), LinearGradientMode.ForwardDiagonal); g.FillEllipse(moon, 800, 100, 130, 130);
        using var far = new SolidBrush(Color.FromArgb(45, 12, 11, 41));
        for (int i = 0; i < 13; i++) { float x = (i * 177 - _camera * .32f) % 1450; if (x < -90) x += 1450; float h = 105 + i * 37 % 120; g.FillRectangle(far, x, 415 - h, 64, h); g.FillPolygon(far, new[] { new PointF(x - 12, 415 - h + 10), new PointF(x + 32, 385 - h), new PointF(x + 76, 415 - h + 10) }); }
    }

    private void DrawLevel(Graphics g)
    {
        foreach (var p in _platforms)
        {
            if (p.Right < _camera - 30 || p.Left > _camera + 1140) continue;
            using var ground = new LinearGradientBrush(p, Color.FromArgb(55, 47, 105), Color.FromArgb(20, 18, 47), LinearGradientMode.Vertical); g.FillRectangle(ground, p);
            using var edge = new Pen(Color.FromArgb(105, 95, 235), 3); g.DrawLine(edge, p.Left, p.Top, p.Right, p.Top);
            using var seam = new Pen(Color.FromArgb(55, 186, 125, 255), 1); for (float x = p.Left + 25; x < p.Right; x += 56) g.DrawLine(seam, x, p.Top + 8, x, Math.Min(p.Bottom, p.Top + 24));
        }
        foreach (var shard in _shards.Where(s => !s.Collected))
        {
            using var glow = new SolidBrush(Color.FromArgb(45, 76, 237, 255)); g.FillEllipse(glow, shard.X - 13, shard.Y - 13, 26, 26);
            using var crystal = new SolidBrush(Color.FromArgb(80, 231, 255)); g.FillPolygon(crystal, new[] { new PointF(shard.X, shard.Y - 9), new PointF(shard.X + 7, shard.Y), new PointF(shard.X, shard.Y + 9), new PointF(shard.X - 7, shard.Y) });
        }
        foreach (var enemy in _enemies.Where(en => en.Alive)) DrawEnemy(g, enemy);
        if (_bossAwakened && !_bossDefeated) DrawBoss(g);
        DrawBeacon(g, 2535, GroundY);
        if (_checkpoint > 100) DrawBeacon(g, _checkpoint, GroundY, true);
    }

    private static void DrawBeacon(Graphics g, float x, float y, bool checkpoint = false)
    {
        using var pole = new Pen(checkpoint ? Color.FromArgb(90, 221, 255) : Color.FromArgb(255, 205, 113), 4); g.DrawLine(pole, x, y, x, y - 95);
        using var flag = new SolidBrush(checkpoint ? Color.FromArgb(60, 208, 255) : Color.FromArgb(255, 184, 84)); g.FillPolygon(flag, new[] { new PointF(x + 2, y - 93), new PointF(x + 43, y - 80), new PointF(x + 2, y - 65) });
        using var glow = new SolidBrush(Color.FromArgb(48, checkpoint ? 60 : 255, checkpoint ? 208 : 184, checkpoint ? 255 : 84)); g.FillEllipse(glow, x - 15, y - 113, 34, 34);
    }

    private static void DrawEnemy(Graphics g, Patroller enemy)
    {
        var body = new RectangleF(enemy.X, enemy.Y, 34, 30);
        using var glow = new SolidBrush(Color.FromArgb(55, 255, 66, 145)); g.FillEllipse(glow, enemy.X - 5, enemy.Y - 5, 44, 40);
        using var fill = new LinearGradientBrush(body, Color.FromArgb(245, 65, 103), Color.FromArgb(103, 26, 76), LinearGradientMode.Vertical); g.FillEllipse(fill, body);
        using var eyes = new SolidBrush(Color.FromArgb(255, 231, 161)); g.FillEllipse(eyes, enemy.X + 8, enemy.Y + 9, 5, 5); g.FillEllipse(eyes, enemy.X + 21, enemy.Y + 9, 5, 5);
    }

    private void DrawBoss(Graphics g)
    {
        var body = new RectangleF(2460, GroundY - 118, 104, 118);
        using var aura = new SolidBrush(Color.FromArgb(46, 255, 38, 123)); g.FillEllipse(aura, body.X - 30, body.Y - 20, body.Width + 60, body.Height + 35);
        using var armor = new LinearGradientBrush(body, Color.FromArgb(105, 26, 71), Color.FromArgb(20, 8, 35), LinearGradientMode.Vertical); g.FillEllipse(armor, body);
        using var edge = new Pen(Color.FromArgb(255, 247, 86, 171), 3); g.DrawEllipse(edge, body);
        using var horn = new SolidBrush(Color.FromArgb(32, 9, 36));
        g.FillPolygon(horn, new[] { new PointF(2477, 452), new PointF(2454, 389), new PointF(2500, 429) });
        g.FillPolygon(horn, new[] { new PointF(2545, 429), new PointF(2585, 386), new PointF(2562, 455) });
        using var eye = new SolidBrush(Color.FromArgb(255, 255, 202, 86)); g.FillEllipse(eye, 2481, 444, 15, 9); g.FillEllipse(eye, 2527, 444, 15, 9);
        using var font = new Font("Bahnschrift", 9, FontStyle.Bold); using var text = new SolidBrush(Color.White);
        using var barBg = new SolidBrush(Color.FromArgb(220, 14, 8, 26)); g.FillRectangle(barBg, 2438, 389, 150, 21);
        using var bar = new SolidBrush(Color.FromArgb(239, 58, 137)); g.FillRectangle(bar, 2442, 393, 142 * _bossHealth / 6f, 13);
        g.DrawString($"GUARDIÁN  {_bossHealth}/6", font, text, 2466, 365);
    }

    private void DrawPlayer(Graphics g)
    {
        float x = _x, y = _y;
        if (_knightSprite is not null)
        {
            g.DrawImage(_knightSprite, x - 17, y - 41, 82, 104);
            if (_attackTicks > 0) DrawAxe(g, x, y);
            return;
        }
        using var aura = new SolidBrush(Color.FromArgb(52, 29, 148, 255)); g.FillEllipse(aura, x - 12, y + 13, 70, 62);
        using var armor = new SolidBrush(Color.FromArgb(12, 15, 28));
        using var armorEdge = new Pen(Color.FromArgb(255, 42, 185, 255), 2.2f);
        using var trim = new Pen(Color.FromArgb(190, 40, 157, 255), 1.5f);
        using var blue = new SolidBrush(Color.FromArgb(28, 146, 235));
        using var face = new SolidBrush(Color.FromArgb(51, 164, 255));
        using var hornShade = new SolidBrush(Color.FromArgb(4, 8, 20));
        // Cuernos curvos y casco oscuro con silueta de pequeño caballero.
        g.FillPolygon(hornShade, new[] { new PointF(x + 9, y + 20), new PointF(x - 4, y + 4), new PointF(x + 1, y - 3), new PointF(x + 14, y + 6), new PointF(x + 17, y + 20) });
        g.FillPolygon(hornShade, new[] { new PointF(x + 34, y + 20), new PointF(x + 47, y + 4), new PointF(x + 42, y - 3), new PointF(x + 29, y + 6), new PointF(x + 27, y + 20) });
        using var hornGlow = new Pen(Color.FromArgb(90, 39, 152, 255), 2);
        g.DrawBezier(hornGlow, new PointF(x + 9, y + 19), new PointF(x - 3, y + 4), new PointF(x + 3, y - 1), new PointF(x + 13, y + 5));
        g.DrawBezier(hornGlow, new PointF(x + 34, y + 19), new PointF(x + 48, y + 4), new PointF(x + 42, y - 1), new PointF(x + 31, y + 5));
        FillRoundRect(g, armor, x + 3, y + 18, 40, 39, 12); DrawRoundRect(g, armorEdge, x + 3, y + 18, 40, 39, 12);
        g.FillEllipse(face, x + 8, y + 19, 30, 29);
        // Visera puntiaguda, ojos claros y contorno eléctrico.
        g.FillPolygon(armor, new[] { new PointF(x + 4, y + 18), new PointF(x + 9, y + 8), new PointF(x + 23, y + 3), new PointF(x + 38, y + 8), new PointF(x + 43, y + 18), new PointF(x + 36, y + 31), new PointF(x + 23, y + 24), new PointF(x + 10, y + 31) });
        using var helmLine = new Pen(Color.FromArgb(190, 58, 187, 255), 2); g.DrawArc(helmLine, x + 5, y + 8, 36, 35, 195, 150);
        using var eye = new SolidBrush(Color.FromArgb(255, 238, 180));
        g.FillPolygon(eye, new[] { new PointF(x + 11, y + 27), new PointF(x + 20, y + 29), new PointF(x + 16, y + 34) });
        g.FillPolygon(eye, new[] { new PointF(x + 35, y + 27), new PointF(x + 26, y + 29), new PointF(x + 30, y + 34) });
        using var torso = new LinearGradientBrush(new RectangleF(x + 7, y + 38, 32, 20), Color.FromArgb(36, 46, 70), Color.FromArgb(4, 8, 18), LinearGradientMode.Vertical);
        FillRoundRect(g, torso, x + 7, y + 38, 32, 20, 6); g.DrawLine(trim, x + 13, y + 43, x + 23, y + 50); g.DrawLine(trim, x + 33, y + 43, x + 23, y + 50); g.DrawLine(armorEdge, x + 14, y + 55, x + 32, y + 55);
        using var boots = new SolidBrush(Color.FromArgb(5, 8, 18)); FillRoundRect(g, boots, x + 5, y + 54, 16, 8, 3); FillRoundRect(g, boots, x + 26, y + 54, 16, 8, 3);
        g.DrawLine(armorEdge, x + 8, y + 61, x + 19, y + 61); g.DrawLine(armorEdge, x + 29, y + 61, x + 40, y + 61);
        // Escudo azul al brazo opuesto del hacha.
        bool shieldLeft = _facingRight;
        float shieldX = shieldLeft ? x - 9 : x + 32;
        g.FillPolygon(armor, new[] { new PointF(shieldX + 1, y + 32), new PointF(shieldX + 16, y + 29), new PointF(shieldX + 19, y + 47), new PointF(shieldX + 10, y + 57), new PointF(shieldX + 2, y + 48) });
        g.FillPolygon(blue, new[] { new PointF(shieldX + 5, y + 35), new PointF(shieldX + 13, y + 34), new PointF(shieldX + 14, y + 46), new PointF(shieldX + 10, y + 51), new PointF(shieldX + 6, y + 46) });
        using var shieldEdge = new Pen(Color.FromArgb(255, 63, 195, 255), 2); g.DrawPolygon(shieldEdge, new[] { new PointF(shieldX + 1, y + 32), new PointF(shieldX + 16, y + 29), new PointF(shieldX + 19, y + 47), new PointF(shieldX + 10, y + 57), new PointF(shieldX + 2, y + 48) });
        DrawAxe(g, x, y);
    }

    private void DrawAxe(Graphics g, float x, float y)
    {
        GraphicsState state = g.Save();
        float pivotX = _facingRight ? x + 35 : x + 11, pivotY = y + 42;
        g.TranslateTransform(pivotX, pivotY);
        g.ScaleTransform(_facingRight ? 1 : -1, 1);
        float swing = _attackTicks > 0 ? -95 + (16 - _attackTicks) * 10 : -28;
        g.RotateTransform(swing);
        using var handle = new Pen(Color.FromArgb(12, 17, 31), 5); g.DrawLine(handle, 0, 3, 32, -29);
        using var grip = new Pen(Color.FromArgb(53, 173, 245), 1.4f); g.DrawLine(grip, 7, -4, 14, -11); g.DrawLine(grip, 18, -16, 25, -23);
        using var blade = new SolidBrush(Color.FromArgb(8, 15, 32));
        var bladeShape = new[] { new PointF(25, -23), new PointF(34, -39), new PointF(51, -42), new PointF(45, -28), new PointF(53, -18), new PointF(38, -17), new PointF(31, -24) };
        g.FillPolygon(blade, bladeShape);
        using var bladeEdge = new Pen(Color.FromArgb(255, 57, 187, 255), 2.5f); g.DrawPolygon(bladeEdge, bladeShape);
        using var bladeShine = new Pen(Color.FromArgb(175, 97, 211, 255), 2); g.DrawLine(bladeShine, 35, -37, 47, -39);
        g.Restore(state);
    }

    private void DrawHud(Graphics g)
    {
        using var plate = new SolidBrush(Color.FromArgb(220, 10, 9, 30)); FillRoundRect(g, plate, 22, 20, 390, 72, 14);
        using var border = new Pen(Color.FromArgb(130, 153, 118, 255), 1); DrawRoundRect(g, border, 22, 20, 390, 72, 14);
        using var title = new Font("Bahnschrift", 15, FontStyle.Bold); using var small = new Font("Bahnschrift", 9, FontStyle.Bold); using var brush = new SolidBrush(Color.White); using var muted = new SolidBrush(Color.FromArgb(197, 188, 224));
        g.DrawString("SALTO AL UMBRAL", title, brush, 39, 29); g.DrawString($"PUNTOS  {_score:0000}     VIDAS  {new string('♥', _lives)}", small, new SolidBrush(Color.FromArgb(124, 229, 255)), 40, 62);
        using var help = new SolidBrush(Color.FromArgb(205, 10, 9, 30)); FillRoundRect(g, help, 770, 20, 308, 54, 12); using var helpFont = new Font("Bahnschrift", 8.5f, FontStyle.Bold); g.DrawString("A / D MOVER · ESPACIO SALTAR · J / X HACHA", helpFont, muted, 785, 38);
        if (_noticeTicks > 0 || _won || _gameOver)
        {
            using var noticeBg = new SolidBrush(Color.FromArgb(214, 10, 9, 30)); FillRoundRect(g, noticeBg, 285, 552, 530, 42, 12);
            using var noticePen = new Pen(Color.FromArgb(100, 153, 118, 255)); DrawRoundRect(g, noticePen, 285, 552, 530, 42, 12);
            using var noticeText = new Font("Bahnschrift", 10, FontStyle.Bold); using var noticeBrush = new SolidBrush(Color.White); var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_notice, noticeText, noticeBrush, new RectangleF(296, 557, 508, 32), sf);
        }
    }

    private static void FillRoundRect(Graphics g, Brush brush, float x, float y, float width, float height, float radius)
    { using var path = RoundedPath(x, y, width, height, radius); g.FillPath(brush, path); }

    private static void DrawRoundRect(Graphics g, Pen pen, float x, float y, float width, float height, float radius)
    { using var path = RoundedPath(x, y, width, height, radius); g.DrawPath(pen, path); }

    private static GraphicsPath RoundedPath(float x, float y, float width, float height, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(width, height));
        var path = new GraphicsPath();
        path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }

    private sealed class Shard(float x, float y) { public float X { get; } = x; public float Y { get; } = y; public bool Collected { get; set; } }
    private sealed class Patroller(float x, float y, float min, float max) { public float X { get; set; } = x; public float Y { get; } = y; public float Min { get; } = min; public float Max { get; } = max; public float Speed { get; set; } = 1.35f; public bool Alive { get; set; } = true; }
}

