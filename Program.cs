using Raylib_cs;
using System.Numerics;
using System.Runtime.InteropServices;
using ResetyKile.Core;
using ResetyKile.Entities;
using ResetyKile.Render;
using ResetyKile.UI;
using ResetyKile.World;

namespace ResetyKile;

public static class Program
{
    private const float FixedDt      = 1f / 120f;
    private const float MaxFrameDt   = 0.25f;
    private const float PlayerRespawnDelay = 1.5f;

    private const float NormalAttackDamage   = 2.0f;
    private const float DashAttackDamage     = 2.0f;
    private const float DashAttackResidual   = 0.5f;
    private const int   DashAttackMaxKills   = 2;

    private const float KnockbackStrength = 140f;

    // Estado de fullscreen
    private static bool _isBorderlessFullscreen = false;

    // ============================================================
    // P/INVOKE — pegar resolução nativa do monitor (Windows)
    // ============================================================
    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    private const int ENUM_CURRENT_SETTINGS = -1;

    private static (int w, int h) GetNativeResolution()
    {
        var devMode = new DEVMODE();
        devMode.dmDeviceName = new string(new char[32]);
        devMode.dmFormName = new string(new char[32]);
        devMode.dmSize = (short)Marshal.SizeOf(devMode);

        if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
            return (devMode.dmPelsWidth, devMode.dmPelsHeight);

        return (Raylib.GetMonitorWidth(0), Raylib.GetMonitorHeight(0));
    }

    // ============================================================
    // MAIN
    // ============================================================
    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(1280, 720, "ResetyKile");

        var renderer     = new Renderer();
        var shake        = new ScreenShake();
        var hitStop      = new HitStop();
        var particles    = new Particles();
        var dashTrail    = new DashTrail();
        var floatingText = new FloatingText();
        var stats        = new SessionStats();

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
        int dashTrailCounter = 0;
        float sessionTime = 0f;
        float gameTime = 0f;
        bool paused = false;

        while (!Raylib.WindowShouldClose())
        {
            float frameDt = MathF.Min(Raylib.GetFrameTime(), MaxFrameDt);
            float now = (float)Raylib.GetTime();
            accumulator += frameDt;

            var input = Input.Read();

            // ---- Fullscreen: F11 ou Alt+Enter ----
            bool altEnter = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);
            if (Raylib.IsKeyPressed(KeyboardKey.F11) || (altEnter && Raylib.IsKeyPressed(KeyboardKey.Enter)))
                ToggleBorderlessFullscreen();

            // ---- Pause ----
            if (input.PausePressed)
            {
                paused = !paused;
                input.JumpPressed   = false;
                input.JumpReleased  = false;
                input.DashPressed   = false;
                input.AttackPressed = false;
                input.SuperPressed  = false;
                input.PausePressed  = false;
            }

