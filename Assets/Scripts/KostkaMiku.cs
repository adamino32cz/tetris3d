using System.Collections.Generic;
using UnityEngine;

// Jedna pózovaná Tda Miku na figurku (tvar kusu). Po dopadu zůstane figurka na desce,
// dokud se nesmaže aspoň jedna její buňka.
public class KostkaMiku : MonoBehaviour
{
    static GameObject prefab;
    static bool loadTried;

    readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
    readonly Dictionary<string, Quaternion> rest = new Dictionary<string, Quaternion>();
    Renderer[] rends;
    Material[] ghostMats;
    bool ghost;
    string poseId;
    bool ready;
    Quaternion pieceOrient = Quaternion.identity;

    public static KostkaMiku Attach(Transform parent)
    {
        var go = new GameObject("MikuKus");
        go.transform.SetParent(parent, false);
        var m = go.AddComponent<KostkaMiku>();
        m.Build();
        return m;
    }

    public bool Ready => ready;

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowPose(string shapeId, IList<Vector3> cellCenters, bool asGhost, bool localSpace, Quaternion orient)
    {
        pieceOrient = orient;
        if (!ready)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);
        if (poseId != shapeId)
        {
            ResetPose();
            ApplyPose(shapeId);
            poseId = shapeId;
        }
        SetGhost(asGhost);
        Fit(cellCenters, localSpace);
    }

    void Build()
    {
        if (!loadTried)
        {
            loadTried = true;
            prefab = Resources.Load<GameObject>("TdaMiku/TdaMiku");
            if (prefab == null) prefab = Resources.Load<GameObject>("TdaMiku");
        }
        if (prefab == null)
        {
            Debug.LogWarning("[Kostka] TdaMiku se nenačetla.");
            return;
        }
        var inst = Object.Instantiate(prefab, transform);
        inst.name = "TdaMiku";
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        var anim = inst.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;
        CacheBones(inst.transform);
        rends = inst.GetComponentsInChildren<Renderer>();
        ConvertToUrp();
        rends = inst.GetComponentsInChildren<Renderer>();
        ready = bones.Count > 0 || (rends != null && rends.Length > 0);
    }

    static Texture2D LoadTex(string file)
    {
        var t = Resources.Load<Texture2D>("TdaMiku/Textures/" + file);
        if (t == null) t = Resources.Load<Texture2D>("TdaMiku/Textures/" + file + ".tga");
        return t;
    }

    // Importovaný MMD/FBX Phong/Standard v URP (hlavně WebGL) vychází bílý/šedý.
    // Převedeme na URP Unlit + base map z Resources, ať jsou barvy ve buildu jisté.
    void ConvertToUrp()
    {
        var body = LoadTex("mikuv4x_body00");
        var face = LoadTex("face_Mikuv4x");
        var hair = LoadTex("hair_Mikuv4x");
        var ap = LoadTex("body02_MikuAp");
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            if (r == null) continue;
            var srcs = r.sharedMaterials;
            if (srcs == null || srcs.Length == 0) continue;
            var dsts = new Material[srcs.Length];
            for (int m = 0; m < srcs.Length; m++)
            {
                var src = srcs[m];
                Texture tex = null;
                if (src != null)
                {
                    if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                    if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                }
                string nm = ((src != null ? src.name : "") + " " + r.gameObject.name).ToLowerInvariant();
                int idx = -1;
                if (src != null)
                {
                    string n = src.name;
                    // Unity často pojmenuje sloty "0"…"17" nebo "Material #12"
                    for (int k = n.Length - 1, acc = 0, p = 1; k >= 0; k--)
                    {
                        char c = n[k];
                        if (c >= '0' && c <= '9') { acc += (c - '0') * p; p *= 10; idx = acc; }
                        else if (idx >= 0) break;
                    }
                }
                bool isHair = idx == 11 || idx == 12 || nm.Contains("hair") || nm.Contains("kami") || nm.Contains("髪");
                bool isFace = (idx >= 7 && idx <= 10) || idx == 13 || idx == 14 || nm.Contains("face") || nm.Contains("eye") || nm.Contains("head") || nm.Contains("顔") || nm.Contains("目");
                bool isAp = idx == 16 || idx == 17 || nm.Contains("ap") || nm.Contains("acc");
                if (tex == null)
                {
                    if (isHair) tex = hair;
                    else if (isFace) tex = face;
                    else if (isAp) tex = ap;
                    else tex = body;
                }
                bool cutout = isHair || (tex != null && (tex.name.ToLowerInvariant().Contains("hair") || tex.name.ToLowerInvariant().Contains("ap")));
                dsts[m] = KostkaVisual.UnlitTextured(tex, Color.white, cutout);
            }
            r.sharedMaterials = dsts;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    void CacheBones(Transform t)
    {
        if (!bones.ContainsKey(t.name))
        {
            bones[t.name] = t;
            rest[t.name] = t.localRotation;
        }
        for (int i = 0; i < t.childCount; i++) CacheBones(t.GetChild(i));
    }

    void ResetPose()
    {
        foreach (var kv in rest)
            if (bones.TryGetValue(kv.Key, out var t) && t != null)
                t.localRotation = kv.Value;
    }

    // Relativní Euler vůči rest póze. MMD panty často točí kolem Z.
    void Bone(string name, float x, float y, float z)
    {
        if (!bones.TryGetValue(name, out var t) || t == null || !rest.ContainsKey(name)) return;
        t.localRotation = rest[name] * Quaternion.Euler(x, y, z);
    }

    void BoneD(string primary, string fallback, float x, float y, float z)
    {
        if (bones.ContainsKey(primary)) Bone(primary, x, y, z);
        else Bone(fallback, x, y, z);
    }

    void ApplyPose(string id)
    {
        // Výchozí Tda bind je typicky T-póza — ideální pro téčko.
        switch (id)
        {
            case "I":
                // Leží natáhlá (rotace kořene v Fit). Ruce podél těla.
                Bone("左腕", 0f, 0f, 70f);
                Bone("右腕", 0f, 0f, -70f);
                Bone("左ひじ", 0f, 0f, 10f);
                Bone("右ひじ", 0f, 0f, -10f);
                break;
            case "O":
                // Klubíčko / dřep
                Bone("上半身", 35f, 0f, 0f);
                Bone("上半身2", 25f, 0f, 0f);
                Bone("下半身", -15f, 0f, 0f);
                Bone("首", 15f, 0f, 0f);
                BoneD("左足D", "左足", -25f, 15f, 0f);
                BoneD("右足D", "右足", -25f, -15f, 0f);
                BoneD("左ひざD", "左ひざ", 0f, 0f, 125f);
                BoneD("右ひざD", "右ひざ", 0f, 0f, 125f);
                Bone("左腕", 0f, 20f, 50f);
                Bone("右腕", 0f, -20f, -50f);
                Bone("左ひじ", 0f, 0f, -85f);
                Bone("右ひじ", 0f, 0f, 85f);
                break;
            case "L":
                // Sed / kopnutí do strany (tvar L)
                Bone("上半身", 8f, 0f, 12f);
                BoneD("左足D", "左足", -10f, 5f, 0f);
                BoneD("右足D", "右足", -15f, -35f, 0f);
                BoneD("左ひざD", "左ひざ", 0f, 0f, 95f);
                BoneD("右ひざD", "右ひざ", 0f, 0f, 20f);
                BoneD("右足首D", "右足首", 0f, 0f, 15f);
                Bone("左腕", 0f, 0f, 20f);
                Bone("右腕", -10f, 0f, -25f);
                Bone("右ひじ", 0f, 0f, 15f);
                break;
            case "T":
                // T-póza (bind), jen drobné srovnání paží
                Bone("左腕", 0f, 0f, 0f);
                Bone("右腕", 0f, 0f, 0f);
                break;
            case "S":
                // Esíčko: prohnutý trup a protisměrné ruce
                Bone("上半身", 8f, 25f, 8f);
                Bone("上半身2", 0f, 18f, 0f);
                Bone("下半身", 0f, -20f, 0f);
                Bone("左腕", 15f, 30f, 15f);
                Bone("右腕", -15f, -30f, -15f);
                BoneD("左足D", "左足", 5f, 20f, 0f);
                BoneD("右足D", "右足", -5f, -20f, 0f);
                break;
            case "Roh":
                // Sed v rohu, kolena u sebe
                Bone("上半身", 20f, -10f, 0f);
                Bone("上半身2", 10f, 0f, 0f);
                BoneD("左足D", "左足", -20f, 10f, 0f);
                BoneD("右足D", "右足", -20f, -10f, 0f);
                BoneD("左ひざD", "左ひざ", 0f, 0f, 110f);
                BoneD("右ひざD", "右ひざ", 0f, 0f, 110f);
                Bone("左腕", 0f, 0f, 40f);
                Bone("右腕", 0f, 0f, -15f);
                Bone("左ひじ", 0f, 0f, -60f);
                break;
            case "Sroub":
                // Zkroucená (šroub)
                Bone("上半身", 5f, 40f, 0f);
                Bone("上半身2", 0f, 25f, 0f);
                Bone("下半身", 0f, -30f, 0f);
                Bone("左腕", -60f, 0f, 20f);
                Bone("右腕", 40f, 0f, -20f);
                Bone("左ひじ", 0f, 0f, -40f);
                Bone("右ひじ", 0f, 0f, 40f);
                BoneD("左足D", "左足", 0f, 15f, 0f);
                BoneD("右足D", "右足", 0f, -15f, 0f);
                break;
            case "Kriz":
                // Plus / hvězda: T-póza, nohy od sebe
                BoneD("左足D", "左足", 0f, 25f, 0f);
                BoneD("右足D", "右足", 0f, -25f, 0f);
                Bone("左腕", 0f, 0f, -5f);
                Bone("右腕", 0f, 0f, 5f);
                break;
            default:
                break;
        }
    }

    void SetGhost(bool asGhost)
    {
        ghost = asGhost;
        // Plný model jako duch vypadá jako druhá figurka — schováme ho.
        if (asGhost) gameObject.SetActive(false);
    }


    void Fit(IList<Vector3> centers, bool localSpace)
    {
        if (centers == null || centers.Count == 0) return;
        Vector3 min = centers[0], max = centers[0], mid = Vector3.zero;
        for (int i = 0; i < centers.Count; i++)
        {
            mid += centers[i];
            min = Vector3.Min(min, centers[i]);
            max = Vector3.Max(max, centers[i]);
        }
        mid /= centers.Count;
        Vector3 size = (max - min) + Vector3.one; // buněčný rozsah v jednotkách
        // modelové bounds ve rest+póze
        Bounds mb = new Bounds(transform.position, Vector3.zero);
        bool any = false;
        if (rends != null)
        {
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null || !rends[i].gameObject.activeInHierarchy) continue;
                if (!any) { mb = rends[i].bounds; any = true; }
                else mb.Encapsulate(rends[i].bounds);
            }
        }
        // dočasně identita pro změření ve světě — raději měříme lokální renderer bounds
        Vector3 modelSize = any ? mb.size : new Vector3(1f, 1.6f, 1f);
        if (modelSize.x < 0.01f) modelSize = new Vector3(1f, 1.6f, 1f);

        // Základní póza: I je v kanonickém tvaru vodorovná podél X (Miku se položí).
        // kusOrient z rotací kusu (Q/E, Shift+WASD) se násobí na tuto bázi, ať figurka
        // sleduje každou otočku, nejen změnu AABB.
        Quaternion baseRot = poseId == "I" ? Quaternion.Euler(0f, 0f, -90f) : Quaternion.identity;
        Quaternion rot = localSpace ? Quaternion.identity : (pieceOrient * baseRot);
        float targetX = size.x, targetY = size.y, targetZ = size.z;
        if (poseId == "I")
        {
            if (size.y >= size.x && size.y >= size.z)
            {
                targetY = size.y;
                targetX = Mathf.Max(0.9f, size.x);
            }
            else if (size.x >= size.z)
            {
                targetX = size.x;
                targetY = 1.1f;
            }
            else
            {
                targetZ = size.z;
                targetY = 1.1f;
            }
        }
        else if (poseId == "O")
        {
            targetX = size.x; targetZ = size.z; targetY = Mathf.Max(size.y, 1.2f);
        }
        else
        {
            targetX = Mathf.Max(size.x, size.z);
            targetZ = Mathf.Min(size.x, size.z);
            targetY = Mathf.Max(size.y, 2.2f);
        }
        if (localSpace)
        {
            targetX = 1.6f; targetY = 1.8f; targetZ = 1.2f;
        }

        // Shodit aktuální scale/rot na identitu, spočítat bounds znovu přes mesh?
        // Použijeme poměr cíle a aktuálních world bounds po nastavení rotace na 1.
        transform.localScale = Vector3.one;
        if (localSpace) transform.localRotation = rot;
        else transform.rotation = rot;

        // znovu bounds
        any = false;
        if (rends != null)
        {
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                if (!any) { mb = rends[i].bounds; any = true; }
                else mb.Encapsulate(rends[i].bounds);
            }
        }
        modelSize = any ? mb.size : modelSize;
        float sx = targetX / Mathf.Max(0.05f, modelSize.x);
        float sy = targetY / Mathf.Max(0.05f, modelSize.y);
        float sz = targetZ / Mathf.Max(0.05f, modelSize.z);
        // téměř uniformní, ať se Miku nedeformuje moc; overhang je v pohodě
        float s = Mathf.Clamp((sx + sy + sz) / 3f, 0.15f, 4.5f);
        // pro I vodorovně protáhnout trochu víc
        if (poseId == "I") s = Mathf.Clamp(Mathf.Max(sx, sy, sz) * 0.95f, 0.2f, 5f);
        if (localSpace) s = Mathf.Clamp(s * 2.2f, 0.55f, 6f);
        transform.localScale = Vector3.one * s;

        if (localSpace) transform.localPosition = mid;
        else transform.position = mid;

        // po změně scale posunout, ať střed modelu sedí na středu buněk
        // (renderer.bounds je ve světě — u náhledu proto cílíme parent.TransformPoint(mid))
        any = false;
        if (rends != null)
        {
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                if (!any) { mb = rends[i].bounds; any = true; }
                else mb.Encapsulate(rends[i].bounds);
            }
        }
        if (any)
        {
            Vector3 target = mid;
            if (localSpace && transform.parent != null)
                target = transform.parent.TransformPoint(mid);
            Vector3 delta = target - mb.center;
            if (localSpace) transform.localPosition += transform.InverseTransformVector(delta);
            else transform.position += delta;
        }
    }
}
