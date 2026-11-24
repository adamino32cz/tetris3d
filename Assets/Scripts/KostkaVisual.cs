using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Sdílené vizuály: zaoblená kostka, pastové materiály, mřížka, souřadnice.
public static class KostkaVisual
{
    public const float CubeScale = 0.90f;

    public static Mesh RoundedMesh;
    static Material litTemplate, unlitTemplate;
    static readonly Dictionary<Color, Material> litCache = new Dictionary<Color, Material>();
    static readonly Dictionary<Color, Material> ghostCache = new Dictionary<Color, Material>();
    static Material gridMat, floorMat, postMat, wallMat, groundMat, skyMat;
    static Sprite _roundSprite;

    public static Vector3 World(Vector3Int g)
    {
        return World((Vector3)g);
    }

    public static Vector3 World(Vector3 g)
    {
        return new Vector3(g.x - 3f, g.y + 0.5f, g.z - 3f);
    }

    public static void SetTemplates(Material lit, Material unlit)
    {
        if (lit != null) litTemplate = lit;
        if (unlit != null) unlitTemplate = unlit;
    }

    public static void Init()
    {
        if (RoundedMesh == null) RoundedMesh = BuildRoundedCube(4, 0.18f);
        if (litTemplate == null)
        {
            litTemplate = Resources.Load<Material>("KostkaLit");
            if (litTemplate == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (s != null) litTemplate = new Material(s);
            }
        }
        if (unlitTemplate == null)
        {
            unlitTemplate = Resources.Load<Material>("KostkaUnlit");
            if (unlitTemplate == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
                if (s != null) unlitTemplate = new Material(s);
            }
        }
        if (unlitTemplate == null) unlitTemplate = litTemplate;
    }

    static Material NewFrom(Material template)
    {
        if (template == null)
            throw new System.InvalidOperationException("Chybí materiálová šablona (shader se nenačetl do buildu).");
        return new Material(template);
    }

    public static Material Lit(Color c)
    {
        Init();
        if (litCache.TryGetValue(c, out var m)) return m;
        m = NewFrom(litTemplate);
        ApplyLit(m, c, 0.32f, false, 1f);
        litCache[c] = m;
        return m;
    }

    public static Material Unlit(Color c)
    {
        Init();
        var m = NewFrom(unlitTemplate);
        SetColor(m, c);
        if (c.a < 0.99f) MakeTransparent(m, c.a);
        return m;
    }

    public static Material Ghost(Color c)
    {
        Init();
        var g = new Color(c.r, c.g, c.b, 0.28f);
        if (ghostCache.TryGetValue(g, out var m)) return m;
        m = NewFrom(litTemplate);
        ApplyLit(m, g, 0.15f, true, 0.28f);
        ghostCache[g] = m;
        return m;
    }

    public static Material FloorMat()
    {
        if (floorMat != null) return floorMat;
        Init();
        floorMat = NewFrom(litTemplate);
        ApplyLit(floorMat, new Color(0.93f, 0.86f, 0.78f), 0.18f, false, 1f);
        floorMat.SetTexture("_BaseMap", GridTex());
        floorMat.SetTextureScale("_BaseMap", new Vector2(7f, 7f));
        return floorMat;
    }

    public static Material GroundMat()
    {
        if (groundMat != null) return groundMat;
        Init();
        groundMat = NewFrom(litTemplate);
        ApplyLit(groundMat, new Color(0.90f, 0.82f, 0.73f), 0.12f, false, 1f);
        return groundMat;
    }

    public static Material PostMat()
    {
        if (postMat != null) return postMat;
        Init();
        postMat = NewFrom(litTemplate);
        ApplyLit(postMat, new Color(0.86f, 0.76f, 0.68f), 0.22f, false, 1f);
        return postMat;
    }

    public static Material WallMat()
    {
        if (wallMat != null) return wallMat;
        Init();
        wallMat = NewFrom(unlitTemplate);
        SetColor(wallMat, new Color(1f, 0.96f, 0.92f, 0.10f));
        MakeTransparent(wallMat, 0.10f);
        return wallMat;
    }

