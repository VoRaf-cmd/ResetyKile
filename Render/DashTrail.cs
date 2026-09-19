using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Rastro do dash: cada "fantasma" é um snapshot da posição/tamanho
/// do player que desvanece em ~0.3s.
/// </summary>
public class DashTrail
{
    private struct Ghost
    {
        public Vector2 Position;
        public Vector2 Size;
        public float Life;
        public float MaxLife;
        public Color Color;
    }

    private const int MaxGhosts = 32;
    private readonly Ghost[] _pool = new Ghost[MaxGhosts];
    private int _next;

    public void Emit(Vector2 pos, Vector2 size, Color color, float life = 0.3f)
    {
        ref var g = ref _pool[_next];
        _next = (_next + 1) % MaxGhosts;

        g.Position = pos;
        g.Size = size;
        g.Life = life;
        g.MaxLife = life;
        g.Color = color;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < MaxGhosts; i++)
        {
            ref var g = ref _pool[i];
            if (g.Life <= 0f) continue;
            g.Life -= dt;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < MaxGhosts; i++)
        {
            ref var g = ref _pool[i];
            if (g.Life <= 0f) continue;

            float t = g.Life / g.MaxLife;
            byte alpha = (byte)(120 * t);

            var rect = new Rectangle(
                g.Position.X - g.Size.X / 2f,
                g.Position.Y - g.Size.Y / 2f,
                g.Size.X,
                g.Size.Y);

            var c = new Color(g.Color.R, g.Color.G, g.Color.B, alpha);
            Raylib.DrawRectangleRec(rect, c);
        }
    }
}