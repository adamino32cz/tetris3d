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
    public void Clear() => Chord(new[] { 523f, 659f, 784f }, 0.16f, 0.35f);
    public void Over() => Chord(new[] { 330f, 247f, 196f }, 0.28f, 0.4f);
    public void StartGame() => Chord(new[] { 392f, 523f, 659f }, 0.12f, 0.3f);

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
