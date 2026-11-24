using System.Collections.Generic;
using UnityEngine;

// Původní zjednodušená figurka inspirovaná Astolfem: dlouhé růžové vlasy, jeden cop,
// velká černá mašle, fialová mikina, černá sukně, bílé podkolenky.
// Každý tvar kusu = jedna pózovaná figurka (troll skin, smí přečnívat).
public class KostkaAstolfo : MonoBehaviour
{
    readonly Dictionary<string, Transform> parts = new Dictionary<string, Transform>();
    readonly Dictionary<string, Quaternion> rest = new Dictionary<string, Quaternion>();
    Renderer[] rends;
    Material[] ghostMats;
    bool ghost, ready;
    string poseId;
    static Texture2D faceTex, hairTex, clothTex, blackTex, skinTex;

    public static KostkaAstolfo Attach(Transform parent)
    {
        var go = new GameObject("AstolfoKus");
        go.transform.SetParent(parent, false);
        var a = go.AddComponent<KostkaAstolfo>();
        a.Build();
        return a;
    }

    public bool Ready => ready;

    public void Hide() { gameObject.SetActive(false); }

    public void ShowPose(string shapeId, IList<Vector3> cellCenters, bool asGhost, bool localSpace)
    {
        if (!ready) { gameObject.SetActive(false); return; }
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
        EnsureTex();
        var root = new GameObject("Root").transform;
        root.SetParent(transform, false);

        // fialová mikina + černá sukně
        var purple = new Color(0.48f, 0.32f, 0.72f);
        var pink = new Color(1f, 0.62f, 0.80f);
        var skin = new Color(1f, 0.86f, 0.80f);
        Part(root, "Hip", PrimitiveType.Sphere, new Vector3(0f, 0.70f, 0f), new Vector3(0.46f, 0.26f, 0.36f), blackTex, new Color(0.08f, 0.07f, 0.09f));
        // skládaná sukně (širší disk)
        Part(root, "Skirt", PrimitiveType.Sphere, new Vector3(0f, 0.62f, 0f), new Vector3(0.72f, 0.22f, 0.62f), blackTex, new Color(0.07f, 0.06f, 0.08f));
        Part(root, "Spine", PrimitiveType.Capsule, new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.30f, 0.32f), clothTex, purple);
        Part(root, "Chest", PrimitiveType.Sphere, new Vector3(0f, 1.46f, 0f), new Vector3(0.56f, 0.38f, 0.36f), clothTex, purple);
        // kapuce
        Part(root, "Hood", PrimitiveType.Sphere, new Vector3(0f, 1.68f, -0.12f), new Vector3(0.55f, 0.28f, 0.40f), clothTex, new Color(0.42f, 0.28f, 0.66f));

