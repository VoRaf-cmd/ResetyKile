using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Core;

public struct InputState
{
    public Vector2 Move;
    public int MoveXInt;

    public bool JumpHeld, JumpPressed, JumpReleased;
    public bool DashHeld, DashPressed;
    public bool AttackHeld, AttackPressed;
    public bool SuperHeld, SuperPressed;

    public bool PausePressed;
    public bool ConfirmPressed;
}

public static class Input
{
    private const GamepadButton GP_Jump   = GamepadButton.RightFaceDown;   // A / Cross
    private const GamepadButton GP_Dash   = GamepadButton.RightFaceLeft;   // X / Square
    private const GamepadButton GP_Super  = GamepadButton.RightFaceRight;  // B / Circle
    private const GamepadButton GP_Pause  = GamepadButton.MiddleRight;     // Start / Options

    private const GamepadAxis GP_AX_LX = GamepadAxis.LeftX;
    private const GamepadAxis GP_AX_LY = GamepadAxis.LeftY;
    private const GamepadAxis GP_AX_RT = GamepadAxis.RightTrigger;

    private const float Deadzone        = 0.25f;
    private const float TriggerDeadzone = 0.5f;

    // Cache do estado anterior do gatilho (pra edge detection)
    private static bool _prevTriggerDown = false;

    public static InputState Read()
    {
        var s = new InputState();
        bool padOn = Raylib.IsGamepadAvailable(0);

        // ---- Eixo X ----
        float mx = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) mx -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) mx += 1f;

        if (padOn)
        {
            float ax = Raylib.GetGamepadAxisMovement(0, GP_AX_LX);
            if (MathF.Abs(ax) > Deadzone) mx = ax;

            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft))  mx = -1f;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) mx =  1f;
        }

        s.Move = new Vector2(mx, 0f);
        s.MoveXInt = MathF.Abs(mx) < Deadzone ? 0 : Math.Sign(mx);

        // ---- Jump ----
        s.JumpHeld     = Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.C)
                      || (padOn && Raylib.IsGamepadButtonDown(0, GP_Jump));
        s.JumpPressed  = Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.C)
                      || (padOn && Raylib.IsGamepadButtonPressed(0, GP_Jump));
        s.JumpReleased = Raylib.IsKeyReleased(KeyboardKey.Space) || Raylib.IsKeyReleased(KeyboardKey.C)
                      || (padOn && Raylib.IsGamepadButtonReleased(0, GP_Jump));

        // ---- Dash ----
        s.DashHeld    = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.X)
                     || (padOn && Raylib.IsGamepadButtonDown(0, GP_Dash));
        s.DashPressed = Raylib.IsKeyPressed(KeyboardKey.LeftShift) || Raylib.IsKeyPressed(KeyboardKey.X)
                     || (padOn && Raylib.IsGamepadButtonPressed(0, GP_Dash));

        // ---- Attack (RT / R2 — tratado como botão via gatilho analógico) ----
        bool triggerDown = false;
        if (padOn)
        {
            float triggerValue = Raylib.GetGamepadAxisMovement(0, GP_AX_RT);
            triggerDown = triggerValue > TriggerDeadzone;
        }

        bool attackKeyDown     = Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.J);
        bool attackKeyPressed  = Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.J);

        s.AttackHeld    = attackKeyDown || triggerDown;
        s.AttackPressed = attackKeyPressed || (triggerDown && !_prevTriggerDown);

        _prevTriggerDown = triggerDown;

        // ---- Super ----
        s.SuperHeld    = Raylib.IsKeyDown(KeyboardKey.E) || Raylib.IsKeyDown(KeyboardKey.K)
                      || (padOn && Raylib.IsGamepadButtonDown(0, GP_Super));
        s.SuperPressed = Raylib.IsKeyPressed(KeyboardKey.E) || Raylib.IsKeyPressed(KeyboardKey.K)
                      || (padOn && Raylib.IsGamepadButtonPressed(0, GP_Super));

        // ---- Pause ----
        s.PausePressed = Raylib.IsKeyPressed(KeyboardKey.Escape)
                      || (padOn && Raylib.IsGamepadButtonPressed(0, GP_Pause));

        return s;
    }

    public static (int x, int y) DashDirection(InputState s, int facing)
    {
        int x = 0, y = 0;
        bool padOn = Raylib.IsGamepadAvailable(0);

        if (Raylib.IsKeyDown(KeyboardKey.A)) x -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D)) x += 1;
        if (Raylib.IsKeyDown(KeyboardKey.W)) y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S)) y += 1;

        if (padOn)
        {
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft))  x = -1;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) x =  1;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceUp))    y = -1;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceDown))  y =  1;

            float ax = Raylib.GetGamepadAxisMovement(0, GP_AX_LX);
            float ay = Raylib.GetGamepadAxisMovement(0, GP_AX_LY);
            if (MathF.Abs(ax) > 0.5f) x = Math.Sign(ax);
            if (MathF.Abs(ay) > 0.5f) y = Math.Sign(ay);
        }

        if (x == 0 && y == 0) x = facing;
        return (x, y);
    }
}