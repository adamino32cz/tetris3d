using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Procedurální pavouk pro krátký jumpscare (tělo, zadeček, 8 nohou, červené oči).
public class JumpscarePavouk : MonoBehaviour
{
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
            script.Noha(root, telo, side, i);

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
        float z = Mathf.Lerp(0.22f, -0.45f, i / 3f);
        var kyc = new GameObject("Kyc" + strana + i).transform;
        kyc.SetParent(root, false);
        kyc.localPosition = new Vector3(strana * 0.22f, 0.02f, z);
        float yaw = strana * (40f + i * 14f);
        float roll = strana * (28f + i * 4f);
        float pitch = -8f + i * 6f;
        kyc.localRotation = Quaternion.Euler(pitch, yaw, roll);
        kyce.Add(kyc);
        kyceRest.Add(kyc.localRotation);

        // stehno
        var stehno = Valec(kyc, "Stehno", new Vector3(0f, 0.28f, 0f), new Vector3(0.07f, 0.28f, 0.07f), mat);
        // koleno + holeň dolů a ven
        var koleno = new GameObject("Koleno").transform;
        koleno.SetParent(stehno, false);
        koleno.localPosition = new Vector3(0f, 0.95f, 0f);
        koleno.localRotation = Quaternion.Euler(0f, 0f, strana * -55f);
        Valec(koleno, "Holen", new Vector3(0f, 0.32f, 0f), new Vector3(0.055f, 0.32f, 0.055f), mat);
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
}
