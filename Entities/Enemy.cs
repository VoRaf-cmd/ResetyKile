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

    public const float MaxHp = 2.0f;

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
        Position = pos;
        SpawnPosition = pos;
        _level = level;
    }

    /// Aplica dano. Retorna true se morreu com esse dano.
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

    /// Mata instantâneo (uso interno / super).
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

        Velocity.Y = MathF.Min(Velocity.Y + Gravity * dt, MaxFall);

        MoveX(Velocity.X * dt, _level);
        MoveY(Velocity.Y * dt, _level);

        if (State == EnemyState.Patrol)
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
        else
        {
            if (_wallDir != 0 && _onGround) Velocity.Y = -160f;
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
}