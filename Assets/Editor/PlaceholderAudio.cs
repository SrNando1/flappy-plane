using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sintetiza os efeitos sonoros por código e grava-os como WAV 16 bits mono.
/// Mesma ideia da arte: nada vem de fora. Para usar sons a sério, substitua
/// os ficheiros em Assets/Audio mantendo os nomes — o gerador não escreve por
/// cima do que já lá estiver, tal como não toca na arte desenhada à mão.
/// </summary>
public static class PlaceholderAudio
{
    public const string Dir = "Assets/Audio";
    const int Rate = 44100;

    enum Wave { Sine, Square, Saw, Triangle }

    /// <summary>
    /// Sintetiza os sons que faltam e deixa em paz os que já existem. É o que
    /// permite trocar um deles por um som a sério sem que o próximo 'Gerar
    /// tudo' o apague — para voltar aos sintetizados, é o 1f.
    /// </summary>
    [MenuItem("Flappy/1b - Gerar sons placeholder")]
    public static void Generate()
    {
        Generate(false);
    }

    /// <summary>
    /// Repõe os seis sons sintetizados, por cima do que lá estiver. O irmão
    /// do 1d e do 1e, que fazem o mesmo pela nuvem e pelo avião.
    /// </summary>
    [MenuItem("Flappy/1f - Repor sons placeholder")]
    public static void Restore()
    {
        Generate(true);
    }

    static void Generate(bool overwrite)
    {
        Directory.CreateDirectory(Dir);

        var kept = new List<string>();
        if (!Save("sfx_score", BuildScore(), overwrite)) kept.Add("sfx_score");
        if (!Save("sfx_crash", BuildCrash(), overwrite)) kept.Add("sfx_crash");
        if (!Save("sfx_start", BuildStart(), overwrite)) kept.Add("sfx_start");
        if (!Save("sfx_record", BuildRecord(), overwrite)) kept.Add("sfx_record");
        if (!Save("sfx_engine", BuildEngineLoop(), overwrite)) kept.Add("sfx_engine");
        if (!Save("sfx_pigeon", BuildPigeon(), overwrite)) kept.Add("sfx_pigeon");
        if (!Save("sfx_coin", BuildCoin(), overwrite)) kept.Add("sfx_coin");

        AssetDatabase.Refresh();

        if (kept.Count == 0)
        {
            Debug.Log("[Flappy] Sons placeholder gerados em " + Dir);
            return;
        }

        Debug.Log("[Flappy] Sons placeholder gerados em " + Dir + ". Ficaram como estavam, por já " +
                  "existirem: " + string.Join(", ", kept) +
                  " — 'Flappy/1f - Repor sons placeholder' sintetiza por cima.");
    }

    // ------------------------------------------------------------------ sons

    /// <summary>Dois bips curtos, o segundo mais agudo — o "ponto".</summary>
    static float[] BuildScore()
    {
        var buf = new float[Seconds(0.18f)];

        Tone(buf, 0f, 0.06f, 880f, 880f, Wave.Square, 0.35f, 0.003f, 0.05f);
        Tone(buf, 0.055f, 0.11f, 1318f, 1318f, Wave.Square, 0.35f, 0.003f, 0.09f);

        Normalize(buf, 0.7f);
        return buf;
    }

    /// <summary>Estouro: ruído branco por cima de um grave a cair.</summary>
    static float[] BuildCrash()
    {
        var buf = new float[Seconds(1f)];
        var rng = new System.Random(1234);

        Noise(buf, 0f, 0.95f, 0.55f, 0.28f, rng);
        Tone(buf, 0f, 0.7f, 200f, 32f, Wave.Sine, 0.55f, 0.002f, 0.22f);
        Tone(buf, 0f, 0.25f, 320f, 60f, Wave.Square, 0.18f, 0.001f, 0.07f);

        Distort(buf, 1.6f);
        Normalize(buf, 0.92f);
        return buf;
    }

