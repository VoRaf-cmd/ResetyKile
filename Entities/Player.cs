using Raylib_cs;
using System.Numerics;
using ResetyKile.Core;
using ResetyKile.World;

namespace ResetyKile.Entities;

public enum PlayerState { Normal, Dashing, WallSlide, LickingKatana, Super, Dead }

public class Player
{
    // ---------- Constantes de tuning ----------
    private const float Gravity       = 900f;
    private const float MaxFall       = 160f;
    private const float RunSpeed      = 90f;
    private const float RunAccel      = 1000f;
    private const float RunDeccel     = 400f;
    private const float AirAccelMult  = 0.65f;
    private const float JumpSpeed     = 105f;
    private const float JumpHBoost    = 40f;
    private const float VarJumpTime   = 0.2f;
    private const float CoyoteTime    = 0.10f;
    private const float JumpBuffer    = 0.10f;
    private const float DashSpeed     = 240f;
    private const float DashTime      = 0.15f;
    private const float DashCooldown  = 0.20f;
    private const float DashEndSpeed  = 160f;

    // Super
    private const float LickDuration  = 0.6f;
    private const float SuperDuration = 8.0f;
    private const int   SoulsToSuper  = 10;

    // ---------- Estado ----------
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Size = new(8, 11);
    public int Facing = 1;

    public PlayerState State = PlayerState.Normal;
    public int Souls { get; private set; }
    public float SuperTimer { get; private set; }
    public bool HasDashAvailable { get; private set; } = true;

    private float _coyote, _jumpBuf, _varJump, _dashTimer, _dashCd, _lickTimer;
    private bool  _onGround;
    private int   _dashDirX, _dashDirY;
    private int   _wallDir;

    // ---------- API pública ----------
    public Rectangle Bounds => new(Position.X - Size.X / 2f, Position.Y - Size.Y / 2f, Size.X, Size.Y);
    public bool OnGround => _onGround;
    public bool IsDashing => State == PlayerState.Dashing;
    public bool IsInvulnerable => State == PlayerState.Dashing || State == PlayerState.Super;

    public void AddSoul() => Souls = Math.Min(Souls + 1, SoulsToSuper);

