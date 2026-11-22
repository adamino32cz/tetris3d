using System.Collections.Generic;
using UnityEngine;

// Šachta 7×7×15: obsazenost, kolize a mazání plných řad (7 kostek v X nebo Z).
public class Board
{
    public const int W = 7;
    public const int D = 7;
    public const int H = 15;

    // 0 = prázdné, jinak index tvaru + 1
    readonly int[,,] cells = new int[W, H, D];

    // Řada = 7 kostek v jednom patře: buď podél X (pevné z), nebo podél Z (pevné x).
    public struct Rada
    {
        public int y;
        public bool podelX;
        public int pevna;
    }

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

    public List<Rada> NajdiPlneRadky()
    {
        var list = new List<Rada>();
        for (int y = 0; y < H; y++)
        {
            for (int z = 0; z < D; z++)
            {
                bool full = true;
                for (int x = 0; x < W && full; x++)
                    if (cells[x, y, z] == 0) full = false;
                if (full) list.Add(new Rada { y = y, podelX = true, pevna = z });
            }
            for (int x = 0; x < W; x++)
            {
                bool full = true;
                for (int z = 0; z < D && full; z++)
                    if (cells[x, y, z] == 0) full = false;
                if (full) list.Add(new Rada { y = y, podelX = false, pevna = x });
            }
        }
        return list;
    }

    public bool JePlnePatro(int y)
    {
        if (y < 0 || y >= H) return false;
        for (int x = 0; x < W; x++)
        for (int z = 0; z < D; z++)
            if (cells[x, y, z] == 0) return false;
        return true;
    }

    public int PocetPlnychPater()
    {
        int n = 0;
        for (int y = 0; y < H; y++)
            if (JePlnePatro(y)) n++;
        return n;
    }

    // Smaže buňky řad a kostky nad nimi ve stejném sloupci klesnou o počet děr pod nimi.
    // Předem existující mezery se nezatahují. Vrací mapování stará pozice → nová.
    public Dictionary<Vector3Int, Vector3Int> SmazASesun(List<Rada> rady, out HashSet<Vector3Int> odstranene)
    {
        odstranene = new HashSet<Vector3Int>();
        var move = new Dictionary<Vector3Int, Vector3Int>();
        if (rady == null || rady.Count == 0) return move;

        for (int i = 0; i < rady.Count; i++)
        {
            var r = rady[i];
            if (r.podelX)
            {
                for (int x = 0; x < W; x++)
                    odstranene.Add(new Vector3Int(x, r.y, r.pevna));
            }
            else
            {
                for (int z = 0; z < D; z++)
                    odstranene.Add(new Vector3Int(r.pevna, r.y, z));
            }
        }

        var next = new int[W, H, D];
        for (int x = 0; x < W; x++)
        for (int z = 0; z < D; z++)
        {
            int removedBelow = 0;
            for (int y = 0; y < H; y++)
            {
                var p = new Vector3Int(x, y, z);
                if (odstranene.Contains(p))
                {
                    removedBelow++;
                    continue;
                }
                int val = cells[x, y, z];
                if (val == 0) continue;
                int ny = y - removedBelow;
                next[x, ny, z] = val;
                if (ny != y) move[p] = new Vector3Int(x, ny, z);
            }
        }
        System.Array.Clear(cells, 0, cells.Length);
        System.Array.Copy(next, cells, next.Length);
        return move;
    }
}
