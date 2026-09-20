using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Core;

public class Renderer
{
    public const int InternalW = 320;
    public const int InternalH = 180;

    private readonly RenderTexture2D _target;

    public Renderer()
    {
        _target = Raylib.LoadRenderTexture(InternalW, InternalH);
        Raylib.SetTextureFilter(_target.Texture, TextureFilter.Point);
    }

    public void Begin() => Raylib.BeginTextureMode(_target);

    public void End()
    {
        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        // Calcula a maior escala que CABE mantendo proporção
        float scaleX = (float)screenW / InternalW;
        float scaleY = (float)screenH / InternalH;
        float scale = MathF.Min(scaleX, scaleY);

        int dstW = (int)(InternalW * scale);
        int dstH = (int)(InternalH * scale);
        int dstX = (screenW - dstW) / 2;
        int dstY = (screenH - dstH) / 2;

        var src = new Rectangle(0, 0, InternalW, -InternalH);
        var dst = new Rectangle(dstX, dstY, dstW, dstH);
        Raylib.DrawTexturePro(_target.Texture, src, dst, Vector2.Zero, 0f, Color.White);

        Raylib.EndDrawing();
    }

    public void Unload() => Raylib.UnloadRenderTexture(_target);
}