            if (!paused)
            {
                sessionTime += frameDt;
                gameTime += frameDt;

                while (accumulator >= FixedDt)
                {
                    if (hitStop.Active)
                    {
                        hitStop.Update(FixedDt);
                        accumulator -= FixedDt;

                        input.JumpPressed   = false;
                        input.JumpReleased  = false;
                        input.DashPressed   = false;
                        input.AttackPressed = false;
                        input.SuperPressed  = false;
                        input.PausePressed  = false;

                        continue;
                    }

                    player.Update(FixedDt, input, level);
                    player.TryAttack(input);

                    // ---- Ataque ----
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
                            bool isDashAttack = player.IsDashing;
                            bool isDoubleHit  = hitTargets.Count >= 2;

                            if (isDashAttack)
                                ProcessDashAttack(hitTargets, player, shake, hitStop, particles, stats, floatingText);
                            else
                                ProcessNormalAttack(hitTargets, player, shake, hitStop, particles, stats, floatingText);

                            if (player.Hp <= 4 || isDoubleHit)
                            {
                                floatingText.Spawn("Damn!", player.Position + new Vector2(0f, -14f),
                                    new Color((byte)255, (byte)220, (byte)80, (byte)255), 0.7f);
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

                            player.TakeDamage(2, (int)e.Position.X);
                            shake.AddTrauma(0.5f);
                            hitStop.Trigger(0.1f);
                            particles.Burst(player.Position, 10, new Color((byte)255, (byte)80, (byte)80, (byte)255),
                                            60f, 130f, 0.4f, gravity: 350f, size: 2f);
                            break;
                        }
                    }

                    // ---- Dash trail ----
                    if (player.State == PlayerState.Dashing)
                    {
                        dashTrailCounter++;
                        if (dashTrailCounter % 2 == 0)
                            dashTrail.Emit(player.Position, player.Size, new Color((byte)120, (byte)220, (byte)255, (byte)255));
                    }
                    else
                    {
                        dashTrailCounter = 0;
                    }

                    particles.Update(FixedDt);
                    dashTrail.Update(FixedDt);
                    floatingText.Update(FixedDt);

                    foreach (var e in enemies)
                        e.Update(FixedDt, player.Position);

                    foreach (var e in enemies)
                        e.SeparateFrom(enemies, FixedDt);

                    // ---- Morte → respawn ----
                    if (player.State == PlayerState.Dead && !playerIsDead)
                    {
                        playerIsDead = true;
                        playerDeathTimer = PlayerRespawnDelay;
                        shake.AddTrauma(0.8f);
                        hitStop.Trigger(0.15f);
                        particles.Burst(player.Position, 20, new Color((byte)255, (byte)60, (byte)100, (byte)255),
                                        80f, 180f, 0.7f, gravity: 400f, size: 2f);
                        stats.ResetPoints();
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

                shake.Update(frameDt);
                var shakeOffset = shake.GetOffset();
                camera.Offset = new Vector2(
                    Renderer.InternalW / 2f + shakeOffset.X,
                    Renderer.InternalH / 2f + shakeOffset.Y);
                camera.Rotation = shake.GetRoll();
            }
            else
            {
                accumulator = 0f;
            }

            // =================== RENDER ===================
            renderer.Begin();
            Raylib.ClearBackground(new Color((byte)18, (byte)16, (byte)28, (byte)255));

            Raylib.BeginMode2D(camera);

            level.Draw(camera, new Color((byte)80, (byte)80, (byte)110, (byte)255));

            DrawEnemies(enemies);

            // Aura Super
            if (player.State == PlayerState.Super)
            {
                float pulse = (MathF.Sin(now * 10f) + 1f) * 0.5f;
                float auraSize = 18f + pulse * 3f;
                var auraRect = new Rectangle(
                    player.Position.X - auraSize / 2f,
                    player.Position.Y - auraSize / 2f,
                    auraSize, auraSize);
                Raylib.DrawRectangleRec(auraRect, new Color((byte)255, (byte)90, (byte)180, (byte)50));
            }

            // Aura Escudo
            if (player.Shield)
            {
                float pulse = (MathF.Sin(now * 4f) + 1f) * 0.5f;
                float shieldSize = 16f + pulse * 2f;
                var shieldRect = new Rectangle(
                    player.Position.X - shieldSize / 2f,
                    player.Position.Y - shieldSize / 2f,
                    shieldSize, shieldSize);
                Raylib.DrawRectangleLinesEx(shieldRect, 1f, new Color((byte)210, (byte)220, (byte)240, (byte)180));
            }

            dashTrail.Draw();

            // Kile
            Color kileColor = player.State switch
            {
                PlayerState.Dashing       => new Color((byte)120, (byte)220, (byte)255, (byte)255),
                PlayerState.LickingKatana => new Color((byte)255, (byte)220, (byte)120, (byte)255),
                PlayerState.Super         => new Color((byte)255, (byte)90, (byte)180, (byte)255),
                PlayerState.WallSlide     => new Color((byte)200, (byte)200, (byte)220, (byte)255),
                PlayerState.Dead          => new Color((byte)120, (byte)40, (byte)60, (byte)255),
                _                         => new Color((byte)240, (byte)240, (byte)240, (byte)255),
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

            particles.Draw();
            floatingText.Draw();

            // Katana
            if (player.IsAttacking)
            {
                var hb = player.AttackHitbox;
                Color katanaColor = player.IsDashing
                    ? new Color((byte)150, (byte)240, (byte)255, (byte)200)
                    : new Color((byte)255, (byte)255, (byte)255, (byte)180);
                Raylib.DrawRectangleRec(hb, katanaColor);

                Raylib.DrawLineEx(
                    new Vector2(player.Position.X, player.Position.Y),
                    new Vector2(player.Facing > 0 ? hb.X + hb.Width : hb.X, hb.Y + hb.Height / 2f),
                    2f,
                    new Color((byte)220, (byte)240, (byte)255, (byte)255));
            }

            Raylib.EndMode2D();

            // ---- HUD ----
            Hud.Draw(player, gameTime);
            SessionStats.Draw(stats, sessionTime);

            if (playerIsDead)
            {
                string msg = "Voce morreu...";
                int fontSize = 10;
                int w = Raylib.MeasureText(msg, fontSize);
                Raylib.DrawText(msg, Renderer.InternalW / 2 - w / 2, Renderer.InternalH / 2 - 20, fontSize,
                    new Color((byte)255, (byte)90, (byte)90, (byte)255));
            }

            // ---- Overlay de Pause ----
            if (paused)
            {
                Raylib.DrawRectangle(0, 0, Renderer.InternalW, Renderer.InternalH,
                    new Color((byte)0, (byte)0, (byte)0, (byte)170));

                string title = "PAUSADO";
                int titleSize = 20;
                int titleW = Raylib.MeasureText(title, titleSize);
                int titleX = Renderer.InternalW / 2 - titleW / 2;
                int titleY = Renderer.InternalH / 2 - 20;

                Raylib.DrawText(title, titleX + 1, titleY + 1, titleSize, new Color((byte)0, (byte)0, (byte)0, (byte)200));
                Raylib.DrawText(title, titleX, titleY, titleSize, new Color((byte)240, (byte)240, (byte)250, (byte)255));

                string hint = "Aperte Esc / Start para continuar";
                int hintSize = 10;
                int hintW = Raylib.MeasureText(hint, hintSize);
                int hintX = Renderer.InternalW / 2 - hintW / 2;
                int hintY = titleY + titleSize + 6;

                Raylib.DrawText(hint, hintX + 1, hintY + 1, hintSize, new Color((byte)0, (byte)0, (byte)0, (byte)200));
                Raylib.DrawText(hint, hintX, hintY, hintSize, new Color((byte)180, (byte)180, (byte)200, (byte)255));
            }

            renderer.End();
        }

        renderer.Unload();
        Raylib.CloseWindow();
    }

