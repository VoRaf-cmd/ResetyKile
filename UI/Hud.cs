using Raylib_cs;
using ResetyKile.Entities;

namespace ResetyKile.UI;

public static class Hud
{
    private static readonly Color Outline      = new((byte)15, (byte)12, (byte)22, (byte)255);
    private static readonly Color HeartFull    = new((byte)230, (byte)60, (byte)90, (byte)255);
    private static readonly Color HeartShield  = new((byte)210, (byte)220, (byte)240, (byte)255);
    private static readonly Color HeartShieldDark = new((byte)140, (byte)150, (byte)175, (byte)255);
    private static readonly Color HeartEmpty   = new((byte)70, (byte)55, (byte)75, (byte)255);

    private static readonly Color BoltFull     = new((byte)255, (byte)230, (byte)100, (byte)255);
    private static readonly Color BoltEmpty    = new((byte)70, (byte)60, (byte)50, (byte)255);
    private static readonly Color BoltCharging = new((byte)200, (byte)180, (byte)80, (byte)255);

    private static readonly Color SoulFull     = new((byte)255, (byte)210, (byte)90, (byte)255);
    private static readonly Color SoulEmpty    = new((byte)70, (byte)55, (byte)75, (byte)255);
    private static readonly Color SoulReady    = new((byte)255, (byte)90, (byte)180, (byte)255);
    private static readonly Color SuperBarBg   = new((byte)40, (byte)30, (byte)50, (byte)255);
    private static readonly Color SuperBarFg   = new((byte)255, (byte)90, (byte)180, (byte)255);

    private const int SlotSize  = 5;
    private const int SlotGap   = 1;
    private const int RowHeight = 8;

    public static void Draw(Player player, float time)
    {
        int x = 4;
        int y = 4;

        DrawHeartRow(x, y, player.Hp, Player.MaxHp, player.Shield, time);

        int staminaY = y + RowHeight + 1;
        DrawStaminaRow(x, staminaY, player.Stamina, Player.MaxStamina, time);

        int soulsY = staminaY + RowHeight + 1;
        DrawSoulRow(x, soulsY, player.Souls, Player.MaxSouls, player.SuperReady);

        if (player.State == PlayerState.Super)
        {
            int superY = soulsY + RowHeight + 2;
            int superW = 60;
            int superH = 3;
            float t = player.SuperTimer / 8f;

            Raylib.DrawRectangle(x - 1, superY - 1, superW + 2, superH + 2, Outline);
            Raylib.DrawRectangle(x, superY, superW, superH, SuperBarBg);
            Raylib.DrawRectangle(x, superY, (int)(superW * t), superH, SuperBarFg);
        }
    }

    // ============================================================
    // VIDA
    // ============================================================
    private static void DrawHeartRow(int x, int y, int hp, int maxHp, bool shield, float time)
    {
        int hearts = maxHp / 4;

        for (int i = 0; i < hearts; i++)
        {
            int hx = x + i * (SlotSize + SlotGap);
            int hpNoCoracao = hp - (i * 4);

            bool isTopHeart = (i == hearts - 1 && shield && hpNoCoracao > 0);
            Color fillColor;

            if (isTopHeart)
            {
                float pulse = (MathF.Sin(time * 6f) + 1f) * 0.5f;
                fillColor = LerpColor(HeartShieldDark, HeartShield, pulse);
            }
            else
            {
                fillColor = HeartFull;
            }

            DrawHeartWithFill(hx, y, SlotSize, hpNoCoracao, fillColor);
        }
    }

    private static readonly string[] HeartMask =
    {
        "01101",
        "11111",
        "11111",
        "01110",
        "00100",
    };

    private static void DrawHeartWithFill(int x, int y, int size, int fill, Color fullColor)
    {
        Raylib.DrawRectangle(x - 1, y - 1, size + 2, size + 2, Outline);

        DrawHeartMask(x, y, size, HeartEmpty, 5);

        if (fill > 0)
        {
            int colsToFill = fill switch
            {
                1 => 1,
                2 => 3,
                3 => 4,
                4 => 5,
                _ => 5,
            };
            DrawHeartMaskPartialCols(x, y, size, fullColor, colsToFill);
        }
    }

