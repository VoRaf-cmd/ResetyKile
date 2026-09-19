using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Textos flutuantes que aparecem, sobem e desaparecem.
/// Pool fixo, sem alocação por frame.
/// </summary>
public class FloatingText
{
    private struct Entry
    {
        public string Text;
        public Vector2 Position;
        public float Life;
        public float MaxLife;
        public Color Color;
        public bool Active;
    }

    private const int MaxEntries = 16;
    private readonly Entry[] _pool = new Entry[MaxEntries];
    private int _next;

    public void Spawn(string text, Vector2 position, Color color, float life = 0.6f)
    {
        ref var e = ref _pool[_next];
        _next = (_next + 1) % MaxEntries;

        e.Text = text;
        e.Position = position;
        e.Life = life;
        e.MaxLife = life;
        e.Color = color;
        e.Active = true;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            ref var e = ref _pool[i];
            if (!e.Active) continue;

            e.Life -= dt;
            // Sobe devagar (com easing)
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

            int fontSize = 10;
            int w = Raylib.MeasureText(e.Text, fontSize);

            int x = (int)(e.Position.X - w / 2f);
            int y = (int)(e.Position.Y - fontSize / 2f);

            // Sombra pra ficar legível em fundo escuro
            Raylib.DrawText(e.Text, x + 1, y + 1, fontSize,
                new Color((byte)0, (byte)0, (byte)0, alpha));
            // Texto
            Raylib.DrawText(e.Text, x, y, fontSize, c);
        }
    }
}