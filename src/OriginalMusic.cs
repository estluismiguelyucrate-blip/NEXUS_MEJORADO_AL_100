using System.Media;
using System.Text;

namespace NexusCopia;

/// <summary>Procedural original soundtrack. No third-party recording or copyrighted composition is bundled.</summary>
internal sealed class OriginalMusic : IDisposable
{
    private SoundPlayer? _player;
    public bool Enabled { get; private set; } = true;
    private bool _battleTrack;

    public void Toggle()
    {
        Enabled = !Enabled;
        if (Enabled) { if (_battleTrack) PlayBattle(); else PlayMenu(); } else Stop();
    }

    public void PlayMenu() { _battleTrack = false; Play("menu", 82); }
    public void PlayBattle() { _battleTrack = true; Play("battle", 116); }

    private void Play(string mood, int bpm)
    {
        if (!Enabled) return;
        Stop();
        string path = Path.Combine(Path.GetTempPath(), $"nexus-copy-{mood}.wav");
        if (!File.Exists(path)) File.WriteAllBytes(path, Compose(bpm, mood == "battle"));
        _player = new SoundPlayer(path);
        _player.PlayLooping();
    }

    private void Stop()
    {
        if (_player is null) return;
        _player.Stop(); _player.Dispose(); _player = null;
    }

    private static byte[] Compose(int bpm, bool battle)
    {
        const int sampleRate = 22050, bars = 4;
        double beat = 60d / bpm, duration = bars * 4 * beat;
        int samples = (int)(sampleRate * duration);
        int[] roots = battle ? [45, 41, 48, 43] : [48, 44, 41, 43];
        int[] melody = battle ? [0,7,10,7, 3,7,12,10, 5,12,10,7, 2,7,9,5] : [0,3,7,10, 7,10,12,10, 5,7,10,14, 2,5,9,12];
        using var ms = new MemoryStream(); using var writer = new BinaryWriter(ms, Encoding.ASCII, true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
        for (int i = 0; i < samples; i++)
        {
            double t = i / (double)sampleRate;
            int beatIndex = (int)(t / beat), bar = (beatIndex / 4) % bars;
            double withinBeat = (t / beat) % 1;
            int root = roots[bar];
            double bass = Math.Sin(2 * Math.PI * Midi(root - 12) * t) * (battle ? .18 : .13);
            double pad = (Math.Sin(2 * Math.PI * Midi(root) * t) + Math.Sin(2 * Math.PI * Midi(root + 7) * t) + Math.Sin(2 * Math.PI * Midi(root + 12) * t)) * .055;
            int step = (int)(withinBeat * 2);
            int note = root + melody[(beatIndex * 2 + step) % melody.Length] + 12;
            double local = (t % (beat / 2)) / (beat / 2);
            double envelope = Math.Pow(Math.Max(0, 1 - local), 2) * (battle ? .16 : .12);
            double lead = Math.Sin(2 * Math.PI * Midi(note) * t) * envelope;
            if (battle && beatIndex % 4 == 2) lead += Math.Sin(2 * Math.PI * Midi(root + 19) * t) * envelope * .35;
            short value = (short)(Math.Clamp((bass + pad + lead) * 2.1, -.92, .92) * short.MaxValue);
            writer.Write(value);
        }
        writer.Flush(); return ms.ToArray();
    }

    private static double Midi(int note) => 440 * Math.Pow(2, (note - 69) / 12d);
    public void Dispose() => Stop();
}

