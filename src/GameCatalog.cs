namespace NexusCopia;

internal sealed record DungeonDefinition(
    string Id, string Name, string Region, string BossName,
    Color SkyTop, Color SkyBottom, Color GroundTop, Color GroundBottom,
    Color Accent, int BossHealth, RectangleF[] Platforms,
    float[][] Patrols);

internal static class DungeonCatalog
{
    public static readonly DungeonDefinition[] All =
    [
        new("umbral", "Ruinas del Umbral", "Falla dimensional · rango E", "Guardián del Umbral",
            Color.FromArgb(58,46,122), Color.FromArgb(9,10,30), Color.FromArgb(55,47,105), Color.FromArgb(20,18,47),
            Color.FromArgb(239,58,137), 6,
            [new(0,535,2600,180),new(250,440,150,20),new(500,365,150,20),new(770,430,180,20),new(1050,350,160,20),new(1350,420,180,20),new(1635,340,170,20),new(1930,415,180,20),new(2210,345,160,20)],
            [[430,505,405,490],[900,505,850,1010],[1240,505,1220,1320],[1800,505,1830,1900],[2130,505,2110,2180]]),
        new("bosque", "Bosque Carmesí", "Bosque mutado · rango D", "Matriarca de Raíces",
            Color.FromArgb(94,37,67), Color.FromArgb(21,10,31), Color.FromArgb(86,47,74), Color.FromArgb(27,15,38),
            Color.FromArgb(242,104,93), 8,
            [new(0,535,2600,180),new(210,420,190,20),new(510,350,145,20),new(755,445,190,20),new(1030,370,190,20),new(1330,305,150,20),new(1550,430,185,20),new(1850,355,190,20),new(2165,420,165,20)],
            [[385,505,340,455],[830,505,800,960],[1190,505,1140,1285],[1675,505,1600,1760],[2070,505,2000,2145]]),
        new("ciudad", "Ciudad Sumergida", "Distrito inundado · rango C", "Abisal de la Corona",
            Color.FromArgb(25,83,111), Color.FromArgb(7,18,38), Color.FromArgb(41,87,112), Color.FromArgb(13,30,52),
            Color.FromArgb(81,221,231), 10,
            [new(0,535,2600,180),new(245,455,165,20),new(480,370,185,20),new(775,315,155,20),new(1020,425,180,20),new(1315,350,185,20),new(1605,285,170,20),new(1870,410,190,20),new(2195,345,165,20)],
            [[400,505,350,460],[890,505,820,980],[1240,505,1160,1310],[1760,505,1680,1820],[2110,505,2050,2180]])
    ];
}

internal sealed record HunterArchetype(string Id, string Name, string ClassName, string Title, int AttackRange, int Damage, Color Aura);

internal static class HunterCatalog
{
    public static readonly HunterArchetype[] All =
    [
        new("soren", "Soren Valc", "Vanguardia", "El Muro Errante", 120, 2, Color.FromArgb(52,174,255)),
        new("ariadna", "Ariadna Vela", "Arcanista", "Tejedora de Ecos", 210, 1, Color.FromArgb(185,111,255)),
        new("kael", "Kael Ordo", "Asesino", "Filo Silencioso", 155, 2, Color.FromArgb(79,239,190))
    ];
}