        var head = Part(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0f), Vector3.one * 0.42f, skinTex, skin);
        var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
        face.name = "Face";
        face.transform.SetParent(head, false);
        face.transform.localPosition = new Vector3(0f, 0.02f, 0.47f);
        face.transform.localScale = new Vector3(0.98f, 1.08f, 1f);
        Object.Destroy(face.GetComponent<Collider>());
        var fmr = face.GetComponent<MeshRenderer>();
        fmr.sharedMaterial = Mat(faceTex, Color.white);
        fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // dlouhé růžové vlasy + jeden cop (ne twintaily)
        Part(head, "HairCap", PrimitiveType.Sphere, new Vector3(0f, 0.14f, -0.04f), new Vector3(1.28f, 0.95f, 1.28f), hairTex, pink);
        Part(head, "HairBack", PrimitiveType.Sphere, new Vector3(0f, -0.05f, -0.35f), new Vector3(1.10f, 1.00f, 0.70f), hairTex, new Color(0.98f, 0.55f, 0.75f));
        Part(head, "Bangs", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0.28f), new Vector3(0.95f, 0.35f, 0.35f), hairTex, pink);
        // jeden dlouhý cop přes levé rameno
        var braid = Part(head, "Braid", PrimitiveType.Capsule, new Vector3(-0.28f, -0.85f, 0.12f), new Vector3(0.28f, 0.85f, 0.28f), hairTex, new Color(1f, 0.58f, 0.78f));
        braid.localRotation = Quaternion.Euler(18f, 0f, 18f);
        Part(braid, "BraidTip", PrimitiveType.Sphere, new Vector3(0f, -1.05f, 0f), Vector3.one * 0.55f, hairTex, new Color(1f, 0.55f, 0.76f));
        // velká černá mašle nahoře
        Part(head, "BowL", PrimitiveType.Sphere, new Vector3(-0.28f, 0.52f, 0.08f), new Vector3(0.42f, 0.28f, 0.14f), blackTex, Color.black);
        Part(head, "BowR", PrimitiveType.Sphere, new Vector3(0.28f, 0.52f, 0.08f), new Vector3(0.42f, 0.28f, 0.14f), blackTex, Color.black);
        Part(head, "BowC", PrimitiveType.Sphere, new Vector3(0f, 0.50f, 0.16f), new Vector3(0.16f, 0.16f, 0.16f), blackTex, Color.black);

        // rukávy mikiny + ruce
        var larm = Part(root, "LArm", PrimitiveType.Capsule, new Vector3(-0.58f, 1.44f, 0f), new Vector3(0.18f, 0.32f, 0.18f), clothTex, purple);
        larm.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Part(larm, "LHand", PrimitiveType.Sphere, new Vector3(0f, -0.95f, 0f), Vector3.one * 0.55f, skinTex, skin);
        var rarm = Part(root, "RArm", PrimitiveType.Capsule, new Vector3(0.58f, 1.44f, 0f), new Vector3(0.18f, 0.32f, 0.18f), clothTex, purple);
        rarm.localRotation = Quaternion.Euler(0f, 0f, -90f);
        Part(rarm, "RHand", PrimitiveType.Sphere, new Vector3(0f, -0.95f, 0f), Vector3.one * 0.55f, skinTex, skin);

        // bílé podkolenky + černé boty (stehna pod sukní)
        var lleg = Part(root, "LLeg", PrimitiveType.Capsule, new Vector3(-0.16f, 0.36f, 0f), new Vector3(0.17f, 0.28f, 0.17f), clothTex, Color.white);
        Part(lleg, "LFoot", PrimitiveType.Sphere, new Vector3(0f, -1.0f, 0.10f), new Vector3(0.75f, 0.38f, 1.05f), blackTex, Color.black);
        var rleg = Part(root, "RLeg", PrimitiveType.Capsule, new Vector3(0.16f, 0.36f, 0f), new Vector3(0.17f, 0.28f, 0.17f), clothTex, Color.white);
        Part(rleg, "RFoot", PrimitiveType.Sphere, new Vector3(0f, -1.0f, 0.10f), new Vector3(0.75f, 0.38f, 1.05f), blackTex, Color.black);

        Cache(root);
        rends = GetComponentsInChildren<Renderer>();
        ready = true;
        poseId = "";
        ApplyPose("T");
        poseId = "T";
    }

    Transform Part(Transform parent, string name, PrimitiveType typ, Vector3 pos, Vector3 scale, Texture2D tex, Color tint)
    {
        var go = GameObject.CreatePrimitive(typ);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Object.Destroy(go.GetComponent<Collider>());
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = Mat(tex, tint);
        return go.transform;
    }

    static Material Mat(Texture2D tex, Color tint)
    {
        // Unlit, ať barvy na WebGL nevyblednou.
        return KostkaVisual.UnlitTextured(tex, tint, false);
    }

    static void EnsureTex()
    {
        if (faceTex == null) faceTex = Resources.Load<Texture2D>("SkinFaces/AstolfoFace");
        if (hairTex == null) hairTex = Solid(new Color(1f, 0.62f, 0.80f));
        if (clothTex == null) clothTex = Solid(new Color(0.48f, 0.32f, 0.72f));
        if (blackTex == null) blackTex = Solid(new Color(0.08f, 0.06f, 0.08f));
        if (skinTex == null) skinTex = Solid(new Color(1f, 0.88f, 0.82f));
    }

    static Texture2D Solid(Color c)
    {
        var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) t.SetPixel(x, y, c);
        t.Apply(false, true);
        return t;
    }

    void Cache(Transform t)
    {
        if (!parts.ContainsKey(t.name))
        {
            parts[t.name] = t;
            rest[t.name] = t.localRotation;
        }
        for (int i = 0; i < t.childCount; i++) Cache(t.GetChild(i));
    }

    void ResetPose()
    {
        foreach (var kv in rest)
            if (parts.TryGetValue(kv.Key, out var t) && t != null)
                t.localRotation = kv.Value;
    }

    void Bone(string name, float x, float y, float z)
    {
        if (!parts.TryGetValue(name, out var t) || t == null || !rest.ContainsKey(name)) return;
        t.localRotation = rest[name] * Quaternion.Euler(x, y, z);
    }

    void ApplyPose(string id)
    {
        switch (id)
        {
            case "I":
                Bone("LArm", 0f, 0f, 70f);
                Bone("RArm", 0f, 0f, -70f);
                break;
            case "O":
                Bone("Spine", 35f, 0f, 0f);
                Bone("Chest", 20f, 0f, 0f);
                Bone("Head", 15f, 0f, 0f);
                Bone("LLeg", 15f, 0f, 20f);
                Bone("RLeg", 15f, 0f, -20f);
                Bone("LArm", 20f, 0f, 40f);
                Bone("RArm", 20f, 0f, -40f);
                break;
            case "L":
                Bone("LLeg", 80f, 0f, 10f);
                Bone("RLeg", 10f, 0f, -25f);
                Bone("Spine", 8f, 0f, 8f);
                Bone("RArm", -20f, 0f, -15f);
                break;
            case "S":
                Bone("Spine", 8f, 25f, 0f);
                Bone("Chest", 0f, 15f, 0f);
                Bone("LArm", 15f, 20f, 10f);
                Bone("RArm", -15f, -20f, -10f);
                Bone("LLeg", 10f, 15f, 0f);
                Bone("RLeg", -5f, -15f, 0f);
                break;
            case "Roh":
                Bone("LLeg", 95f, 0f, 8f);
                Bone("RLeg", 95f, 0f, -8f);
                Bone("Spine", 18f, -8f, 0f);
                Bone("LArm", 0f, 0f, 35f);
                break;
            case "Sroub":
                Bone("Spine", 5f, 40f, 0f);
                Bone("Chest", 0f, 20f, 0f);
                Bone("LArm", -50f, 0f, 10f);
                Bone("RArm", 40f, 0f, -10f);
                break;
            case "Kriz":
                Bone("LLeg", 0f, 0f, 20f);
                Bone("RLeg", 0f, 0f, -20f);
                break;
            default: // T — ruce do stran už jsou v T-póze z Build
                break;
        }
    }

    void SetGhost(bool asGhost)
    {
        // U figurky ducha jen ztlumíme; když materiály nepodporují alfa, schováme ho.
        if (rends == null) return;
        if (asGhost)
        {
            gameObject.SetActive(true);
            // schovat ducha figurky, ať není druhá plná postava (troll může přečnívat)
            gameObject.SetActive(false);
        }
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
        Vector3 size = (max - min) + Vector3.one;

        Quaternion rot = Quaternion.identity;
        float targetY = Mathf.Max(size.y, localSpace ? 1.6f : 2.2f);
        float targetX = Mathf.Max(size.x, size.z);
        if (poseId == "I")
        {
            if (size.y >= size.x && size.y >= size.z) { rot = Quaternion.identity; targetY = size.y; }
            else if (size.x >= size.z) { rot = Quaternion.Euler(0f, 0f, -90f); targetX = size.x; targetY = 1.1f; }
            else { rot = Quaternion.Euler(90f, 0f, 0f); targetX = size.z; targetY = 1.1f; }
        }
        else if (size.z > size.x + 0.4f) rot = Quaternion.Euler(0f, 90f, 0f);
        if (localSpace) { rot = Quaternion.identity; targetY = 1.7f; targetX = 1.5f; }

        transform.localScale = Vector3.one;
        if (localSpace) transform.localRotation = rot;
        else transform.rotation = rot;

        Bounds mb = new Bounds(transform.position, Vector3.one);
        bool any = Encapsulate(ref mb);
        Vector3 ms = any ? mb.size : new Vector3(1f, 2f, 0.6f);
        float s = Mathf.Clamp(Mathf.Max(targetX / Mathf.Max(0.05f, ms.x), targetY / Mathf.Max(0.05f, ms.y)) * (localSpace ? 0.7f : 0.9f), 0.2f, 4.5f);
        if (localSpace) s = Mathf.Clamp(s * 2.2f, 0.55f, 6f);
        transform.localScale = Vector3.one * s;
        if (localSpace) transform.localPosition = mid;
        else transform.position = mid;
        if (Encapsulate(ref mb))
        {
            Vector3 target = mid;
            if (localSpace && transform.parent != null)
                target = transform.parent.TransformPoint(mid);
            Vector3 delta = target - mb.center;
            if (localSpace) transform.localPosition += transform.InverseTransformVector(delta);
            else transform.position += delta;
        }
    }

    bool Encapsulate(ref Bounds mb)
    {
        bool any = false;
        if (rends == null) return false;
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] == null) continue;
            if (!any) { mb = rends[i].bounds; any = true; }
            else mb.Encapsulate(rends[i].bounds);
        }
        return any;
    }
}
