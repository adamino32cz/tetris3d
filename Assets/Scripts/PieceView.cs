using System.Collections.Generic;
using UnityEngine;

// Vykreslí sadu buněk jako zaoblené kostky (aktivní kus, duch i náhled).
public class PieceView
{
    public readonly Transform root;
    readonly List<GameObject> cubes = new List<GameObject>();
    bool ghost;

    public PieceView(string name, Transform parent, bool ghost)
    {
        this.ghost = ghost;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        root = go.transform;
    }

    public void Hide()
    {
        root.gameObject.SetActive(false);
    }

    public void Show()
    {
        root.gameObject.SetActive(true);
    }

    public void Set(Vector3Int origin, Vector3Int[] local, Color color, bool asGhost)
    {
        ghost = asGhost;
        Show();
        int n = local.Length;
        while (cubes.Count < n)
            cubes.Add(KostkaVisual.MakeCube(root, Vector3.zero, KostkaVisual.Lit(color)));
        for (int i = 0; i < cubes.Count; i++)
        {
            bool on = i < n;
            cubes[i].SetActive(on);
            if (!on) continue;
            cubes[i].transform.position = KostkaVisual.World(origin + local[i]);
            cubes[i].transform.localScale = Vector3.one * KostkaVisual.CubeScale;
            var mr = cubes[i].GetComponent<MeshRenderer>();
            mr.sharedMaterial = ghost ? KostkaVisual.Ghost(color) : KostkaVisual.Lit(color);
            mr.shadowCastingMode = ghost ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    // Náhled: kostky v lokálních souřadnicích, vycentrované k rootu (ne do šachty).
    public void SetLocal(Vector3Int[] local, Color color)
    {
        ghost = false;
        Show();
        int n = local.Length;
        Vector3 stred = Vector3.zero;
        for (int i = 0; i < n; i++) stred += (Vector3)(Vector3Int)local[i];
        if (n > 0) stred /= n;
        while (cubes.Count < n)
            cubes.Add(KostkaVisual.MakeCube(root, Vector3.zero, KostkaVisual.Lit(color)));
        for (int i = 0; i < cubes.Count; i++)
        {
            bool on = i < n;
            cubes[i].SetActive(on);
            if (!on) continue;
            cubes[i].transform.localPosition = (Vector3)(Vector3Int)local[i] - stred;
            cubes[i].transform.localRotation = Quaternion.identity;
            cubes[i].transform.localScale = Vector3.one * KostkaVisual.CubeScale;
            var mr = cubes[i].GetComponent<MeshRenderer>();
            mr.sharedMaterial = KostkaVisual.Lit(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }

    public void SetWorld(IList<Vector3Int> cells, Color color, bool asGhost)
    {
        ghost = asGhost;
        Show();
        int n = cells.Count;
        while (cubes.Count < n)
            cubes.Add(KostkaVisual.MakeCube(root, Vector3.zero, KostkaVisual.Lit(color)));
        for (int i = 0; i < cubes.Count; i++)
        {
            bool on = i < n;
            cubes[i].SetActive(on);
            if (!on) continue;
            cubes[i].transform.position = KostkaVisual.World(cells[i]);
            cubes[i].transform.localScale = Vector3.one * KostkaVisual.CubeScale;
            var mr = cubes[i].GetComponent<MeshRenderer>();
            mr.sharedMaterial = ghost ? KostkaVisual.Ghost(color) : KostkaVisual.Lit(color);
            mr.shadowCastingMode = ghost ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < cubes.Count; i++)
            if (cubes[i] != null) Object.Destroy(cubes[i]);
        cubes.Clear();
    }
}
