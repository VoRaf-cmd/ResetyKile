using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

public class DashTrail
{
    private struct Ghost
    {
        public SpriteSheet? Sheet;
        public int Frame;
        public Vector2 Position;
        public bool FlipX;
        public Color Color;
        public float Life;
        public float MaxLife;
    }

    private const int MaxGhosts = 48;
    private readonly Ghost[] _pool = new Ghost[MaxGhosts];
    private int _next;

    public void Emit(SpriteSheet sheet, int frame, Vector2 center, bool flipX, Color color, float life = 0.35f)
    {
        ref var g = ref _pool[_next];
        _next = (_next + 1) % MaxGhosts;

        g.Sheet    = sheet;
        g.Frame    = frame;
        g.Position = center;
        g.FlipX    = flipX;
        g.Color    = color;
        g.Life     = life;
        g.MaxLife  = life;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < MaxGhosts; i++)
        {
            ref var g = ref _pool[i];
            if (g.Sheet == null) continue;
            if (g.Life <= 0f) continue;
            g.Life -= dt;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < MaxGhosts; i++)
        {
            ref var g = ref _pool[i];
            if (g.Sheet == null) continue;
            if (g.Life <= 0f) continue;

            float alpha = g.Life / g.MaxLife;

            var topLeft = new Vector2(
                g.Position.X - g.Sheet.FrameWidth / 2f,
                g.Position.Y - g.Sheet.FrameHeight / 2f);

            g.Sheet.DrawTinted(g.Frame, topLeft, g.FlipX, g.Color, alpha);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < MaxGhosts; i++)
            _pool[i].Sheet = null;
    }
}