using Raylib_cs;
using System.Numerics;
using ResetyKile.Core;
using ResetyKile.Render;
using ResetyKile.World;

namespace ResetyKile.Entities;

public enum PlayerState { Normal, Dashing, WallSlide, LickingKatana, Super, Dead }

public class Player
{
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

    // Wall jump
    private const float WallJumpTapDelay    = 0.10f;
    private const float WallJumpChargeTime  = 0.6f;
    private const float WallJumpMinBoost    = 120f;
    private const float WallJumpMaxBoost    = 280f;
    private const float WallJumpMinCharge   = 0.33f;
    private const float WallJumpCooldown    = 0.15f;
    private const float WallJumpInputLock   = 0.15f;
    private const float WallJumpAnimDuration = 0.35f;

    // Momentum aéreo
    private const float AirMomentumDuration = 0.5f;
    private const float AirMomentumDecel    = 30f;

    // Stamina
    public  const float MaxStamina              = 3f;
    public  const float DashCost                = 1.0f;
    private const float StaminaRegenWithCharges = 12f;
    private const float StaminaRegenEmpty       = 35f;
    private const float ThreatRange             = 150f;
    private const int   MaxThreatsAllowed       = 2;

    // Recompensa de dash-attack
    private const float DashAttackRewardOne = 0.5f;
    private const float DashAttackRewardTwo = 1.0f;

    private const float LickDuration  = 0.6f;
    private const float SuperDuration = 8.0f;
    private const int   SoulsToSuper  = 10;

    private const float AttackDuration = 0.15f;
    private const float AttackCooldown = 0.25f;
    private const float AttackRange    = 21f;
    private const float AttackHeight   = 13f;

    public  const int   MaxHp              = 20;
    private const float InvulnDuration     = 1.0f;
    private const float KnockbackX         = 120f;
    private const float KnockbackY         = -90f;

    public const int SpriteWidth  = 16;
    public const int SpriteHeight = 24;
    private const float SpriteYOffset = -6f;
    public  const float SpriteYOffsetPublic = -6f;

    private const float DropDuration = 0.2f;

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
    private float _staminaRegenTimer = 0f;
    private int   _nearbyThreats = 0;

    public float InvulnTimer { get; private set; }
    public bool IsInvulnerable => InvulnTimer > 0f
                                || State == PlayerState.Dashing
                                || State == PlayerState.Super;

    private float _coyote, _jumpBuf, _varJump, _dashTimer, _lickTimer;
    private bool  _onGround;
    private int   _dashDirX, _dashDirY;
    private int   _wallDir;
    private float _wallJumpInputLockTimer;
    private float _wallJumpAnimTimer;
    private float _wallJumpCooldownTimer;
    private float _airMomentumTimer;

    private bool  _jumpHeldOnWall = false;
    private float _wallJumpHoldTimer = 0f;
    private bool  _chargingWallJump = false;
    private float _wallJumpCharge = 0f;

    private float _attackTimer;
    private float _attackCd;
    public bool IsAttacking => _attackTimer > 0f;
    public bool HasHitThisSwing { get; private set; }

    private float _dropTimer;
    public bool IsDroppingThrough => _dropTimer > 0f;

    private AnimationPlayer _anim = new();
    private Dictionary<string, Animation> _animations = new();

    private Katana _katana = new();
    private WallChargeArrow _chargeArrow = new();
    private FloatingText _floatingText = new();

    public const int MaxSouls = 10;

    public Rectangle Bounds => new(Position.X - Size.X / 2f, Position.Y - Size.Y / 2f, Size.X, Size.Y);
    public bool OnGround => _onGround;
    public bool IsDashing => State == PlayerState.Dashing;
    public bool SuperReady => Souls >= MaxSouls;

    public bool CanDash(float cost)
    {
        if (State == PlayerState.Super) return true;
        return Stamina >= cost;
    }

