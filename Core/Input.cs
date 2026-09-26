using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Core;

public enum InputDevice { Keyboard, Gamepad }
public enum GamepadBrand { Unknown, Xbox, PlayStation }

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

    // Novo: mapa
    public bool MapPressed;          // abre/fecha
    public bool MapConfirmPressed;   // confirma
    public int  MapNavigateX;        // -1, 0, +1
    public int  MapNavigateY;        // -1, 0, +1
}

public static class Input
{
    private const GamepadButton GP_Jump  = GamepadButton.RightFaceDown;  // A / Cross
    private const GamepadButton GP_Dash  = GamepadButton.RightFaceLeft;  // X / Square
    private const GamepadButton GP_Super = GamepadButton.RightFaceRight; // B / Circle
    private const GamepadButton GP_Pause = GamepadButton.MiddleRight;    // Start / Options
    private const GamepadButton GP_Map   = GamepadButton.MiddleLeft;     // Select / Share

    private const GamepadAxis AX_LX = GamepadAxis.LeftX;
    private const GamepadAxis AX_LY = GamepadAxis.LeftY;
    private const GamepadAxis AX_RT = GamepadAxis.RightTrigger;

    private const float Deadzone        = 0.25f;
    private const float TriggerDeadzone = 0.5f;

    private static bool _prevTriggerDown;

    public static InputDevice LastDevice { get; private set; } = InputDevice.Keyboard;
    public static GamepadBrand Brand    { get; private set; } = GamepadBrand.Unknown;

