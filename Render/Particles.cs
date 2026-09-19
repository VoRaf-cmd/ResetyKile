using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Sistema simples de partículas. Pool fixo, sem alocação por frame.
/// Cada partícula é um retângulo 1x1 ou 2x2 que sai voando e desvanece.
/// </summary>
public class Particles
{
    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;         // segundos restantes
        public float MaxLife;
        public Color Color;
        public float Size;         // 1, 2 ou 3 px
        public float Gravity;
        public bool Active;
    }

    private const int MaxParticles = 512;
    private readonly Particle[] _pool = new Particle[MaxParticles];
    private readonly Random _rng = new();
    private int _next;

    /// Emite N partículas em leque a partir de uma posição.
    public void Burst(Vector2 origin, int count, Color color,
                      float speedMin, float speedMax, float life,
                      float gravity = 200f, float size = 2f, float spreadRadians = MathF.PI * 2f)
    {
        for (int i = 0; i < count; i++)
        {
            ref var p = ref _pool[_next];
            _next = (_next + 1) % MaxParticles;

            float angle = RandomRange(0f, spreadRadians);
            float speed = RandomRange(speedMin, speedMax);

            p.Position = origin;
            p.Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
            p.MaxLife = life * RandomRange(0.7f, 1.3f);
            p.Life = p.MaxLife;
            p.Color = color;
            p.Size = size;
            p.Gravity = gravity;
            p.Active = true;
        }
    }

    /// Emite uma única partícula (pra trail, poeira, etc).
    public void Emit(Vector2 origin, Vector2 velocity, Color color, float life,
                     float gravity = 0f, float size = 2f)
    {
        ref var p = ref _pool[_next];
        _next = (_next + 1) % MaxParticles;

        p.Position = origin;
        p.Velocity = velocity;
        p.MaxLife = life;
        p.Life = life;
        p.Color = color;
        p.Size = size;
        p.Gravity = gravity;
        p.Active = true;
    }

    public void Update(float dt)
    {
        for (int i = 0; i < MaxParticles; i++)
        {
            ref var p = ref _pool[i];
            if (!p.Active) continue;

            p.Life -= dt;
            if (p.Life <= 0f)
            {
                p.Active = false;
                continue;
            }

            p.Velocity.Y += p.Gravity * dt;
            p.Position += p.Velocity * dt;
        }
    }

    public void Draw()
    {
        for (int i = 0; i < MaxParticles; i++)
        {
            ref var p = ref _pool[i];
            if (!p.Active) continue;

            // Fade: alpha cai conforme a vida acaba
            float t = p.Life / p.MaxLife;
            byte alpha = (byte)(255 * t);

            var c = new Color(p.Color.R, p.Color.G, p.Color.B, alpha);

            Raylib.DrawRectangle(
                (int)p.Position.X,
                (int)p.Position.Y,
                (int)p.Size,
                (int)p.Size,
                c);
        }
    }

    private float RandomRange(float min, float max)
        => min + (float)_rng.NextDouble() * (max - min);
}