    // ---------- Update ----------
    public void Update(float dt, InputState input, Level level)
    {
        // ---- Timers ----
        _coyote  = _onGround ? CoyoteTime : MathF.Max(0, _coyote - dt);
        _jumpBuf = input.JumpPressed ? JumpBuffer : MathF.Max(0, _jumpBuf - dt);
        _varJump = MathF.Max(0, _varJump - dt);
        _dashTimer = MathF.Max(0, _dashTimer - dt);
        _dashCd    = MathF.Max(0, _dashCd - dt);

        if (State == PlayerState.Super)
        {
            SuperTimer -= dt;
            if (SuperTimer <= 0f) State = PlayerState.Normal;
        }

        // ---- Lamber katana (windup do Super) ----
        if (State == PlayerState.LickingKatana)
        {
            _lickTimer -= dt;
            Velocity.X = MathUtil.Approach(Velocity.X, 0f, RunDeccel * 2f * dt);
            Velocity.Y = MathF.Min(Velocity.Y + Gravity * dt, MaxFall);
            MoveX(Velocity.X * dt, level);
            MoveY(Velocity.Y * dt, level);

            if (_lickTimer <= 0f)
            {
                State = PlayerState.Super;
                SuperTimer = SuperDuration;
                Souls = 0;
            }
            return;
        }

        // ---- Ativar Super ----
        if (input.SuperPressed && Souls >= SoulsToSuper && State == PlayerState.Normal)
        {
            State = PlayerState.LickingKatana;
            _lickTimer = LickDuration;
            Velocity = Vector2.Zero;
            return;
        }

        // ---- Dash ----
        if (input.DashPressed && HasDashAvailable && _dashCd <= 0f && State != PlayerState.Dashing)
        {
            State = PlayerState.Dashing;
            _dashTimer = DashTime;
            _dashCd = DashTime + DashCooldown;
            HasDashAvailable = false;
            (_dashDirX, _dashDirY) = Input.DashDirection(input, Facing);
            if (_dashDirX != 0) Facing = _dashDirX;
        }

        if (State == PlayerState.Dashing)
        {
            Velocity = new Vector2(_dashDirX, _dashDirY) * DashSpeed;
            if (_dashTimer <= 0f)
            {
                State = PlayerState.Normal;
                Velocity = Vector2.Normalize(Velocity) * DashEndSpeed;
            }
        }
        else
        {
            // ---- Horizontal ----
            float targetX = input.MoveXInt * RunSpeed;
            float accel = _onGround
                ? (input.MoveXInt != 0 ? RunAccel : RunDeccel)
                : RunAccel * AirAccelMult;
            Velocity.X = MathUtil.Approach(Velocity.X, targetX, accel * dt);
            if (input.MoveXInt != 0) Facing = input.MoveXInt;

            // ---- Gravidade ----
            float g = Gravity;
            if (Velocity.Y < 0 && input.JumpHeld) g *= 0.5f;
            if (MathF.Abs(Velocity.Y) < 40f && input.JumpHeld) g *= 0.5f;
            Velocity.Y = MathF.Min(Velocity.Y + g * dt, MaxFall);

            // ---- Wall slide ----
            _wallDir = DetectWall(level);
            if (!_onGround && _wallDir != 0 && input.MoveXInt == _wallDir && Velocity.Y > 0)
            {
                State = PlayerState.WallSlide;
                Velocity.Y = MathF.Min(Velocity.Y, 40f);
            }
            else if (State == PlayerState.WallSlide)
            {
                State = PlayerState.Normal;
            }

            // ---- Pulo ----
            if (_jumpBuf > 0f)
            {
                if (_coyote > 0f)
                {
                    Velocity.Y = -JumpSpeed;
                    Velocity.X += input.MoveXInt * JumpHBoost;
                    _jumpBuf = 0f; _coyote = 0f; _varJump = VarJumpTime;
                }
                else if (_wallDir != 0)
                {
                    Velocity.X = -_wallDir * JumpHBoost * 1.5f;
                    Velocity.Y = -JumpSpeed;
                    _jumpBuf = 0f; _varJump = VarJumpTime;
                    Facing = -_wallDir;
                }
            }

            // Pulo variável
            if (_varJump > 0f)
            {
                if (!input.JumpHeld) _varJump = 0f;
                else Velocity.Y = MathF.Min(Velocity.Y, -JumpSpeed * 0.4f);
            }
        }

        // ---- Mover + colidir ----
        MoveX(Velocity.X * dt, level);
        MoveY(Velocity.Y * dt, level);

        if (_onGround) HasDashAvailable = true;
    }

    // ---------- Colisão ----------
    private int DetectWall(Level level)
    {
        var r = Bounds;
        var probeL = new Rectangle(r.X - 1f, r.Y, 1f, r.Height);
        var probeR = new Rectangle(r.X + r.Width, r.Y, 1f, r.Height);
        if (level.CollidesAny(probeL)) return -1;
        if (level.CollidesAny(probeR)) return  1;
        return 0;
    }

    private void MoveX(float dx, Level level)
    {
        Position.X += dx;
        var rect = Bounds;
        foreach (var t in level.SolidTilesNear(rect))
        {
            if (!Raylib.CheckCollisionRecs(rect, t)) continue;
            if (dx > 0) Position.X = t.X - Size.X / 2f;
            else        Position.X = t.X + t.Width + Size.X / 2f;
            Velocity.X = 0;
            rect = Bounds;
        }
    }

    private void MoveY(float dy, Level level)
    {
        _onGround = false;
        Position.Y += dy;
        var rect = Bounds;
        foreach (var t in level.SolidTilesNear(rect))
        {
            if (!Raylib.CheckCollisionRecs(rect, t)) continue;
            if (dy > 0) { Position.Y = t.Y - Size.Y / 2f; _onGround = true; }
            else        { Position.Y = t.Y + t.Height + Size.Y / 2f; }
            Velocity.Y = 0;
            rect = Bounds;
        }
    }
}