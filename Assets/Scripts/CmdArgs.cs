// Parametry z příkazové řádky (-autoplay, -screenshots, -shotdir).
// V prohlížeči (WebGL) žádné nejsou, tak vracíme prázdné pole.
public static class CmdArgs
{
    static string[] cache;

    public static string[] Get()
    {
        if (cache != null) return cache;
#if UNITY_WEBGL && !UNITY_EDITOR
        cache = new string[0];
#else
        try { cache = System.Environment.GetCommandLineArgs(); }
        catch { cache = new string[0]; }
#endif
        return cache;
    }

    public static bool Has(string a) => System.Array.IndexOf(Get(), a) >= 0;

    public static string Value(string key, string fallback = "")
    {
        var a = Get();
        int i = System.Array.IndexOf(a, key);
        if (i >= 0 && i + 1 < a.Length) return a[i + 1];
        return fallback;
    }
}
