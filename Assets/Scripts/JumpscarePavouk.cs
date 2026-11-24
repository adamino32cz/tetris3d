using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Procedurální pavouk pro krátký jumpscare (tělo, zadeček, 8 nohou, červené oči).
public class JumpscarePavouk : MonoBehaviour
{
    public static int VideoFrameIndex = -1;
    readonly List<Transform> kyce = new List<Transform>();
    readonly List<Quaternion> kyceRest = new List<Quaternion>();

    public static GameObject Postav(Transform parent)
    {
        var rootGo = new GameObject("Pavouk");
        var root = rootGo.transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(0f, -0.05f, 2.4f);
        root.localRotation = Quaternion.Euler(8f, 0f, 0f);
        root.localScale = Vector3.one * 0.2f;

        var script = rootGo.AddComponent<JumpscarePavouk>();
        var telo = KostkaVisual.Lit(new Color(0.07f, 0.04f, 0.03f));
        var oko = KostkaVisual.Unlit(new Color(1f, 0.08f, 0.05f));
        var zornice = KostkaVisual.Unlit(new Color(0.02f, 0.01f, 0.01f));
        var nohaMat = KostkaVisual.Lit(new Color(0.16f, 0.08f, 0.05f));

        // Zadeček
        Koule(root, "Zadecel", new Vector3(0f, 0.08f, 0.42f), new Vector3(0.85f, 0.70f, 1.05f), telo);
        // Hruď
        Koule(root, "Hrud", new Vector3(0f, 0.06f, -0.22f), new Vector3(0.62f, 0.48f, 0.62f), telo);
        // Hlava
        Koule(root, "Hlava", new Vector3(0f, 0.04f, -0.52f), new Vector3(0.38f, 0.32f, 0.36f), telo);
        // Klepítka
        Koule(root, "KlepitkoL", new Vector3(-0.10f, -0.06f, -0.68f), new Vector3(0.08f, 0.10f, 0.16f), telo);
        Koule(root, "KlepitkoP", new Vector3( 0.10f, -0.06f, -0.68f), new Vector3(0.08f, 0.10f, 0.16f), telo);

        // Oči
        Koule(root, "OkoL", new Vector3(-0.13f, 0.15f, -0.70f), Vector3.one * 0.18f, oko);
        Koule(root, "OkoP", new Vector3( 0.13f, 0.15f, -0.70f), Vector3.one * 0.18f, oko);
        Koule(root, "ZorniceL", new Vector3(-0.13f, 0.15f, -0.78f), Vector3.one * 0.08f, zornice);
        Koule(root, "ZorniceP", new Vector3( 0.13f, 0.15f, -0.78f), Vector3.one * 0.08f, zornice);

        for (int side = -1; side <= 1; side += 2)
        for (int i = 0; i < 4; i++)
            script.Noha(root, nohaMat, side, i);

        return rootGo;
    }

