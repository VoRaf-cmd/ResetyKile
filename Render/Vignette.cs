using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Vinheta radial — escurece as bordas da tela pra dar foco no centro.
/// Desenhada DEPOIS do jogo e ANTES do HUD.
/// </summary>
public static class Vignette
{
    /// Intensidade: 0 = nada, 1 = muito escuro
    public const float Intensity = 0.45f;

    /// Raio do círculo claro (0..1) — quanto maior, menos borda escura
    private const float InnerRadius = 0.55f;

    /// Quantos anéis desenhar (mais = mais suave, mais lento)
    private const int Rings = 24;

    public static void Draw()
    {
        int w = Core.Renderer.InternalW;
        int h = Core.Renderer.InternalH;

        var center = new Vector2(w / 2f, h / 2f);

        // Distância máxima (canto da tela)
        float maxRadius = MathF.Sqrt(center.X * center.X + center.Y * center.Y);

        // Raio interno (onde ainda tá "claro")
        float innerPx = maxRadius * InnerRadius;

        // Desenha anéis do centro pra fora, cada vez mais escuros
        for (int i = 0; i < Rings; i++)
        {
            float t = (float)i / Rings;             // 0..1
            float tNext = (float)(i + 1) / Rings;

            float radius = innerPx + (maxRadius - innerPx) * t;
            float radiusNext = innerPx + (maxRadius - innerPx) * tNext;

            // Alpha cresce com t (0 no centro, Intensity na borda)
            float alpha = Intensity * t * t;        // curva suave

            byte a = (byte)(alpha * 255);

            // Desenha "anel" — desenha o círculo maior, e o menor por baixo
            // Mais fácil: desenha um círculo grande com alpha, e a "máscara" por cima
            // Truque: desenha um círculo grande com a cor, sobrepondo por cima do anterior
            Raylib.DrawRing(
                center,
                radius,
                radiusNext,
                0f, 360f,
                64,
                new Color((byte)0, (byte)0, (byte)0, a));
        }
    }
}