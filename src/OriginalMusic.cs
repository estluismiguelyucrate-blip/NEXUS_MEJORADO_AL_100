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
        string path = Path.Combine(Path.GetTempPath(), $"nexus-copy-{mood}-v3.wav");
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
        // Original minor-key progression and motifs: cinematic anime-fantasy energy,
        // composed for Nexus Copy without samples or borrowed melodies.
        int[] roots = battle ? [45, 48, 41, 43] : [48, 45, 41, 43];
        int[] melody = battle ? [0,7,12,10, 7,3,10,14, 5,12,17,14, 2,9,12,7] : [0,7,10,14, 12,7,10,17, 5,10,14,17, 2,9,12,14];
        int[][] chordTones = [[0, 3, 7, 12], [0, 4, 7, 12], [0, 3, 7, 10], [0, 4, 7, 11]];
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
            double beatPhase = t % beat;
            double chordAge = (t % (beat * 4)) / (beat * 4);
            double chordEnvelope = Math.Min(1, chordAge * 18) * Math.Min(1, (1 - chordAge) * 16);
            int[] chord = chordTones[bar];
            double pad = 0;
            foreach (int tone in chord)
            {
                double hz = Midi(root + tone);
                // Detuned saw/string partials plus soft upper octaves make the menu sound broad.
                pad += (Math.Sin(2 * Math.PI * hz * t) * .63 + Math.Sin(2 * Math.PI * hz * 1.006 * t) * .37) * .032;
                if (tone is 0 or 7) pad += Math.Sin(2 * Math.PI * hz * 2 * t) * .012;
            }
            pad *= chordEnvelope;
            double bass = (Math.Sin(2 * Math.PI * Midi(root - 12) * t) * .8 + Math.Sin(2 * Math.PI * Midi(root - 12) * 2 * t) * .2) * (battle ? .20 : .16);
            int step = (int)(withinBeat * 2);
            int note = root + melody[(beatIndex * 2 + step) % melody.Length] + 12;
            double local = (t % (beat / 2)) / (beat / 2);
            double envelope = Math.Pow(Math.Max(0, 1 - local), 2) * (battle ? .16 : .12);
            double noteHz = Midi(note);
            double lead = (Math.Sin(2 * Math.PI * noteHz * t) * .66 + Math.Sin(2 * Math.PI * noteHz * 2.01 * t) * .22 + Math.Sin(2 * Math.PI * noteHz * 3 * t) * .12) * envelope;
            // A quiet eighth-note bell arpeggio supplies motion without crowding the melody.
            int arpTone = chord[(beatIndex * 2 + step) % chord.Length];
            int arpNote = root + arpTone + 24;
            double arpAge = (t % (beat / 2)) / (beat / 2);
            double arpEnvelope = Math.Exp(-arpAge * 5.4) * (battle ? .10 : .075);
            double bell = Math.Sin(2 * Math.PI * Midi(arpNote) * t) * arpEnvelope;
            double percussion = 0;
            if (withinBeat < .12)
            {
                double hit = Math.Exp(-withinBeat * (battle ? 23 : 30));
                double noise = Math.Sin(i * 12.9898 + beatIndex * 78.233) * Math.Sin(i * 0.017 + 1.7);
                percussion += noise * hit * (beatIndex % 4 == 0 ? (battle ? .18 : .095) : (battle ? .065 : .027));
                if (beatIndex % 4 == 2) percussion += Math.Sin(2 * Math.PI * 68 * t) * hit * (battle ? .12 : .045);
            }
            if (battle && beatIndex % 4 == 2) lead += Math.Sin(2 * Math.PI * Midi(root + 19) * t) * envelope * .32;
            double mix = (bass + pad + lead + bell + percussion) * (battle ? 1.9 : 2.05);
            short value = (short)(Math.Clamp(Math.Tanh(mix), -.96, .96) * short.MaxValue);
            writer.Write(value);
        }
        writer.Flush(); return ms.ToArray();
    }

    private static double Midi(int note) => 440 * Math.Pow(2, (note - 69) / 12d);
    public void Dispose() => Stop();
}
