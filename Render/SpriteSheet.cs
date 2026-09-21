using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

public class SpriteSheet : IDisposable
{
    public int FrameWidth  { get; }
    public int FrameHeight { get; }
    public int FrameCount  { get; }

    private readonly Texture2D _texture;
    private Texture2D? _tintedTexture;
    private Color _tintColor;

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
            FrameCount = 4;
            var img = Raylib.GenImageColor(frameWidth * FrameCount, frameHeight, placeholderColor);
            Raylib.ImageDrawRectangle(ref img, frameWidth - 4, 4, 2, 2, Color.Black);

            _texture = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            Raylib.SetTextureFilter(_texture, TextureFilter.Point);
        }
    }

    /// Gera (ou retorna cache) uma versão do sprite todo tingido de uma cor.
    public Texture2D GetTintedTexture(Color tint)
    {
        if (_tintedTexture.HasValue && _tintColor.Equals(tint))
            return _tintedTexture.Value;

        if (_tintedTexture.HasValue)
            Raylib.UnloadTexture(_tintedTexture.Value);

        var img = Raylib.LoadImageFromTexture(_texture);

        unsafe
        {
            var pixels = (Color*)img.Data;
            int total = img.Width * img.Height;

            for (int i = 0; i < total; i++)
            {
                if (pixels[i].A > 0)
                {
                    pixels[i] = new Color(tint.R, tint.G, tint.B, pixels[i].A);
                }
            }
        }

        _tintedTexture = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        Raylib.SetTextureFilter(_tintedTexture.Value, TextureFilter.Point);
        _tintColor = tint;

        return _tintedTexture.Value;
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

    /// Desenha o frame usando a versão tingida (silhueta sólida).
    public void DrawTinted(int frameIndex, Vector2 topLeft, bool flipX, Color tint, float alpha)
    {
        var tex = GetTintedTexture(tint);

        var src = GetFrameRect(frameIndex);
        if (flipX)
        {
            src.X += FrameWidth;
            src.Width = -FrameWidth;
        }

        var dst = new Rectangle(topLeft.X, topLeft.Y, FrameWidth, FrameHeight);
        var drawColor = new Color((byte)255, (byte)255, (byte)255, (byte)(alpha * 255));

        Raylib.DrawTexturePro(tex, src, dst, Vector2.Zero, 0f, drawColor);
    }

    public void Dispose()
    {
        if (_tintedTexture.HasValue)
            Raylib.UnloadTexture(_tintedTexture.Value);
        Raylib.UnloadTexture(_texture);
    }
}