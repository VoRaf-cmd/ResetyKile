using Raylib_cs;
using System.Numerics;
using ResetyKile.World;

namespace ResetyKile.Entities;

public enum EnemyState { Patrol, Chase, Dying, Dead }

public class Enemy
{
    // ---------- Constantes ----------
    private const float Gravity       = 900f;
    private const float MaxFall       = 160f;
    private const float PatrolSpeed   = 25f;
    private const float ChaseSpeed    = 55f;
    private const float DetectRange   = 70f;
    private const float LoseRange     = 110f;
    private const float RespawnTime   = 6f;
    private const float DeathFlicker  = 0.15f;
    private const float HurtFlicker   = 0.12f;

    // Separação entre inimigos
    private const float MinSeparation = 5f;
    private const float SeparateForce = 40f;

    // Knockback
    private const float KnockbackDecel = 600f;   // desaceleração horizontal
    private const float KnockbackMinSpeed = 10f; // abaixo disso, IA retoma controle

    public const float MaxHp = 2.0f;

    private static int _nextId = 1;
    public int Id { get; }

    // ---------- Estado ----------
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 Size = new(8, 8);
    public int Facing = 1;
    public EnemyState State = EnemyState.Patrol;
    public float RespawnTimer;
    public float Hp = MaxHp;
    public Vector2 SpawnPosition;

    private bool _onGround;
    private int  _wallDir;
    private float _deathTimer;
    private float _hurtTimer;
    private readonly Level _level;

    public Rectangle Bounds => new(Position.X - Size.X / 2f, Position.Y - Size.Y / 2f, Size.X, Size.Y);
    public bool IsDead  => State == EnemyState.Dead;
    public bool IsDying => State == EnemyState.Dying;
    public bool IsAlive => State == EnemyState.Patrol || State == EnemyState.Chase;
    public bool IsHurt  => _hurtTimer > 0f;

    public Enemy(Vector2 pos, Level level)
    {
        Id = _nextId++;
        Position = pos;
        SpawnPosition = pos;
        _level = level;
    }

    public bool TakeDamage(float dmg)
    {
        if (!IsAlive) return false;

        Hp -= dmg;
        _hurtTimer = HurtFlicker;

        if (Hp <= 0f)
        {
            State = EnemyState.Dying;
            _deathTimer = DeathFlicker;
            return true;
        }
        return false;
    }

    /// Empurra o inimigo na direção 'dirX' (-1 ou +1) com força.
    public void ApplyKnockback(int dirX, float strength)
    {
        if (!IsAlive) return;
        Velocity.X = dirX * strength;
        Velocity.Y = -60f; // pulinho
    }

    public void Kill()
    {
        if (!IsAlive) return;
        Hp = 0f;
        State = EnemyState.Dying;
        _deathTimer = DeathFlicker;
    }

    public void Update(float dt, Vector2 playerPos)
    {
        _hurtTimer = MathF.Max(0f, _hurtTimer - dt);

        // ---------- Respawn ----------
        if (State == EnemyState.Dead)
        {
            RespawnTimer -= dt;
            if (RespawnTimer <= 0f)
            {
                Position = SpawnPosition;
                Velocity = Vector2.Zero;
                Hp = MaxHp;
                State = EnemyState.Patrol;
            }
            return;
        }

        // ---------- Morrendo ----------
        if (State == EnemyState.Dying)
        {
            _deathTimer -= dt;
            if (_deathTimer <= 0f)
            {
                State = EnemyState.Dead;
                RespawnTimer = RespawnTime;
            }
            return;
        }

        // ---------- IA ----------
        float dist = Vector2.Distance(Position, playerPos);
        float range = State == EnemyState.Chase ? LoseRange : DetectRange;

        if (dist < range && MathF.Abs(playerPos.Y - Position.Y) < 40f)
            State = EnemyState.Chase;
        else
            State = EnemyState.Patrol;

        float speed = State == EnemyState.Chase ? ChaseSpeed : PatrolSpeed;

        // ---------- Knockback vs IA ----------
        // Se a velocidade horizontal é alta (veio de knockback),
        // aplica atrito e NÃO sobrescreve com IA
        bool inKnockback = MathF.Abs(Velocity.X) > KnockbackMinSpeed;

        if (inKnockback)
        {
            // Aplica atrito
            float sign = MathF.Sign(Velocity.X);
            Velocity.X = MathUtil_Approach(Velocity.X, 0f, KnockbackDecel * dt);
            if (MathF.Sign(Velocity.X) != sign) Velocity.X = 0f;
        }
        else
        {
            // IA controla normalmente
            if (State == EnemyState.Chase)
            {
                int dir = playerPos.X > Position.X ? 1 : -1;
                Facing = dir;
                Velocity.X = dir * speed;
            }
            else
            {
                Velocity.X = Facing * speed;
            }
        }

        Velocity.Y = MathF.Min(Velocity.Y + Gravity * dt, MaxFall);

        MoveX(Velocity.X * dt, _level);
        MoveY(Velocity.Y * dt, _level);

        // ---------- Comportamento com parede/beirada ----------
        if (State == EnemyState.Patrol && !inKnockback)
        {
            if (_wallDir != 0) Facing = -_wallDir;
            if (_onGround)
            {
                var frontFoot = new Rectangle(
                    Facing > 0 ? Bounds.X + Bounds.Width : Bounds.X - 2f,
                    Bounds.Y + Bounds.Height,
                    2f, 2f);
                if (!_level.CollidesAny(frontFoot)) Facing = -Facing;
            }
        }
        else if (State == EnemyState.Chase && !inKnockback)
        {
            if (_wallDir != 0 && _onGround) Velocity.Y = -160f;
        }
    }

    public void SeparateFrom(List<Enemy> others, float dt)
    {
        if (!IsAlive) return;

        foreach (var other in others)
        {
            if (other == this) continue;
            if (!other.IsAlive) continue;

            float dx = other.Position.X - Position.X;
            float adx = MathF.Abs(dx);

            if (adx >= MinSeparation) continue;
            if (MathF.Abs(other.Position.Y - Position.Y) > 6f) continue;

            int pushDir;
            if (adx < 0.01f)
                pushDir = (Id % 2 == 0) ? 1 : -1;
            else
                pushDir = dx > 0 ? -1 : 1;

            float overlap = MinSeparation - adx;
            float push = SeparateForce * dt * (overlap / MinSeparation);
            Position.X += pushDir * push;
        }
    }

    private void MoveX(float dx, Level level)
    {
        _wallDir = 0;
        Position.X += dx;
        var rect = Bounds;
        foreach (var t in level.SolidTilesNear(rect))
        {
            if (!Raylib.CheckCollisionRecs(rect, t)) continue;
            if (dx > 0) { Position.X = t.X - Size.X / 2f; _wallDir = 1; }
            else        { Position.X = t.X + t.Width + Size.X / 2f; _wallDir = -1; }
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

    // Helper local (não pode usar MathUtil.Approach direto porque tá em Core)
    private static float MathUtil_Approach(float v, float target, float maxDelta)
    {
        if (v < target) return MathF.Min(v + maxDelta, target);
        if (v > target) return MathF.Max(v - maxDelta, target);
        return v;
    }
}