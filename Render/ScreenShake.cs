using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Screen shake baseado em "trauma" (0..1).
/// Eventos adicionam trauma; ele decai sozinho ao longo do tempo.
/// O offset aplicado é trauma^2 pra dar curva de queda mais natural.
/// </summary>
public class ScreenShake
{
    private const float DecayRate = 1.8f;   // trauma perdido por segundo
    private const float MaxOffset = 6f;     // pixels de deslocamento máximo
    private const float MaxRoll   = 0.04f;  // radianos de rotação máxima

    private readonly Random _rng = new();

    public float Trauma { get; private set; }

    public void AddTrauma(float amount)
        => Trauma = Math.Clamp(Trauma + amount, 0f, 1f);

    public void Update(float dt)
        => Trauma = MathF.Max(0f, Trauma - DecayRate * dt);

    public Vector2 GetOffset()
    {
        if (Trauma <= 0f) return Vector2.Zero;
        float amount = Trauma * Trauma;
        float ox = RandomRange(-1f, 1f) * MaxOffset * amount;
        float oy = RandomRange(-1f, 1f) * MaxOffset * amount;
        return new Vector2(ox, oy);
    }

    public float GetRoll()
    {
        if (Trauma <= 0f) return 0f;
        float amount = Trauma * Trauma;
        return RandomRange(-1f, 1f) * MaxRoll * amount;
    }

    private float RandomRange(float min, float max)
        => min + (float)_rng.NextDouble() * (max - min);
}