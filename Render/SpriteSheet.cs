using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Spritesheet de UMA linha (N frames lado a lado).
/// Se o arquivo não existir, gera um placeholder colorido.
/// </summary>
public class SpriteSheet : IDisposable
{
    public int FrameWidth  { get; }
    public int FrameHeight { get; }
    public int FrameCount  { get; }

    private readonly Texture2D _texture;

    public SpriteSheet(string path, int frameWidth, int frameHeight, Color placeholderColor)
    {
        FrameWidth  = frameWidth;
        FrameHeight = frameHeight;

        if (File.Exists(path))
        {
            _texture = Raylib.LoadTexture(path);
            Raylib.SetTextureFilter(_texture, TextureFilter.Point);

            if (_texture.Width % frameWidth != 0)
                throw new Exception($"Sprite sheet {path} tem largura {_texture.Width} que não é múltipla de {frameWidth}");

            FrameCount = _texture.Width / frameWidth;
        }
        else
        {
            // Placeholder: 4 frames de retângulo colorido
            FrameCount = 4;

            var img = Raylib.GenImageColor(frameWidth * FrameCount, frameHeight, placeholderColor);
            Raylib.ImageDrawRectangle(ref img, frameWidth - 4, 4, 2, 2, Color.Black);

            _texture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            Raylib.SetTextureFilter(_texture, TextureFilter.Point);
        }
    }

    public Rectangle GetFrameRect(int frameIndex)
    {
        int f = frameIndex % FrameCount;
        return new Rectangle(f * FrameWidth, 0, FrameWidth, FrameHeight);
    }

    public void Draw(int frameIndex, Vector2 topLeft, bool flipX, Color tint)
    {
        var src = GetFrameRect(frameIndex);

        if (flipX)
        {
            src.X += FrameWidth;
            src.Width = -FrameWidth;
        }

        var dst = new Rectangle(topLeft.X, topLeft.Y, FrameWidth, FrameHeight);
        Raylib.DrawTexturePro(_texture, src, dst, Vector2.Zero, 0f, tint);
    }

    public void Dispose() => Raylib.UnloadTexture(_texture);
}