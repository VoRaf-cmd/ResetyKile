using Raylib_cs;
using System.Numerics;
using ResetyKile.Core;
using ResetyKile.Entities;
using ResetyKile.World;

namespace ResetyKile;

public static class Program
{
    private const float FixedDt    = 1f / 120f;
    private const float MaxFrameDt = 0.25f;

    public static void Main()
    {
        Raylib.InitWindow(1280, 720, "ResetyKile");
        Raylib.SetTargetFPS(120);

        var renderer = new Renderer();

        var level  = Level.TestRoom();
        var player = new Player { Position = new Vector2(64, 64) };

        var camera = new Camera2D
        {
            Offset = new Vector2(Renderer.InternalW / 2f, Renderer.InternalH / 2f),
            Target = player.Position,
            Zoom   = 1.5f,
        };

        float accumulator = 0f;

        while (!Raylib.WindowShouldClose())
        {
            float frameDt = MathF.Min(Raylib.GetFrameTime(), MaxFrameDt);
            accumulator += frameDt;

            var input = Input.Read();

            while (accumulator >= FixedDt)
            {
                player.Update(FixedDt, input, level);
                accumulator -= FixedDt;

                input.JumpPressed   = false;
                input.JumpReleased  = false;
                input.DashPressed   = false;
                input.AttackPressed = false;
                input.SuperPressed  = false;
                input.PausePressed  = false;
            }

            camera.Target = MathUtil.ExpLerp(camera.Target, player.Position, 12f, frameDt);

            renderer.Begin();
            Raylib.ClearBackground(new Color(18, 16, 28, 255));

            Raylib.BeginMode2D(camera);

            level.Draw(camera, new Color(80, 80, 110, 255));

            Color kileColor = player.State switch
            {
                PlayerState.Dashing       => new Color(120, 220, 255, 255),
                PlayerState.LickingKatana => new Color(255, 220, 120, 255),
                PlayerState.Super         => new Color(255, 90, 180, 255),
                PlayerState.WallSlide     => new Color(200, 200, 220, 255),
                _                         => new Color(240, 240, 240, 255),
            };

            Raylib.DrawRectangleRec(player.Bounds, kileColor);

            var r = player.Bounds;
            float eyeX = player.Facing > 0 ? r.X + r.Width - 3f : r.X + 1f;
            Raylib.DrawRectangle((int)eyeX, (int)(r.Y + 2f), 2, 2, Color.Black);

            Raylib.EndMode2D();

            Raylib.DrawText($"Souls: {player.Souls}/10", 8, 8, 10, Color.White);
            if (player.State == PlayerState.Super)
                Raylib.DrawText("SUPER!", 8, 22, 10, new Color(255, 90, 180, 255));

            renderer.End();
        }

        renderer.Unload();
        Raylib.CloseWindow();
    }
}