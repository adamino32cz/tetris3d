using System.Collections.Generic;
using UnityEngine;

// Šachta 7×7×15: obsazenost, kolize a mazání plných pater.
public class Board
{
    public const int W = 7;
    public const int D = 7;
    public const int H = 15;

    // 0 = prázdné, jinak index tvaru + 1
    readonly int[,,] cells = new int[W, H, D];

    public void Reset()
    {
        System.Array.Clear(cells, 0, cells.Length);
    }

    public bool InBounds(Vector3Int p)
    {
        return p.x >= 0 && p.x < W && p.z >= 0 && p.z < D && p.y >= 0 && p.y < H;
    }

    public bool Occupied(Vector3Int p)
    {
        if (!InBounds(p)) return true;
        return cells[p.x, p.y, p.z] != 0;
    }

    public int Get(Vector3Int p) => InBounds(p) ? cells[p.x, p.y, p.z] : 0;

    public void Set(Vector3Int p, int shapeIdPlus)
    {
        if (InBounds(p)) cells[p.x, p.y, p.z] = shapeIdPlus;
    }

    public bool CanPlace(Vector3Int origin, Vector3Int[] local)
    {
        for (int i = 0; i < local.Length; i++)
        {
            var p = origin + local[i];
            if (!InBounds(p) || cells[p.x, p.y, p.z] != 0) return false;
        }
        return true;
    }

    public void Place(Vector3Int origin, Vector3Int[] local, int shapeIdPlus)
    {
        for (int i = 0; i < local.Length; i++)
        {
            var p = origin + local[i];
            if (InBounds(p)) cells[p.x, p.y, p.z] = shapeIdPlus;
        }
    }

    // Vrátí indexy plných pater (zdola nahoru).
    public List<int> FullLayers()
    {
        var list = new List<int>();
        for (int y = 0; y < H; y++)
        {
            bool full = true;
            for (int x = 0; x < W && full; x++)
            for (int z = 0; z < D && full; z++)
                if (cells[x, y, z] == 0) full = false;
            if (full) list.Add(y);
        }
        return list;
    }

    // Smaže uvedená patra a všechno nad nimi posune dolů. Vrací mapování staré Y -> nová Y (pro vizuály).
    public Dictionary<Vector3Int, Vector3Int> Collapse(List<int> fullYs)
    {
        var move = new Dictionary<Vector3Int, Vector3Int>();
        if (fullYs == null || fullYs.Count == 0) return move;

        var removed = new bool[H];
        for (int i = 0; i < fullYs.Count; i++)
            if (fullYs[i] >= 0 && fullYs[i] < H) removed[fullYs[i]] = true;

        var next = new int[W, H, D];
        int ny = 0;
        for (int y = 0; y < H; y++)
        {
            if (removed[y]) continue;
            for (int x = 0; x < W; x++)
            for (int z = 0; z < D; z++)
            {
                next[x, ny, z] = cells[x, y, z];
                if (cells[x, y, z] != 0 && ny != y)
                    move[new Vector3Int(x, y, z)] = new Vector3Int(x, ny, z);
            }
            ny++;
        }
        System.Array.Clear(cells, 0, cells.Length);
        System.Array.Copy(next, cells, next.Length);
        return move;
    }
}
