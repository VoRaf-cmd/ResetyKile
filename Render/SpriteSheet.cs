using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Spritesheet de UMA linha (N frames lado a lado).
/// Se o arquivo não existir, gera um placeholder colorido
/// com N retângulos — assim dá pra testar o sistema sem arte.
/// </summary>
public class SpriteSheet : IDisposable
{
    public int FrameWidth  { get; }
    public int FrameHeight { get; }
    public int FrameCount  { get; }

    private readonly Texture2D _texture;
    private readonly bool _isPlaceholder;
    private readonly Color _placeholderColor;

    public SpriteSheet(string path, int frameWidth, int frameHeight, Color placeholderColor)
    {
        FrameWidth  = frameWidth;
        FrameHeight = frameHeight;
        _placeholderColor = placeholderColor;

        if (File.Exists(path))
        {
            _texture = Raylib.LoadTexture(path);
            Raylib.SetTextureFilter(_texture, TextureFilter.Point);
            _isPlaceholder = false;

            if (_texture.Width % frameWidth != 0)
                throw new Exception($"Sprite sheet {path} tem largura {_texture.Width} que não é múltipla de {frameWidth}");

            FrameCount = _texture.Width / frameWidth;
        }
        else
        {
            // Placeholder: 1 frame por padrão, gera uma textura colorida
            _isPlaceholder = true;
            FrameCount = 4; // assume 4 frames por padrão
            
            // Gera textura procedural (retângulo colorido)
            var img = Raylib.GenImageColor(frameWidth * FrameCount, frameHeight, placeholderColor);
            
            // Adiciona um "olho" pra dar direção
            Raylib.ImageDrawRectangle(ref img, frameWidth - 4, 4, 2, 2, Color.Black);
            
            _texture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            Raylib.SetTextureFilter(_texture, TextureFilter.Point);
        }
    }

    /// Retorna o retângulo (source) do frame na textura.
    public Rectangle GetFrameRect(int frameIndex)
    {
        int f = frameIndex % FrameCount;
        return new Rectangle(f * FrameWidth, 0, FrameWidth, FrameHeight);
    }

    /// Desenha um frame numa posição (canto superior esquerdo).
    /// `flipX` vira horizontalmente.
    public void Draw(int frameIndex, Vector2 topLeft, bool flipX, Color tint)
    {
        var src = GetFrameRect(frameIndex);

        // Se flipX, inverte a largura do source (Raylib suporta negativo)
        if (flipX)
        {
            src.X += FrameWidth;
            src.Width = -FrameWidth;
        }

        var dst = new Rectangle(topLeft.X, topLeft.Y, FrameWidth, FrameHeight);
        Raylib.DrawTexturePro(_texture, src, dst, Vector2.Zero, 0f, tint);
    }

    public void Dispose()
    {
        if (!_isPlaceholder)
            Raylib.UnloadTexture(_texture);
        // placeholders também são texturas, precisa unload
        else
            Raylib.UnloadTexture(_texture);
    }
}