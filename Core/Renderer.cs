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

        var src = new Rectangle(0, 0, InternalW, -InternalH);
        var dst = new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        Raylib.DrawTexturePro(_target.Texture, src, dst, Vector2.Zero, 0f, Color.White);

        Raylib.EndDrawing();
    }

    public void Unload() => Raylib.UnloadRenderTexture(_target);
}