    // ============================================================
    // FULLSCREEN — resolução nativa via Windows API
    // ============================================================
    private static void ToggleBorderlessFullscreen()
    {
        if (!_isBorderlessFullscreen)
        {
            var (w, h) = GetNativeResolution();

            Raylib.SetWindowSize(w, h);
            Raylib.SetWindowPosition(0, 0);
            Raylib.SetWindowState(ConfigFlags.UndecoratedWindow | ConfigFlags.FullscreenMode);

            _isBorderlessFullscreen = true;
        }
        else
        {
            Raylib.ClearWindowState(ConfigFlags.UndecoratedWindow | ConfigFlags.FullscreenMode);
            Raylib.SetWindowSize(1280, 720);
            Raylib.SetWindowPosition(100, 100);

            _isBorderlessFullscreen = false;
        }
    }

    // ============================================================
    // ATAQUE NORMAL
    // ============================================================
    private static void ProcessNormalAttack(List<Enemy> hitTargets, Player player,
        ScreenShake shake, HitStop hitStop, Particles particles,
        SessionStats stats, FloatingText floatingText)
    {
        bool isDoubleHit = hitTargets.Count >= 2;
        float dmgPerEnemy = NormalAttackDamage / hitTargets.Count;
        int kbDir = player.Facing;

        if (isDoubleHit)
        {
            hitStop.Trigger(0.08f);
            shake.AddTrauma(0.4f);
            particles.Burst(player.Position, 12, new Color((byte)255, (byte)180, (byte)80, (byte)255),
                            70f, 160f, 0.5f, gravity: 400f, size: 2f);
        }
        else
        {
            hitStop.Trigger(0.06f);
            particles.Burst(player.Position, 6, new Color((byte)255, (byte)255, (byte)200, (byte)255),
                            40f, 90f, 0.3f, gravity: 300f, size: 1f);
        }

        foreach (var e in hitTargets)
        {
            bool died = e.TakeDamage(dmgPerEnemy);
            e.ApplyKnockback(kbDir, KnockbackStrength);

            if (died)
            {
                player.AddSoul(1);
                stats.RegisterKill(false);
                shake.AddTrauma(0.35f);
                hitStop.Trigger(0.08f);
                particles.Burst(e.Position, 12, new Color((byte)255, (byte)120, (byte)120, (byte)255),
                                60f, 140f, 0.5f, gravity: 400f, size: 2f);

                floatingText.Spawn("+5", e.Position + new Vector2(0f, -10f),
                    new Color((byte)255, (byte)220, (byte)100, (byte)255), 0.6f);
            }
            else
            {
                shake.AddTrauma(0.25f);
                particles.Burst(e.Position, 4, new Color((byte)255, (byte)220, (byte)120, (byte)255),
                                40f, 80f, 0.25f, gravity: 300f, size: 1f);
            }
        }
    }

