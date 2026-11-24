using System.Collections.Generic;
using UnityEngine;

// Vykreslí sadu buněk jako zaoblené kostky (aktivní kus, duch i náhled).
public class PieceView
{
    public readonly Transform root;
    readonly List<GameObject> cubes = new List<GameObject>();
    bool ghost;
    int tvar = -1;
    KostkaMiku miku;
    KostkaAstolfo astolfo;

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
        if (miku != null) miku.Hide();
        if (astolfo != null) astolfo.Hide();
    }

    public void Show()
    {
        root.gameObject.SetActive(true);
    }

    public void Set(Vector3Int origin, Vector3Int[] local, Color color, bool asGhost)
    {
        Set(origin, local, color, asGhost, tvar);
    }

    public void Set(Vector3Int origin, Vector3Int[] local, Color color, bool asGhost, int tvarIndex)
    {
        SetSmooth((Vector3)origin, local, color, asGhost, tvarIndex);
    }

    public void SetSmooth(Vector3 origin, Vector3Int[] local, Color color, bool asGhost, int tvarIndex)
    {
        ghost = asGhost;
        tvar = tvarIndex;
        Show();
        int n = local.Length;
        while (cubes.Count < n)
            cubes.Add(Make(KostkaVisual.Lit(color)));
        var centers = new List<Vector3>(n);
        bool hideCubes = KostkaSkin.IsCharacter;
        for (int i = 0; i < cubes.Count; i++)
        {
            bool on = i < n && !hideCubes;
            cubes[i].SetActive(on);
            if (i >= n) continue;
            Vector3 wp = KostkaVisual.World(origin + (Vector3)(Vector3Int)local[i]);
            centers.Add(wp);
            if (!on) continue;
            cubes[i].transform.position = wp;
            cubes[i].transform.localScale = Vector3.one * KostkaVisual.CubeScale;
            ApplyMat(cubes[i]);
        }
        UpdateMiku(centers, false);
    }

    // Náhled: kostky v lokálních souřadnicích, vycentrované k rootu (ne do šachty).
    public void SetLocal(Vector3Int[] local, Color color)
    {
        SetLocal(local, color, tvar);
    }

    public void SetLocal(Vector3Int[] local, Color color, int tvarIndex)
    {
        ghost = false;
        tvar = tvarIndex;
        Show();
        int n = local.Length;
        Vector3 stred = Vector3.zero;
        for (int i = 0; i < n; i++) stred += (Vector3)(Vector3Int)local[i];
        if (n > 0) stred /= n;
        while (cubes.Count < n)
            cubes.Add(Make(KostkaVisual.Lit(color)));
        var centers = new List<Vector3>(n);
        bool hideCubes = KostkaSkin.IsCharacter;
        for (int i = 0; i < cubes.Count; i++)
        {
            bool on = i < n && !hideCubes;
            cubes[i].SetActive(on);
            if (i >= n) continue;
            Vector3 lp = (Vector3)(Vector3Int)local[i] - stred;
            centers.Add(lp);
            if (!on) continue;
            cubes[i].transform.localPosition = lp;
            cubes[i].transform.localRotation = Quaternion.identity;
            cubes[i].transform.localScale = Vector3.one * KostkaVisual.CubeScale;
            var mr = cubes[i].GetComponent<MeshRenderer>();
            ApplyMat(cubes[i]);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        UpdateMiku(centers, true);
    }

    public void SetWorld(IList<Vector3Int> cells, Color color, bool asGhost)
    {
        SetWorld(cells, color, asGhost, tvar);
    }

    public void SetWorld(IList<Vector3Int> cells, Color color, bool asGhost, int tvarIndex)
    {
        ghost = asGhost;
        tvar = tvarIndex;
        Show();
        int n = cells.Count;
        while (cubes.Count < n)
            cubes.Add(Make(KostkaVisual.Lit(color)));
        var centers = new List<Vector3>(n);
        bool hideCubes = KostkaSkin.IsCharacter;
        for (int i = 0; i < cubes.Count; i++)
        {
            bool on = i < n && !hideCubes;
            cubes[i].SetActive(on);
            if (i >= n) continue;
            Vector3 wp = KostkaVisual.World(cells[i]);
            centers.Add(wp);
            if (!on) continue;
            cubes[i].transform.position = wp;
            cubes[i].transform.localScale = Vector3.one * KostkaVisual.CubeScale;
            ApplyMat(cubes[i]);
        }
        UpdateMiku(centers, false);
    }

    public void Clear()
    {
        for (int i = 0; i < cubes.Count; i++)
            if (cubes[i] != null) Object.Destroy(cubes[i]);
        cubes.Clear();
        if (miku != null) { Object.Destroy(miku.gameObject); miku = null; }
        if (astolfo != null) { Object.Destroy(astolfo.gameObject); astolfo = null; }
    }

    GameObject Make(Material mat)
    {
        var go = KostkaVisual.MakeCube(root, Vector3.zero, mat);
        var b = go.AddComponent<KostkaBunka>();
        b.tvar = tvar;
        b.ghost = ghost;
        return go;
    }

    void ApplyMat(GameObject go)
    {
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = KostkaSkin.Material(tvar < 0 ? 0 : tvar, ghost);
        mr.shadowCastingMode = ghost ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        var b = go.GetComponent<KostkaBunka>();
        if (b != null) { b.tvar = tvar; b.ghost = ghost; }
    }

    void UpdateMiku(List<Vector3> centers, bool localSpace)
    {
        string id = (tvar >= 0 && tvar < PieceShapes.All.Length) ? PieceShapes.All[tvar].id : "T";
        if (KostkaSkin.IsMiku)
        {
            if (astolfo != null) astolfo.Hide();
            if (miku == null) miku = KostkaMiku.Attach(root);
            miku.ShowPose(id, centers, ghost, localSpace);
            return;
        }
        if (KostkaSkin.IsAstolfo)
        {
            if (miku != null) miku.Hide();
            if (astolfo == null) astolfo = KostkaAstolfo.Attach(root);
            astolfo.ShowPose(id, centers, ghost, localSpace);
            return;
        }
        if (miku != null) miku.Hide();
        if (astolfo != null) astolfo.Hide();
    }
}
