using Raylib_cs;
using ResetyKile.Entities;

namespace ResetyKile.UI;

public static class Hud
{
    private static readonly Color Outline     = new(15, 12, 22, 255);
    private static readonly Color HeartFull   = new(230, 60, 90, 255);
    private static readonly Color HeartShield = new(210, 220, 240, 255);   // prata
    private static readonly Color HeartShieldDark = new(140, 150, 175, 255); // prata escuro (borda animada)
    private static readonly Color HeartEmpty  = new(70, 55, 75, 255);
    private static readonly Color SoulFull    = new(255, 210, 90, 255);
    private static readonly Color SoulEmpty   = new(70, 55, 75, 255);
    private static readonly Color SoulReady   = new(255, 90, 180, 255);
    private static readonly Color SuperBarBg  = new(40, 30, 50, 255);
    private static readonly Color SuperBarFg  = new(255, 90, 180, 255);

    private const int SlotSize  = 5;
    private const int SlotGap   = 1;
    private const int RowHeight = 6;

    public static void Draw(Player player, float time)
    {
        int x = 4;
        int y = 4;

        DrawHeartRow(x, y, player.Hp, Player.MaxHp, player.Shield, time);

        int soulsY = y + RowHeight + 2;
        DrawSoulRow(x, soulsY, player.Souls, Player.MaxSouls, player.SuperReady);

        if (player.State == PlayerState.Super)
        {
            int superY = soulsY + RowHeight + 3;
            int superW = 60;
            int superH = 3;
            float t = player.SuperTimer / 8f;

            Raylib.DrawRectangle(x - 1, superY - 1, superW + 2, superH + 2, Outline);
            Raylib.DrawRectangle(x, superY, superW, superH, SuperBarBg);
            Raylib.DrawRectangle(x, superY, (int)(superW * t), superH, SuperBarFg);
        }
    }

    private static void DrawHeartRow(int x, int y, int hp, int maxHp, bool shield, float time)
    {
        for (int i = 0; i < maxHp; i++)
        {
            int hx = x + i * (SlotSize + SlotGap);
            bool full = i < hp;

            Color color;
            if (full && shield && i == hp - 1)
            {
                // Último coração cheio + escudo → prata com pulsação
                // pulsa entre prata e prata escuro
                float pulse = (MathF.Sin(time * 6f) + 1f) * 0.5f; // 0..1
                color = LerpColor(HeartShieldDark, HeartShield, pulse);
            }
            else if (full)
            {
                color = HeartFull;
            }
            else
            {
                color = HeartEmpty;
            }

            DrawPixelHeart(hx, y, SlotSize, color);
        }
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        byte r = (byte)Math.Clamp(a.R + (b.R - a.R) * t, 0f, 255f);
        byte g = (byte)Math.Clamp(a.G + (b.G - a.G) * t, 0f, 255f);
        byte bl = (byte)Math.Clamp(a.B + (b.B - a.B) * t, 0f, 255f);
        return new Color(r, g, bl, (byte)255);
    }

    private static readonly string[] HeartMask =
    {
        "01101",
        "11111",
        "11111",
        "01110",
        "00100",
    };

    private static void DrawPixelHeart(int x, int y, int size, Color color)
    {
        Raylib.DrawRectangle(x - 1, y - 1, size + 2, size + 2, Outline);

        float px = size / 5f;
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 5; col++)
                if (HeartMask[row][col] == '1')
                    Raylib.DrawRectangle(
                        (int)(x + col * px),
                        (int)(y + row * px),
                        (int)MathF.Ceiling(px),
                        (int)MathF.Ceiling(px),
                        color);
    }

    private static void DrawSoulRow(int x, int y, int souls, int maxSouls, bool superReady)
    {
        for (int i = 0; i < maxSouls; i++)
        {
            int sx = x + i * (SlotSize + SlotGap);
            bool full = i < souls;

            Color color = full
                ? (superReady ? SoulReady : SoulFull)
                : SoulEmpty;

            Raylib.DrawRectangle(sx - 1, y - 1, SlotSize + 2, SlotSize + 2, Outline);
            Raylib.DrawRectangle(sx, y, SlotSize, SlotSize, color);

            if (full)
                Raylib.DrawRectangle(sx, y, SlotSize, 1, new Color(255, 255, 255, 140));
        }
    }
}