    // ============================================================
    // DASH-ATTACK
    // ============================================================
    private static void ProcessDashAttack(List<Enemy> hitTargets, Player player,
        ScreenShake shake, HitStop hitStop, Particles particles,
        SessionStats stats, FloatingText floatingText)
    {
        int kbDir = player.Facing;

        var sorted = new List<Enemy>(hitTargets);
        sorted.Sort((a, b) =>
        {
            float da = Vector2.DistanceSquared(a.Position, player.Position);
            float db = Vector2.DistanceSquared(b.Position, player.Position);
            return da.CompareTo(db);
        });

        shake.AddTrauma(0.5f);
        particles.Burst(player.Position, 16, new Color((byte)140, (byte)230, (byte)255, (byte)255),
                        80f, 180f, 0.4f, gravity: 300f, size: 2f);

        int killCount = 0;

        for (int i = 0; i < sorted.Count; i++)
        {
            var e = sorted[i];

            if (killCount < DashAttackMaxKills)
            {
                bool died = e.TakeDamage(DashAttackDamage);
                e.ApplyKnockback(kbDir, KnockbackStrength * 1.3f);

                if (died)
                {
                    killCount++;
                    player.AddSoul(1);
                    stats.RegisterKill(true);
                    particles.Burst(e.Position, 14, new Color((byte)255, (byte)120, (byte)120, (byte)255),
                                    70f, 160f, 0.5f, gravity: 400f, size: 2f);

                    floatingText.Spawn("+10", e.Position + new Vector2(0f, -10f),
                        new Color((byte)140, (byte)230, (byte)255, (byte)255), 0.7f);
                }
            }
            else
            {
                bool died = e.TakeDamage(DashAttackResidual);
                e.ApplyKnockback(kbDir, KnockbackStrength);

                if (died)
                {
                    killCount++;
                    player.AddSoul(1);
                    stats.RegisterKill(true);
                    particles.Burst(e.Position, 14, new Color((byte)255, (byte)120, (byte)120, (byte)255),
                                    70f, 160f, 0.5f, gravity: 400f, size: 2f);

                    floatingText.Spawn("+10", e.Position + new Vector2(0f, -10f),
                        new Color((byte)140, (byte)230, (byte)255, (byte)255), 0.7f);
                }
                else
                {
                    particles.Burst(e.Position, 6, new Color((byte)255, (byte)180, (byte)80, (byte)255),
                                    50f, 100f, 0.3f, gravity: 300f, size: 1f);
                }
            }
        }
    }