    public static InputState Read()
    {
        var s = new InputState();
        bool pad = Raylib.IsGamepadAvailable(0);

        if (pad && Brand == GamepadBrand.Unknown)
            Brand = DetectBrand();

        // ---- Movimento horizontal ----
        float mx = 0f;
        bool keyboardUsed = false;
        bool gamepadUsed = false;

        if (Raylib.IsKeyDown(KeyboardKey.A)) { mx -= 1f; keyboardUsed = true; }
        if (Raylib.IsKeyDown(KeyboardKey.D)) { mx += 1f; keyboardUsed = true; }

        if (pad)
        {
            float ax = Raylib.GetGamepadAxisMovement(0, AX_LX);
            if (MathF.Abs(ax) > Deadzone) { mx = ax; gamepadUsed = true; }

            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft))  { mx = -1f; gamepadUsed = true; }
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) { mx =  1f; gamepadUsed = true; }
        }

        s.Move     = new Vector2(mx, 0f);
        s.MoveXInt = MathF.Abs(mx) < Deadzone ? 0 : Math.Sign(mx);

        // ---- Movimento vertical ----
        bool padDownPressed = pad && Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceDown);
        bool stickDown      = pad && Raylib.GetGamepadAxisMovement(0, AX_LY) > 0.5f;
        bool sKeyPressed    = Raylib.IsKeyPressed(KeyboardKey.S);
        bool downKeyPressed = Raylib.IsKeyPressed(KeyboardKey.Down);

        s.MoveDownPressed = sKeyPressed || downKeyPressed || padDownPressed || stickDown;

        if (sKeyPressed || downKeyPressed) keyboardUsed = true;
        if (padDownPressed || stickDown) gamepadUsed = true;

        // ---- Jump ----
        s.JumpHeld     = Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.C)
                      || (pad && Raylib.IsGamepadButtonDown(0, GP_Jump));
        s.JumpPressed  = Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.C)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GP_Jump));
        s.JumpReleased = Raylib.IsKeyReleased(KeyboardKey.Space) || Raylib.IsKeyReleased(KeyboardKey.C)
                      || (pad && Raylib.IsGamepadButtonReleased(0, GP_Jump));

        if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.C)) keyboardUsed = true;
        if (pad && Raylib.IsGamepadButtonPressed(0, GP_Jump)) gamepadUsed = true;

        // ---- Dash ----
        s.DashHeld    = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.X)
                     || (pad && Raylib.IsGamepadButtonDown(0, GP_Dash));
        s.DashPressed = Raylib.IsKeyPressed(KeyboardKey.LeftShift) || Raylib.IsKeyPressed(KeyboardKey.X)
                     || (pad && Raylib.IsGamepadButtonPressed(0, GP_Dash));

        if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) || Raylib.IsKeyPressed(KeyboardKey.X)) keyboardUsed = true;
        if (pad && Raylib.IsGamepadButtonPressed(0, GP_Dash)) gamepadUsed = true;

        // ---- Attack ----
        bool trigger = pad && Raylib.GetGamepadAxisMovement(0, AX_RT) > TriggerDeadzone;
        bool atkKeyDown    = Raylib.IsKeyDown(KeyboardKey.Z)    || Raylib.IsKeyDown(KeyboardKey.J);
        bool atkKeyPressed = Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.J);

        s.AttackHeld    = atkKeyDown || trigger;
        s.AttackPressed = atkKeyPressed || (trigger && !_prevTriggerDown);
        _prevTriggerDown = trigger;

        if (atkKeyPressed) keyboardUsed = true;
        if (trigger) gamepadUsed = true;

        // ---- Super ----
        s.SuperHeld    = Raylib.IsKeyDown(KeyboardKey.E) || Raylib.IsKeyDown(KeyboardKey.K)
                      || (pad && Raylib.IsGamepadButtonDown(0, GP_Super));
        s.SuperPressed = Raylib.IsKeyPressed(KeyboardKey.E) || Raylib.IsKeyPressed(KeyboardKey.K)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GP_Super));

        if (Raylib.IsKeyPressed(KeyboardKey.E) || Raylib.IsKeyPressed(KeyboardKey.K)) keyboardUsed = true;
        if (pad && Raylib.IsGamepadButtonPressed(0, GP_Super)) gamepadUsed = true;

        // ---- Pause ----
        s.PausePressed = Raylib.IsKeyPressed(KeyboardKey.Escape)
                      || (pad && Raylib.IsGamepadButtonPressed(0, GP_Pause));

        // ---- Reset (L3 + R3 juntos) ----
        bool l3 = pad && Raylib.IsGamepadButtonDown(0, GamepadButton.LeftThumb);
        bool r3 = pad && Raylib.IsGamepadButtonDown(0, GamepadButton.RightThumb);
        bool resetPad = l3 && r3;
        bool resetKey = Raylib.IsKeyPressed(KeyboardKey.R);

        s.ResetPressed = resetKey || resetPad;

        // ---- Mapa ----
        bool mapKey = Raylib.IsKeyPressed(KeyboardKey.Tab);
        bool mapPad = pad && Raylib.IsGamepadButtonPressed(0, GP_Map);
        s.MapPressed = mapKey || mapPad;

        // Confirma no mapa
        bool confirmKey = Raylib.IsKeyPressed(KeyboardKey.Enter);
        bool confirmPad = pad && Raylib.IsGamepadButtonPressed(0, GP_Jump);   // A / Cross
        s.MapConfirmPressed = confirmKey || confirmPad;

        // Navegação no mapa (X e Y)
        int navX = 0, navY = 0;

        if (Raylib.IsKeyPressed(KeyboardKey.Left))  navX = -1;
        if (Raylib.IsKeyPressed(KeyboardKey.Right)) navX =  1;
        if (Raylib.IsKeyPressed(KeyboardKey.Up))    navY = -1;
        if (Raylib.IsKeyPressed(KeyboardKey.Down))  navY =  1;

        if (pad)
        {
            if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceLeft))  navX = -1;
            if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceRight)) navX =  1;
            if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceUp))    navY = -1;
            if (Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceDown))  navY =  1;
        }

        s.MapNavigateX = navX;
        s.MapNavigateY = navY;

        // ---- Atualiza último dispositivo ----
        if (gamepadUsed) LastDevice = InputDevice.Gamepad;
        else if (keyboardUsed) LastDevice = InputDevice.Keyboard;

        return s;
    }

    private static GamepadBrand DetectBrand()
    {
        try
        {
            unsafe
            {
                sbyte* raw = Raylib.GetGamepadName(0);
                if (raw == null) return GamepadBrand.Unknown;

                string name = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((IntPtr)raw) ?? "";
                name = name.ToLowerInvariant();

                if (name.Contains("sony") || name.Contains("dualshock") || name.Contains("dualsense")
                    || name.Contains("playstation") || name.Contains("ps3") || name.Contains("ps4") || name.Contains("ps5")
                    || name.Contains("wireless controller"))
                    return GamepadBrand.PlayStation;

                if (name.Contains("xbox") || name.Contains("xinput") || name.Contains("microsoft"))
                    return GamepadBrand.Xbox;

                return GamepadBrand.Unknown;
            }
        }
        catch
        {
            return GamepadBrand.Unknown;
        }
    }

    public static string GetJumpButtonLabel()
    {
        if (LastDevice == InputDevice.Gamepad)
        {
            return Brand switch
            {
                GamepadBrand.PlayStation => "X",
                GamepadBrand.Xbox        => "A",
                _                         => "A",
            };
        }

        return "SPACE";
    }

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