    /// <summary>Motor a pegar, para o arranque da partida.</summary>
    static float[] BuildStart()
    {
        var buf = new float[Seconds(0.45f)];
        var rng = new System.Random(99);

        Tone(buf, 0f, 0.42f, 70f, 150f, Wave.Saw, 0.35f, 0.06f, 0.5f);
        Tone(buf, 0f, 0.42f, 140f, 300f, Wave.Saw, 0.15f, 0.06f, 0.4f);
        Noise(buf, 0f, 0.42f, 0.12f, 0.5f, rng);

        Normalize(buf, 0.75f);
        return buf;
    }

    /// <summary>Arpejo de quatro notas para o novo recorde.</summary>
    static float[] BuildRecord()
    {
        var buf = new float[Seconds(0.65f)];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };

        for (int i = 0; i < notes.Length; i++)
        {
            Tone(buf, i * 0.1f, 0.3f, notes[i], notes[i], Wave.Square, 0.3f, 0.005f, 0.16f);
        }

        Normalize(buf, 0.8f);
        return buf;
    }

    /// <summary>
    /// O pombo: um baque surdo, o grasnido ofendido a descer aos solavancos, e
    /// o motor a tossir três vezes, cada tosse mais fraca. É o som que tem de
    /// dizer "isto não é a morte" antes de o jogador ter tempo de se assustar.
    /// </summary>
    static float[] BuildPigeon()
    {
        var buf = new float[Seconds(0.75f)];
        var rng = new System.Random(4242);

        // o baque de penas contra metal: grave, curto, com um sopro por cima
        Tone(buf, 0f, 0.12f, 190f, 70f, Wave.Sine, 0.7f, 0.001f, 0.05f);
        Noise(buf, 0f, 0.1f, 0.35f, 0.03f, rng);

        // o grasnido: dentes de serra a cair em escada, cada degrau a voltar
        // um bocadinho acima — é o tremido que o faz soar a bicho e não a apito
        float[] steps = { 1500f, 1150f, 1320f, 980f, 1120f, 820f };
        for (int i = 0; i < steps.Length; i++)
        {
            float next = i + 1 < steps.Length ? steps[i + 1] : 700f;
            Tone(buf, 0.03f + i * 0.035f, 0.05f, steps[i], next, Wave.Saw, 0.22f, 0.003f, 0.2f);
        }

        // as tosses do motor: estalo de ruído com um grave quadrado por baixo
        float[] coughs = { 0.34f, 0.47f, 0.62f };
        for (int i = 0; i < coughs.Length; i++)
        {
            float amp = 0.5f - i * 0.12f;
            Noise(buf, coughs[i], 0.08f, amp, 0.025f, rng);
            Tone(buf, coughs[i], 0.07f, 110f, 60f, Wave.Square, amp * 0.6f, 0.002f, 0.03f);
        }

        Distort(buf, 1.3f);
        Normalize(buf, 0.85f);
        return buf;
    }

    /// <summary>Duas notas curtas a subir, brilhantes — o tilintar de apanhar uma moeda.</summary>
    static float[] BuildCoin()
    {
        var buf = new float[Seconds(0.16f)];

        Tone(buf, 0f, 0.07f, 1568f, 1568f, Wave.Square, 0.3f, 0.002f, 0.04f);
        Tone(buf, 0.045f, 0.1f, 2093f, 2093f, Wave.Square, 0.3f, 0.002f, 0.08f);

        Normalize(buf, 0.65f);
        return buf;
    }

    /// <summary>
    /// Hélice em ciclo. Todas as frequências são múltiplos de 1/duração,
    /// senão a emenda do loop estala.
    /// </summary>
    static float[] BuildEngineLoop()
    {
        const float duration = 0.5f;   // período de 0,5 s → só múltiplos de 2 Hz emendam
        int n = Seconds(duration);
        var buf = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float chop = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 18f * t);

            float s = Osc(Wave.Saw, Frac(90f * t)) * 0.5f
                    + Osc(Wave.Saw, Frac(180f * t)) * 0.2f
                    + Osc(Wave.Sine, Frac(46f * t)) * 0.35f;

            buf[i] = s * chop;
        }

        Distort(buf, 1.3f);
        Normalize(buf, 0.55f);
        return buf;
    }

    // --------------------------------------------------------------- síntese

    static int Seconds(float s) => Mathf.RoundToInt(s * Rate);

    static float Frac(float v) => v - Mathf.Floor(v);

    static float Osc(Wave shape, float phase)
    {
        switch (shape)
        {
            case Wave.Square: return phase < 0.5f ? 1f : -1f;
            case Wave.Saw: return phase * 2f - 1f;
            case Wave.Triangle: return 1f - 4f * Mathf.Abs(phase - 0.5f);
            default: return Mathf.Sin(phase * Mathf.PI * 2f);
        }
    }

    /// <summary>Soma um oscilador com varrimento de frequência e envelope.</summary>
    static void Tone(float[] buf, float start, float duration, float freqFrom, float freqTo,
                     Wave shape, float amp, float attack, float decay)
    {
        int i0 = Seconds(start);
        int n = Seconds(duration);
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            int k = i0 + i;
            if (k < 0 || k >= buf.Length) continue;

            float progress = i / (float)n;
            float freq = Mathf.Lerp(freqFrom, freqTo, progress);

            phase += freq / Rate;
            phase -= Mathf.Floor(phase);

            buf[k] += Osc(shape, phase) * amp * Envelope(i / (float)Rate, duration, attack, decay);
        }
    }

    static void Noise(float[] buf, float start, float duration, float amp, float decay, System.Random rng)
    {
        int i0 = Seconds(start);
        int n = Seconds(duration);

        for (int i = 0; i < n; i++)
        {
            int k = i0 + i;
            if (k < 0 || k >= buf.Length) continue;

            float t = i / (float)Rate;
            float env = Mathf.Exp(-t / decay) * Mathf.Clamp01((duration - t) / 0.02f);

            buf[k] += (float)(rng.NextDouble() * 2.0 - 1.0) * amp * env;
        }
    }

    /// <summary>Ataque linear, queda exponencial e um corte final que evita o clique.</summary>
    static float Envelope(float time, float duration, float attack, float decay)
    {
        float a = attack <= 0f ? 1f : Mathf.Clamp01(time / attack);
        float d = decay <= 0f ? 1f : Mathf.Exp(-time / decay);
        float tail = Mathf.Clamp01((duration - time) / 0.01f);
        return a * d * tail;
    }

    static void Distort(float[] buf, float drive)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            buf[i] = (float)System.Math.Tanh(buf[i] * drive);
        }
    }

    static void Normalize(float[] buf, float peak)
    {
        float max = 0f;
        foreach (var s in buf) max = Mathf.Max(max, Mathf.Abs(s));
        if (max <= 0.0001f) return;

        float gain = peak / max;
        for (int i = 0; i < buf.Length; i++) buf[i] *= gain;
    }

    // ----------------------------------------------------------------- WAV

    /// <summary>
    /// Grava um som sintetizado. Devolve falso quando já existe um ficheiro com
    /// esse nome e não foi pedida a reposição: som trazido de fora não se apaga
    /// por engano.
    /// </summary>
    static bool Save(string name, float[] samples, bool overwrite)
    {
        string path = Dir + "/" + name + ".wav";
        if (!overwrite && File.Exists(path)) return false;

        File.WriteAllBytes(path, EncodeWav(samples));

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.SaveAndReimport();
        return true;
    }

    static byte[] EncodeWav(float[] samples)
    {
        int dataSize = samples.Length * 2;

        using (var stream = new MemoryStream())
        using (var w = new BinaryWriter(stream))
        {
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataSize);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));

            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);                    // tamanho do bloco fmt
            w.Write((short)1);              // PCM
            w.Write((short)1);              // mono
            w.Write(Rate);
            w.Write(Rate * 2);              // bytes por segundo
            w.Write((short)2);              // alinhamento de bloco
            w.Write((short)16);             // bits por amostra

            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(dataSize);

            foreach (var s in samples)
            {
                w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }

            w.Flush();
            return stream.ToArray();
        }
    }
}
