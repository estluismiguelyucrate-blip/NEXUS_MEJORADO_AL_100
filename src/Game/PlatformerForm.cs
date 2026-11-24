using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace NexusCopia;

/// <summary>Modo arcade de plataformas de NEXUS, independiente de la expedición por sectores.</summary>
internal sealed class PlatformerForm : Form
{
    private const float WorldWidth = 3900, GroundY = 535, PlayerWidth = 54, PlayerHeight = 74;
    private const float BossCenterX = WorldWidth - 250;
    private readonly Panel _canvas;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _simulationClock = Stopwatch.StartNew();
    private double _simulationAccumulator;
    private readonly Button _saveButton;
    private DungeonDefinition _dungeon = null!;
    private readonly DifficultyDefinition _difficulty;
    private int _dungeonIndex;
    private readonly HunterProgress _profile;
    private readonly OriginalMusic _music;
    private readonly Image _playerSprite;
    private Image _bossSprite = null!;
    private Image _dungeonBackground = null!;
    private readonly HashSet<Keys> _held = new();
    private readonly List<RectangleF> _platforms = new();
    private readonly List<Shard> _shards = new();
    private readonly List<Patroller> _enemies = new();
    private readonly List<SpikeTrap> _traps = new();
    private readonly List<CombatProjectile> _enemyShots = new();
    private readonly List<CombatProjectile> _bossShots = new();
    private readonly List<CombatProjectile> _hunterShots = new();
    private float _x = 65, _y = GroundY - PlayerHeight, _vx, _vy, _camera;
    private float _checkpoint = 65;
    private bool _grounded, _facingRight = true, _won, _gameOver;
    private int _attackTicks, _attackCooldown;
    private int _worldTicks;
    private int _lives = 3, _score;
    private int _hurtTicks;
    private int _bossHealth = 6, _bossMaxHealth = 6, _bossAttackCooldown, _bossWindup, _bossAttackIndex;
    private BossAttackDefinition? _bossAttack;
    private readonly List<float> _bossWarningX = new();
    private bool _bossAwakened, _bossDefeated;
    private bool _rewardClaimed;
    private string _rewardText = "";
    private string _renderStage = "inicio";
    private string _notice = "Esquiva centinelas y salta las placas cuando brillen en rojo.";
    private int _noticeTicks = 220;