    // ============================================================
    // DESENHO DOS INIMIGOS
    // ============================================================
    private static void DrawEnemies(List<Enemy> enemies)
    {
        var sorted = new List<Enemy>(enemies.Count);
        foreach (var e in enemies)
            if (!e.IsDead) sorted.Add(e);
        sorted.Sort((a, b) =>
        {
            int cmp = a.Position.Y.CompareTo(b.Position.Y);
            if (cmp != 0) return cmp;
            return a.Id.CompareTo(b.Id);
        });

        foreach (var e in sorted)
        {
            int stackOffset = 0;
            foreach (var other in sorted)
            {
                if (other == e) continue;
                if (other.Id >= e.Id) continue;
                float dx = MathF.Abs(other.Position.X - e.Position.X);
                float dy = MathF.Abs(other.Position.Y - e.Position.Y);
                if (dx < 5f && dy < 5f) stackOffset++;
            }
            stackOffset = Math.Min(stackOffset, 2);

            var drawPos = new Vector2(e.Position.X, e.Position.Y - stackOffset);

            Color bodyColor;
            if (e.IsDying || e.IsHurt)
                bodyColor = new Color((byte)255, (byte)255, (byte)255, (byte)255);
            else if (e.State == EnemyState.Chase)
                bodyColor = new Color((byte)255, (byte)100, (byte)60, (byte)255);
            else
                bodyColor = new Color((byte)200, (byte)60, (byte)60, (byte)255);

            Color outlineColor = Darken(bodyColor, 0.35f);

            var rect = new Rectangle(
                drawPos.X - e.Size.X / 2f,
                drawPos.Y - e.Size.Y / 2f,
                e.Size.X, e.Size.Y);

            var shadowRect = new Rectangle(
                e.Position.X - e.Size.X / 2f,
                e.Position.Y + e.Size.Y / 2f - 1f,
                e.Size.X,
                1f);
            Raylib.DrawRectangleRec(shadowRect, new Color((byte)0, (byte)0, (byte)0, (byte)100));

            var outlineRect = new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2);
            Raylib.DrawRectangleRec(outlineRect, outlineColor);

            Raylib.DrawRectangleRec(rect, bodyColor);

            var eb2 = rect;
            float eEyeX = e.Facing > 0 ? eb2.X + eb2.Width - 3f : eb2.X + 1f;
            Raylib.DrawRectangle((int)eEyeX, (int)(eb2.Y + 2f), 2, 2, Color.Black);

            bool showBar = (e.IsAlive && e.Hp < Enemy.MaxHp) || stackOffset > 0;
            if (showBar && e.IsAlive)
            {
                float pct = e.Hp / Enemy.MaxHp;
                int bw = (int)e.Size.X;
                int bx = (int)(e.Position.X - e.Size.X / 2f);
                int by = (int)(e.Position.Y - e.Size.Y / 2f) - 4;

                Raylib.DrawRectangle(bx, by, bw, 1, new Color((byte)60, (byte)30, (byte)30, (byte)255));
                Raylib.DrawRectangle(bx, by, (int)(bw * pct), 1, new Color((byte)255, (byte)200, (byte)80, (byte)255));
            }
        }
    }

    private static Color Darken(Color c, float factor)
    {
        float m = 1f - factor;
        return new Color(
            (byte)(c.R * m),
            (byte)(c.G * m),
            (byte)(c.B * m),
            (byte)255);
    }
}