    public static Material GridMat()
    {
        if (gridMat != null) return gridMat;
        Init();
        gridMat = NewFrom(unlitTemplate);
        SetColor(gridMat, new Color(0.72f, 0.60f, 0.52f, 0.55f));
        MakeTransparent(gridMat, 0.55f);
        return gridMat;
    }

    public static Material SkyMat()
    {
        if (skyMat != null) return skyMat;
        Init();
        skyMat = NewFrom(unlitTemplate);
        SetColor(skyMat, Color.white);
        if (skyMat.HasProperty("_BaseMap")) skyMat.SetTexture("_BaseMap", SkyTex());
        if (skyMat.HasProperty("_MainTex")) skyMat.SetTexture("_MainTex", SkyTex());
        skyMat.renderQueue = 1000;
        return skyMat;
    }

    public static Sprite RoundSprite()
    {
        if (_roundSprite != null) return _roundSprite;
        int s = 64, r = 16;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var clear = new Color(1, 1, 1, 0);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float dx = Mathf.Min(x + 0.5f, s - x - 0.5f);
            float dy = Mathf.Min(y + 0.5f, s - y - 0.5f);
            float d;
            if (dx >= r && dy >= r) d = 1f;
            else if (dx >= r) d = dy;
            else if (dy >= r) d = dx;
            else
            {
                float px = r - dx, py = r - dy;
                d = r - Mathf.Sqrt(px * px + py * py);
            }
            float a = Mathf.Clamp01(d);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        _roundSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return _roundSprite;
    }

    static void ApplyLit(Material m, Color c, float smooth, bool transparent, float alpha)
    {
        SetColor(m, c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_WorkflowMode")) m.SetFloat("_WorkflowMode", 1f);
        if (transparent) MakeTransparent(m, alpha);
        else
        {
            m.renderQueue = 2000;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
        }
    }

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    static void MakeTransparent(Material m, float alpha)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        var c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
        c.a = alpha;
        SetColor(m, c);
    }

    static Texture2D _grid;
    static Texture2D GridTex()
    {
        if (_grid != null) return _grid;
        int s = 64;
        _grid = new Texture2D(s, s, TextureFormat.RGBA32, false);
        _grid.filterMode = FilterMode.Bilinear;
        _grid.wrapMode = TextureWrapMode.Repeat;
        var baseC = new Color(0.95f, 0.89f, 0.82f, 1f);
        var line = new Color(0.78f, 0.68f, 0.58f, 1f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            bool edge = x < 2 || y < 2 || x > s - 3 || y > s - 3;
            _grid.SetPixel(x, y, edge ? line : baseC);
        }
        _grid.Apply();
        return _grid;
    }

    static Texture2D _sky;
    static Texture2D SkyTex()
    {
        if (_sky != null) return _sky;
        int h = 256;
        _sky = new Texture2D(4, h, TextureFormat.RGBA32, false);
        _sky.wrapMode = TextureWrapMode.Clamp;
        _sky.filterMode = FilterMode.Bilinear;
        var top = new Color(0.99f, 0.80f, 0.68f);
        var mid = new Color(1.00f, 0.91f, 0.84f);
        var bot = new Color(0.96f, 0.90f, 0.84f);
        for (int y = 0; y < h; y++)
        {
            float t = y / (h - 1f);
            Color c = t < 0.55f
                ? Color.Lerp(bot, mid, t / 0.55f)
                : Color.Lerp(mid, top, (t - 0.55f) / 0.45f);
            for (int x = 0; x < 4; x++) _sky.SetPixel(x, y, c);
        }
        _sky.Apply();
        return _sky;
    }