    public float StaminaRegenProgress
    {
        get
        {
            if (Stamina >= MaxStamina) return 1f;
            float timeNeeded = Stamina >= 1f ? StaminaRegenWithCharges : StaminaRegenEmpty;
            return Math.Clamp(_staminaRegenTimer / timeNeeded, 0f, 1f);
        }
    }

    public int NearbyThreats => _nearbyThreats;
    public bool IsStaminaPaused => _nearbyThreats > MaxThreatsAllowed;

    public float WallChargeProgress => _chargingWallJump ? _wallJumpCharge : 0f;
    public bool IsChargingWallJump => _chargingWallJump;

    public SpriteSheet? CurrentSheet
    {
        get
        {
            if (_animations.TryGetValue(_anim.CurrentName, out var anim))
                return anim.Sheet;
            return null;
        }
    }

    public int CurrentFrame => _anim.CurrentFrameIndex;

    public Player()
    {
        LoadAnimations();
    }

    private void LoadAnimations()
    {
        const string basePath = "Assets/sprites/kile";

        AddAnim("idle",       $"{basePath}/idle.png",      4, 0.15f, true,  new Color((byte)240, (byte)240, (byte)240, (byte)255));
        AddAnim("run",        $"{basePath}/run.png",       6, 0.12f, true,  new Color((byte)120, (byte)220, (byte)255, (byte)255));
        AddAnim("jump",       $"{basePath}/jump.png",      2, 0.10f, false, new Color((byte)140, (byte)255, (byte)140, (byte)255));
        AddAnim("fall",       $"{basePath}/fall.png",      2, 0.15f, true,  new Color((byte)200, (byte)180, (byte)255, (byte)255));
        AddAnim("dash",       $"{basePath}/dash.png",      2, 0.10f, false, new Color((byte)120, (byte)220, (byte)255, (byte)255));
        AddAnim("wall_slide", $"{basePath}/wall_slide.png",2, 0.15f, true,  new Color((byte)200, (byte)200, (byte)220, (byte)255));
        AddAnim("wall_jump",  $"{basePath}/wall_jump.png", 2, 0.20f, false, new Color((byte)160, (byte)220, (byte)160, (byte)255));
        AddAnim("attack",     $"{basePath}/attack.png",    4, 0.04f, false, new Color((byte)255, (byte)220, (byte)120, (byte)255));
        AddAnim("lick",       $"{basePath}/lick.png",      6, 0.10f, false, new Color((byte)255, (byte)200, (byte)140, (byte)255));
        AddAnim("super_idle", $"{basePath}/super_idle.png",4, 0.15f, true,  new Color((byte)255, (byte)90, (byte)180, (byte)255));
        AddAnim("hurt",       $"{basePath}/hurt.png",      2, 0.10f, false, new Color((byte)255, (byte)100, (byte)100, (byte)255));
        AddAnim("death",      $"{basePath}/death.png",     6, 0.15f, false, new Color((byte)120, (byte)40, (byte)60, (byte)255));
    }

    private void AddAnim(string name, string path, int defaultFrames, float frameDuration, bool loop, Color placeholder)
    {
        var sheet = new SpriteSheet(path, SpriteWidth, SpriteHeight, placeholder);
        _animations[name] = new Animation(sheet, frameDuration, loop);
    }

    public void UnloadAnimations()
    {
        foreach (var anim in _animations.Values)
            anim.Sheet.Dispose();
        _animations.Clear();
    }

    public void AddSoul(int amount = 1)
        => Souls = Math.Clamp(Souls + amount, 0, MaxSouls);

    public void AddStamina(float amount)
    {
        float before = Stamina;
        Stamina = Math.Clamp(Stamina + amount, 0f, MaxStamina);

        float gained = Stamina - before;
        if (gained > 0.01f)
        {
            string label = gained >= 1f
                ? $"+{(int)gained}"
                : $"+{gained:0.#}";

            _floatingText.Spawn(label, Position + new Vector2(0f, -18f),
                new Color((byte)255, (byte)220, (byte)100, (byte)255), 0.9f,
                FloatingIcon.Bolt);
        }
    }

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

