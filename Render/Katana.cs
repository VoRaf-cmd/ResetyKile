using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

public class Katana
{
    public bool  Enabled  { get; set; }
    public Vector2 Position { get; set; }
    public float Progress { get; set; }
    public float Alpha    { get; set; } = 1f;
    public int   Facing   { get; set; } = 1;

    private const int MaxLength = 22;

    private static readonly Color BladeLeft  = new((byte)130, (byte)130, (byte)140, (byte)255);
    private static readonly Color BladeRight = new((byte)245, (byte)250, (byte)255, (byte)255);
    private static readonly Color Outline    = new((byte)20, (byte)15, (byte)25, (byte)255);

    public void Draw(Vector2 playerCenter)
    {
        if (!Enabled || Alpha <= 0.01f || Progress <= 0f) return;

        int length = (int)(MaxLength * Math.Clamp(Progress, 0f, 1f));
        if (length <= 0) return;

        int startX = (int)MathF.Floor(playerCenter.X + Position.X * Facing);
        int startY = (int)MathF.Floor(playerCenter.Y + Position.Y);

        Color outlineColor = ApplyAlpha(Outline, Alpha);

        // Borda: desenha a "linha preta" antes (atrás e em cima/embaixo)
        for (int i = -1; i <= length; i++)
        {
            int x = startX + (Facing > 0 ? i : -i);
            Raylib.DrawRectangle(x, startY - 1, 1, 1, outlineColor);
            Raylib.DrawRectangle(x, startY + 1, 1, 1, outlineColor);
        }

        // Miolo: gradiente
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / (MaxLength - 1);
            Color c = LerpColor(BladeLeft, BladeRight, t);
            c = ApplyAlpha(c, Alpha);

            int x = startX + (Facing > 0 ? i : -i);
            Raylib.DrawRectangle(x, startY, 1, 1, c);
        }
    }

    private static Color ApplyAlpha(Color c, float a)
        => new Color(c.R, c.G, c.B, (byte)(c.A * a));

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (byte)Math.Clamp(a.R + (b.R - a.R) * t, 0, 255),
            (byte)Math.Clamp(a.G + (b.G - a.G) * t, 0, 255),
            (byte)Math.Clamp(a.B + (b.B - a.B) * t, 0, 255),
            (byte)255);
    }
}