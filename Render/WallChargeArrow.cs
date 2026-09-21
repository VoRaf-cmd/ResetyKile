using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Indicador flutuante acima do Kile durante o carregamento do wall jump.
/// Mostra o botão de pulo + barra de carga.
/// </summary>
public class WallChargeArrow
{
    public bool Enabled { get; set; }
    public float Charge { get; set; }
    public string ButtonLabel { get; set; } = "SPACE";

    // Fade
    private float _fadeAlpha = 0f;

    // Cores por estágio de carga
    private static readonly Color ColorLow  = new((byte)100, (byte)220, (byte)130, (byte)255);
    private static readonly Color ColorMid  = new((byte)240, (byte)220, (byte)80, (byte)255);
    private static readonly Color ColorHigh = new((byte)240, (byte)100, (byte)60, (byte)255);

    private static readonly Color BgColor      = new((byte)20, (byte)20, (byte)28, (byte)255);
    private static readonly Color BarBgColor   = new((byte)60, (byte)60, (byte)75, (byte)255);
    private static readonly Color BorderColor  = new((byte)15, (byte)12, (byte)22, (byte)255);
    private static readonly Color TextColor    = new((byte)240, (byte)240, (byte)250, (byte)255);

    // Layout
private const int ButtonPaddingX = 2;
private const int ButtonPaddingY = 1;
private const int Gap            = 1;
private const int BarWidth       = 12;
private const int BarHeight      = 4;
private const int VerticalOffset = 16;  // distância acima do Kile

    public void UpdateFade(float dt)
    {
        const float FadeSpeed = 6f;
        float target = Enabled ? 1f : 0f;

        if (_fadeAlpha < target)
            _fadeAlpha = MathF.Min(_fadeAlpha + dt * FadeSpeed, target);
        else if (_fadeAlpha > target)
            _fadeAlpha = MathF.Max(_fadeAlpha - dt * FadeSpeed, target);
    }

    public void Draw(Vector2 playerCenter, float time)
    {
        if (_fadeAlpha <= 0.01f) return;

        byte alpha = (byte)(255 * _fadeAlpha);

        // Medir o texto do botão
        int fontSize = 8;
        int textW = Raylib.MeasureText(ButtonLabel, fontSize);
        int buttonW = textW + ButtonPaddingX * 2;
        int buttonH = fontSize + ButtonPaddingY * 2;

        // Largura total: botão + gap + barra
        int totalW = buttonW + Gap + BarWidth;
        int totalH = Math.Max(buttonH, BarHeight);

        // Posição central (acima do Kile)
        int centerX = (int)MathF.Floor(playerCenter.X);
        int baseY   = (int)MathF.Floor(playerCenter.Y) - VerticalOffset - totalH;
        int startX  = centerX - totalW / 2;

        // ---- Botão (retângulo com texto) ----
        int btnY = baseY + (totalH - buttonH) / 2;

        Color border = new Color(BorderColor.R, BorderColor.G, BorderColor.B, alpha);
        Color bg     = new Color(BgColor.R,     BgColor.G,     BgColor.B,     alpha);
        Color text   = new Color(TextColor.R,   TextColor.G,   TextColor.B,   alpha);

        // Contorno do botão
        Raylib.DrawRectangle(startX - 1, btnY - 1, buttonW + 2, buttonH + 2, border);
        // Fundo
        Raylib.DrawRectangle(startX, btnY, buttonW, buttonH, bg);
        // Texto
        Raylib.DrawText(ButtonLabel, startX + ButtonPaddingX, btnY + ButtonPaddingY, fontSize, text);

        // ---- Barra de carga ----
        int barX = startX + buttonW + Gap;
        int barY = baseY + (totalH - BarHeight) / 2;

        // Cor da carga (pulsa)
        float pulse = MathF.Sin(time * 10f) * 0.5f + 0.5f;
        Color fillColor = LerpColorByCharge(Charge, pulse);

        Color barBg     = new Color(BarBgColor.R, BarBgColor.G, BarBgColor.B, alpha);
        Color barFill   = new Color(fillColor.R,  fillColor.G,  fillColor.B,  alpha);

        // Contorno da barra
        Raylib.DrawRectangle(barX - 1, barY - 1, BarWidth + 2, BarHeight + 2, border);
        // Fundo da barra
        Raylib.DrawRectangle(barX, barY, BarWidth, BarHeight, barBg);

        // Preenchimento (mínimo 1/3)
        float visibleFrac = 0.33f + 0.67f * Math.Clamp(Charge, 0f, 1f);
        int fillW = (int)(BarWidth * visibleFrac);
        if (fillW > 0)
            Raylib.DrawRectangle(barX, barY, fillW, BarHeight, barFill);
    }

    private static Color LerpColorByCharge(float charge, float pulse)
    {
        Color baseColor;
        if (charge < 0.5f)
            baseColor = LerpColor(ColorLow, ColorMid, charge * 2f);
        else
            baseColor = LerpColor(ColorMid, ColorHigh, (charge - 0.5f) * 2f);

        // Aplica pulso no brilho
        byte r = (byte)Math.Clamp(baseColor.R + 30 * pulse, 0, 255);
        byte g = (byte)Math.Clamp(baseColor.G + 30 * pulse, 0, 255);
        byte b = (byte)Math.Clamp(baseColor.B + 30 * pulse, 0, 255);

        return new Color(r, g, b, (byte)255);
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        return new Color(
            (byte)Math.Clamp(a.R + (b.R - a.R) * t, 0, 255),
            (byte)Math.Clamp(a.G + (b.G - a.G) * t, 0, 255),
            (byte)Math.Clamp(a.B + (b.B - a.B) * t, 0, 255),
            (byte)255);
    }
}