    public PlatformerForm(int dungeonIndex, int difficultyIndex, HunterProgress profile, OriginalMusic music, ExpeditionSave? savedRun = null)
    {
        if (savedRun is not null) { dungeonIndex = savedRun.DungeonIndex; difficultyIndex = savedRun.DifficultyIndex; }
        _difficulty = DifficultyCatalog.All[Math.Clamp(difficultyIndex, 0, DifficultyCatalog.All.Length - 1)];
        _profile = profile; _music = music; _lives = _difficulty.Lives;
        _playerSprite = LoadArt(_profile.Hunter.Sprite);
        LoadDungeon(dungeonIndex);
        Text = $"NEXUS | {_dungeon.Name}";
        ClientSize = new Size(1100, 680);
        MinimumSize = new Size(850, 570);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(7, 8, 22);
        KeyPreview = true;
        _canvas = new GameCanvas { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 12, 34) };
        _canvas.Paint += PaintWorld;
        _canvas.Resize += (_, _) => _canvas.Invalidate();
        Controls.Add(_canvas);
        _saveButton = new Button
        {
            Text = "GUARDAR PARTIDA",
            Location = new Point(ClientSize.Width - 200, 82),
            Size = new Size(176, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(39, 27, 71),
            ForeColor = Color.FromArgb(236, 226, 255),
            Font = new Font("Bahnschrift", 9, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _saveButton.FlatAppearance.BorderColor = Color.FromArgb(157, 118, 247);
        _saveButton.FlatAppearance.BorderSize = 1;
        _saveButton.Click += (_, _) => SaveExpeditionToSql(announce: true);
        Controls.Add(_saveButton);
        KeyDown += OnKeyDown;
        KeyUp += (_, e) => _held.Remove(e.KeyCode);
        Deactivate += (_, _) => _held.Clear();
        if (savedRun is not null) ApplySavedExpedition(savedRun);
        _simulationAccumulator = 0;
        _simulationClock.Restart();
        _timer = new System.Windows.Forms.Timer { Interval = 8 };
        _timer.Tick += (_, _) => AdvanceSimulation();
        _timer.Start();
        _music.PlayBattle();
        FormClosed += (_, _) => { _timer.Stop(); _timer.Dispose(); _playerSprite.Dispose(); _bossSprite.Dispose(); _dungeonBackground.Dispose(); _music.PlayMenu(); };
    }

    private void AdvanceSimulation()
    {
        const double fixedStep = 1.0 / 60.0;
        _simulationAccumulator += Math.Min(_simulationClock.Elapsed.TotalSeconds, .1);
        _simulationClock.Restart();
        int steps = 0;
        while (_simulationAccumulator >= fixedStep && steps < 5)
        {
            UpdateGame();
            _simulationAccumulator -= fixedStep;
            steps++;
        }
        // Evita que Windows provoque una ráfaga de pasos al mover o restaurar la ventana.
        if (steps == 5 && _simulationAccumulator > fixedStep * 2) _simulationAccumulator = fixedStep;
        _canvas.Invalidate();
    }

    private static Image LoadArt(string relativePath)
    {
        string path = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) throw new FileNotFoundException($"Falta un recurso del juego: {relativePath}", path);
        return Image.FromFile(path);
    }

    private void LoadDungeon(int dungeonIndex)
    {
        _dungeonIndex = Math.Clamp(dungeonIndex, 0, DungeonCatalog.All.Length - 1);
        if (_bossSprite is not null) _bossSprite.Dispose();
        if (_dungeonBackground is not null) _dungeonBackground.Dispose();
        _dungeon = DungeonCatalog.All[_dungeonIndex];
        _bossSprite = LoadArt(_dungeon.BossSprite);
        _dungeonBackground = LoadArt(_dungeon.BackgroundSprite);
        _bossMaxHealth = Math.Max(1, (int)Math.Ceiling(_dungeon.BossHealth * _difficulty.BossHealthScale));
        _bossHealth = _bossMaxHealth;
        _platforms.Clear(); _platforms.AddRange(_dungeon.Platforms);
        _shards.Clear(); _enemies.Clear(); _traps.Clear(); _hunterShots.Clear(); _enemyShots.Clear(); _bossShots.Clear();
        SeedLevel();
    }

    private void SeedLevel()
    {
        foreach (float x in new float[] { 180,290,350,535,590,810,875,1090,1150,1395,1465,1675,1740,1970,2040,2250,2310,2445,2580,2700,2860,3000,3150,3290,3410 })
        {
            float y = _platforms.Where(p => x >= p.Left - 25 && x <= p.Right + 25).Select(p => p.Top - 24).DefaultIfEmpty(GroundY - 24).Min();
            _shards.Add(new Shard(x, y));
        }
        foreach (float[] patrol in _dungeon.Patrols)
        {
            int health = Math.Max(1, (int)Math.Ceiling(_dungeon.EnemyHealth * _difficulty.EnemyHealthScale));
            _enemies.Add(new Patroller(patrol[0], patrol[1], patrol[2], patrol[3], health,
                Math.Max(30, (int)(_dungeon.EnemyAttackCooldown * _difficulty.EnemyFireRateScale))));
        }
        float trapOffset = _dungeon.Id == "bosque" ? 38 : _dungeon.Id == "ciudad" ? 76 : 0;
        float[] trapPositions = [620 + trapOffset, 1240 + trapOffset, 1900 + trapOffset, 2600 + trapOffset, 3200 + trapOffset];
        for (int i = 0; i < trapPositions.Length; i++) _traps.Add(new SpikeTrap(trapPositions[i], 82, i * 41));
    }

    private void ApplySavedExpedition(ExpeditionSave save)
    {
        _x = Math.Clamp(save.PlayerX, 0, WorldWidth - PlayerWidth);
        _y = Math.Clamp(save.PlayerY, 0, GroundY + 100);
        _vx = Math.Clamp(save.VelocityX, -6.5f, 6.5f);
        _vy = Math.Clamp(save.VelocityY, -12.4f, 13);
        _checkpoint = Math.Clamp(save.CheckpointX, 0, WorldWidth - PlayerWidth);
        _score = Math.Max(0, save.Score); _lives = Math.Clamp(save.Lives, 1, _difficulty.Lives);
        _bossHealth = Math.Clamp(save.BossHealth, 0, _bossMaxHealth);
        _bossAwakened = save.BossAwakened; _bossDefeated = save.BossDefeated || _bossHealth == 0;
        _bossAwakened |= _bossDefeated;
        _worldTicks = Math.Max(0, save.WorldTicks); _facingRight = save.FacingRight; _grounded = save.Grounded;
        foreach (int index in save.CollectedShards)
            if (index >= 0 && index < _shards.Count) _shards[index].Collected = true;
        for (int i = 0; i < Math.Min(_enemies.Count, save.Enemies.Length); i++)
        {
            SavedEnemy state = save.Enemies[i]; Patroller enemy = _enemies[i];
            enemy.X = Math.Clamp(state.X, enemy.Min, enemy.Max);
            if (state.Speed != 0) enemy.Speed = Math.Abs(state.Speed) * MathF.Sign(state.Speed);
            enemy.Health = Math.Clamp(state.Health, 0, enemy.MaxHealth); enemy.Alive = state.Alive && enemy.Health > 0;
            enemy.AttackCooldown = Math.Max(0, state.AttackCooldown);
        }
        for (int i = 0; i < Math.Min(_traps.Count, save.TrapPhases.Length); i++) _traps[i].SetPhase(save.TrapPhases[i]);
        _bossAttackCooldown = _bossAwakened ? 80 : 0;
        _hurtTicks = 75;
        _camera = Math.Clamp(_x - 330, 0, WorldWidth - 1100);
        SetNotice($"Partida restaurada desde SQL · {_dungeon.Name} · {_difficulty.Name}");
    }

    private bool SaveExpeditionToSql(bool announce)
    {
        if (_won) return true;
        try
        {
            int difficultyIndex = Array.FindIndex(DifficultyCatalog.All, item => item.Id == _difficulty.Id);
            var save = new ExpeditionSave(_dungeonIndex, Math.Max(0, difficultyIndex), _x, _y, _vx, _vy, _checkpoint,
                _score, Math.Max(1, _lives), _bossHealth, _bossAwakened, _bossDefeated, _worldTicks, _facingRight, _grounded,
                _shards.Select((shard, index) => (shard, index)).Where(item => item.shard.Collected).Select(item => item.index).ToArray(),
                _enemies.Select(enemy => new SavedEnemy(enemy.X, enemy.Speed, enemy.Health, enemy.AttackCooldown, enemy.Alive)).ToArray(),
                _traps.Select(trap => trap.Phase).ToArray());
            ProgressStore.SaveExpedition(save);
            if (announce) SetNotice("Partida guardada en SQL Server. Puedes continuar desde el menú.");
            return true;
        }
        catch (InvalidOperationException ex)
        {
            SetNotice("No se pudo guardar en SQL. Revisa la conexión o pulsa F5 para reintentar.");
            if (announce) MessageBox.Show(this, ex.Message, "No se pudo guardar la partida", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { if (SaveExpeditionToSql(announce: false)) Close(); e.Handled = true; return; }
        if (e.KeyCode == Keys.M) { _music.Toggle(); return; }
        if (e.KeyCode == Keys.R && (_won || _gameOver)) { ResetGame(); return; }
        if (e.KeyCode == Keys.F5) { SaveExpeditionToSql(announce: true); e.Handled = true; return; }
        if (e.KeyCode is Keys.J or Keys.X)
        {
            if (!_held.Contains(e.KeyCode)) SwingAxe();
            _held.Add(e.KeyCode); e.Handled = true; return;
        }
        if (e.KeyCode is Keys.Space or Keys.W or Keys.Up)
        {
            if (!_held.Contains(e.KeyCode) && _grounded && !_won && !_gameOver) { _vy = -12.1f; _grounded = false; }
        }
        _held.Add(e.KeyCode);
        e.Handled = true;
    }

    private void UpdateGame()
    {
        if (_won || _gameOver) return;
        if (_noticeTicks > 0) _noticeTicks--;
        if (_hurtTicks > 0) _hurtTicks--;
        _worldTicks++;
        if (_bossAttackCooldown > 0) _bossAttackCooldown--;
        if (_attackTicks > 0) _attackTicks--;
        if (_attackCooldown > 0) _attackCooldown--;
        float input = (_held.Contains(Keys.Right) || _held.Contains(Keys.D) ? 1 : 0) - (_held.Contains(Keys.Left) || _held.Contains(Keys.A) ? 1 : 0);
        _vx += input * (_grounded ? 1.15f : .48f);
        _vx *= input == 0 ? (_grounded ? .76f : .985f) : .94f;
        float maxSpeed = _grounded ? 6.5f : 5.8f;
        _vx = Math.Clamp(_vx, -maxSpeed, maxSpeed);
        if (input != 0) _facingRight = input > 0;
        float oldBottom = _y + PlayerHeight;
        _x = Math.Clamp(_x + _vx, 0, WorldWidth - PlayerWidth);
        _vy = Math.Min(_vy + .67f, 13);
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
        if (_x > BossCenterX - 550 && !_bossAwakened) { _bossAwakened = true; SetNotice($"¡{_dungeon.BossName} despierta! Acércate y usa tu habilidad."); }
        if (_bossAwakened && !_bossDefeated)
        {
            var boss = new RectangleF(BossCenterX - 58, GroundY - 178, 116, 178);
            if (player.IntersectsWith(boss) && _bossAttackCooldown == 0)
            { _bossAttackCooldown = 55; HitPlayer(); }
            UpdateBossCombat(player);
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
            UpdateEnemyCombat(enemy, player);
            var bounds = new RectangleF(enemy.X, enemy.Y, 34, 30);
            if (!player.IntersectsWith(bounds)) continue;
            if (_vy > 1 && oldBottom <= enemy.Y + 13)
            {
                enemy.Health--; _vy = -7.4f;
                if (enemy.Health <= 0) { enemy.Alive = false; _score += 25; SetNotice("¡Buen salto! Centinela derrotado. +25"); }
                else SetNotice($"Golpe de salto: centinela {enemy.Health}/{enemy.MaxHealth}. ¡Sigue atacando!");
            }
            else HitPlayer();
        }
        foreach (var trap in _traps)
        {
            trap.Update(_worldTicks);
            if (trap.Phase == 132 && Math.Abs(trap.X - _x) < 170)
                SetNotice("¡Placas activándose! Salta cuando veas las púas rojas.");
            if (trap.IsActive && player.IntersectsWith(trap.Hitbox))
            {
                SetNotice("¡Trampa de púas! Salta sobre las placas cuando brillen en rojo.");
                HitPlayer();
                break;
            }
        }
        UpdateHunterProjectiles();
        UpdateProjectiles(_enemyShots, player);
        UpdateProjectiles(_bossShots, player);
        if (_x > 1280 && _checkpoint < 1300) { _checkpoint = 1320; SetNotice("Punto de control SQL activado."); SaveExpeditionToSql(announce: false); }
        if (_x > 2530 && _checkpoint < 2600) { _checkpoint = 2620; SetNotice("Segundo checkpoint guardado en SQL."); SaveExpeditionToSql(announce: false); }
        if (_x > 3100 && _checkpoint < 3190) { _checkpoint = 3190; SetNotice("Último checkpoint guardado en SQL."); SaveExpeditionToSql(announce: false); }
        if (_y > 720) HitPlayer();
        if (_x > WorldWidth - 110 && _bossDefeated) AdvanceExpedition();
        _camera = Math.Clamp(_x - 330, 0, WorldWidth - 1100);
    }

    private void SwingAxe()
    {
        if (_won || _gameOver || _attackCooldown > 0) return;
        _attackTicks = 24;
        _attackCooldown = _profile.Hunter.Id == "soren" ? 27 : _profile.Hunter.Id == "ariadna" ? 35 : 48;
        float playerCenter = _x + PlayerWidth / 2;
        if (_profile.Hunter.Id == "soren")
        {
            foreach (var enemy in _enemies.Where(en => en.Alive))
            {
                if (IsInFront(enemy.X + 17, enemy.Y + 15, playerCenter, 150))
                { DamageEnemy(enemy, _profile.Hunter.Damage); return; }
            }
            if (_bossAwakened && !_bossDefeated && IsInFront(BossCenterX, GroundY - 82, playerCenter, 150))
            {
                DamageBoss(_profile.Hunter.Damage);
                return;
            }
            SetNotice("¡Eclipse Gemelo! Acércate y apunta hacia el rival para conectar ambos cortes.");
            return;
        }

        float direction = _facingRight ? 1 : -1;
        float shotX = playerCenter + direction * 31;
        float shotY = _y + PlayerHeight * .43f;
        if (_profile.Hunter.Id == "ariadna")
        {
            _hunterShots.Add(new CombatProjectile(shotX, shotY, direction * 12.5f, 0,
                _profile.Hunter.Aura, 36, 26, _profile.Hunter.AbilityName, 190, _profile.Hunter.Damage));
        }
        else
        {
            for (int i = -1; i <= 1; i++)
                _hunterShots.Add(new CombatProjectile(shotX, shotY + i * 10, direction * 15, i * .22f,
                    _profile.Hunter.Aura, 27, 9, _profile.Hunter.AbilityName, 150, _profile.Hunter.Damage));
        }
        SetNotice($"¡{_profile.Hunter.AbilityName}! {(_profile.Hunter.Id == "kael" ? "Ráfaga triple disparada." : "Lanza ígnea en vuelo.")}");
    }

    private bool IsInFront(float targetX, float targetY, float playerCenter, float range)
    {
        float distance = _facingRight ? targetX - playerCenter : playerCenter - targetX;
        return distance > 0 && distance < range && Math.Abs(targetY - (_y + PlayerHeight / 2)) < 105;
    }

    private void DamageEnemy(Patroller enemy, int damage)
    {
        enemy.Health -= damage;
        if (enemy.Health <= 0)
        {
            enemy.Alive = false; _score += 35;
            SetNotice($"{_profile.Hunter.AbilityName}: centinela derrotado · +35");
        }
        else SetNotice($"{_profile.Hunter.AbilityName}: centinela {enemy.Health}/{enemy.MaxHealth}");
    }

    private void DamageBoss(int damage)
    {
        if (_bossDefeated) return;
        _bossHealth = Math.Max(0, _bossHealth - damage); _score += 50;
        if (_bossHealth > 0)
        {
            SetNotice($"{_profile.Hunter.AbilityName}: {_dungeon.BossName} · {_bossHealth}/{_bossMaxHealth}");
            return;
        }
        _score += 250;
        if (!_rewardClaimed)
        {
            int shards = _shards.Count(s => s.Collected);
            (int Experience, int Credits, string RankBefore, string RankAfter) reward;
            try { reward = ProgressStore.Complete(_profile, _score, shards); }
            catch (InvalidOperationException ex)
            {
                _score -= 250; _bossHealth = 1;
                MessageBox.Show(this, ex.Message + "\n\nVuelve a conectar SQL Server y derrota al jefe para reintentar el guardado.", "No se pudo guardar en SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetNotice("SQL Server no disponible. El jefe sigue vulnerable; el progreso no se guardó.");
                return;
            }
            _bossDefeated = _rewardClaimed = true;
            _rewardText = $"+{reward.Experience} XP · +{reward.Credits} créditos · Rango {reward.RankAfter}" + (reward.RankAfter != reward.RankBefore ? " · ¡ASCENSO!" : "");
        }
        _bossShots.Clear(); _bossWarningX.Clear(); _bossWindup = 0;
        SaveExpeditionToSql(announce: false);
        SetNotice($"¡{_dungeon.BossName} derrotado! Sigue la luz del portal para continuar.");
    }

    private void AdvanceExpedition()
    {
        if (_dungeonIndex + 1 >= DungeonCatalog.All.Length)
        {
            _won = true;
            bool cleared = true;
            try { ProgressStore.DeleteExpedition(); }
            catch (InvalidOperationException) { cleared = false; }
            SetNotice(cleared
                ? "¡Expedición completada! Derrotaste a todos los guardianes."
                : "¡Expedición completada! SQL no pudo borrar el guardado anterior.");
            return;
        }
        LoadDungeon(_dungeonIndex + 1);
        _x = 65; _y = GroundY - PlayerHeight; _vx = _vy = _camera = 0; _checkpoint = 65;
        _bossAttackCooldown = _bossWindup = _bossAttackIndex = 0; _bossAttack = null;
        _bossAwakened = _bossDefeated = _rewardClaimed = false; _rewardText = "";
        _hurtTicks = 75;
        Text = $"NEXUS | {_dungeon.Name} · Expedición {_dungeonIndex + 1}/{DungeonCatalog.All.Length}";
        SaveExpeditionToSql(announce: false);
        SetNotice($"Portal abierto: llegaste a {_dungeon.Name}. ¡Mantén tu avance!");
    }

    private void UpdateHunterProjectiles()
    {
        foreach (var shot in _hunterShots)
        {
            shot.Bounds = new RectangleF(shot.Bounds.X + shot.Vx, shot.Bounds.Y + shot.Vy, shot.Bounds.Width, shot.Bounds.Height);
            shot.Life--;
            bool hit = false;
            foreach (var enemy in _enemies.Where(en => en.Alive))
            {
                if (!shot.Bounds.IntersectsWith(new RectangleF(enemy.X, enemy.Y, 34, 30))) continue;
                DamageEnemy(enemy, shot.Damage); hit = true; break;
            }
            if (!hit && _bossAwakened && !_bossDefeated && shot.Bounds.IntersectsWith(new RectangleF(BossCenterX - 58, GroundY - 178, 116, 178)))
            { DamageBoss(shot.Damage); hit = true; }
            if (hit || shot.Life <= 0 || shot.Bounds.Right < _camera - 60 || shot.Bounds.Left > _camera + 1180) shot.Life = 0;
        }
        _hunterShots.RemoveAll(shot => shot.Life <= 0);
    }

    private void UpdateEnemyCombat(Patroller enemy, RectangleF player)
    {
        if (enemy.AttackCooldown > 0) { enemy.AttackCooldown--; return; }
        float dx = _x + PlayerWidth / 2 - (enemy.X + 17);
        float dy = _y + PlayerHeight / 2 - (enemy.Y + 15);
        if (Math.Abs(dx) > 410) return;
        float length = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
        float speed = _dungeon.Id == "umbral" ? 3.25f : _dungeon.Id == "bosque" ? 3.0f : 3.55f;
        _enemyShots.Add(new CombatProjectile(enemy.X + 17, enemy.Y + 14, dx / length * speed, dy / length * speed,
            _dungeon.EnemyAttackColor, _dungeon.Id == "bosque" ? 20 : 14, _dungeon.Id == "bosque" ? 30 : 14,
            _dungeon.Id == "bosque" ? "Espina Carmesí" : _dungeon.Id == "ciudad" ? "Orbe de Marea" : "Bola Umbría"));
        enemy.AttackCooldown = _dungeon.EnemyAttackCooldown;
    }

    private void UpdateBossCombat(RectangleF player)
    {
        if (_bossAttackCooldown > 0) return;
        if (_bossWindup > 0)
        {
            _bossWindup--;
            if (_bossWindup == 0) ReleaseBossAttack();
            return;
        }
        if (_bossShots.Count > 0) return;
        _bossAttack = _dungeon.BossAttacks[_bossAttackIndex++ % _dungeon.BossAttacks.Length];
        _bossWindup = 54;
        _bossWarningX.Clear();
        if (_bossAttack.Kind == BossAttackKind.VoidRain)
        {
            float target = _x + PlayerWidth / 2;
            _bossWarningX.AddRange([target - 68, target, target + 68]);
        }
        else if (_bossAttack.Kind == BossAttackKind.RootSnare)
            _bossWarningX.Add(_x + PlayerWidth / 2);
        SetNotice($"{_dungeon.BossName}: ¡{_bossAttack.Name}! Prepárate para esquivar.");
    }

    private void ReleaseBossAttack()
    {
        BossAttackDefinition attack = _bossAttack!;
        switch (attack.Kind)
        {
            case BossAttackKind.VoidRain:
                foreach (float x in _bossWarningX) _bossShots.Add(new CombatProjectile(x, 255, 0, 7.5f, _dungeon.Accent, 25, 55, attack.Name));
                break;
            case BossAttackKind.SpectralScythe:
                _bossShots.Add(new CombatProjectile(BossCenterX - 75, Math.Clamp(_y + PlayerHeight / 2, 392, 485), -9.8f, 0, _dungeon.Accent, 112, 38, attack.Name));
                break;
            case BossAttackKind.RootSnare:
                _bossShots.Add(new CombatProjectile(Math.Clamp(_x + PlayerWidth / 2 - 26, BossCenterX - 560, BossCenterX + 24), GroundY - 132, 0, 0, _dungeon.Accent, 52, 132, attack.Name, 34));
                break;
            case BossAttackKind.CrimsonLash:
                _bossShots.Add(new CombatProjectile(BossCenterX - 180, GroundY - 92, -11.5f, 0, _dungeon.Accent, 140, 36, attack.Name));
                break;
            case BossAttackKind.TidalWave:
                _bossShots.Add(new CombatProjectile(BossCenterX - 190, GroundY - 60, -12.5f, 0, _dungeon.Accent, 105, 50, attack.Name));
                break;
            case BossAttackKind.CrownOrbs:
                float dx = _x + PlayerWidth / 2 - BossCenterX;
                foreach (float y in new[] { 428f, 480f })
                {
                    float dy = _y + PlayerHeight / 2 - y, len = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
                    _bossShots.Add(new CombatProjectile(BossCenterX - 75, y, dx / len * 6.3f, dy / len * 6.3f, _dungeon.Accent, 30, 30, attack.Name));
                }
                break;
        }
        _bossWarningX.Clear();
        _bossAttackCooldown = 120;
    }

    private void UpdateProjectiles(List<CombatProjectile> shots, RectangleF player)
    {
        foreach (var shot in shots)
        {
            if (shot.Life <= 0) continue;
            shot.Bounds = new RectangleF(shot.Bounds.X + shot.Vx, shot.Bounds.Y + shot.Vy, shot.Bounds.Width, shot.Bounds.Height);
            shot.Life--;
            if (player.IntersectsWith(shot.Bounds))
            {
                SetNotice($"{shot.Name} te alcanza.");
                shots.Clear();
                _bossAttackCooldown = Math.Max(_bossAttackCooldown, 70);
                HitPlayer();
                return;
            }
            if (shot.Bounds.Right < _camera - 80 || shot.Bounds.Left > _camera + 1200 || shot.Bounds.Top > 630) shot.Life = 0;
        }
        shots.RemoveAll(s => s.Life <= 0);
    }

    private void HitPlayer()
    {
        if (_hurtTicks > 0) return;
        _lives--;
        if (_lives <= 0) { _gameOver = true; SetNotice("Fin de la partida. Presiona R para intentarlo otra vez."); return; }
        _hurtTicks = 85;
        _x = _checkpoint; _y = GroundY - PlayerHeight - 5; _vx = _vy = 0;
        SetNotice($"¡Cuidado! Te queda{(_lives == 1 ? "" : "n")} {_lives} vida{(_lives == 1 ? "" : "s")}. Regresas al punto de control.");
    }

    private void ResetGame()
    {
        _x = 65; _y = GroundY - PlayerHeight; _vx = _vy = _camera = 0; _checkpoint = 65; _attackTicks = _attackCooldown = 0;
        _lives = _difficulty.Lives; _score = 0; _hurtTicks = 0; _worldTicks = 0; _won = _gameOver = false; _held.Clear();
        _bossHealth = _bossMaxHealth; _bossAttackCooldown = _bossWindup = _bossAttackIndex = 0; _bossAttack = null;
        _bossWarningX.Clear(); _bossShots.Clear(); _enemyShots.Clear(); _hunterShots.Clear();
        _bossAwakened = _bossDefeated = _rewardClaimed = false; _rewardText = "";
        foreach (var shard in _shards) shard.Collected = false;
        foreach (var enemy in _enemies) { enemy.Alive = true; enemy.Health = enemy.MaxHealth; enemy.AttackCooldown = 35; }
        foreach (var trap in _traps) trap.Update(0);
        SetNotice("Esquiva centinelas y salta las placas cuando brillen en rojo.");
    }

    private void SetNotice(string message) { _notice = message; _noticeTicks = 180; }

    private void PaintWorld(object? sender, PaintEventArgs e)
    {
        try
        {
            PaintWorldFrame(e.Graphics);
        }
        catch (Exception ex)
        {
            e.Graphics.ResetTransform();
            using var panel = new SolidBrush(Color.FromArgb(235, 33, 8, 28));
            using var border = new Pen(Color.FromArgb(255, 255, 90, 120), 2);
            using var font = new Font("Segoe UI", 11, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            var message = $"ERROR DE ESCENA [{_renderStage}]: {ex.GetType().Name} · {ex.Message}";
            e.Graphics.FillRectangle(panel, 12, 110, Math.Max(200, _canvas.ClientSize.Width - 24), 58);
            e.Graphics.DrawRectangle(border, 12, 110, Math.Max(200, _canvas.ClientSize.Width - 24), 58);
            e.Graphics.DrawString(message, font, brush, new RectangleF(22, 116, Math.Max(180, _canvas.ClientSize.Width - 44), 48));
            try
            {
                string log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusCopy", "render-error.log");
                Directory.CreateDirectory(Path.GetDirectoryName(log)!);
                File.AppendAllText(log, $"{DateTime.Now:O} [{_renderStage}] {ex}\n\n");
            }
            catch { }
        }
    }

    private void PaintWorldFrame(Graphics g)
    {
        _renderStage = "medidas";
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        float sx = _canvas.ClientSize.Width / 1100f, sy = _canvas.ClientSize.Height / 620f;
        g.ScaleTransform(sx, sy);
        var viewport = new Rectangle(0, 0, 1100, 620);
        using (var sky = new LinearGradientBrush(viewport, _dungeon.SkyTop, _dungeon.SkyBottom, LinearGradientMode.Vertical)) g.FillRectangle(sky, viewport);
        _renderStage = "fondo";
        DrawBackdrop(g);
        g.TranslateTransform(-_camera, 0);
        _renderStage = "mapa";
        DrawLevel(g);
        _renderStage = "cazador";
        DrawPlayer(g);
        g.ResetTransform(); g.ScaleTransform(sx, sy);
        _renderStage = "interfaz";
        DrawHud(g);
    }

    private void DrawBackdrop(Graphics g)
    {
        // El fondo de cada mazmorra se desplaza a una velocidad menor que el mapa para dar profundidad.
        float tileWidth = 1100;
        float offset = -((_camera * .20f) % tileWidth);
        for (int i = -1; i < 3; i++)
            g.DrawImage(_dungeonBackground, new RectangleF(offset + i * tileWidth, 0, tileWidth, 620));
        using (var shade = new SolidBrush(Color.FromArgb(85, 4, 5, 16))) g.FillRectangle(shade, 0, 0, 1100, 620);
        DrawAmbientEffects(g);
    }

    private void DrawAmbientEffects(Graphics g)
    {
        // Partículas ligeras, con movimiento distinto por mundo y coordenadas en pantalla para no marear la cámara.
        float time = _worldTicks / 60f;
        int count = _dungeon.Id == "bosque" ? 28 : 36;
        for (int i = 0; i < count; i++)
        {
            float seed = i * 83.17f;
            float drift = _dungeon.Id == "ciudad" ? -.35f : _dungeon.Id == "bosque" ? .55f : .15f;
            float x = (seed + time * (12 + i % 9) * drift + 1100) % 1100;
            float y = _dungeon.Id == "ciudad"
                ? 570 - ((i * 29 + time * (10 + i % 6)) % 570)
                : _dungeon.Id == "bosque"
                    ? (i * 71 + MathF.Sin(time * 1.4f + i) * 34 + time * 8) % 535
                    : 35 + (i * 53 + time * (13 + i % 7)) % 485;
            int pulse = (int)(95 + 80 * (0.5f + 0.5f * MathF.Sin(time * 2 + i * 1.7f)));
            Color tint = _dungeon.Id == "bosque" ? Color.FromArgb(pulse, i % 3 == 0 ? 255 : 127, i % 3 == 0 ? 166 : 255, 94)
                : _dungeon.Id == "ciudad" ? Color.FromArgb(pulse, 255, 119 + i % 3 * 25, 54)
                : Color.FromArgb(pulse, 179 + i % 3 * 25, 113, 255);
            using var particle = new SolidBrush(tint);
            float size = 2 + i % 4;
            if (_dungeon.Id == "umbral")
            {
                g.FillPolygon(particle, new[] { new PointF(x, y - size * 1.7f), new PointF(x + size, y), new PointF(x, y + size * 1.7f), new PointF(x - size, y) });
            }
            else if (_dungeon.Id == "bosque")
            {
                GraphicsState state = g.Save();
                g.TranslateTransform(x, y); g.RotateTransform((float)(Math.Sin(time + i) * 55));
                g.FillEllipse(particle, -size, -size * .45f, size * 2.2f, size * .9f);
                g.Restore(state);
            }
            else
            {
                g.FillEllipse(particle, x, y, size, size * 1.8f);
                using var trail = new Pen(Color.FromArgb(pulse / 2, tint), 1);
                g.DrawLine(trail, x + size / 2, y + size, x + size / 2 - 4, y + size * 3);
            }
        }
        // Pulsos de energía ambiental en la parte baja: aportan vida sin cubrir la zona de combate.
        int beat = (int)(20 + 18 * (0.5 + 0.5 * Math.Sin(time * (_dungeon.Id == "ciudad" ? 1.5 : 1.0))));
        using var floorGlow = new SolidBrush(Color.FromArgb(beat, _dungeon.Accent));
        g.FillEllipse(floorGlow, 210, 500, 680, 90);
    }

    private void DrawLevel(Graphics g)
    {
        foreach (var p in _platforms)
        {
            if (p.Right < _camera - 30 || p.Left > _camera + 1140) continue;
            using var ground = new LinearGradientBrush(p, _dungeon.GroundTop, _dungeon.GroundBottom, LinearGradientMode.Vertical); g.FillRectangle(ground, p);
            using var edge = new Pen(Color.FromArgb(190, _dungeon.Accent), 3); g.DrawLine(edge, p.Left, p.Top, p.Right, p.Top);
            using var seam = new Pen(Color.FromArgb(55, _dungeon.Accent), 1); for (float x = p.Left + 25; x < p.Right; x += 56) g.DrawLine(seam, x, p.Top + 8, x, Math.Min(p.Bottom, p.Top + 24));
        }
        foreach (var shard in _shards.Where(s => !s.Collected))
        {
            using var glow = new SolidBrush(Color.FromArgb(48, _dungeon.Accent)); g.FillEllipse(glow, shard.X - 13, shard.Y - 13, 26, 26);
            using var crystal = new SolidBrush(Color.FromArgb(240, _dungeon.Accent)); g.FillPolygon(crystal, new[] { new PointF(shard.X, shard.Y - 9), new PointF(shard.X + 7, shard.Y), new PointF(shard.X, shard.Y + 9), new PointF(shard.X - 7, shard.Y) });
        }
        foreach (var trap in _traps) DrawTrap(g, trap);
        foreach (var enemy in _enemies.Where(en => en.Alive)) DrawEnemy(g, enemy);
        foreach (var shot in _enemyShots) DrawCombatProjectile(g, shot);
        foreach (var shot in _bossShots) DrawCombatProjectile(g, shot);
        foreach (var shot in _hunterShots) DrawCombatProjectile(g, shot);
        if (_bossWindup > 0) DrawBossTelegraph(g);
        if (_bossAwakened && !_bossDefeated) DrawBoss(g);
        DrawExitPortal(g);
        if (_checkpoint > 100) DrawBeacon(g, _checkpoint, GroundY, true);
    }

    private void DrawTrap(Graphics g, SpikeTrap trap)
    {
        var plate = new RectangleF(trap.X, GroundY - 8, trap.Width, 10);
        using var baseBrush = new SolidBrush(Color.FromArgb(215, 16, 14, 33));
        using var plateEdge = new Pen(Color.FromArgb(trap.IsWarning ? 230 : 150, trap.IsActive ? Color.OrangeRed : _dungeon.Accent), 2);
        g.FillRectangle(baseBrush, plate); g.DrawRectangle(plateEdge, Rectangle.Round(plate));
        if (trap.IsWarning)
        {
            using var warningGlow = new SolidBrush(Color.FromArgb(80, 255, 67, 94));
            using var warningEdge = new Pen(Color.FromArgb(210, 255, 153, 176), 2) { DashPattern = [3, 3] };
            g.FillEllipse(warningGlow, trap.X - 5, GroundY - 49, trap.Width + 10, 42);
            g.DrawLine(warningEdge, trap.X + trap.Width / 2, GroundY - 45, trap.X + trap.Width / 2, GroundY - 11);
        }
        if (!trap.IsActive) return;
        using var spikeBrush = new LinearGradientBrush(new RectangleF(trap.X, GroundY - 42, trap.Width, 35), Color.FromArgb(255, 255, 113, 121), Color.FromArgb(255, 81, 21, 65), LinearGradientMode.Vertical);
        using var spikeEdge = new Pen(Color.FromArgb(245, 255, 204, 220), 1.2f);
        const int teeth = 4;
        for (int i = 0; i < teeth; i++)
        {
            float left = trap.X + i * trap.Width / teeth, right = trap.X + (i + 1) * trap.Width / teeth;
            PointF[] points = [new(left, GroundY - 8), new((left + right) / 2, GroundY - 40), new(right, GroundY - 8)];
            g.FillPolygon(spikeBrush, points); g.DrawPolygon(spikeEdge, points);
        }
    }

    private void DrawExitPortal(Graphics g)
    {
        float x = WorldWidth - 72, y = GroundY - 112;
        Color color = _bossDefeated ? Color.FromArgb(113, 233, 255) : Color.FromArgb(117, 100, 149);
        using var glow = new SolidBrush(Color.FromArgb(_bossDefeated ? 48 : 20, color));
        using var edge = new Pen(Color.FromArgb(_bossDefeated ? 230 : 90, color), 5);
        using var inner = new Pen(Color.FromArgb(_bossDefeated ? 180 : 55, 212, 202, 255), 2);
        g.FillEllipse(glow, x - 35, y - 12, 72, 126);
        g.DrawEllipse(edge, x - 20, y, 42, 94); g.DrawEllipse(inner, x - 12, y + 8, 26, 78);
        if (_bossDefeated)
        {
            using var light = new SolidBrush(Color.FromArgb(95, 135, 220, 255));
            g.FillEllipse(light, x - 8, y + 10, 18, 74);
            using var labelFont = new Font("Bahnschrift", 8, FontStyle.Bold);
            using var labelBrush = new SolidBrush(Color.FromArgb(235, 236, 233, 255));
            var label = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(_dungeonIndex + 1 < DungeonCatalog.All.Length ? "SIGUIENTE" : "EXTRACCIÓN", labelFont, labelBrush, new RectangleF(x - 70, y - 24, 140, 16), label);
        }
    }

    private static void DrawBeacon(Graphics g, float x, float y, bool checkpoint = false)
    {
        using var pole = new Pen(checkpoint ? Color.FromArgb(90, 221, 255) : Color.FromArgb(255, 205, 113), 4); g.DrawLine(pole, x, y, x, y - 95);
        using var flag = new SolidBrush(checkpoint ? Color.FromArgb(60, 208, 255) : Color.FromArgb(255, 184, 84)); g.FillPolygon(flag, new[] { new PointF(x + 2, y - 93), new PointF(x + 43, y - 80), new PointF(x + 2, y - 65) });
        using var glow = new SolidBrush(Color.FromArgb(48, checkpoint ? 60 : 255, checkpoint ? 208 : 184, checkpoint ? 255 : 84)); g.FillEllipse(glow, x - 15, y - 113, 34, 34);
    }

    private void DrawEnemy(Graphics g, Patroller enemy)
    {
        var body = new RectangleF(enemy.X, enemy.Y, 34, 30);
        using var glow = new SolidBrush(Color.FromArgb(65, _dungeon.EnemyAttackColor)); g.FillEllipse(glow, enemy.X - 7, enemy.Y - 7, 48, 44);
        using var fill = new LinearGradientBrush(body, Color.FromArgb(245, _dungeon.EnemyAttackColor), Color.FromArgb(100, 16, 19, 40), LinearGradientMode.Vertical); g.FillEllipse(fill, body);
        using var outline = new Pen(Color.FromArgb(220, _dungeon.EnemyAttackColor), 1.5f); g.DrawEllipse(outline, body);
        using var eyes = new SolidBrush(Color.FromArgb(255, 246, 235)); g.FillEllipse(eyes, enemy.X + 8, enemy.Y + 9, 5, 5); g.FillEllipse(eyes, enemy.X + 21, enemy.Y + 9, 5, 5);
        using var back = new SolidBrush(Color.FromArgb(210, 13, 8, 24)); g.FillRectangle(back, enemy.X - 2, enemy.Y - 9, 38, 5);
        using var hp = new SolidBrush(_dungeon.EnemyAttackColor); g.FillRectangle(hp, enemy.X, enemy.Y - 8, 34 * enemy.Health / (float)enemy.MaxHealth, 3);
    }

    private void DrawBoss(Graphics g)
    {
        var body = new RectangleF(BossCenterX - 88, GroundY - 190, 176, 190);
        using var aura = new SolidBrush(Color.FromArgb(42, _dungeon.Accent)); g.FillEllipse(aura, body.X - 20, body.Y + 15, body.Width + 40, body.Height - 4);
        g.DrawImage(_bossSprite, body);
        using var font = new Font("Bahnschrift", 9, FontStyle.Bold); using var text = new SolidBrush(Color.White);
        float barX = BossCenterX - 79;
        using var barBg = new SolidBrush(Color.FromArgb(228, 14, 8, 26)); g.FillRectangle(barBg, barX, 365, 158, 23);
        using var bar = new SolidBrush(_dungeon.Accent); g.FillRectangle(bar, barX + 4, 369, 150 * _bossHealth / (float)_bossMaxHealth, 14);
        g.DrawString($"{_dungeon.BossName.ToUpperInvariant()} · {_bossHealth}/{_bossMaxHealth}", font, text, BossCenterX - 110, 343);
    }

    private void DrawBossTelegraph(Graphics g)
    {
        using var warning = new Pen(Color.FromArgb(170, _dungeon.Accent), 3) { DashPattern = [4, 5] };
        using var glow = new SolidBrush(Color.FromArgb(60, _dungeon.Accent));
        foreach (float x in _bossWarningX)
        {
            g.FillEllipse(glow, x - 22, GroundY - 9, 44, 15);
            g.DrawLine(warning, x, 245, x, GroundY - 8);
        }
    }

    private void DrawCombatProjectile(Graphics g, CombatProjectile shot)
    {
        var b = shot.Bounds;
        if (shot.Name.Contains("Bola", StringComparison.OrdinalIgnoreCase) || shot.Name.Contains("Orbe de Marea", StringComparison.OrdinalIgnoreCase))
        {
            float speed = MathF.Max(1, MathF.Sqrt(shot.Vx * shot.Vx + shot.Vy * shot.Vy));
            float cx = b.X + b.Width / 2, cy = b.Y + b.Height / 2;
            float pulse = .9f + .1f * MathF.Sin(_worldTicks * .24f);
            using var trail = new Pen(Color.FromArgb(115, shot.Color), 7) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var trailCore = new Pen(Color.FromArgb(105, 255, 255, 255), 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(trail, cx, cy, cx - shot.Vx / speed * 27, cy - shot.Vy / speed * 27);
            g.DrawLine(trailCore, cx, cy, cx - shot.Vx / speed * 18, cy - shot.Vy / speed * 18);
            using var aura = new SolidBrush(Color.FromArgb(62, shot.Color));
            g.FillEllipse(aura, cx - 17 * pulse, cy - 17 * pulse, 34 * pulse, 34 * pulse);
            using var bodyPath = new GraphicsPath();
            bodyPath.AddEllipse(b.X, b.Y, b.Width, b.Height);
            using var orb = new PathGradientBrush(bodyPath)
            {
                CenterColor = Color.FromArgb(255, 249, 255, 255),
                SurroundColors = [shot.Color]
            };
            g.FillEllipse(orb, b);
            using var rim = new Pen(Color.FromArgb(230, Color.White), 1.5f);
            g.DrawEllipse(rim, b);
            using var arc = new Pen(Color.FromArgb(205, 255, 255, 255), 1.4f);
            g.DrawArc(arc, b.X + 3, b.Y + 2, Math.Max(2, b.Width - 6), Math.Max(2, b.Height - 5), _worldTicks * 5 % 360, 210);
            return;
        }
        using var glow = new SolidBrush(Color.FromArgb(85, shot.Color)); g.FillEllipse(glow, b.X - 10, b.Y - 8, b.Width + 20, b.Height + 16);
        using var fill = new SolidBrush(Color.FromArgb(235, shot.Color));
        if (shot.Name.Contains("Lanza Solar", StringComparison.OrdinalIgnoreCase))
        {
            float dir = shot.Vx >= 0 ? 1 : -1;
            g.FillPolygon(fill, new PointF[] { new(b.X + b.Width * .5f, b.Y - 4), new(b.X + b.Width, b.Y + b.Height * .5f), new(b.X + b.Width * .5f, b.Bottom + 4), new(b.X, b.Y + b.Height * .5f) });
            using var core = new SolidBrush(Color.FromArgb(255, 255, 249, 213));
            g.FillEllipse(core, b.X + b.Width * .35f, b.Y + b.Height * .28f, b.Width * .36f, b.Height * .44f);
            using var tail = new Pen(Color.FromArgb(210, 255, 164, 55), 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(tail, b.X + (dir > 0 ? 2 : b.Width - 2), b.Y + b.Height / 2, b.X - dir * 20, b.Y + b.Height / 2);
        }
        else if (shot.Name.Contains("Ráfaga Esmeralda", StringComparison.OrdinalIgnoreCase))
        {
            float dir = shot.Vx >= 0 ? 1 : -1;
            using var beam = new SolidBrush(Color.FromArgb(220, shot.Color));
            using var core = new Pen(Color.FromArgb(255, 223, 255, 228), 2);
            g.FillRectangle(beam, b.X, b.Y + b.Height * .25f, b.Width, b.Height * .5f);
            g.DrawLine(core, b.X, b.Y + b.Height / 2, b.X + b.Width, b.Y + b.Height / 2);
            using var tail = new Pen(Color.FromArgb(175, shot.Color), 3);
            g.DrawLine(tail, b.X + (dir > 0 ? 0 : b.Width), b.Y + b.Height / 2, b.X - dir * 17, b.Y + b.Height / 2);
        }
        else if (shot.Name.Contains("Raíz", StringComparison.OrdinalIgnoreCase) || shot.Name.Contains("Prisión", StringComparison.OrdinalIgnoreCase))
        {
            g.FillPolygon(fill, new[] { new PointF(b.Left, b.Bottom), new PointF(b.Left + b.Width * .2f, b.Top + b.Height * .38f), new PointF(b.Left + b.Width * .38f, b.Bottom), new PointF(b.Left + b.Width * .58f, b.Top), new PointF(b.Right, b.Bottom) });
        }
        else if (shot.Name.Contains("Marea", StringComparison.OrdinalIgnoreCase) || shot.Name.Contains("Latigazo", StringComparison.OrdinalIgnoreCase))
        {
            g.FillEllipse(fill, b);
            using var edge = new Pen(Color.FromArgb(235, 228, 246, 255), 3); g.DrawArc(edge, b.X + 3, b.Y + 2, Math.Max(2, b.Width - 6), b.Height - 4, 190, 160);
        }
        else
        {
            g.FillEllipse(fill, b);
            using var core = new SolidBrush(Color.FromArgb(245, 255, 255, 235)); g.FillEllipse(core, b.X + b.Width * .28f, b.Y + b.Height * .25f, b.Width * .35f, b.Height * .35f);
        }
    }

    private void DrawPlayer(Graphics g)
    {
        float x = _x, y = _y;
        float width = 94, height = 108;
        float drawX = x + PlayerWidth / 2 - width / 2, drawY = y + PlayerHeight - height;
        using var aura = new SolidBrush(Color.FromArgb(65, _profile.Hunter.Aura)); g.FillEllipse(aura, drawX - 10, drawY + 32, width + 20, height - 20);
        if (_hurtTicks > 0 && _hurtTicks % 10 < 5) return;
        if (!_facingRight)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(x + PlayerWidth / 2, 0); g.ScaleTransform(-1, 1); g.TranslateTransform(-(x + PlayerWidth / 2), 0);
            g.DrawImage(_playerSprite, new RectangleF(drawX, drawY, width, height)); g.Restore(state);
        }
        else g.DrawImage(_playerSprite, new RectangleF(drawX, drawY, width, height));
        if (_attackTicks > 0) DrawHunterSkill(g, x, y);
    }

    private void DrawHunterSkill(Graphics g, float x, float y)
    {
        float dir = _facingRight ? 1 : -1;
        float start = x + PlayerWidth / 2 + dir * 34;
        float progress = 1 - _attackTicks / 24f;
        using var aura = new SolidBrush(Color.FromArgb(95, _profile.Hunter.Aura));
        using var core = new SolidBrush(Color.FromArgb(245, _profile.Hunter.Aura));
        if (_profile.Hunter.Id == "soren")
        {
            float sweep = 80 + progress * 110;
            using var outer = new Pen(Color.FromArgb(95, _profile.Hunter.Aura), 16) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var slash = new Pen(Color.FromArgb(245, _profile.Hunter.Aura), 7) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var shine = new Pen(Color.FromArgb(255, 238, 235, 255), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawArc(outer, start - 30, y + 4, 108, 92, _facingRight ? 205 : -25, _facingRight ? sweep : -sweep);
            g.DrawArc(slash, start - 30, y + 4, 108, 92, _facingRight ? 205 : -25, _facingRight ? sweep : -sweep);
            g.DrawLine(shine, start, y + 49, start + dir * 68, y + 9);
            g.DrawLine(shine, start + dir * 8, y + 21, start + dir * 69, y + 60);
            using var sparks = new SolidBrush(Color.White);
            for (int i = 0; i < 4; i++) g.FillEllipse(sparks, start + dir * (45 + i * 8), y + 18 + (i % 2) * 29, 4, 4);
            return;
        }
        if (_profile.Hunter.Id == "ariadna")
        {
            float radius = 9 + 20 * MathF.Sin(progress * MathF.PI);
            float px = start + dir * 14, py = y + 38;
            g.FillEllipse(aura, px - radius * 1.8f, py - radius * 1.8f, radius * 3.6f, radius * 3.6f);
            g.FillEllipse(core, px - radius, py - radius, radius * 2, radius * 2);
            using var ring = new Pen(Color.FromArgb(220, 255, 233, 144), 2.5f);
            g.DrawEllipse(ring, px - radius * 1.4f, py - radius * 1.4f, radius * 2.8f, radius * 2.8f);
            using var rays = new Pen(Color.FromArgb(210, 255, 197, 73), 3);
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)(i * Math.PI / 3 + progress * 2), dx = MathF.Cos(angle), dy = MathF.Sin(angle);
                g.DrawLine(rays, px + dx * (radius + 4), py + dy * (radius + 4), px + dx * (radius + 13), py + dy * (radius + 13));
            }
            return;
        }
        using var flash = new SolidBrush(Color.FromArgb((int)(170 * (1 - progress)), 102, 255, 168));
        g.FillEllipse(flash, start - 4, y + 28, 30, 24);
        using var muzzle = new Pen(Color.FromArgb(220, 210, 255, 225), 3);
        g.DrawLine(muzzle, start + dir * 7, y + 33, start + dir * 28, y + 33);
        g.DrawLine(muzzle, start + dir * 7, y + 45, start + dir * 28, y + 45);
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
        using var hunterBrush = new SolidBrush(_profile.Hunter.Aura);
        g.DrawString(_dungeon.Name.ToUpperInvariant(), title, brush, 39, 29); g.DrawString($"RANGO {_profile.Rank} · {_difficulty.Name} · {_profile.Hunter.AbilityName} · {_score:0000} · {new string('♥', _lives)}", small, hunterBrush, 40, 62);
        using var help = new SolidBrush(Color.FromArgb(205, 10, 9, 30)); FillRoundRect(g, help, 770, 20, 308, 54, 12);
        using var helpFont = new Font("Bahnschrift", 7.8f, FontStyle.Bold);
        g.DrawString("A / D MOVER · ESPACIO SALTAR", helpFont, muted, 784, 26);
        g.DrawString("J / X HABILIDAD · F5 GUARDAR SQL", helpFont, muted, 784, 42);
        g.DrawString("M MÚSICA · ESC GUARDA Y SALE", helpFont, muted, 784, 58);
        using var saveFont = new Font("Bahnschrift", 8, FontStyle.Bold);
        g.DrawString("PROGRESO: GUARDADO AUTOMÁTICO EN SQL SERVER AL VENCER CADA JEFE", saveFont, muted, new RectangleF(28, 600, 610, 18));
        if (_noticeTicks > 0 || _won || _gameOver)
        {
            using var noticeBg = new SolidBrush(Color.FromArgb(214, 10, 9, 30)); FillRoundRect(g, noticeBg, 285, 552, 530, 42, 12);
            using var noticePen = new Pen(Color.FromArgb(100, 153, 118, 255)); DrawRoundRect(g, noticePen, 285, 552, 530, 42, 12);
            using var noticeText = new Font("Bahnschrift", 10, FontStyle.Bold); using var noticeBrush = new SolidBrush(Color.White); var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_notice, noticeText, noticeBrush, new RectangleF(296, 557, 508, 32), sf);
        }
        if (_bossDefeated)
        {
            using var rewardBg = new SolidBrush(Color.FromArgb(226, 10, 9, 30)); FillRoundRect(g, rewardBg, 365, 492, 370, 42, 12);
            using var rewardFont = new Font("Bahnschrift", 9.5f, FontStyle.Bold); using var rewardBrush = new SolidBrush(Color.FromArgb(255, 226, 157));
            var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_rewardText, rewardFont, rewardBrush, new RectangleF(377, 497, 346, 32), center);
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
    private sealed class Patroller(float x, float y, float min, float max, int health, int attackCooldown)
    {
        public float X { get; set; } = x; public float Y { get; } = y; public float Min { get; } = min; public float Max { get; } = max;
        public float Speed { get; set; } = 1.35f; public int Health { get; set; } = health; public int MaxHealth { get; } = health;
        public int AttackCooldown { get; set; } = attackCooldown; public bool Alive { get; set; } = true;
    }
    private sealed class SpikeTrap(float x, float width, int phaseOffset)
    {
        public float X { get; } = x; public float Width { get; } = width; public int Phase { get; private set; }
        public bool IsWarning => Phase is >= 132 and < 176;
        public bool IsActive => Phase >= 176;
        public RectangleF Hitbox => new(X + 4, GroundY - 39, Width - 8, 39);
        public void Update(int tick) => Phase = (tick + phaseOffset) % 220;
        public void SetPhase(int phase) => Phase = Math.Clamp(phase, 0, 219);
    }
    private sealed class CombatProjectile(float x, float y, float vx, float vy, Color color, float width, float height, string name, int life = 240, int damage = 0)
    {
        public float Vx { get; } = vx; public float Vy { get; } = vy; public Color Color { get; } = color; public string Name { get; } = name;
        public RectangleF Bounds { get; set; } = new(x, y, width, height); public int Life { get; set; } = life; public int Damage { get; } = damage;
    }

    private sealed class GameCanvas : Panel
    {
        public GameCanvas()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }
    }
}
