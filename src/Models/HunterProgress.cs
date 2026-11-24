namespace NexusCopia;

internal sealed class HunterProgress
{
    public string HunterId { get; set; } = "soren";
    public int Experience { get; set; }
    public int Credits { get; set; }
    public int ClearedDungeons { get; set; }
    public int BestScore { get; set; }
    public string Rank => Experience >= 1000 ? "S" : Experience >= 700 ? "A" : Experience >= 450 ? "B" : Experience >= 250 ? "C" : Experience >= 100 ? "D" : "E";
    public int NextRankExperience => Experience >= 1000 ? 1000 : new[] { 100, 250, 450, 700, 1000 }.First(x => x > Experience);
    public HunterArchetype Hunter => HunterCatalog.All.FirstOrDefault(x => x.Id == HunterId) ?? HunterCatalog.All[0];
}
