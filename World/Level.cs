using Raylib_cs;
using ResetyKile.Core;

namespace ResetyKile.World;

public enum TileType
{
    Empty = 0,
    Solid,       // chão/parede sólido (colide todos os lados)
    Platform,    // plataforma atravessável (colide só por cima)
}

public class Level
{
    public const int TileSize = 8;
    private readonly TileType[,] _grid;

    public int Width  => _grid.GetLength(0);
    public int Height => _grid.GetLength(1);

    public Level(TileType[,] grid) { _grid = grid; }

    // ---------- Colisão SÓLIDA (todos os lados) ----------
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

    // ---------- Colisão PLATAFORMA (só por cima) ----------
    /// Retorna os tiles de plataforma próximos do retângulo.
    /// A física decide se colide (só se estiver caindo e acima do topo).
    public IEnumerable<Rectangle> PlatformTilesNear(Rectangle r)
    {
        int x0 = Math.Max(0, (int)(r.X / TileSize) - 1);
        int y0 = Math.Max(0, (int)(r.Y / TileSize) - 1);
        int x1 = Math.Min(Width  - 1, (int)((r.X + r.Width ) / TileSize) + 1);
        int y1 = Math.Min(Height - 1, (int)((r.Y + r.Height) / TileSize) + 1);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (_grid[x, y] == TileType.Platform)
                    yield return new Rectangle(x * TileSize, y * TileSize, TileSize, 3); // topo da plataforma
    }

    // ---------- Colisão unificada ----------
    /// Checa se colide com tile sólido OU plataforma (dependendo do estado).
    /// `falling` = player tá caindo (Vel.Y >= 0) — só aí plataforma colide.
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

    // ---------- Desenho ----------
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
                    // Só o topo é "sólido" visualmente
                    Raylib.DrawRectangle(px, py, TileSize, 3, ColorPlatformTop);
                    Raylib.DrawRectangle(px, py + 3, TileSize, 2, ColorPlatform);
                    Raylib.DrawRectangle(px, py + 5, TileSize, 1, ColorPlatformDark);
                }
            }
        }
    }

    // ============================================================
    // FASE — Oriental Village
    // ============================================================
    public static Level OrientalVillage()
    {
        const int W = 80;
        const int H = 40;
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

        // Paredes laterais
        FillRect(0, 0, 1, H, TileType.Solid);
        FillRect(W - 1, 0, 1, H, TileType.Solid);

        // Plataformas iniciais (atravessáveis)
        FillRect(8,  H - 6, 6, 1, TileType.Platform);
        FillRect(16, H - 9, 6, 1, TileType.Platform);
        FillRect(24, H - 12, 6, 1, TileType.Platform);

        // Pagode central
        FillRect(32, H - 6, 12, 1, TileType.Solid);
        FillRect(34, H - 10, 8, 1, TileType.Platform);
        FillRect(36, H - 14, 4, 1, TileType.Platform);

        // Parede escalável
        FillRect(48, H - 16, 1, 10, TileType.Solid);

        // Área de treino
        FillRect(56, H - 8, 3, 1, TileType.Platform);
        FillRect(62, H - 12, 3, 1, TileType.Platform);
        FillRect(68, H - 16, 3, 1, TileType.Platform);

        // Topo
        FillRect(28, H - 20, 4, 1, TileType.Platform);
        FillRect(40, H - 22, 4, 1, TileType.Platform);

        return new Level(g);
    }
}