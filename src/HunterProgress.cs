using System.Text.Json;

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

internal static class ProgressStore
{
    private static readonly string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusCopy", "hunter-save.json");

    public static HunterProgress Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return new HunterProgress();
            return JsonSerializer.Deserialize<HunterProgress>(File.ReadAllText(SavePath)) ?? new HunterProgress();
        }
        catch { return new HunterProgress(); }
    }

    public static (int Experience, int Credits, string RankBefore, string RankAfter) Complete(HunterProgress profile, int score, int shards)
    {
        string before = profile.Rank;
        int xp = 140 + Math.Max(0, shards) * 5 + Math.Max(0, score) / 80;
        int credits = 60 + Math.Max(0, shards) * 2;
        profile.Experience += xp;
        profile.Credits += credits;
        profile.ClearedDungeons++;
        profile.BestScore = Math.Max(profile.BestScore, score);
        Save(profile);
        return (xp, credits, before, profile.Rank);
    }

    public static void Save(HunterProgress profile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
        File.WriteAllText(SavePath, JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
    }
}

