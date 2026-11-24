using UnityEngine;

// Jednoduché procedurální zvuky (pípnutí), bez souborů.
public class SoundFX : MonoBehaviour
{
    public static SoundFX I;
    AudioSource src;

    void Awake()
    {
        I = this;
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0.7f;
    }

    public void Move() => Play(620f, 0.04f, 0.18f, 0.4f);
    public void Rotate() => Play(740f, 0.05f, 0.22f, 0.5f);
    public void Lock() => Play(280f, 0.09f, 0.28f, 0.7f);
    public void Drop() => Play(180f, 0.12f, 0.35f, 0.9f);
    public void Clear()
    {
        if (KostkaSkin.TryPlayClearOverride()) return;
        Chord(new[] { 523f, 659f, 784f }, 0.16f, 0.35f);
    }
    public void Over() => Chord(new[] { 330f, 247f, 196f }, 0.28f, 0.4f);
    public void StartGame() => Chord(new[] { 392f, 523f, 659f }, 0.12f, 0.3f);

    // Ostrý výkřik k jumpscare (šum + klesající vřískot).
    public void PlayClip(AudioClip clip, float vol = 1f)
    {
        if (src == null || clip == null) return;
        src.volume = 1f;
        src.PlayOneShot(clip, vol);
    }

    public void Scream()
    {
        if (src == null) return;
        int rate = 22050;
        float dur = 0.85f;
        int n = Mathf.Max(64, (int)(dur * rate));
        var data = new float[n];
        float phase = 0f;
        var rng = new System.Random(17);
        for (int i = 0; i < n; i++)
        {
            float tm = i / (float)rate;
            float env = Mathf.Exp(-tm * 2.4f) * (tm < 0.04f ? tm / 0.04f : 1f);
            float freq = Mathf.Lerp(1900f, 220f, tm / dur) + Mathf.Sin(tm * 55f) * 180f;
            phase += 2f * Mathf.PI * freq / rate;
            float buzz = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2.17f) + 0.2f * Mathf.Sin(phase * 3.3f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            data[i] = Mathf.Clamp(buzz * 0.6f + noise * 0.55f, -1f, 1f) * env;
        }
        var clip = AudioClip.Create("scream", n, 1, rate, false);
        clip.SetData(data, 0);
        src.PlayOneShot(clip, 1f);
    }

    // Silnější screamer: rána, zkreslený vřískot a šum. Hlasitěji než Scream().
    public void ScreamSilny()
    {
        if (src == null) return;
        src.volume = 1f;
        src.PlayOneShot(MakeImpact(), 1f);
        src.PlayOneShot(MakeHarshScream(), 1f);
        src.PlayOneShot(MakeNoiseBurst(), 0.95f);
    }

    static AudioClip MakeImpact()
    {
        int rate = 22050;
        float dur = 0.22f;
        int n = Mathf.Max(64, (int)(dur * rate));
        var data = new float[n];
        var rng = new System.Random(91);
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float tm = i / (float)rate;
            float env = Mathf.Exp(-tm * 14f);
            float freq = Mathf.Lerp(90f, 32f, tm / dur);
            phase += 2f * Mathf.PI * freq / rate;
            float wave = Mathf.Sin(phase) * 0.85f + (float)(rng.NextDouble() * 2.0 - 1.0) * 0.55f;
            data[i] = Mathf.Clamp((float)System.Math.Tanh(wave * 2.4f) * env, -1f, 1f);
        }
        var clip = AudioClip.Create("impact", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeHarshScream()
    {
        int rate = 22050;
        float dur = 1.15f;
        int n = Mathf.Max(64, (int)(dur * rate));
        var data = new float[n];
        var rng = new System.Random(23);
        float phase = 0f, phase2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float tm = i / (float)rate;
            float k = tm / dur;
            float env = (tm < 0.02f ? tm / 0.02f : 1f) * Mathf.Exp(-tm * 1.55f);
            float freq;
            if (k < 0.12f)
                freq = Mathf.Lerp(700f, 2400f, k / 0.12f);
            else
                freq = Mathf.Lerp(2400f, 160f, (k - 0.12f) / 0.88f);
            freq += Mathf.Sin(tm * 70f) * 220f + Mathf.Sin(tm * 17f) * 90f;
            phase += 2f * Mathf.PI * freq / rate;
            phase2 += 2f * Mathf.PI * (freq * 1.53f + 40f) / rate;
            float buzz = Mathf.Sin(phase) + 0.55f * Mathf.Sin(phase2) + 0.28f * Mathf.Sin(phase * 2.7f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float mix = buzz * 0.72f + noise * (0.38f + 0.25f * env);
            data[i] = Mathf.Clamp((float)System.Math.Tanh(mix * 2.6f) * env, -1f, 1f);
        }
        var clip = AudioClip.Create("screamHarsh", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeNoiseBurst()
    {
        int rate = 22050;
        float dur = 0.38f;
        int n = Mathf.Max(64, (int)(dur * rate));
        var data = new float[n];
        var rng = new System.Random(41);
        for (int i = 0; i < n; i++)
        {
            float tm = i / (float)rate;
            float env = (tm < 0.015f ? tm / 0.015f : 1f) * Mathf.Exp(-tm * 8f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            data[i] = Mathf.Clamp(noise * env, -1f, 1f);
        }
        var clip = AudioClip.Create("noiseBurst", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Play(float freq, float dur, float vol, float decay)
    {
        if (src == null) return;
        src.PlayOneShot(Make(freq, dur, vol, decay), vol);
    }

    void Chord(float[] freqs, float dur, float vol)
    {
        for (int i = 0; i < freqs.Length; i++)
            Play(freqs[i], dur + i * 0.03f, vol * (1f - i * 0.15f), 1.1f);
    }

    static AudioClip Make(float freq, float dur, float vol, float decay)
    {
        int rate = 22050;
        int n = Mathf.Max(64, (int)(dur * rate));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 8f * decay) * (1f - i / (float)n);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
        }
        var clip = AudioClip.Create("beep", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
