using Raylib_cs;
using ResetyKile.Core;

namespace ResetyKile.World;

public enum TileType
{
    Empty = 0,
    Solid,
    Platform,
}

public class Level
{
    public const int TileSize = 8;
    private readonly TileType[,] _grid;

    public int Width  => _grid.GetLength(0);
    public int Height => _grid.GetLength(1);

    public Level(TileType[,] grid) { _grid = grid; }

    public IEnumerable<Rectangle> SolidTilesNear(Rectangle r)
    {
        int x0 = Math.Max(0, (int)(r.X / TileSize) - 1);
        int y0 = Math.Max(0, (int)(r.Y / TileSize) - 1);
        int x1 = Math.Min(Width  - 1, (int)((r.X + r.Width ) / TileSize) + 1);
        int y1 = Math.Min(Height - 1, (int)((r.Y + r.Height) / TileSize) + 1);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_grid[x, y] == TileType.Solid)
                    yield return new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
    }

    public IEnumerable<Rectangle> PlatformTilesNear(Rectangle r)
    {
        int x0 = Math.Max(0, (int)(r.X / TileSize) - 1);
        int y0 = Math.Max(0, (int)(r.Y / TileSize) - 1);
        int x1 = Math.Min(Width  - 1, (int)((r.X + r.Width ) / TileSize) + 1);
        int y1 = Math.Min(Height - 1, (int)((r.Y + r.Height) / TileSize) + 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (_grid[x, y] != TileType.Platform) continue;

                // Só considera "topo da plataforma" se o tile ACIMA não é plataforma.
                // Se o tile acima é plataforma, essa parte é "corpo" — não colide.
                bool isTop = (y == 0) || (_grid[x, y - 1] != TileType.Platform);

                if (!isTop) continue;

                // Colisão fina no topo (3px de altura)
                yield return new Rectangle(x * TileSize, y * TileSize, TileSize, 3);
            }
        }
    }

    public bool CollidesAny(Rectangle r, bool falling = true)
    {
        foreach (var t in SolidTilesNear(r))
            if (Raylib.CheckCollisionRecs(r, t)) return true;

        if (falling)
        {
            foreach (var t in PlatformTilesNear(r))
                if (Raylib.CheckCollisionRecs(r, t)) return true;
        }

        return false;
    }

    private static readonly Color ColorSolidTop    = new((byte)120, (byte)90, (byte)60, (byte)255);
    private static readonly Color ColorSolidBody   = new((byte)90, (byte)65, (byte)45, (byte)255);
    private static readonly Color ColorSolidEdge   = new((byte)60, (byte)40, (byte)25, (byte)255);
    private static readonly Color ColorPlatform    = new((byte)150, (byte)110, (byte)70, (byte)255);
    private static readonly Color ColorPlatformTop = new((byte)180, (byte)140, (byte)90, (byte)255);
    private static readonly Color ColorPlatformDark = new((byte)90, (byte)60, (byte)35, (byte)255);

    public void Draw(Camera2D cam)
    {
        float halfW = Renderer.InternalW / (2f * cam.Zoom);
        float halfH = Renderer.InternalH / (2f * cam.Zoom);
        var view = new Rectangle(cam.Target.X - halfW, cam.Target.Y - halfH, halfW * 2f, halfH * 2f);

        int x0 = Math.Max(0, (int)(view.X / TileSize) - 1);
        int y0 = Math.Max(0, (int)(view.Y / TileSize) - 1);
        int x1 = Math.Min(Width  - 1, (int)((view.X + view.Width ) / TileSize) + 1);
        int y1 = Math.Min(Height - 1, (int)((view.Y + view.Height) / TileSize) + 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var type = _grid[x, y];
                if (type == TileType.Empty) continue;

                int px = x * TileSize;
                int py = y * TileSize;

                if (type == TileType.Solid)
                {
                    bool isTop = (y == 0) || (_grid[x, y - 1] != TileType.Solid);

                    Raylib.DrawRectangle(px, py, TileSize, TileSize, ColorSolidBody);
                    if (isTop)
                        Raylib.DrawRectangle(px, py, TileSize, 2, ColorSolidTop);
                    Raylib.DrawRectangle(px + TileSize - 1, py, 1, TileSize, ColorSolidEdge);
                    Raylib.DrawRectangle(px, py + TileSize - 1, TileSize, 1, ColorSolidEdge);
                }
                else if (type == TileType.Platform)
                {
                    Raylib.DrawRectangle(px, py, TileSize, 3, ColorPlatformTop);
                    Raylib.DrawRectangle(px, py + 3, TileSize, 2, ColorPlatform);
                    Raylib.DrawRectangle(px, py + 5, TileSize, 1, ColorPlatformDark);
                }
            }
        }
    }

    // ============================================================
    // FASE 1 — Aeroporto (tutorial, sem inimigos)
    // ============================================================
    public static Level Airport()
    {
        const int W = 60, H = 30;
        var g = new TileType[W, H];

        void FillRect(int x0, int y0, int w, int h, TileType t)
        {
            for (int yy = y0; yy < y0 + h; yy++)
                for (int xx = x0; xx < x0 + w; xx++)
                    if (xx >= 0 && xx < W && yy >= 0 && yy < H)
                        g[xx, yy] = t;
        }

        // Chão
        FillRect(0, H - 3, W, 3, TileType.Solid);
        // Paredes
        FillRect(0, 0, 1, H, TileType.Solid);
        FillRect(W - 1, 0, 1, H, TileType.Solid);

        // Plataformas de tutorial (pulo)
        FillRect(6,  H - 7, 5, 1, TileType.Platform);
        FillRect(14, H - 10, 5, 1, TileType.Platform);
        FillRect(22, H - 13, 5, 1, TileType.Platform);

        // Parede pra wall jump
        FillRect(32, H - 12, 1, 8, TileType.Solid);

        // Área de dash
        FillRect(40, H - 6, 10, 1, TileType.Solid);

        return new Level(g);
    }

    // ============================================================
    // FASE 2 — Casa do Tio (área segura, sem inimigos)
    // ============================================================
    public static Level UncleHouse()
    {
        const int W = 40, H = 20;
        var g = new TileType[W, H];

        void FillRect(int x0, int y0, int w, int h, TileType t)
        {
            for (int yy = y0; yy < y0 + h; yy++)
                for (int xx = x0; xx < x0 + w; xx++)
                    if (xx >= 0 && xx < W && yy >= 0 && yy < H)
                        g[xx, yy] = t;
        }

        // Chão
        FillRect(0, H - 2, W, 2, TileType.Solid);
        // Paredes
        FillRect(0, 0, 1, H, TileType.Solid);
        FillRect(W - 1, 0, 1, H, TileType.Solid);
        // Teto
        FillRect(0, 0, W, 1, TileType.Solid);

        // Móveis (plataformas decorativas)
        FillRect(6, H - 5, 4, 1, TileType.Platform);
        FillRect(14, H - 7, 3, 1, TileType.Platform);
        FillRect(24, H - 5, 5, 1, TileType.Platform);

        return new Level(g);
    }

    // ============================================================
    // FASE 3 — Cassino (combate)
    // ============================================================
    public static Level Casino()
    {
        const int W = 70, H = 30;
        var g = new TileType[W, H];

        void FillRect(int x0, int y0, int w, int h, TileType t)
        {
            for (int yy = y0; yy < y0 + h; yy++)
                for (int xx = x0; xx < x0 + w; xx++)
                    if (xx >= 0 && xx < W && yy >= 0 && yy < H)
                        g[xx, yy] = t;
        }

        // Chão
        FillRect(0, H - 3, W, 3, TileType.Solid);
        // Paredes
        FillRect(0, 0, 1, H, TileType.Solid);
        FillRect(W - 1, 0, 1, H, TileType.Solid);

        // "Mesas de cassino" (plataformas)
        FillRect(8,  H - 8, 4, 1, TileType.Platform);
        FillRect(18, H - 12, 4, 1, TileType.Platform);
        FillRect(28, H - 8, 4, 1, TileType.Platform);
        FillRect(38, H - 14, 4, 1, TileType.Platform);
        FillRect(48, H - 10, 4, 1, TileType.Platform);
        FillRect(58, H - 8, 4, 1, TileType.Platform);

        // Parede escalável
        FillRect(34, H - 18, 1, 10, TileType.Solid);

        return new Level(g);
    }
}