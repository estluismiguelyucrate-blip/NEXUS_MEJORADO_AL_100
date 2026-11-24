namespace NexusCopia;

internal sealed record DungeonDefinition(
    string Id, string Name, string Region, string BossName,
    string BossSprite, string BackgroundSprite, BossAttackDefinition[] BossAttacks,
    int EnemyHealth, int EnemyAttackCooldown, Color EnemyAttackColor,
    Color SkyTop, Color SkyBottom, Color GroundTop, Color GroundBottom,
    Color Accent, int BossHealth, RectangleF[] Platforms,
    float[][] Patrols);

internal enum BossAttackKind { VoidRain, SpectralScythe, RootSnare, CrimsonLash, TidalWave, CrownOrbs }
internal sealed record BossAttackDefinition(string Name, BossAttackKind Kind);

internal static class DungeonCatalog
{
    public static readonly DungeonDefinition[] All =
    [
        new("umbral", "Ruinas del Umbral", "Falla dimensional · rango E", "Guardián del Umbral",
            "assets/bosses/boss-umbral.png", "assets/backgrounds/world-umbral.jpeg",
            [new("Lluvia del Vacío", BossAttackKind.VoidRain), new("Guadaña Espectral", BossAttackKind.SpectralScythe)],
            2, 152, Color.FromArgb(192, 82, 255),
            Color.FromArgb(58,46,122), Color.FromArgb(9,10,30), Color.FromArgb(55,47,105), Color.FromArgb(20,18,47),
            Color.FromArgb(239,58,137), 6,
            [new(0,535,2600,180),new(250,440,150,20),new(500,365,150,20),new(770,430,180,20),new(1050,350,160,20),new(1350,420,180,20),new(1635,340,170,20),new(1930,415,180,20),new(2210,345,160,20)],
            [[430,505,405,490],[900,505,850,1010],[1240,505,1220,1320],[1800,505,1830,1900],[2130,505,2110,2180]]),
        new("bosque", "Bosque de los Colmillos", "Bosque mutado · rango D", "Devorador del Bosque",
            "assets/bosses/boss-ciudad.png", "assets/backgrounds/world-bosque.jpeg",
            [new("Aullido de Raíces", BossAttackKind.RootSnare), new("Orbes de Cacería", BossAttackKind.CrownOrbs)],
            3, 125, Color.FromArgb(255, 69, 84),
            Color.FromArgb(94,37,67), Color.FromArgb(21,10,31), Color.FromArgb(86,47,74), Color.FromArgb(27,15,38),
            Color.FromArgb(242,104,93), 8,
            [new(0,535,2600,180),new(210,420,190,20),new(510,350,145,20),new(755,445,190,20),new(1030,370,190,20),new(1330,305,150,20),new(1550,430,185,20),new(1850,355,190,20),new(2165,420,165,20)],
            [[385,505,340,455],[830,505,800,960],[1190,505,1140,1285],[1675,505,1600,1760],[2070,505,2000,2145]]),
        new("ciudad", "Bastión Carmesí", "Fortaleza en ruinas · rango C", "Caballero Carmesí",
            "assets/bosses/boss-bosque.png", "assets/backgrounds/world-bastion.jpeg",
            [new("Tajo del Último Guardián", BossAttackKind.CrimsonLash), new("Oleada Carmesí", BossAttackKind.TidalWave)],
            4, 98, Color.FromArgb(74, 229, 241),
            Color.FromArgb(25,83,111), Color.FromArgb(7,18,38), Color.FromArgb(41,87,112), Color.FromArgb(13,30,52),
            Color.FromArgb(81,221,231), 10,
            [new(0,535,2600,180),new(245,455,165,20),new(480,370,185,20),new(775,315,155,20),new(1020,425,180,20),new(1315,350,185,20),new(1605,285,170,20),new(1870,410,190,20),new(2195,345,165,20)],
            [[400,505,350,460],[890,505,820,980],[1240,505,1160,1310],[1760,505,1680,1820],[2110,505,2050,2180]])
    ];
}

internal sealed record HunterArchetype(string Id, string Name, string ClassName, string Title, string Sprite, string AbilityName, string AbilityHint, int AttackRange, int Damage, Color Aura);

internal static class HunterCatalog
{
    public static readonly HunterArchetype[] All =
    [
        new("soren", "Soren Valc", "Vanguardia", "El Muro Errante", "assets/characters/hunter-soren.png", "Eclipse Gemelo", "Corte doble frontal de gran daño", 135, 3, Color.FromArgb(160,104,255)),
        new("ariadna", "Ariadna Vela", "Arcanista", "Portadora de la Llama", "assets/characters/hunter-ariadna.png", "Lanza Solar", "Proyectil ígneo de largo alcance", 245, 2, Color.FromArgb(255,197,73)),
        new("kael", "Kael Ordo", "Tirador", "Doble Pulso", "assets/characters/hunter-kael.png", "Ráfaga Esmeralda", "Tres disparos rápidos en línea", 195, 1, Color.FromArgb(79,239,142))
    ];
}