    public static IEnumerator Hraj(Camera cam, float trvani = 1.05f)
    {
        if (cam == null) yield break;
        // Tmavé plátno za pavoukem (ne přes UI, ať je pavouk vidět).
        var dim = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dim.name = "PavoukStin";
        dim.transform.SetParent(cam.transform, false);
        dim.transform.localPosition = new Vector3(0f, 0f, 1.35f);
        dim.transform.localRotation = Quaternion.identity;
        dim.transform.localScale = new Vector3(5f, 3.5f, 1f);
        var dmr = dim.GetComponent<MeshRenderer>();
        dmr.sharedMaterial = KostkaVisual.Unlit(new Color(0.10f, 0.01f, 0.01f, 0.5f));
        dmr.shadowCastingMode = ShadowCastingMode.Off;
        dmr.receiveShadows = false;
        Object.Destroy(dim.GetComponent<Collider>());

        var go = Postav(cam.transform);
        var anim = go.GetComponent<JumpscarePavouk>();
        if (SoundFX.I != null) SoundFX.I.Scream();
        if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.45f;

        float t = 0f;
        while (t < trvani)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / trvani);
            float z, s;
            if (k < 0.22f)
            {
                float u = k / 0.22f;
                float e = 1f - (1f - u) * (1f - u);
                z = Mathf.Lerp(3.4f, 1.55f, e);
                s = Mathf.Lerp(0.12f, 0.95f, e);
            }
            else if (k < 0.72f)
            {
                z = 1.55f + Mathf.Sin(t * 22f) * 0.05f;
                s = 0.95f + Mathf.Sin(t * 18f) * 0.03f;
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.28f;
            }
            else
            {
                float u = (k - 0.72f) / 0.28f;
                z = Mathf.Lerp(1.55f, 3.6f, u * u);
                s = Mathf.Lerp(0.95f, 0.10f, u * u);
            }
            go.transform.localPosition = new Vector3(Mathf.Sin(t * 24f) * 0.06f, -0.15f + Mathf.Sin(t * 20f) * 0.04f, z);
            go.transform.localScale = Vector3.one * s;
            go.transform.localRotation = Quaternion.Euler(12f + Mathf.Sin(t * 14f) * 3f, Mathf.Sin(t * 10f) * 6f, Mathf.Sin(t * 16f) * 5f);
            if (anim != null) anim.Vlnit(t);
            yield return null;
        }
        Object.Destroy(go);
        if (dim != null) Object.Destroy(dim);
    }

    void Noha(Transform root, Material mat, int strana, int i)
    {
        // Přední nohy (i=0,1) míří kameře naproti, zadní do stran.
        float z = Mathf.Lerp(-0.55f, 0.20f, i / 3f);
        var kyc = new GameObject("Kyc" + strana + i).transform;
        kyc.SetParent(root, false);
        kyc.localPosition = new Vector3(strana * 0.34f, -0.04f, z);
        float yaw = strana * (18f + i * 30f);
        float roll = strana * (58f + i * 5f);
        float pitch = i <= 1 ? -40f + i * 10f : 8f + (i - 2) * 8f;
        kyc.localRotation = Quaternion.Euler(pitch, yaw, roll);
        kyce.Add(kyc);
        kyceRest.Add(kyc.localRotation);

        var stehno = Valec(kyc, "Stehno", new Vector3(0f, 0.38f, 0f), new Vector3(0.11f, 0.38f, 0.11f), mat);
        var koleno = new GameObject("Koleno").transform;
        koleno.SetParent(stehno, false);
        koleno.localPosition = new Vector3(0f, 0.95f, 0f);
        koleno.localRotation = Quaternion.Euler(i <= 1 ? 25f : 0f, 0f, strana * -62f);
        Valec(koleno, "Holen", new Vector3(0f, 0.42f, 0f), new Vector3(0.09f, 0.42f, 0.09f), mat);
        // malé chodidlo, ať je konec nohy čitelný
        Koule(koleno, "Chodidlo", new Vector3(0f, 0.88f, 0f), Vector3.one * 0.16f, mat);
    }

    public void Vlnit(float t)
    {
        for (int i = 0; i < kyce.Count; i++)
        {
            if (kyce[i] == null) continue;
            float a = t * 20f + i * 0.9f;
            kyce[i].localRotation = kyceRest[i] * Quaternion.Euler(Mathf.Sin(a) * 14f, 0f, Mathf.Sin(a * 0.8f + 1f) * 10f);
        }
    }

    static Transform Koule(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.identity;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        Object.Destroy(go.GetComponent<Collider>());
        return go.transform;
    }

    static Transform Valec(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.identity;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        Object.Destroy(go.GetComponent<Collider>());
        return go.transform;
    }

    // Klasický jumpscare: fotka přes celou obrazovku, zoom, flash, třes.
    public static IEnumerator HrajKlasik(Canvas canvas, Texture2D foto, float trvani = 1.05f)
    {
        if (canvas == null || foto == null) yield break;
        if (SoundFX.I != null) SoundFX.I.Scream();
        if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.55f;

        var root = new GameObject("PavoukKlasikUI");
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(root.transform, false);
        var flash = flashGo.AddComponent<Image>();
        flash.color = new Color(0.7f, 0.02f, 0.02f, 0.95f);
        var frt = flash.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        var fotoGo = new GameObject("Foto");
        fotoGo.transform.SetParent(root.transform, false);
        var raw = fotoGo.AddComponent<RawImage>();
        raw.texture = foto;
        raw.color = Color.white;
        var prt = raw.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        // Pivot výš, ať zoom míří na oči, ne na čelisti.
        prt.pivot = new Vector2(0.5f, 0.62f);
        var canvRt = canvas.GetComponent<RectTransform>();
        float h = (canvRt != null && canvRt.rect.height > 1f) ? canvRt.rect.height : 1080f;
        float aspect = (float)foto.width / Mathf.Max(1, foto.height);
        prt.sizeDelta = new Vector2(h * aspect, h);
        prt.localScale = Vector3.one;

        float t = 0f;
        while (t < trvani)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / trvani);
            if (k < 0.08f)
            {
                float u = k / 0.08f;
                flash.color = Color.Lerp(new Color(0.75f, 0.02f, 0.02f, 0.95f), new Color(0f, 0f, 0f, 0.55f), u);
            }
            else if (k < 0.7f)
            {
                flash.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.55f, 0.15f, (k - 0.08f) / 0.62f));
            }
            else
            {
                float u = (k - 0.7f) / 0.3f;
                flash.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.15f, 0f, u));
                var c = raw.color;
                c.a = 1f - u;
                raw.color = c;
            }

            float scale;
            if (k < 0.22f)
            {
                float e = 1f - Mathf.Pow(1f - k / 0.22f, 3f);
                scale = Mathf.Lerp(1.02f, 1.48f, e);
            }
            else if (k < 0.7f)
            {
                scale = 1.48f + Mathf.Sin(t * 28f) * 0.04f;
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.35f;
            }
            else
            {
                scale = Mathf.Lerp(1.48f, 1.72f, (k - 0.7f) / 0.3f);
            }
            prt.localScale = Vector3.one * scale;
            prt.anchoredPosition = new Vector2(Mathf.Sin(t * 40f) * 18f, Mathf.Cos(t * 32f) * 12f);
            yield return null;
        }
        Object.Destroy(root);
    }



    // Animovaný jumpscare: tma, prudký skok z dálky, živé snímky, glitch, šum, třes.
    public static IEnumerator HrajAnimovany(Canvas canvas, Texture2D[] snimky, float trvani = 1.25f)
    {
        if (canvas == null || snimky == null || snimky.Length == 0) yield break;
        var foto = snimky[0];
        if (foto == null) yield break;

        var root = new GameObject("PavoukAnimUI");
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var flashGo = new GameObject("Flash");
        flashGo.transform.SetParent(root.transform, false);
        var flash = flashGo.AddComponent<Image>();
        flash.color = new Color(0f, 0f, 0f, 1f);
        var frt = flash.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        var fotoRootGo = new GameObject("FotoRoot");
        fotoRootGo.transform.SetParent(root.transform, false);
        var fotoRoot = fotoRootGo.AddComponent<RectTransform>();
        fotoRoot.anchorMin = new Vector2(0.5f, 0.5f);
        fotoRoot.anchorMax = new Vector2(0.5f, 0.5f);
        fotoRoot.pivot = new Vector2(0.5f, 0.62f);
        var canvRt = canvas.GetComponent<RectTransform>();
        float h = (canvRt != null && canvRt.rect.height > 1f) ? canvRt.rect.height : 1080f;
        float aspect = (float)foto.width / Mathf.Max(1, foto.height);
        fotoRoot.sizeDelta = new Vector2(h * aspect, h);
        fotoRoot.localScale = Vector3.one * 0.08f;

        RawImage AddFoto(string name, Color tint, Vector2 offset, int sib)
        {
            var go = new GameObject(name);
            go.transform.SetParent(fotoRoot, false);
            var raw = go.AddComponent<RawImage>();
            raw.texture = foto;
            raw.color = tint;
            raw.raycastTarget = false;
            var rt = raw.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = offset;
            go.transform.SetSiblingIndex(sib);
            return raw;
        }

        var after = AddFoto("After", new Color(1f, 1f, 1f, 0f), Vector2.zero, 0);
        after.rectTransform.localScale = Vector3.one * 1.08f;
        var main = AddFoto("Foto", Color.white, Vector2.zero, 1);
        var chromaR = AddFoto("ChromaR", new Color(1f, 0.05f, 0.05f, 0f), new Vector2(-10f, 0f), 2);
        var chromaB = AddFoto("ChromaB", new Color(0.05f, 0.7f, 1f, 0f), new Vector2(10f, 0f), 3);

        var noiseTex = new Texture2D(192, 192, TextureFormat.RGBA32, false);
        noiseTex.wrapMode = TextureWrapMode.Repeat;
        noiseTex.filterMode = FilterMode.Point;
        var pix = new Color32[192 * 192];
        var rng = new System.Random(77);

        var noiseGo = new GameObject("Sum");
        noiseGo.transform.SetParent(root.transform, false);
        var noise = noiseGo.AddComponent<RawImage>();
        noise.texture = noiseTex;
        noise.raycastTarget = false;
        noise.color = new Color(1f, 1f, 1f, 0f);
        var nrt = noise.rectTransform;
        nrt.anchorMin = Vector2.zero;
        nrt.anchorMax = Vector2.one;
        nrt.offsetMin = Vector2.zero;
        nrt.offsetMax = Vector2.zero;

        bool screamPlayed = false;
        float t = 0f;
        while (t < trvani)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / trvani);

            if (!screamPlayed && t >= 0.10f)
            {
                screamPlayed = true;
                if (SoundFX.I != null) SoundFX.I.ScreamSilny();
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.85f;
            }

            // Snímky – po výpadu živější přepínání
            int fi;
            if (t < 0.10f) fi = 0;
            else if (t < 0.28f) fi = Mathf.Min(snimky.Length - 1, 1 + Mathf.FloorToInt((t - 0.10f) * 18f) % Mathf.Max(1, snimky.Length - 1));
            else fi = Mathf.FloorToInt(t * 16f) % snimky.Length;
            var cur = snimky[Mathf.Clamp(fi, 0, snimky.Length - 1)];
            if (cur != null)
            {
                main.texture = cur;
                after.texture = cur;
                chromaR.texture = cur;
                chromaB.texture = cur;
            }

            float scale;
            if (t < 0.10f)
            {
                scale = 0.08f;
                flash.color = new Color(0f, 0f, 0f, 1f);
                main.color = new Color(1f, 1f, 1f, 0f);
            }
            else if (t < 0.28f)
            {
                float u = (t - 0.10f) / 0.18f;
                // Drží se malý, pak prudce vyplní obrazovku (overshoot).
                float snap = u < 0.42f
                    ? Mathf.Lerp(0f, 0.18f, u / 0.42f)
                    : 0.18f + (1f - Mathf.Pow(1f - (u - 0.42f) / 0.58f, 3f)) * 0.82f;
                scale = Mathf.Lerp(0.10f, 1.62f, snap);
                if (u < 0.12f)
                    flash.color = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), new Color(0.8f, 0.02f, 0.02f, 0.9f), u / 0.12f);
                else
                    flash.color = Color.Lerp(new Color(0.8f, 0.02f, 0.02f, 0.9f), new Color(0f, 0f, 0f, 0.35f), (u - 0.12f) / 0.88f);
                main.color = Color.white;
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.85f;
            }
            else if (t < 0.72f)
            {
                float u = (t - 0.28f) / 0.44f;
                // Škubání v obličeji, občas trhnutí blíž.
                float jerk = (Mathf.FloorToInt(t * 11f) % 5 == 0) ? 0.16f : 0f;
                scale = 1.50f + Mathf.Sin(t * 33f) * 0.05f + jerk;
                flash.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.35f, 0.12f, u));
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.55f + jerk * 0.8f;
            }
            else if (t < 1.00f)
            {
                float u = (t - 0.72f) / 0.28f;
                scale = Mathf.Lerp(1.50f, 1.92f, 1f - Mathf.Pow(1f - u, 2f));
                flash.color = Color.Lerp(new Color(0.55f, 0.0f, 0.0f, 0.45f), new Color(0f, 0f, 0f, 0.2f), u);
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.7f;
            }
            else
            {
                float u = (t - 1.00f) / Mathf.Max(0.01f, trvani - 1.00f);
                scale = Mathf.Lerp(1.92f, 2.15f, u);
                float fade = 1f - u;
                main.color = new Color(1f, 1f, 1f, fade);
                flash.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.2f, 0f, u));
            }

            // Chromatic aberration a afterimage po výpadu
            float glitch = t >= 0.10f && t < 1.00f ? (t < 0.28f ? 0.55f : 0.35f) : 0f;
            if (t >= 1.00f) glitch = main.color.a * 0.2f;
            chromaR.color = new Color(1f, 0.05f, 0.05f, glitch * 0.55f);
            chromaB.color = new Color(0.05f, 0.7f, 1f, glitch * 0.5f);
            float split = 8f + Mathf.Sin(t * 40f) * 10f + ((rng.Next(0, 7) == 0) ? 16f : 0f);
            chromaR.rectTransform.anchoredPosition = new Vector2(-split, Mathf.Sin(t * 21f) * 4f);
            chromaB.rectTransform.anchoredPosition = new Vector2(split, Mathf.Cos(t * 19f) * 4f);
            after.color = new Color(1f, 1f, 1f, glitch * 0.28f);
            after.rectTransform.localScale = Vector3.one * (1.06f + Mathf.Sin(t * 25f) * 0.04f);

            fotoRoot.localScale = Vector3.one * scale;
            float jx = t >= 0.10f ? Mathf.Sin(t * 47f) * 22f + ((rng.Next(0, 6) == 0) ? rng.Next(-28, 29) : 0) : 0f;
            float jy = t >= 0.10f ? Mathf.Cos(t * 39f) * 16f + ((rng.Next(0, 6) == 0) ? rng.Next(-20, 21) : 0) : 0f;
            fotoRoot.anchoredPosition = new Vector2(jx, jy);
            fotoRoot.localRotation = Quaternion.Euler(0f, 0f, t >= 0.10f ? Mathf.Sin(t * 29f) * 3.5f : 0f);

            // Statický šum / glitch
            if (t >= 0.10f)
            {
                for (int i = 0; i < pix.Length; i++)
                {
                    byte v = (byte)rng.Next(0, 256);
                    pix[i] = new Color32(v, v, v, 255);
                }
                noiseTex.SetPixels32(pix);
                noiseTex.Apply(false);
                float na = t < 1.00f ? (0.10f + (rng.Next(0, 5) == 0 ? 0.22f : 0f)) : main.color.a * 0.08f;
                noise.color = new Color(1f, 1f, 1f, na);
                noise.uvRect = new Rect((float)rng.NextDouble(), (float)rng.NextDouble(), 2.4f, 2.4f);
            }

            yield return null;
        }

        Object.Destroy(noiseTex);
        Object.Destroy(root);
    }


    // Videojumpscare: vystřižený pavouk z klipu přes ztmavenou hru, třes a vlastní zvuk.
    public static IEnumerator HrajVideo(Canvas canvas, Texture2D[] snimky, AudioClip zvuk, float fps = 20f)
    {
        if (canvas == null || snimky == null || snimky.Length == 0) yield break;
        if (zvuk != null && SoundFX.I != null) SoundFX.I.PlayClip(zvuk, 1f);
        else if (SoundFX.I != null) SoundFX.I.ScreamSilny();

        var root = new GameObject("PavoukVideoUI");
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(root.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.01f, 0.02f, 0.82f);
        var drt = dim.rectTransform;
        drt.anchorMin = Vector2.zero;
        drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero;
        drt.offsetMax = Vector2.zero;

        var fotoGo = new GameObject("Video");
        fotoGo.transform.SetParent(root.transform, false);
        var raw = fotoGo.AddComponent<RawImage>();
        raw.texture = snimky[0];
        raw.color = Color.white;
        var prt = raw.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.32f, 0.38f);
        var canvRt = canvas.GetComponent<RectTransform>();
        float h = (canvRt != null && canvRt.rect.height > 1f) ? canvRt.rect.height : 1080f;
        float aspect = (float)snimky[0].width / Mathf.Max(1, snimky[0].height);
        prt.sizeDelta = new Vector2(h * aspect, h);
        prt.localScale = Vector3.one * 1.7f;

        int n = snimky.Length;
        // Poslední snímek je skoro černý motion blur — držíme předposlední (skok do tváře).
        int peak = Mathf.Max(0, n - 2);
        float frameDur = 1f / Mathf.Max(1f, fps);
        float videoDur = (peak + 1) * frameDur;
        float hold = 0.22f;
        float fade = 0.18f;
        float total = videoDur + hold + fade;
        int lastIdx = -1;
        float t = 0f;
        bool leapShake = false;
        VideoFrameIndex = 0;
        while (t < total)
        {
            t += Time.unscaledDeltaTime;
            int idx = Mathf.Clamp((int)(t / frameDur), 0, peak);
            if (idx != lastIdx)
            {
                raw.texture = snimky[idx];
                lastIdx = idx;
                VideoFrameIndex = idx;
            }

            // Posledních ~18 % je skok do kamery.
            float k = Mathf.Clamp01(t / videoDur);
            float scale, px, py;
            if (k < 0.82f)
            {
                float u = k / 0.82f;
                scale = Mathf.Lerp(1.75f, 1.25f, u);
                px = Mathf.Lerp(0.32f, 0.42f, u);
                py = Mathf.Lerp(0.38f, 0.45f, u);
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.28f;
            }
            else
            {
                float u = (k - 0.82f) / 0.18f;
                if (t > videoDur) u = 1f;
                scale = Mathf.Lerp(1.25f, 1.08f, Mathf.Clamp01(u));
                px = Mathf.Lerp(0.42f, 0.50f, Mathf.Clamp01(u));
                py = Mathf.Lerp(0.45f, 0.50f, Mathf.Clamp01(u));
                if (CameraOrbit.I != null) CameraOrbit.I.shake = 0.85f;
                leapShake = true;
            }
            prt.pivot = new Vector2(px, py);
            prt.localScale = Vector3.one * scale;
            float jx = Mathf.Sin(t * 51f) * (leapShake ? 18f : 6f);
            float jy = Mathf.Cos(t * 43f) * (leapShake ? 12f : 5f);
            prt.anchoredPosition = new Vector2(jx, jy);

            if (t > videoDur + hold)
            {
                float f = Mathf.Clamp01((t - videoDur - hold) / fade);
                raw.color = new Color(1f, 1f, 1f, 1f - f);
                dim.color = new Color(0.02f, 0.01f, 0.02f, 0.82f * (1f - f));
            }
            yield return null;
        }

        if (CameraOrbit.I != null) CameraOrbit.I.shake = 0f;
        VideoFrameIndex = -1;
        Object.Destroy(root);
    }


}
