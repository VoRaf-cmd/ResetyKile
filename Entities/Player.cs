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
    private const float JumpSpeed     = 220f;
    private const float JumpHBoost    = 40f;
    private const float VarJumpTime   = 0.2f;
    private const float CoyoteTime    = 0.10f;
    private const float JumpBuffer    = 0.10f;
    private const float DashSpeed     = 240f;
    private const float DashTime      = 0.15f;

    // Super
    private const float LickDuration  = 0.6f;
    private const float SuperDuration = 8.0f;
    private const int   SoulsToSuper  = 10;

    // Ataque
    private const float AttackDuration = 0.15f;
    private const float AttackCooldown = 0.25f;
    private const float AttackRange    = 21f;
    private const float AttackHeight   = 13f;

    // Vida e dano
    public  const int   MaxHp              = 20;
    private const float InvulnDuration     = 1.0f;
    private const float KnockbackX         = 120f;
    private const float KnockbackY         = -90f;

    // Stamina de dash
    public  const int   MaxStamina         = 3;
    public  const float DashCost           = 1.0f;
    public  const float DashAttackCost     = 1.5f;
    public  const float StaminaRegenGround = 1f / 1.2f;
    public  const float StaminaRegenAir    = 1f / 3.0f;

    // ---------- Estado ----------
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Size = new(8, 11);
    public int Facing = 1;

    public PlayerState State = PlayerState.Normal;
    public int Souls { get; private set; }
    public int Hp    { get; private set; } = MaxHp;
    public bool Shield { get; private set; }
    public float SuperTimer { get; private set; }
    public float Stamina { get; private set; } = MaxStamina;

    public float InvulnTimer { get; private set; }
    public bool IsInvulnerable => InvulnTimer > 0f
                                || State == PlayerState.Dashing
                                || State == PlayerState.Super;

    private float _coyote, _jumpBuf, _varJump, _dashTimer, _lickTimer;
    private bool  _onGround;
    private int   _dashDirX, _dashDirY;
    private int   _wallDir;

    private float _attackTimer;
    private float _attackCd;
    public bool IsAttacking => _attackTimer > 0f;
    public bool HasHitThisSwing { get; private set; }

    // ---------- API pública ----------
    public const int MaxSouls = 10;

    public Rectangle Bounds => new(Position.X - Size.X / 2f, Position.Y - Size.Y / 2f, Size.X, Size.Y);
    public bool OnGround => _onGround;
    public bool IsDashing => State == PlayerState.Dashing;
    public bool SuperReady => Souls >= MaxSouls;
    public bool CanDash(float cost) => Stamina >= cost || State == PlayerState.Super;

    public void AddSoul(int amount = 1)
        => Souls = Math.Clamp(Souls + amount, 0, MaxSouls);

    public void TakeDamage(int amount, int fromDirX)
    {
        if (IsInvulnerable) return;

        if (Shield)
        {
            Shield = false;
            InvulnTimer = InvulnDuration;

            int dirS = fromDirX == 0 ? -Facing : (Position.X < fromDirX ? -1 : 1);
            Velocity.X = dirS * KnockbackX * 0.7f;
            Velocity.Y = KnockbackY * 0.7f;
            return;
        }

        Hp = Math.Max(0, Hp - amount);
        InvulnTimer = InvulnDuration;

        int dir = fromDirX == 0 ? -Facing : (Position.X < fromDirX ? -1 : 1);
        Velocity.X = dir * KnockbackX;
        Velocity.Y = KnockbackY;

        if (Hp <= 0) State = PlayerState.Dead;
    }

    public Rectangle AttackHitbox
    {
        get
        {
            float w = AttackRange;
            float h = AttackHeight;
            float x = Facing > 0 ? Position.X : Position.X - w;
            float y = Position.Y - h / 2f;
            return new Rectangle(x, y, w, h);
        }
    }

    public bool TryAttack(InputState input)
    {
        if (input.AttackPressed && _attackCd <= 0f && !IsAttacking && State != PlayerState.Dead)
        {
            _attackTimer = AttackDuration;
            _attackCd    = AttackDuration + AttackCooldown;
            HasHitThisSwing = false;
            return true;
        }
        return false;
    }

    public void MarkHit() => HasHitThisSwing = true;

    public void Respawn(Vector2 spawnPos)
    {
        Position = spawnPos;
        Velocity = Vector2.Zero;
        State = PlayerState.Normal;
        Hp = MaxHp;
        Souls = 0;
        Shield = false;
        SuperTimer = 0f;
        InvulnTimer = 0f;
        Stamina = MaxStamina;
        _onGround = false;
        _coyote = _jumpBuf = _varJump = 0f;
        _dashTimer = _lickTimer = 0f;
        _attackTimer = _attackCd = 0f;
        Facing = 1;
    }

    // ---------- Update ----------
    public void Update(float dt, InputState input, Level level)
    {
        _coyote      = _onGround ? CoyoteTime : MathF.Max(0, _coyote - dt);
        _jumpBuf     = input.JumpPressed ? JumpBuffer : MathF.Max(0, _jumpBuf - dt);
        _varJump     = MathF.Max(0, _varJump - dt);
        _dashTimer   = MathF.Max(0, _dashTimer - dt);
        _attackTimer = MathF.Max(0, _attackTimer - dt);
        _attackCd    = MathF.Max(0, _attackCd - dt);
        InvulnTimer  = MathF.Max(0, InvulnTimer - dt);

        if (State == PlayerState.Dead)
        {
            Velocity.Y = MathF.Min(Velocity.Y + Gravity * dt, MaxFall);
            MoveX(Velocity.X * dt, level);
            MoveY(Velocity.Y * dt, level);
            Velocity.X = MathUtil.Approach(Velocity.X, 0f, RunDeccel * dt);
            return;
        }

        if (State == PlayerState.Super)
        {
            SuperTimer -= dt;
            if (SuperTimer <= 0f) State = PlayerState.Normal;
        }

        // ---- Regeneração de stamina ----
        if (State != PlayerState.Super && Stamina < MaxStamina)
        {
            float regenRate = _onGround ? StaminaRegenGround : StaminaRegenAir;
            Stamina = MathF.Min(Stamina + regenRate * dt, MaxStamina);
        }

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
                Shield = true;
                Stamina = MaxStamina;
            }
            return;
        }

        if (input.SuperPressed && SuperReady && State == PlayerState.Normal)
        {
            State = PlayerState.LickingKatana;
            _lickTimer = LickDuration;
            Velocity = Vector2.Zero;
            return;
        }

        // ---- Dash (custo variável: dash-attack custa mais) ----
        // Se o jogador aperta dash + ataque no mesmo tick, cobra DashAttackCost
        bool attackBuffered = input.AttackPressed;
        bool wantDash = input.DashPressed && State != PlayerState.Dashing;

        if (wantDash)
        {
            float cost = attackBuffered ? DashAttackCost : DashCost;
            if (CanDash(cost))
            {
                State = PlayerState.Dashing;
                _dashTimer = DashTime;
                if (State != PlayerState.Super)
                    Stamina = MathF.Max(0f, Stamina - cost);
                (_dashDirX, _dashDirY) = Input.DashDirection(input, Facing);
                if (_dashDirX != 0) Facing = _dashDirX;
            }
        }

        if (State == PlayerState.Dashing)
        {
            Velocity = new Vector2(_dashDirX, _dashDirY) * DashSpeed;
            if (_dashTimer <= 0f)
            {
                State = PlayerState.Normal;
                Velocity = Vector2.Normalize(Velocity) * 0.5f;
            }
        }
        else
        {
            float targetX = input.MoveXInt * RunSpeed;
            float accel = _onGround
                ? (input.MoveXInt != 0 ? RunAccel : RunDeccel)
                : RunAccel * AirAccelMult;
            Velocity.X = MathUtil.Approach(Velocity.X, targetX, accel * dt);
            if (input.MoveXInt != 0) Facing = input.MoveXInt;

            float g = Gravity;
            if (Velocity.Y < 0 && input.JumpHeld) g *= 0.5f;
            if (MathF.Abs(Velocity.Y) < 40f && input.JumpHeld) g *= 0.5f;
            Velocity.Y = MathF.Min(Velocity.Y + g * dt, MaxFall);

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

            if (_varJump > 0f)
            {
                if (!input.JumpHeld) _varJump = 0f;
                else Velocity.Y = MathF.Min(Velocity.Y, -JumpSpeed * 0.4f);
            }
        }

        MoveX(Velocity.X * dt, level);
        MoveY(Velocity.Y * dt, level);

        if (!IsAttacking) HasHitThisSwing = false;
    }

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