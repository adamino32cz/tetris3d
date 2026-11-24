using System.Collections.Generic;
using UnityEngine;

// Jeden vzhled kostek: barevná paleta, nebo tajná textura / model.
[System.Serializable]
public class KostkaSkinData
{
    public string id;
    public string displayName;
    public bool secret;
    public string unlockCode;
    public bool usesTexture; // animované 67 na stěnách
    public bool usesFace;    // portrét na všech stěnách
    public string faceRes;
    public bool usesMiku;    // pózovaná Miku (MIKU EXTRA)
    public bool usesAstolfo; // pózovaný Astolfo (ASTOLFO EXTRA)
    public Color[] colors;   // 8 barev kousků
    public Color swatch;
}

// Katalog skinů, PlayerPrefs a materiály (včetně animace 67).
public static class KostkaSkin
{
    public const string PrefsSelected = "Kostka.Skin";
    public const string PrefsUnlockPrefix = "Kostka.SkinUnlocked_";
    public const string PrefsDev = "Kostka.DevUnlocked";
    public const string PrefsSfxPrefix = "Kostka.SkinSfx_";
    public const string PrefsJumpPrefix = "Kostka.SkinJump_";

    public static readonly string[] SfxIds = { "default", "meow", "yamete", "67" };
    public static readonly string[] SfxLabels = { "Výchozí", "Mňau", "Yamete", "67" };
    public static readonly string[] JumpIds = { "spider", "astolfo", "miku", "67" };
    public static readonly string[] JumpLabels = { "Pavouk", "Astolfo", "Miku", "67" };

    public static readonly KostkaSkinData[] All =
    {
        Palette("pastel", "Pastel",
            C(0.55f, 0.86f, 0.78f), C(0.98f, 0.86f, 0.45f), C(0.96f, 0.62f, 0.42f), C(0.76f, 0.62f, 0.93f),
            C(0.55f, 0.78f, 0.96f), C(0.98f, 0.70f, 0.62f), C(0.62f, 0.82f, 0.58f), C(0.96f, 0.58f, 0.70f),
            C(0.96f, 0.78f, 0.70f)),
        Palette("neon", "Neon",
            C(0.05f, 1.00f, 0.85f), C(1.00f, 0.92f, 0.10f), C(1.00f, 0.35f, 0.05f), C(0.85f, 0.15f, 1.00f),
            C(0.10f, 0.55f, 1.00f), C(1.00f, 0.15f, 0.55f), C(0.20f, 1.00f, 0.20f), C(1.00f, 0.20f, 0.85f),
            C(0.20f, 1.00f, 0.75f)),
        Palette("mono", "Černobílá",
            C(0.95f, 0.95f, 0.95f), C(0.80f, 0.80f, 0.80f), C(0.62f, 0.62f, 0.62f), C(0.45f, 0.45f, 0.45f),
            C(0.30f, 0.30f, 0.30f), C(0.72f, 0.72f, 0.72f), C(0.18f, 0.18f, 0.18f), C(0.55f, 0.55f, 0.55f),
            C(0.55f, 0.55f, 0.55f)),
        Palette("candy", "Bonbóny",
            C(1.00f, 0.55f, 0.75f), C(1.00f, 0.85f, 0.30f), C(1.00f, 0.45f, 0.55f), C(0.75f, 0.55f, 1.00f),
            C(0.45f, 0.95f, 0.90f), C(1.00f, 0.70f, 0.80f), C(0.60f, 1.00f, 0.55f), C(1.00f, 0.40f, 0.70f),
            C(1.00f, 0.55f, 0.75f)),
        Palette("ocean", "Oceán",
            C(0.15f, 0.55f, 0.80f), C(0.95f, 0.85f, 0.55f), C(0.95f, 0.45f, 0.35f), C(0.35f, 0.55f, 0.85f),
            C(0.25f, 0.82f, 0.85f), C(0.55f, 0.78f, 0.90f), C(0.10f, 0.40f, 0.45f), C(0.20f, 0.28f, 0.50f),
            C(0.25f, 0.65f, 0.80f)),
        Palette("forest", "Les",
            C(0.45f, 0.70f, 0.35f), C(0.85f, 0.75f, 0.35f), C(0.62f, 0.38f, 0.18f), C(0.45f, 0.30f, 0.45f),
            C(0.30f, 0.55f, 0.35f), C(0.80f, 0.50f, 0.30f), C(0.25f, 0.40f, 0.22f), C(0.70f, 0.25f, 0.28f),
            C(0.40f, 0.60f, 0.30f)),
        new KostkaSkinData {
            id = "67", displayName = "67", secret = true, unlockCode = "67",
            usesTexture = true,
            colors = Repeat(C(1f, 0.92f, 0.15f), 8),
            swatch = C(1f, 0.92f, 0.15f)
        },
        new KostkaSkinData {
            id = "miku", displayName = "MIKU", secret = true, unlockCode = "MIKU",
            usesFace = true, faceRes = "SkinFaces/MikuFace",
            colors = Repeat(C(0.55f, 0.95f, 0.92f), 8),
            swatch = C(0.25f, 0.90f, 0.85f)
        },
        new KostkaSkinData {
            id = "miku_extra", displayName = "MIKU EXTRA", secret = true, unlockCode = "MIKU EXTRA",
            usesMiku = true,
            colors = new[] {
                C(0.20f, 0.85f, 0.80f), C(0.35f, 0.95f, 0.90f), C(0.15f, 0.18f, 0.22f), C(0.55f, 0.95f, 0.92f),
                C(0.30f, 0.70f, 0.72f), C(1.00f, 0.45f, 0.65f), C(0.08f, 0.45f, 0.48f), C(0.70f, 0.95f, 0.92f)
            },
            swatch = C(0.15f, 0.75f, 0.72f)
        },
        new KostkaSkinData {
            id = "astolfo", displayName = "ASTOLFO", secret = true, unlockCode = "ASTOLFO",
            usesFace = true, faceRes = "SkinFaces/AstolfoFace",
            colors = Repeat(C(1f, 0.70f, 0.82f), 8),
            swatch = C(1f, 0.55f, 0.75f)
        },
        new KostkaSkinData {
            id = "astolfo_extra", displayName = "ASTOLFO EXTRA", secret = true, unlockCode = "ASTOLFO EXTRA",
            usesAstolfo = true,
            colors = new[] {
                C(1.00f, 0.55f, 0.75f), C(1.00f, 0.85f, 0.90f), C(0.12f, 0.10f, 0.12f), C(1.00f, 0.70f, 0.82f),
                C(0.90f, 0.45f, 0.60f), C(1.00f, 0.95f, 0.97f), C(0.20f, 0.15f, 0.20f), C(0.95f, 0.80f, 0.35f)
            },
            swatch = C(0.85f, 0.35f, 0.55f)
        },
    };

