namespace NexusCopia;

internal sealed record SavedEnemy(float X, float Speed, int Health, int AttackCooldown, bool Alive);

internal sealed record ExpeditionSave(
    int DungeonIndex, int DifficultyIndex, float PlayerX, float PlayerY, float VelocityX, float VelocityY,
    float CheckpointX, int Score, int Lives, int BossHealth, bool BossAwakened, bool BossDefeated,
    int WorldTicks, bool FacingRight, bool Grounded, int[] CollectedShards, SavedEnemy[] Enemies, int[] TrapPhases);
