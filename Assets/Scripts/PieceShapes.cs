using UnityEngine;

// Definice 3D kousků (polycubes) a jejich pastových barev.
public struct PieceDef
{
    public string id;
    public string nazev;
    public Color barva;
    public Vector3Int[] bunky;
}

public static class PieceShapes
{
    public static readonly PieceDef[] All =
    {
        Def("I", "Tyčka", C(0.55f, 0.86f, 0.78f), P(0,0,0), P(1,0,0), P(-1,0,0), P(2,0,0)),
        Def("O", "Čtverec", C(0.98f, 0.86f, 0.45f), P(0,0,0), P(1,0,0), P(0,0,1), P(1,0,1)),
        Def("L", "Elko", C(0.96f, 0.62f, 0.42f), P(0,0,0), P(-1,0,0), P(1,0,0), P(1,0,1)),
        Def("T", "Téčko", C(0.76f, 0.62f, 0.93f), P(0,0,0), P(-1,0,0), P(1,0,0), P(0,0,1)),
        Def("S", "Es", C(0.55f, 0.78f, 0.96f), P(0,0,0), P(1,0,0), P(0,0,1), P(-1,0,1)),
        Def("Roh", "Roh", C(0.98f, 0.70f, 0.62f), P(0,0,0), P(1,0,0), P(0,1,0), P(0,0,1)),
        Def("Sroub", "Šroub", C(0.62f, 0.82f, 0.58f), P(0,0,0), P(1,0,0), P(1,1,0), P(1,1,1)),
        Def("Kriz", "Kříž", C(0.96f, 0.58f, 0.70f), P(0,0,0), P(-1,0,0), P(1,0,0), P(0,0,-1), P(0,0,1)),
    };

    static PieceDef Def(string id, string nazev, Color barva, params Vector3Int[] bunky)
    {
        return new PieceDef { id = id, nazev = nazev, barva = barva, bunky = bunky };
    }

    static Color C(float r, float g, float b) => new Color(r, g, b, 1f);
    static Vector3Int P(int x, int y, int z) => new Vector3Int(x, y, z);

    // Rotace buňky o 90° kolem osy (0=X, 1=Y, 2=Z). dir = +1 nebo -1.
    public static Vector3Int RotateCell(Vector3Int c, int axis, int dir)
    {
        if (axis == 0)
            return dir > 0 ? new Vector3Int(c.x, -c.z, c.y) : new Vector3Int(c.x, c.z, -c.y);
        if (axis == 1)
            return dir > 0 ? new Vector3Int(c.z, c.y, -c.x) : new Vector3Int(-c.z, c.y, c.x);
        return dir > 0 ? new Vector3Int(-c.y, c.x, c.z) : new Vector3Int(c.y, -c.x, c.z);
    }

    public static Vector3Int[] RotateAll(Vector3Int[] src, int axis, int dir)
    {
        var d = new Vector3Int[src.Length];
        for (int i = 0; i < src.Length; i++) d[i] = RotateCell(src[i], axis, dir);
        return d;
    }

    // Stejná 90° rotace jako RotateCell, jako kvaternion (pro orientaci figurek).
    public static Quaternion Rotation(int axis, int dir)
    {
        float a = dir > 0 ? 90f : -90f;
        if (axis == 0) return Quaternion.Euler(a, 0f, 0f);
        if (axis == 1) return Quaternion.Euler(0f, a, 0f);
        return Quaternion.Euler(0f, 0f, a);
    }

    public static Vector3Int[] Copy(Vector3Int[] src)
    {
        var d = new Vector3Int[src.Length];
        System.Array.Copy(src, d, src.Length);
        return d;
    }
}
