using Raylib_cs;
using ResetyKile.Core;   // ← NECESSÁRIO pra acessar Renderer.InternalW/H

namespace ResetyKile.World;

public class Level
{
    public const int TileSize = 8;
    private readonly int[,] _grid;

    public int Width  => _grid.GetLength(0);
    public int Height => _grid.GetLength(1);

    public Level(int[,] grid) { _grid = grid; }

    public IEnumerable<Rectangle> SolidTilesNear(Rectangle r)
    {
        int x0 = Math.Max(0, (int)(r.X / TileSize) - 1);
        int y0 = Math.Max(0, (int)(r.Y / TileSize) - 1);
        int x1 = Math.Min(Width  - 1, (int)((r.X + r.Width ) / TileSize) + 1);
        int y1 = Math.Min(Height - 1, (int)((r.Y + r.Height) / TileSize) + 1);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_grid[x, y] == 1)
                    yield return new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
    }

    public bool CollidesAny(Rectangle r)
    {
        foreach (var t in SolidTilesNear(r))
            if (Raylib.CheckCollisionRecs(r, t)) return true;
        return false;
    }

    public void Draw(Camera2D cam, Color color)
    {
        float halfW = Renderer.InternalW / (2f * cam.Zoom);
        float halfH = Renderer.InternalH / (2f * cam.Zoom);
        var view = new Rectangle(cam.Target.X - halfW, cam.Target.Y - halfH, halfW * 2f, halfH * 2f);

        foreach (var t in SolidTilesNear(view))
            Raylib.DrawRectangleRec(t, color);
    }

    // ---------- Fase de teste ----------
    public static Level TestRoom()
    {
        const int W = 60, H = 30;
        var g = new int[W, H];

        // Chão
        for (int x = 0; x < W; x++)
        {
            g[x, H - 1] = 1;
            g[x, H - 2] = 1;
        }

        // Paredes laterais
        for (int y = 0; y < H; y++)
        {
            g[0, y] = 1;
            g[W - 1, y] = 1;
        }

        // Plataformas
        void Plat(int x0, int y0, int len)
        {
            for (int i = 0; i < len; i++) g[x0 + i, y0] = 1;
        }

        Plat(8,  22, 6);
        Plat(18, 18, 5);
        Plat(28, 14, 8);
        Plat(40, 20, 5);
        Plat(46, 12, 6);

        // Parede pra testar wall jump
        for (int y = 8; y < 22; y++) g[36, y] = 1;

        return new Level(g);
    }
}