    static string selectedId = "pastel";
    static Texture2D[] frames67;
    static int frame67;
    static float acc67;
    static Material mat67, mat67Ghost;
    const float Fps67 = 12f;

    public static event System.Action OnSkinChanged;

    static KostkaSkinData Palette(string id, string name, Color a, Color b, Color c, Color d, Color e, Color f, Color g, Color h, Color swatch)
    {
        return new KostkaSkinData { id = id, displayName = name, colors = new[] { a, b, c, d, e, f, g, h }, swatch = swatch };
    }

    static Color C(float r, float g, float b) => new Color(r, g, b, 1f);
    static Color[] Repeat(Color c, int n)
    {
        var a = new Color[n];
        for (int i = 0; i < n; i++) a[i] = c;
        return a;
    }

    public static KostkaSkinData Current() => Get(selectedId) ?? All[0];
    public static bool Is67 => Current() != null && Current().usesTexture;
    public static bool IsMiku => Current() != null && Current().usesMiku;
    public static bool IsAstolfo => Current() != null && Current().usesAstolfo;
    public static bool IsFace => Current() != null && Current().usesFace;
    public static bool IsCharacter => IsMiku || IsAstolfo;
    public static bool DevUnlocked { get; private set; }

    public static KostkaSkinData Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return All[0];
        for (int i = 0; i < All.Length; i++) if (All[i].id == id) return All[i];
        return null;
    }

    public static int IndexOf(string id)
    {
        for (int i = 0; i < All.Length; i++) if (All[i].id == id) return i;
        return 0;
    }

    public static bool IsUnlocked(KostkaSkinData s)
    {
        if (s == null) return false;
        if (!s.secret) return true;
        return PlayerPrefs.GetInt(PrefsUnlockPrefix + s.id, 0) == 1;
    }

    public static void Unlock(KostkaSkinData s)
    {
        if (s == null) return;
        PlayerPrefs.SetInt(PrefsUnlockPrefix + s.id, 1);
        PlayerPrefs.Save();
    }

    public static KostkaSkinData UnlockWithCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        string c = System.Text.RegularExpressions.Regex.Replace((code ?? "").Trim(), @"\s+", " ");
        string cTight = c.Replace(" ", "");
        for (int i = 0; i < All.Length; i++)
        {
            var s = All[i];
            if (!s.secret || string.IsNullOrEmpty(s.unlockCode)) continue;
            string u = s.unlockCode;
            string uTight = u.Replace(" ", "");
            if (string.Equals(u, c, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(uTight, cTight, System.StringComparison.OrdinalIgnoreCase))
            {
                Unlock(s);
                return s;
            }
        }
        return null;
    }

    public static bool Select(string id)
    {
        var s = Get(id);
        if (s == null || !IsUnlocked(s)) return false;
        selectedId = s.id;
        PlayerPrefs.SetString(PrefsSelected, selectedId);
        PlayerPrefs.Save();
        OnSkinChanged?.Invoke();
        return true;
    }

    public static void LoadFromPrefsAndArgs(string[] args)
    {
        DevUnlocked = PlayerPrefs.GetInt(PrefsDev, 0) == 1;
        selectedId = PlayerPrefs.GetString(PrefsSelected, "pastel");
        if (Get(selectedId) == null || !IsUnlocked(Get(selectedId))) selectedId = "pastel";
        if (System.Array.IndexOf(args, "-dev") >= 0) UnlockDev();
        int a = System.Array.IndexOf(args, "-skin");
        if (a >= 0 && a + 1 < args.Length)
        {
            var s = Get(args[a + 1]);
            if (s != null)
            {
                if (s.secret) Unlock(s);
                selectedId = s.id;
                PlayerPrefs.SetString(PrefsSelected, selectedId);
                PlayerPrefs.Save();
            }
        }
    }

    public static Color ColorOf(int tvar)
    {
        var s = Current();
        var cols = (s != null && s.colors != null && s.colors.Length > 0) ? s.colors : All[0].colors;
        if (tvar < 0) tvar = 0;
        return cols[tvar % cols.Length];
    }

    static readonly Dictionary<string, Material> faceLit = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> faceGhost = new Dictionary<string, Material>();
    static AudioClip clipMeow, clipYamete, clip67;
    static bool clipsTried;

    public static Material Material(int tvar, bool ghost)
    {
        if (Is67)
        {
            Ensure67();
            return ghost ? mat67Ghost : mat67;
        }
        var cur = Current();
        if (cur != null && cur.usesFace && !string.IsNullOrEmpty(cur.faceRes))
            return FaceMat(cur.faceRes, ghost);
        var c = ColorOf(tvar);
        return ghost ? KostkaVisual.Ghost(c) : KostkaVisual.Lit(c);
    }

    public static Texture2D FaceTex(string res)
    {
        return string.IsNullOrEmpty(res) ? null : Resources.Load<Texture2D>(res);
    }

    static Material FaceMat(string res, bool ghost)
    {
        var dict = ghost ? faceGhost : faceLit;
        if (dict.TryGetValue(res, out var m) && m != null) return m;
        var tex = FaceTex(res);
        if (ghost) m = KostkaVisual.Ghost(Color.white);
        else m = KostkaVisual.Lit(Color.white);
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        }
        dict[res] = m;
        return m;
    }

    public static void UnlockDev()
    {
        DevUnlocked = true;
        PlayerPrefs.SetInt(PrefsDev, 1);
        PlayerPrefs.Save();
    }

    public static string SfxOf(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || !DevUnlocked) return "default";
        string v = PlayerPrefs.GetString(PrefsSfxPrefix + skinId, "default");
        for (int i = 0; i < SfxIds.Length; i++) if (SfxIds[i] == v) return v;
        return "default";
    }

    public static void SetSfx(string skinId, string sfxId)
    {
        if (string.IsNullOrEmpty(skinId)) return;
        PlayerPrefs.SetString(PrefsSfxPrefix + skinId, sfxId ?? "default");
        PlayerPrefs.Save();
    }

    public static string JumpOf(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || !DevUnlocked) return "spider";
        string v = PlayerPrefs.GetString(PrefsJumpPrefix + skinId, "spider");
        for (int i = 0; i < JumpIds.Length; i++) if (JumpIds[i] == v) return v;
        return "spider";
    }

    public static string JumpOfCurrent() => JumpOf(Current() != null ? Current().id : "pastel");

    public static void SetJump(string skinId, string jumpId)
    {
        if (string.IsNullOrEmpty(skinId)) return;
        PlayerPrefs.SetString(PrefsJumpPrefix + skinId, jumpId ?? "spider");
        PlayerPrefs.Save();
    }

    static void EnsureClips()
    {
        if (clipsTried) return;
        clipsTried = true;
        clipMeow = Resources.Load<AudioClip>("SkinSfx/meow");
        clipYamete = Resources.Load<AudioClip>("SkinSfx/yamete");
        clip67 = Resources.Load<AudioClip>("SkinSfx/sfx67");
    }

    public static AudioClip ClipForSfx(string sfxId)
    {
        EnsureClips();
        if (sfxId == "meow") return clipMeow;
        if (sfxId == "yamete") return clipYamete;
        if (sfxId == "67") return clip67;
        return null;
    }

    public static bool TryPlayClearOverride()
    {
        if (!DevUnlocked) return false;
        var cur = Current();
        if (cur == null) return false;
        string id = SfxOf(cur.id);
        if (id == "default") return false;
        var clip = ClipForSfx(id);
        if (clip == null || SoundFX.I == null) return false;
        // Yamete (a ostatní DEV SFX) naplno — hlasitost je v samotném klipu.
        SoundFX.I.PlayClip(clip, 1f);
        return true;
    }

    public static AudioClip JumpscareClip(string content)
    {
        EnsureClips();
        if (content == "67") return clip67;
        if (content == "miku" || content == "astolfo") return clipYamete != null ? clipYamete : clipMeow;
        return null;
    }

    public static void Tick(float dt)
    {
        if (!Is67 || frames67 == null || frames67.Length == 0) return;
        acc67 += dt;
        float step = 1f / Fps67;
        if (acc67 < step) return;
        acc67 %= step;
        frame67 = (frame67 + 1) % frames67.Length;
        Apply67Frame();
    }

    public static void RefreshRenderers()
    {
        var cells = Object.FindObjectsOfType<KostkaBunka>();
        for (int i = 0; i < cells.Length; i++)
        {
            var b = cells[i];
            if (b == null) continue;
            var mr = b.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = Material(b.tvar, b.ghost);
        }
    }

    static void Ensure67()
    {
        if (frames67 != null && mat67 != null) return;
        frames67 = Build67Frames(16, 256);
        frame67 = 0;
        mat67 = KostkaVisual.Lit(Color.white);
        if (mat67.HasProperty("_BaseMap")) mat67.SetTexture("_BaseMap", frames67[0]);
        if (mat67.HasProperty("_MainTex")) mat67.SetTexture("_MainTex", frames67[0]);
        if (mat67.HasProperty("_Smoothness")) mat67.SetFloat("_Smoothness", 0.2f);
        mat67Ghost = KostkaVisual.Ghost(Color.white);
        if (mat67Ghost.HasProperty("_BaseMap")) mat67Ghost.SetTexture("_BaseMap", frames67[0]);
        if (mat67Ghost.HasProperty("_MainTex")) mat67Ghost.SetTexture("_MainTex", frames67[0]);
        Apply67Frame();
    }

    static void Apply67Frame()
    {
        if (frames67 == null || frames67.Length == 0) return;
        var t = frames67[frame67];
        if (mat67 != null)
        {
            if (mat67.HasProperty("_BaseMap")) mat67.SetTexture("_BaseMap", t);
            if (mat67.HasProperty("_MainTex")) mat67.SetTexture("_MainTex", t);
        }
        if (mat67Ghost != null)
        {
            if (mat67Ghost.HasProperty("_BaseMap")) mat67Ghost.SetTexture("_BaseMap", t);
            if (mat67Ghost.HasProperty("_MainTex")) mat67Ghost.SetTexture("_MainTex", t);
        }
    }

    // Vlastní animace „6 7“ (poskakující čísla + mávající ruka). Žádný cizí GIF.
    public static Texture2D[] Build67Frames(int n, int size)
    {
        var frames = new Texture2D[n];
        for (int f = 0; f < n; f++)
        {
            float u = f / (float)n;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "Skin67_" + f;
            var bgA = new Color(0.12f, 0.04f, 0.18f, 1f);
            var bgB = new Color(0.28f, 0.08f, 0.40f, 1f);
            var ink = new Color(1f, 0.92f, 0.15f, 1f);
            var ink2 = new Color(1f, 0.55f, 0.15f, 1f);
            float bounce = Mathf.Sin(u * 2f * Mathf.PI) * 0.07f;
            float wave = Mathf.Sin(u * 2f * Mathf.PI) * 22f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size;
                float ny = (y + 0.5f) / size;
                Color col = Color.Lerp(bgA, bgB, ny);
                // rámeček
                if (nx < 0.04f || nx > 0.96f || ny < 0.04f || ny > 0.96f)
                    col = Color.Lerp(ink, ink2, 0.4f);
                // čísla
                float n6 = Digit6(nx, ny, 0.28f, 0.50f + bounce, 0.28f);
                float n7 = Digit7(nx, ny, 0.62f, 0.50f - bounce * 0.6f, 0.28f);
                float d = Mathf.Max(n6, n7);
                if (d > 0f) col = Color.Lerp(col, ink, Mathf.Clamp01(d * 8f));
                // ruka vpravo nahoře
                float hx = 0.82f, hy = 0.78f + bounce * 0.15f;
                float ang = (wave - 10f) * Mathf.Deg2Rad;
                float hand = Hand(nx, ny, hx, hy, ang);
                if (hand > 0f) col = Color.Lerp(col, new Color(1f, 0.85f, 0.55f, 1f), Mathf.Clamp01(hand * 8f));
                tex.SetPixel(x, y, col);
            }
            tex.Apply(false, false);
            frames[f] = tex;
        }
        return frames;
    }

    static float Digit6(float x, float y, float cx, float cy, float s)
    {
        float px = (x - cx) / s, py = (y - cy) / s;
        float d = 1f;
        d = Mathf.Min(d, Ring(px, py, 0f, -0.22f, 0.42f, 0.14f)); // dolní smyčka
        d = Mathf.Min(d, Capsule(px, py, -0.42f, -0.22f, -0.42f, 0.62f, 0.14f)); // hřbet
        d = Mathf.Min(d, Capsule(px, py, -0.42f, 0.62f, 0.22f, 0.62f, 0.14f)); // horní čárka
        return Mathf.Clamp01(1f - d / 0.02f);
    }

    static float Digit7(float x, float y, float cx, float cy, float s)
    {
        float px = (x - cx) / s, py = (y - cy) / s;
        float d = 1f;
        d = Mathf.Min(d, Capsule(px, py, -0.42f, 0.68f, 0.42f, 0.68f, 0.13f));
        d = Mathf.Min(d, Capsule(px, py, 0.28f, 0.68f, -0.22f, -0.70f, 0.13f));
        return Mathf.Clamp01(1f - d / 0.02f);
    }

    static float Hand(float x, float y, float cx, float cy, float ang)
    {
        float px = x - cx, py = y - cy;
        float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
        float rx = px * ca + py * sa, ry = -px * sa + py * ca;
        float d = Circle(rx, ry, 0f, 0f, 0.055f);
        // prsty
        for (int i = 0; i < 4; i++)
        {
            float a = (-30f + i * 20f) * Mathf.Deg2Rad;
            float fx = Mathf.Cos(a) * 0.095f, fy = Mathf.Sin(a) * 0.095f;
            d = Mathf.Min(d, Capsule(rx, ry, 0f, 0.02f, fx, 0.02f + fy, 0.018f));
        }
        d = Mathf.Min(d, Capsule(rx, ry, -0.02f, -0.02f, -0.07f, -0.06f, 0.016f)); // palec
        return Mathf.Clamp01(1f - d / 0.01f);
    }

    static float Ring(float x, float y, float cx, float cy, float r, float w)
    {
        float d = Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w;
        return d;
    }

    static float Circle(float x, float y, float cx, float cy, float r)
    {
        return Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
    }

    static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float px = x - ax, py = y - ay, dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01((px * dx + py * dy) / Mathf.Max(1e-6f, dx * dx + dy * dy));
        float qx = px - dx * t, qy = py - dy * t;
        return Mathf.Sqrt(qx * qx + qy * qy) - r;
    }
}

// Identita položené / dekorativní kostky, aby šlo přebarvit při změně skinu.
public class KostkaBunka : MonoBehaviour
{
    public int tvar;
    public bool ghost;
}

// Tiká animaci 67 a hlásí změnu skinu.
public class KostkaSkinHub : MonoBehaviour
{
    void Update()
    {
        KostkaSkin.Tick(Time.unscaledDeltaTime);
    }
}
