using System.Numerics;
using Raylib_cs;
using ResetyKile.Core;

namespace ResetyKile.World;

public class Level
{
    public const int TileSize = 8;
    private readonly int[,] _grid;

    public int Width => _grid.GetLength(0);
    public int Height => _grid.GetLength(1);

    public Level(int[,] grid)
    {
        _grid = grid;
    }

    public IEnumerable<Rectangle> SolidTilesNear(Rectangle r)
    {
        int x0 = Math.Max(0, (int)(r.X / TileSize) - 1);
        int y0 = Math.Max(0, (int)(r.Y / TileSize) - 1);
        int x1 = Math.Min(Width - 1, (int)((r.X + r.Width) / TileSize) + 1);
        int y1 = Math.Min(Height - 1, (int)((r.Y + r.Height) / TileSize) + 1);

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                if (_grid[x, y] == 1)
                {
                    yield return new Rectangle(
                        x * TileSize,
                        y * TileSize,
                        TileSize,
                        TileSize
                    );
                }
            }
        }
    }

    public bool CollidesAny(Rectangle r)
    {
        foreach (var t in SolidTilesNear(r))
        {
            if (Raylib.CheckCollisionRecs(r, t))
                return true;
        }

        return false;
    }

    public void Draw(Camera2D cam, Color color)
    {
        float halfW = Renderer.InternalW / (2f * cam.Zoom);
        float halfH = Renderer.InternalH / (2f * cam.Zoom);

        var view = new Rectangle(
            cam.Target.X - halfW,
            cam.Target.Y - halfH,
            halfW * 2f,
            halfH * 2f
        );

        // Fundo claro
        Raylib.DrawRectangle(
            (int)view.X,
            (int)view.Y,
            (int)view.Width,
            (int)view.Height,
            new Color(205, 235, 255, 255)
        );

        // Pequenas nuvens decorativas
        DrawCloud(view.X + 35, view.Y + 25);
        DrawCloud(view.X + 130, view.Y + 40);
        DrawCloud(view.X + 220, view.Y + 20);

        // Montanhas ao fundo
        DrawMountain(view.X + 40, view.Y + view.Height - 25, 80, 55);
        DrawMountain(view.X + 130, view.Y + view.Height - 25, 100, 70);
        DrawMountain(view.X + 240, view.Y + view.Height - 25, 90, 60);

        // Tiles
        foreach (var t in SolidTilesNear(view))
        {
            // Corpo principal
            Raylib.DrawRectangleRec(t, color);

            // Parte superior clara
            Raylib.DrawRectangle(
                (int)t.X,
                (int)t.Y,
                (int)t.Width,
                2,
                new Color(245, 255, 235, 255)
            );

            // Pequeno detalhe inferior
            Raylib.DrawRectangle(
                (int)t.X,
                (int)(t.Y + t.Height - 2),
                (int)t.Width,
                2,
                new Color(105, 170, 150, 255)
            );
        }
    }

    private static void DrawCloud(float x, float y)
    {
        Color cloud = new Color(245, 250, 255, 230);

        Raylib.DrawCircle((int)x, (int)y, 7, cloud);
        Raylib.DrawCircle((int)x + 7, (int)y - 3, 9, cloud);
        Raylib.DrawCircle((int)x + 16, (int)y, 7, cloud);

        Raylib.DrawRectangle(
            (int)x - 2,
            (int)y,
            20,
            7,
            cloud
        );
    }

    private static void DrawMountain(
        float x,
        float y,
        float width,
        float height)
    {
        Vector2 top = new Vector2(x + width / 2f, y - height);
        Vector2 left = new Vector2(x, y);
        Vector2 right = new Vector2(x + width, y);

        Raylib.DrawTriangle(
            top,
            left,
            right,
            new Color(175, 215, 205, 255)
        );

        // Pico iluminado
        Vector2 snowTop = new Vector2(
            top.X,
            top.Y
        );

        Vector2 snowLeft = new Vector2(
            top.X - width * 0.16f,
            top.Y + height * 0.25f
        );

        Vector2 snowRight = new Vector2(
            top.X + width * 0.16f,
            top.Y + height * 0.25f
        );

        Raylib.DrawTriangle(
            snowTop,
            snowLeft,
            snowRight,
            new Color(235, 250, 245, 255)
        );
    }

    // =========================================================
    // FASE GRANDE DE TESTE
    // =========================================================

    public static Level TestRoom()
    {
        const int W = 120;
        const int H = 45;

        var g = new int[W, H];

        // -----------------------------------------------------
        // CHÃO
        // -----------------------------------------------------

        for (int x = 0; x < W; x++)
        {
            g[x, H - 1] = 1;
            g[x, H - 2] = 1;
            g[x, H - 3] = 1;
        }

        // -----------------------------------------------------
        // PAREDES LATERAIS
        // -----------------------------------------------------

        for (int y = 0; y < H; y++)
        {
            g[0, y] = 1;
            g[W - 1, y] = 1;
        }

        // -----------------------------------------------------
        // FUNÇÃO PARA PLATAFORMAS
        // -----------------------------------------------------

        void Plat(int x, int y, int len)
        {
            for (int i = 0; i < len; i++)
            {
                if (x + i >= 0 && x + i < W &&
                    y >= 0 && y < H)
                {
                    g[x + i, y] = 1;
                }
            }
        }

        // -----------------------------------------------------
        // ÁREA INICIAL
        // -----------------------------------------------------

        Plat(7, 35, 10);
        Plat(21, 31, 7);
        Plat(32, 35, 6);

        // -----------------------------------------------------
        // PRIMEIRA SEÇÃO
        // -----------------------------------------------------

        Plat(43, 29, 9);
        Plat(56, 34, 6);
        Plat(66, 27, 8);

        // -----------------------------------------------------
        // SEÇÃO CENTRAL
        // -----------------------------------------------------

        Plat(78, 23, 7);
        Plat(89, 29, 6);
        Plat(99, 20, 9);

        // -----------------------------------------------------
        // SEÇÃO ALTA
        // -----------------------------------------------------

        Plat(108, 14, 7);
        Plat(96, 10, 6);
        Plat(82, 15, 7);
        Plat(69, 9, 6);

        // -----------------------------------------------------
        // PLATAFORMAS PEQUENAS
        // -----------------------------------------------------

        Plat(14, 26, 4);
        Plat(27, 22, 4);
        Plat(39, 17, 5);
        Plat(53, 21, 4);
        Plat(62, 15, 3);
        Plat(75, 19, 4);
        Plat(87, 12, 4);
        Plat(101, 6, 4);

        // -----------------------------------------------------
        // PAREDES PARA WALL JUMP
        // -----------------------------------------------------

        // Parede 1
        for (int y = 18; y < 34; y++)
            g[36, y] = 1;

        // Parede 2
        for (int y = 11; y < 27; y++)
            g[61, y] = 1;

        // Parede 3
        for (int y = 17; y < 31; y++)
            g[91, y] = 1;

        // -----------------------------------------------------
        // TORRES
        // -----------------------------------------------------

        for (int y = 25; y < 35; y++)
            g[48, y] = 1;

        for (int y = 18; y < 29; y++)
            g[72, y] = 1;

        for (int y = 8; y < 21; y++)
            g[104, y] = 1;

        // -----------------------------------------------------
        // PEQUENOS BLOCOS / OBSTÁCULOS
        // -----------------------------------------------------

        Plat(18, 37, 2);
        Plat(25, 37, 3);

        Plat(58, 37, 2);
        Plat(63, 37, 2);

        Plat(84, 37, 3);
        Plat(93, 37, 2);

        // -----------------------------------------------------
        // ÁREA FINAL
        // -----------------------------------------------------

        Plat(108, 25, 9);
        Plat(101, 31, 5);

        // Parede final
        for (int y = 5; y < 25; y++)
            g[116, y] = 1;

        // Plataforma final
        Plat(105, 5, 10);

        return new Level(g);
    }
}