using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Core;

public struct InputState
{
    public Vector2 Move;
    public int MoveXInt;
    public bool MoveDownPressed;

    public bool JumpHeld, JumpPressed, JumpReleased;
    public bool DashHeld, DashPressed;
    public bool AttackHeld, AttackPressed;
    public bool SuperHeld, SuperPressed;

    public bool PausePressed;
    public bool ResetPressed;
}

public static class Input
{
    // ---- Botões do gamepad ----
    private const GamepadButton GP_Jump  = GamepadButton.RightFaceDown;  // A / Cross
    private const GamepadButton GP_Dash  = GamepadButton.RightFaceLeft;  // X / Square
    private const GamepadButton GP_Super = GamepadButton.RightFaceRight; // B / Circle
    private const GamepadButton GP_Pause = GamepadButton.MiddleRight;    // Start / Options

    // ---- Analógicos ----
    private const GamepadAxis AX_LX = GamepadAxis.LeftX;
    private const GamepadAxis AX_LY = GamepadAxis.LeftY;
    private const GamepadAxis AX_RT = GamepadAxis.RightTrigger;

    // ---- Deadzones ----
    private const float Deadzone        = 0.25f;
    private const float TriggerDeadzone = 0.5f;

    // Cache do gatilho (edge detection do ataque)
    private static bool _prevTriggerDown;

    public static InputState Read()
    {
        var s = new InputState();
        bool pad = Raylib.IsGamepadAvailable(0);

        // ---- Movimento horizontal ----
        float mx = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) mx -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) mx += 1f;

        if (pad)
        {
            float ax = Raylib.GetGamepadAxisMovement(0, AX_LX);
            if (MathF.Abs(ax) > Deadzone) mx = ax;

            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft))  mx = -1f;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) mx =  1f;
        }

        s.Move     = new Vector2(mx, 0f);
        s.MoveXInt = MathF.Abs(mx) < Deadzone ? 0 : Math.Sign(mx);

        // ---- Movimento vertical (baixo) — teclado, D-pad e analógico ----
        bool padDownPressed = pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceDown);
        bool stickDown      = pad && Raylib.GetGamepadAxisMovement(0, AX_LY) > 0.5f;

        s.MoveDownPressed = Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down)
                         || padDownPressed || stickDown;

        // ---- Jump ----
        s.JumpHeld     = Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.C)
                      || (pad && Raylib.IsGamepadButtonDown(0, GP_Jump));
        s.JumpPressed  = Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.C)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GP_Jump));
        s.JumpReleased = Raylib.IsKeyReleased(KeyboardKey.Space) || Raylib.IsKeyReleased(KeyboardKey.C)
                      || (pad && Raylib.IsGamepadButtonReleased(0, GP_Jump));

        // ---- Dash ----
        s.DashHeld    = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.X)
                     || (pad && Raylib.IsGamepadButtonDown(0, GP_Dash));
        s.DashPressed = Raylib.IsKeyPressed(KeyboardKey.LeftShift) || Raylib.IsKeyPressed(KeyboardKey.X)
                     || (pad && Raylib.IsGamepadButtonPressed(0, GP_Dash));

        // ---- Attack (RT/R2 tratado como botão) ----
        bool trigger = pad && Raylib.GetGamepadAxisMovement(0, AX_RT) > TriggerDeadzone;
        bool atkKeyDown    = Raylib.IsKeyDown(KeyboardKey.Z)    || Raylib.IsKeyDown(KeyboardKey.J);
        bool atkKeyPressed = Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.J);

        s.AttackHeld    = atkKeyDown || trigger;
        s.AttackPressed = atkKeyPressed || (trigger && !_prevTriggerDown);
        _prevTriggerDown = trigger;

        // ---- Super ----
        s.SuperHeld    = Raylib.IsKeyDown(KeyboardKey.E) || Raylib.IsKeyDown(KeyboardKey.K)
                      || (pad && Raylib.IsGamepadButtonDown(0, GP_Super));
        s.SuperPressed = Raylib.IsKeyPressed(KeyboardKey.E) || Raylib.IsKeyPressed(KeyboardKey.K)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GP_Super));

        // ---- Pause ----
        s.PausePressed = Raylib.IsKeyPressed(KeyboardKey.Escape)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GP_Pause));

        // ---- Reset (R / Select / L3 / R3) ----
        s.ResetPressed = Raylib.IsKeyPressed(KeyboardKey.R)
                      || (pad && (Raylib.IsGamepadButtonPressed(0, GamepadButton.MiddleLeft)
                              ||  Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftThumb)
                              ||  Raylib.IsGamepadButtonPressed(0, GamepadButton.RightThumb)));

        return s;
    }

    /// Direção do dash (8 direções). Se não houver direção, usa o facing.
    public static (int x, int y) DashDirection(InputState s, int facing)
    {
        int x = 0, y = 0;
        bool pad = Raylib.IsGamepadAvailable(0);

        if (Raylib.IsKeyDown(KeyboardKey.A)) x -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D)) x += 1;
        if (Raylib.IsKeyDown(KeyboardKey.W)) y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S)) y += 1;

        if (pad)
        {
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft))  x = -1;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) x =  1;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceUp))    y = -1;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceDown))  y =  1;

            float ax = Raylib.GetGamepadAxisMovement(0, AX_LX);
            float ay = Raylib.GetGamepadAxisMovement(0, AX_LY);
            if (MathF.Abs(ax) > 0.5f) x = Math.Sign(ax);
            if (MathF.Abs(ay) > 0.5f) y = Math.Sign(ay);
        }

        if (x == 0 && y == 0) x = facing;
        return (x, y);
    }
}