    private static void DrawHeartMaskPartialCols(int x, int y, int size, Color color, int cols)
    {
        float px = size / 5f;
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < cols && col < 5; col++)
                if (HeartMask[row][col] == '1')
                    Raylib.DrawRectangle(
                        (int)(x + col * px),
                        (int)(y + row * px),
                        (int)MathF.Ceiling(px),
                        (int)MathF.Ceiling(px),
                        color);
    }

    private static void DrawHeartMask(int x, int y, int size, Color color, int lines)
    {
        float px = size / 5f;
        for (int row = 0; row < lines && row < 5; row++)
            for (int col = 0; col < 5; col++)
                if (HeartMask[row][col] == '1')
                    Raylib.DrawRectangle(
                        (int)(x + col * px),
                        (int)(y + row * px),
                        (int)MathF.Ceiling(px),
                        (int)MathF.Ceiling(px),
                        color);
    }

    // ============================================================
    // STAMINA (raios) — tremida mais visível
    // ============================================================
    private static readonly string[] BoltMask =
    {
        "00110",
        "01100",
        "11111",
        "00110",
        "01100",
    };

    private static void DrawStaminaRow(int x, int y, float stamina, int maxStamina, float time)
    {
        for (int i = 0; i < maxStamina; i++)
        {
            int bx = x + i * (SlotSize + SlotGap);
            float value = stamina - i;

            Color color;
            bool charging = false;
            if (value >= 1f)
            {
                color = BoltFull;
            }
            else if (value > 0f)
            {
                color = BoltCharging;
                charging = true;
            }
            else
            {
                color = BoltEmpty;
            }

            // Tremida mais visível: 2px, horizontal + vertical,
            // frequência diferente por raio (i) pra não sincronizar
            int shakeX = 0;
            int shakeY = 0;
            if (charging)
            {
                float phase = time * 40f + i * 1.7f;
                shakeX = (int)(MathF.Sin(phase) * 1.5f);
                shakeY = (int)(MathF.Cos(phase * 1.3f) * 1f);
            }

            Raylib.DrawRectangle(bx - 1, y - 1, SlotSize + 2, SlotSize + 2, Outline);
            DrawBoltMask(bx + shakeX, y + shakeY, SlotSize, BoltEmpty);

            if (value > 0f)
            {
                float fillFrac = Math.Clamp(value, 0f, 1f);
                DrawBoltMaskPartialHorizontal(bx + shakeX, y + shakeY, SlotSize, color, fillFrac);
            }
        }
    }

    private static void DrawBoltMask(int x, int y, int size, Color color)
    {
        float px = size / 5f;
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 5; col++)
                if (BoltMask[row][col] == '1')
                    Raylib.DrawRectangle(
                        (int)(x + col * px),
                        (int)(y + row * px),
                        (int)MathF.Ceiling(px),
                        (int)MathF.Ceiling(px),
                        color);
    }

    private static void DrawBoltMaskPartialHorizontal(int x, int y, int size, Color color, float fillFrac)
    {
        float px = size / 5f;
        float maxX = x + size * fillFrac;

        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 5; col++)
            {
                if (BoltMask[row][col] != '1') continue;

                float cellX = x + col * px;
                float cellRight = cellX + px;
                if (cellX > maxX) continue;

                float visible = MathF.Min(cellRight, maxX) - cellX;
                if (visible <= 0f) continue;

                int drawW = (int)MathF.Ceiling(visible);
                Raylib.DrawRectangle(
                    (int)cellX,
                    (int)(y + row * px),
                    drawW,
                    (int)MathF.Ceiling(px),
                    color);
            }
    }

    // ============================================================
    // SOULS
    // ============================================================
    private static void DrawSoulRow(int x, int y, int souls, int maxSouls, bool superReady)
    {
        int quads = maxSouls / 2;

        for (int i = 0; i < quads; i++)
        {
            int sx = x + i * (SlotSize + SlotGap);
            int soulsNoQuad = souls - (i * 2);

            Color color = superReady ? SoulReady : SoulFull;

            Raylib.DrawRectangle(sx - 1, y - 1, SlotSize + 2, SlotSize + 2, Outline);
            Raylib.DrawRectangle(sx, y, SlotSize, SlotSize, SoulEmpty);

            if (soulsNoQuad >= 2)
            {
                Raylib.DrawRectangle(sx, y, SlotSize, SlotSize, color);
                Raylib.DrawRectangle(sx, y, SlotSize, 1, new Color((byte)255, (byte)255, (byte)255, (byte)140));
            }
            else if (soulsNoQuad == 1)
            {
                int halfW = SlotSize / 2;
                Raylib.DrawRectangle(sx, y, halfW, SlotSize, color);
                Raylib.DrawRectangle(sx, y, halfW, 1, new Color((byte)255, (byte)255, (byte)255, (byte)140));
            }
        }
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        byte r = (byte)Math.Clamp(a.R + (b.R - a.R) * t, 0f, 255f);
        byte g = (byte)Math.Clamp(a.G + (b.G - a.G) * t, 0f, 255f);
        byte bl = (byte)Math.Clamp(a.B + (b.B - a.B) * t, 0f, 255f);
        return new Color(r, g, bl, (byte)255);
    }
}