    // Zaoblená jednotková kostka (zaoblení jako u kostičky z modelíny).
    public static Mesh BuildRoundedCube(int seg, float radius)
    {
        radius = Mathf.Clamp(radius, 0.02f, 0.49f);
        float inner = 0.5f - radius;
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        void Face(Vector3 origin, Vector3 axisU, Vector3 axisV, Vector3 nHint)
        {
            int baseIndex = verts.Count;
            for (int j = 0; j <= seg; j++)
            for (int i = 0; i <= seg; i++)
            {
                float u = (float)i / seg;
                float v = (float)j / seg;
                Vector3 p = origin + axisU * u + axisV * v;
                Vector3 c = new Vector3(
                    Mathf.Clamp(p.x, -inner, inner),
                    Mathf.Clamp(p.y, -inner, inner),
                    Mathf.Clamp(p.z, -inner, inner));
                Vector3 d = p - c;
                Vector3 n;
                Vector3 pos;
                if (d.sqrMagnitude < 1e-8f)
                {
                    n = nHint.normalized;
                    pos = p;
                }
                else
                {
                    n = d.normalized;
                    pos = c + n * radius;
                }
                verts.Add(pos);
                norms.Add(n);
                uvs.Add(new Vector2(u, v));
            }
            int row = seg + 1;
            for (int j = 0; j < seg; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = baseIndex + j * row + i;
                int b = a + 1;
                int c = a + row;
                int d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        float o = 0.5f;
        Face(new Vector3(-o, -o,  o), new Vector3(1, 0, 0), new Vector3(0, 1, 0), Vector3.forward);   // +Z
        Face(new Vector3( o, -o, -o), new Vector3(-1,0, 0), new Vector3(0, 1, 0), Vector3.back);      // -Z
        Face(new Vector3(-o, -o, -o), new Vector3(0, 0, 1), new Vector3(0, 1, 0), Vector3.left);      // -X
        Face(new Vector3( o, -o,  o), new Vector3(0, 0,-1), new Vector3(0, 1, 0), Vector3.right);     // +X
        Face(new Vector3(-o,  o,  o), new Vector3(1, 0, 0), new Vector3(0, 0,-1), Vector3.up);        // +Y
        Face(new Vector3(-o, -o, -o), new Vector3(1, 0, 0), new Vector3(0, 0, 1), Vector3.down);      // -Y

        var mesh = new Mesh { name = "RoundedCube" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Sestaví mřížku šachty (tenké hranoly) do jednoho meshe.
    public static Mesh BuildWellGrid()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        float t = 0.018f;
        float x0 = -3.5f, x1 = 3.5f, z0 = -3.5f, z1 = 3.5f, y0 = 0f, y1 = 15f;

        void Box(Vector3 a, Vector3 b)
        {
            Vector3 mid = (a + b) * 0.5f;
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            Vector3 axis = d / len;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            Vector3 ext = axis * (len * 0.5f);
            Vector3 eu = u * t, ev = v * t;
            int i = verts.Count;
            Vector3[] c =
            {
                mid - ext - eu - ev, mid + ext - eu - ev,
                mid + ext + eu - ev, mid - ext + eu - ev,
                mid - ext - eu + ev, mid + ext - eu + ev,
                mid + ext + eu + ev, mid - ext + eu + ev
            };
            verts.AddRange(c);
            int[] idx = { 0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4, 2,3,7, 2,7,6, 0,4,7, 0,7,3, 1,2,6, 1,6,5 };
            for (int k = 0; k < idx.Length; k++) tris.Add(i + idx[k]);
        }

        for (int i = 0; i <= 7; i++)
        {
            float x = i - 3.5f;
            float z = i - 3.5f;
            Box(new Vector3(x, y0, z0), new Vector3(x, y0, z1));
            Box(new Vector3(x0, y0, z), new Vector3(x1, y0, z));
            Box(new Vector3(x, y0, z0), new Vector3(x, y1, z0));
            Box(new Vector3(x, y0, z1), new Vector3(x, y1, z1));
            Box(new Vector3(x0, y0, z), new Vector3(x0, y1, z));
            Box(new Vector3(x1, y0, z), new Vector3(x1, y1, z));
        }
        for (int y = 1; y <= 15; y++)
        {
            float yy = y;
            Box(new Vector3(x0, yy, z0), new Vector3(x1, yy, z0));
            Box(new Vector3(x0, yy, z1), new Vector3(x1, yy, z1));
            Box(new Vector3(x0, yy, z0), new Vector3(x0, yy, z1));
            Box(new Vector3(x1, yy, z0), new Vector3(x1, yy, z1));
        }

        var mesh = new Mesh { name = "WellGrid" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject MakeCube(Transform parent, Vector3 pos, Material mat, string name = "Kostka")
    {
        Init();
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * CubeScale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = RoundedMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.On;
        mr.receiveShadows = true;
        return go;
    }
}
