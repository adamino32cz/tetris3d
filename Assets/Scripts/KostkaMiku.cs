using System.Collections.Generic;
using UnityEngine;

// Jedna pózovaná Tda Miku na figurku (tvar kusu). Položené buňky zůstávají kostkami.
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

    public void ShowPose(string shapeId, IList<Vector3> cellCenters, bool asGhost, bool localSpace)
    {
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
        ready = bones.Count > 0 || (rends != null && rends.Length > 0);
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

        Quaternion rot = Quaternion.identity;
        float targetX = size.x, targetY = size.y, targetZ = size.z;
        // I: když je vodorovná, polož Miku podél nejdelší osy
        if (poseId == "I")
        {
            if (size.y >= size.x && size.y >= size.z)
            {
                rot = Quaternion.identity; // stojí v šachtě
                targetY = size.y;
                targetX = Mathf.Max(0.9f, size.x);
            }
            else if (size.x >= size.z)
            {
                rot = Quaternion.Euler(0f, 0f, -90f); // leží podél X
                targetX = size.x;
                targetY = 1.1f;
            }
            else
            {
                rot = Quaternion.Euler(90f, 0f, 0f); // leží podél Z
                targetZ = size.z;
                targetY = 1.1f;
            }
        }
        else if (poseId == "O")
        {
            rot = Quaternion.identity;
            // klubíčko vyplní 2×2, výška může přečnívat
            targetX = size.x; targetZ = size.z; targetY = Mathf.Max(size.y, 1.2f);
        }
        else
        {
            rot = Quaternion.identity;
            if (size.z > size.x + 0.4f)
                rot = Quaternion.Euler(0f, 90f, 0f);
            targetX = Mathf.Max(size.x, size.z);
            targetZ = Mathf.Min(size.x, size.z);
            targetY = Mathf.Max(size.y, 2.2f);
        }
        if (localSpace)
        {
            rot = Quaternion.identity;
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
