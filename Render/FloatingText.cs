using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

public enum FloatingIcon { None, Bolt }

public class FloatingText
{
    private struct Entry
    {
        public string Text;
        public Vector2 Position;
        public float Life;
        public float MaxLife;
        public Color Color;
        public FloatingIcon Icon;
        public bool Active;
    }

    private const int MaxEntries = 16;
    private readonly Entry[] _pool = new Entry[MaxEntries];
    private int _next;

    public void Spawn(string text, Vector2 position, Color color, float life = 0.6f,
                      FloatingIcon icon = FloatingIcon.None)
    {
        ref var e = ref _pool[_next];
        _next = (_next + 1) % MaxEntries;

        e.Text = text;
        e.Position = position;
        e.Life = life;
        e.MaxLife = life;
        e.Color = color;
        e.Icon = icon;
        e.Active = true;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            ref var e = ref _pool[i];
            if (!e.Active) continue;

            e.Life -= dt;
            e.Position.Y -= 20f * dt;

            if (e.Life <= 0f) e.Active = false;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            ref var e = ref _pool[i];
            if (!e.Active) continue;

            float t = e.Life / e.MaxLife;
            byte alpha = (byte)(255 * t);

            var c = new Color(e.Color.R, e.Color.G, e.Color.B, alpha);
            var shadowColor = new Color((byte)0, (byte)0, (byte)0, alpha);

            int fontSize = 8;
            int iconSize = 7;
            int gap = 2;
            int iconW = e.Icon == FloatingIcon.None ? 0 : iconSize + gap;

            int textW = Raylib.MeasureText(e.Text, fontSize);
            int totalW = textW + iconW;

            int x = (int)(e.Position.X - totalW / 2f);
            int y = (int)(e.Position.Y - fontSize / 2f);

            // Sombra
            Raylib.DrawText(e.Text, x + 1 + iconW, y + 1, fontSize, shadowColor);
            // Texto
            Raylib.DrawText(e.Text, x + iconW, y, fontSize, c);

            // Ícone (raio)
            if (e.Icon == FloatingIcon.Bolt)
            {
                int ix = x;
                int iy = y + (fontSize - iconSize) / 2;
                DrawBolt(ix, iy, iconSize, c, shadowColor);
            }
        }
    }

    private static void DrawBolt(int x, int y, int size, Color color, Color shadow)
    {
        // Máscara 5x5 de raio
        string[] mask =
        {
            "00110",
            "01100",
            "11111",
            "00110",
            "01100",
        };

        float px = size / 5f;

        // Sombra
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 5; col++)
                if (mask[row][col] == '1')
                    Raylib.DrawRectangle(
                        (int)(x + col * px) + 1,
                        (int)(y + row * px) + 1,
                        (int)MathF.Ceiling(px),
                        (int)MathF.Ceiling(px),
                        shadow);

        // Cor
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 5; col++)
                if (mask[row][col] == '1')
                    Raylib.DrawRectangle(
                        (int)(x + col * px),
                        (int)(y + row * px),
                        (int)MathF.Ceiling(px),
                        (int)MathF.Ceiling(px),
                        color);
    }
}