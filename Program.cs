using Raylib_cs;
using System.Numerics;
using ResetyKile.Core;
using ResetyKile.Entities;
using ResetyKile.UI;
using ResetyKile.World;

namespace ResetyKile;

public static class Program
{
    private const float FixedDt      = 1f / 120f;
    private const float MaxFrameDt   = 0.25f;
    private const float PlayerRespawnDelay = 1.5f;

    public static void Main()
    {
        Raylib.InitWindow(1280, 720, "ResetyKile");
        Raylib.SetTargetFPS(120);

        var renderer = new Renderer();

        var level       = Level.TestRoom();
        var playerSpawn = new Vector2(64, 64);
        var player      = new Player { Position = playerSpawn };

        var enemies = new List<Enemy>
        {
            new Enemy(new Vector2(100, 100), level),
            new Enemy(new Vector2(160, 100), level),
            new Enemy(new Vector2(220, 100), level),
            new Enemy(new Vector2(100, 170), level),
            new Enemy(new Vector2(180, 130), level),
            new Enemy(new Vector2(240, 100), level),
            new Enemy(new Vector2(280, 60),  level),
            new Enemy(new Vector2(330, 150), level),
            new Enemy(new Vector2(380, 60),  level),
            new Enemy(new Vector2(430, 60),  level),
        };

        var camera = new Camera2D
        {
            Offset = new Vector2(Renderer.InternalW / 2f, Renderer.InternalH / 2f),
            Target = player.Position,
            Zoom   = 1.5f,
        };

        var hitTargets = new List<Enemy>(16);

        float accumulator = 0f;
        float playerDeathTimer = 0f;
        bool  playerIsDead = false;

        while (!Raylib.WindowShouldClose())
        {
            float frameDt = MathF.Min(Raylib.GetFrameTime(), MaxFrameDt);
            float now = (float)Raylib.GetTime();
            accumulator += frameDt;

            var input = Input.Read();

            while (accumulator >= FixedDt)
            {
                player.Update(FixedDt, input, level);
                player.TryAttack(input);

                // ---- Ataque: dano dividido ----
                if (player.IsAttacking && !player.HasHitThisSwing)
                {
                    var hitbox = player.AttackHitbox;

                    hitTargets.Clear();
                    foreach (var e in enemies)
                    {
                        if (!e.IsAlive) continue;
                        if (!Raylib.CheckCollisionRecs(hitbox, e.Bounds)) continue;
                        hitTargets.Add(e);
                    }

                    if (hitTargets.Count > 0)
                    {
                        float dmgPerEnemy = Player.AttackDamage / hitTargets.Count;

                        foreach (var e in hitTargets)
                        {
                            bool died = e.TakeDamage(dmgPerEnemy);
                            if (died) player.AddSoul(1); // ← 1 soul por inimigo morto
                        }

                        player.MarkHit();
                    }
                }

                // ---- Inimigo machuca Kile ----
                if (!player.IsInvulnerable && player.State != PlayerState.Dead)
                {
                    foreach (var e in enemies)
                    {
                        if (!e.IsAlive) continue;
                        if (!Raylib.CheckCollisionRecs(player.Bounds, e.Bounds)) continue;

                        player.TakeDamage(1, (int)e.Position.X);
                        break;
                    }
                }

                foreach (var e in enemies)
                    e.Update(FixedDt, player.Position);

                // ---- Morte → respawn ----
                if (player.State == PlayerState.Dead && !playerIsDead)
                {
                    playerIsDead = true;
                    playerDeathTimer = PlayerRespawnDelay;
                }

                if (playerIsDead)
                {
                    playerDeathTimer -= FixedDt;
                    if (playerDeathTimer <= 0f)
                    {
                        player.Respawn(playerSpawn);
                        playerIsDead = false;
                    }
                }

                accumulator -= FixedDt;

                input.JumpPressed   = false;
                input.JumpReleased  = false;
                input.DashPressed   = false;
                input.AttackPressed = false;
                input.SuperPressed  = false;
                input.PausePressed  = false;
            }

            camera.Target = MathUtil.ExpLerp(camera.Target, player.Position, 12f, frameDt);

            // =================== RENDER ===================
            renderer.Begin();
            Raylib.ClearBackground(new Color(18, 16, 28, 255));

            Raylib.BeginMode2D(camera);

            level.Draw(camera, new Color(80, 80, 110, 255));

            // ---- Inimigos ----
            foreach (var e in enemies)
            {
                if (e.IsDead) continue;

                Color enemyColor;
                if (e.IsDying || e.IsHurt)
                    enemyColor = new Color(255, 255, 255, 255);
                else if (e.State == EnemyState.Chase)
                    enemyColor = new Color(255, 100, 60, 255);
                else
                    enemyColor = new Color(200, 60, 60, 255);

                Raylib.DrawRectangleRec(e.Bounds, enemyColor);

                // Mini-barra de HP acima do inimigo (mostra o dano dividido)
                if (e.IsAlive && e.Hp < Enemy.MaxHp)
                {
                    float pct = e.Hp / Enemy.MaxHp;
                    var eb = e.Bounds;
                    int bw = (int)eb.Width;
                    Raylib.DrawRectangle((int)eb.X, (int)eb.Y - 3, bw, 1, new Color(60, 30, 30, 255));
                    Raylib.DrawRectangle((int)eb.X, (int)eb.Y - 3, (int)(bw * pct), 1, new Color(255, 200, 80, 255));
                }

                var eb2 = e.Bounds;
                float eEyeX = e.Facing > 0 ? eb2.X + eb2.Width - 3f : eb2.X + 1f;
                Raylib.DrawRectangle((int)eEyeX, (int)(eb2.Y + 2f), 2, 2, Color.Black);
            }

            // ---- Aura do Super (atrás do Kile) ----
            if (player.State == PlayerState.Super)
            {
                float pulse = (MathF.Sin(now * 10f) + 1f) * 0.5f;
                float auraSize = 18f + pulse * 3f;
                var auraRect = new Rectangle(
                    player.Position.X - auraSize / 2f,
                    player.Position.Y - auraSize / 2f,
                    auraSize, auraSize);
                Raylib.DrawRectangleRec(auraRect, new Color(255, 90, 180, 50));
            }

            // ---- Aura do Escudo ----
            if (player.Shield)
            {
                float pulse = (MathF.Sin(now * 4f) + 1f) * 0.5f;
                float shieldSize = 16f + pulse * 2f;
                var shieldRect = new Rectangle(
                    player.Position.X - shieldSize / 2f,
                    player.Position.Y - shieldSize / 2f,
                    shieldSize, shieldSize);
                Raylib.DrawRectangleLinesEx(shieldRect, 1f, new Color(210, 220, 240, 180));
            }

            // ---- Kile ----
            Color kileColor = player.State switch
            {
                PlayerState.Dashing       => new Color(120, 220, 255, 255),
                PlayerState.LickingKatana => new Color(255, 220, 120, 255),
                PlayerState.Super         => new Color(255, 90, 180, 255),
                PlayerState.WallSlide     => new Color(200, 200, 220, 255),
                PlayerState.Dead          => new Color(120, 40, 60, 255),
                _                         => new Color(240, 240, 240, 255),
            };

            bool blink = player.InvulnTimer > 0f
                      && ((int)(player.InvulnTimer * 20f) % 2 == 0);

            if (!blink)
            {
                Raylib.DrawRectangleRec(player.Bounds, kileColor);

                var r = player.Bounds;
                float eyeX = player.Facing > 0 ? r.X + r.Width - 3f : r.X + 1f;
                Raylib.DrawRectangle((int)eyeX, (int)(r.Y + 2f), 2, 2, Color.Black);
            }

            // ---- Katana ----
            if (player.IsAttacking)
            {
                var hb = player.AttackHitbox;
                Raylib.DrawRectangleRec(hb, new Color(255, 255, 255, 180));

                Raylib.DrawLineEx(
                    new Vector2(player.Position.X, player.Position.Y),
                    new Vector2(player.Facing > 0 ? hb.X + hb.Width : hb.X, hb.Y + hb.Height / 2f),
                    2f,
                    new Color(220, 240, 255, 255));
            }

            Raylib.EndMode2D();

            // ---- HUD ----
            Hud.Draw(player, now);

            if (playerIsDead)
            {
                string msg = "Voce morreu...";
                int fontSize = 10;
                int w = Raylib.MeasureText(msg, fontSize);
                Raylib.DrawText(msg, Renderer.InternalW / 2 - w / 2, Renderer.InternalH / 2 - 20, fontSize,
                    new Color(255, 90, 90, 255));
            }

            renderer.End();
        }

        renderer.Unload();
        Raylib.CloseWindow();
    }
}