    public void RegisterDashAttackHit(int totalHits, int killed)
    {
        // Dash-attack é feature (tech), sem recompensa por enquanto.
        // Se um dia quiser dar recompensa, é só descomentar:
        // if (killed <= 0) return;
        // float reward = killed >= 2 ? DashAttackRewardTwo : DashAttackRewardOne;
        // AddStamina(reward);
    }

    public void Reset(Vector2 spawnPos)
    {
        Respawn(spawnPos);
    }

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
        _staminaRegenTimer = 0f;
        _nearbyThreats = 0;
        _onGround = false;
        _coyote = _jumpBuf = _varJump = 0f;
        _dashTimer = _lickTimer = 0f;
        _attackTimer = _attackCd = 0f;
        _dropTimer = 0f;
        _wallJumpInputLockTimer = 0f;
        _wallJumpAnimTimer = 0f;
        _wallJumpCooldownTimer = 0f;
        _airMomentumTimer = 0f;
        _jumpHeldOnWall = false;
        _wallJumpHoldTimer = 0f;
        _chargingWallJump = false;
        _wallJumpCharge = 0f;
        Facing = 1;
        _katana.Enabled = false;
        _chargeArrow.Enabled = false;
    }

        public void ConsumeForHouse()
    {
        Stamina = MathF.Max(0f, Stamina - 1f);
        Souls = Math.Max(0, Souls - 1);
    }

    public void HealHouse()
    {
        Hp = Math.Min(MaxHp, Hp + 10);   // metade de 20
    }

    public void Update(float dt, InputState input, Level level, List<Enemy>? enemies = null)
    {
        _coyote      = _onGround ? CoyoteTime : MathF.Max(0, _coyote - dt);
        _jumpBuf     = input.JumpPressed ? JumpBuffer : MathF.Max(0, _jumpBuf - dt);
        _varJump     = MathF.Max(0, _varJump - dt);
        _dashTimer   = MathF.Max(0, _dashTimer - dt);
        _attackTimer = MathF.Max(0, _attackTimer - dt);
        _attackCd    = MathF.Max(0, _attackCd - dt);
        InvulnTimer  = MathF.Max(0, InvulnTimer - dt);
        _dropTimer   = MathF.Max(0, _dropTimer - dt);
        _wallJumpInputLockTimer = MathF.Max(0, _wallJumpInputLockTimer - dt);
        _wallJumpAnimTimer      = MathF.Max(0, _wallJumpAnimTimer - dt);
        _wallJumpCooldownTimer  = MathF.Max(0, _wallJumpCooldownTimer - dt);
        _airMomentumTimer       = MathF.Max(0, _airMomentumTimer - dt);

        _nearbyThreats = CountNearbyThreats(enemies);
        UpdateStaminaRegen(dt);
        _floatingText.Update(dt);

        if (State == PlayerState.Dead)
        {
            Velocity.Y = MathF.Min(Velocity.Y + Gravity * dt, MaxFall);
            MoveX(Velocity.X * dt, level);
            MoveY(Velocity.Y * dt, level);
            Velocity.X = MathUtil.Approach(Velocity.X, 0f, RunDeccel * dt);
            UpdateAnimation(dt);
            UpdateKatana();
            _chargeArrow.Enabled = false;
            return;
        }

        if (State == PlayerState.Super)
        {
            SuperTimer -= dt;
            if (SuperTimer <= 0f) State = PlayerState.Normal;
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
            UpdateAnimation(dt);
            UpdateKatana();
            _chargeArrow.Enabled = false;
            return;
        }

        if (input.SuperPressed && SuperReady && State == PlayerState.Normal)
        {
            State = PlayerState.LickingKatana;
            _lickTimer = LickDuration;
            Velocity = Vector2.Zero;
            UpdateAnimation(dt);
            UpdateKatana();
            _chargeArrow.Enabled = false;
            return;
        }

        if (_onGround && input.MoveDownPressed && IsOnPlatform(level))
        {
            _dropTimer = DropDuration;
            _onGround = false;
            Velocity.Y = 30f;
        }

        bool wantDash = input.DashPressed && State != PlayerState.Dashing;

        if (wantDash)
        {
            if (CanDash(DashCost))
            {
                State = PlayerState.Dashing;
                _dashTimer = DashTime;

                if (State != PlayerState.Super)
                    Stamina = MathF.Max(0f, Stamina - DashCost);

                (_dashDirX, _dashDirY) = Input.DashDirection(input, Facing);
                if (_dashDirX != 0) Facing = _dashDirX;
                _airMomentumTimer = AirMomentumDuration;

                _chargingWallJump = false;
                _wallJumpHoldTimer = 0f;
                _jumpHeldOnWall = false;
                _wallJumpCharge = 0f;
            }
        }

        if (State == PlayerState.Dashing)
        {
            Velocity = new Vector2(_dashDirX, _dashDirY) * DashSpeed;
            if (_dashTimer <= 0f)
            {
                State = PlayerState.Normal;
                Velocity *= 0.6f;
            }
        }
        else
        {
            int moveInput = _wallJumpInputLockTimer > 0f ? 0 : input.MoveXInt;

            float targetX = moveInput * RunSpeed;
            float accel;
            if (!_onGround && _airMomentumTimer > 0f)
                accel = AirMomentumDecel;
            else if (_onGround)
                accel = moveInput != 0 ? RunAccel : RunDeccel;
            else
                accel = RunAccel * AirAccelMult;

            Velocity.X = MathUtil.Approach(Velocity.X, targetX, accel * dt);
            if (moveInput != 0) Facing = moveInput;

            float g = Gravity;
            if (Velocity.Y < 0 && input.JumpHeld) g *= 0.5f;
            if (MathF.Abs(Velocity.Y) < 40f && input.JumpHeld) g *= 0.5f;
            Velocity.Y = MathF.Min(Velocity.Y + g * dt, MaxFall);

            _wallDir = DetectWall(level);
            bool touchingWall = !_onGround && _wallDir != 0;

            bool movingAwayFromWall = _wallDir != 0 && moveInput == -_wallDir;
            bool movingDown = input.MoveDownPressed;

            bool shouldWallSlide = touchingWall
                                && !movingAwayFromWall
                                && !movingDown
                                && Velocity.Y > 0;

            if (shouldWallSlide)
            {
                State = PlayerState.WallSlide;
                Velocity.Y = MathF.Min(Velocity.Y, 40f);
            }
            else
            {
                if (State == PlayerState.WallSlide)
                    State = PlayerState.Normal;
            }

            bool onWall = State == PlayerState.WallSlide && _wallJumpCooldownTimer <= 0f;

            if (onWall && input.JumpPressed && !_jumpHeldOnWall)
            {
                _jumpHeldOnWall = true;
                _wallJumpHoldTimer = 0f;
                _chargingWallJump = false;
                _wallJumpCharge = 0f;
            }

            if (onWall && _jumpHeldOnWall && input.JumpHeld)
            {
                _wallJumpHoldTimer += dt;
                if (_wallJumpHoldTimer >= WallJumpTapDelay)
                {
                    _chargingWallJump = true;
                    _wallJumpCharge = MathF.Min(_wallJumpCharge + dt / WallJumpChargeTime, 1f);
                }
            }

            bool released = _jumpHeldOnWall && !input.JumpHeld;
            bool cancelAndFire = _chargingWallJump && movingAwayFromWall;

            if (_jumpHeldOnWall && (released || cancelAndFire))
            {
                if (_chargingWallJump && _wallJumpCharge >= WallJumpMinCharge)
                    ExecuteHoldWallJump(_wallJumpCharge);
                else
                    ExecuteTapWallJump();

                _jumpHeldOnWall = false;
                _chargingWallJump = false;
                _wallJumpCharge = 0f;
                _wallJumpHoldTimer = 0f;
            }

            if (!onWall && _jumpHeldOnWall && !cancelAndFire)
            {
                _jumpHeldOnWall = false;
                _chargingWallJump = false;
                _wallJumpCharge = 0f;
                _wallJumpHoldTimer = 0f;
            }

            if (_coyote > 0f && _jumpBuf > 0f && State != PlayerState.WallSlide)
            {
                Velocity.Y = -JumpSpeed;
                Velocity.X += input.MoveXInt * JumpHBoost;
                _jumpBuf = 0f; _coyote = 0f; _varJump = VarJumpTime;
            }

            if (_varJump > 0f)
            {
                if (!input.JumpHeld) _varJump = 0f;
                else Velocity.Y = MathF.Min(Velocity.Y, -JumpSpeed * 0.4f);
            }
        }

        MoveX(Velocity.X * dt, level);
        MoveY(Velocity.Y * dt, level);

        if (_onGround)
        {
            _wallJumpAnimTimer = 0f;
            _airMomentumTimer = 0f;
        }

        if (!IsAttacking) HasHitThisSwing = false;
        UpdateAnimation(dt);
        UpdateKatana();
        _chargeArrow.UpdateFade(dt);
        UpdateChargeArrow();
    }

    private void UpdateStaminaRegen(float dt)
    {
        if (Stamina >= MaxStamina)
        {
            _staminaRegenTimer = 0f;
            return;
        }

        if (State == PlayerState.Super)
            return;

        if (_nearbyThreats > MaxThreatsAllowed)
            return;

        float timePerCharge = Stamina >= 1f ? StaminaRegenWithCharges : StaminaRegenEmpty;

        _staminaRegenTimer += dt;

        while (_staminaRegenTimer >= timePerCharge && Stamina < MaxStamina)
        {
            _staminaRegenTimer -= timePerCharge;
            AddStamina(1f);

            timePerCharge = Stamina >= 1f ? StaminaRegenWithCharges : StaminaRegenEmpty;
        }
    }

    private int CountNearbyThreats(List<Enemy>? enemies)
    {
        if (enemies == null) return 0;

        int count = 0;
        float rangeSq = ThreatRange * ThreatRange;

        foreach (var e in enemies)
        {
            if (e.IsDead) continue;
            if (e.IsDying) continue;

            float d = Vector2.DistanceSquared(Position, e.Position);
            if (d <= rangeSq) count++;
        }

        return count;
    }

    private void ExecuteTapWallJump()
    {
        Velocity.Y = -JumpSpeed;
        Velocity.X = 0f;
        _varJump = VarJumpTime;
        _coyote = 0f;
        _wallJumpInputLockTimer = WallJumpInputLock;
        _wallJumpAnimTimer = WallJumpAnimDuration;
        _wallJumpCooldownTimer = WallJumpCooldown;
    }

    private void ExecuteHoldWallJump(float charge01)
    {
        float boost = WallJumpMinBoost + (WallJumpMaxBoost - WallJumpMinBoost) * Math.Clamp(charge01, 0f, 1f);

        int dirX = -_wallDir;
        Velocity.X = dirX * boost;
        Velocity.Y = -JumpSpeed;
        Facing = dirX;

        _varJump = VarJumpTime;
        _coyote = 0f;
        _wallJumpInputLockTimer = WallJumpInputLock;
        _wallJumpAnimTimer = WallJumpAnimDuration;
        _wallJumpCooldownTimer = WallJumpCooldown;
        _airMomentumTimer = AirMomentumDuration;
    }

    private void UpdateChargeArrow()
    {
        if (_chargingWallJump && _wallDir != 0)
        {
            _chargeArrow.Enabled = true;
            _chargeArrow.Charge = _wallJumpCharge;
            _chargeArrow.ButtonLabel = Core.Input.GetJumpButtonLabel();
        }
        else
        {
            _chargeArrow.Enabled = false;
        }
    }

    private void UpdateAnimation(float dt)
    {
        string targetAnim = ChooseAnimation();
        if (_animations.TryGetValue(targetAnim, out var anim))
            _anim.Play(targetAnim, anim);

        _anim.Update(dt);
    }

    private void UpdateKatana()
    {
        if (!IsAttacking)
        {
            _katana.Enabled = false;
            return;
        }

        _katana.Enabled = true;
        _katana.Facing = Facing;
        _katana.Position = new Vector2(4f, 0f);

        float t = _attackTimer / AttackDuration;

        float progress;
        if (t > 0.5f)
            progress = (1f - t) * 2f;
        else
            progress = t * 2f;

        _katana.Alpha = 1f;
        _katana.Progress = progress;
    }

    private string ChooseAnimation()
    {
        if (_wallJumpAnimTimer > 0f)            return "wall_jump";
        if (State == PlayerState.LickingKatana) return "lick";
        if (State == PlayerState.Dead)          return "death";
        if (IsAttacking)                        return "attack";
        if (State == PlayerState.Dashing)       return "dash";
        if (State == PlayerState.WallSlide)     return "wall_slide";
        if (InvulnTimer > 0f && Hp > 0 && State == PlayerState.Normal) return "hurt";
        if (State == PlayerState.Super && MathF.Abs(Velocity.X) < 1f)  return "super_idle";

        if (!_onGround)
        {
            if (Velocity.Y < -10f) return "jump";
            if (Velocity.Y >  10f) return "fall";
        }

        if (MathF.Abs(Velocity.X) > 5f) return "run";
        return "idle";
    }

    public void Draw(float gameTime)
    {
        bool blink = InvulnTimer > 0f
                  && ((int)(InvulnTimer * 20f) % 2 == 0);

        _chargeArrow.Draw(Position, gameTime);

        if (blink)
        {
            _floatingText.Draw();
            return;
        }

        Color tint = State switch
        {
            PlayerState.Super         => new Color((byte)255, (byte)180, (byte)230, (byte)255),
            PlayerState.Dead          => new Color((byte)150, (byte)100, (byte)120, (byte)255),
            _                         => Color.White,
        };

        var drawPos = new Vector2(Position.X, Position.Y + SpriteYOffset);
        _anim.DrawCentered(drawPos, Facing < 0, tint);

        _katana.Draw(Position);
        _floatingText.Draw();
    }

    private int DetectWall(Level level)
    {
        var r = Bounds;
        var probeL = new Rectangle(r.X - 1f, r.Y, 1f, r.Height);
        var probeR = new Rectangle(r.X + r.Width, r.Y, 1f, r.Height);
        if (level.CollidesAny(probeL, false)) return -1;
        if (level.CollidesAny(probeR, false)) return  1;
        return 0;
    }

    private bool IsOnPlatform(Level level)
    {
        var feet = new Rectangle(
            Position.X - Size.X / 2f + 1f,
            Position.Y + Size.Y / 2f,
            Size.X - 2f,
            2f);

        foreach (var t in level.PlatformTilesNear(feet))
            if (Raylib.CheckCollisionRecs(feet, t))
                return true;

        return false;
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

        if (dy > 0 && !IsDroppingThrough)
        {
            foreach (var t in level.PlatformTilesNear(rect))
            {
                if (!Raylib.CheckCollisionRecs(rect, t)) continue;
                Position.Y = t.Y - Size.Y / 2f;
                _onGround = true;
                Velocity.Y = 0;
                rect = Bounds;
            }
        }
    }
}