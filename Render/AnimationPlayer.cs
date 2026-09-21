using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Render;

/// <summary>
/// Uma animação: spritesheet + duração por frame + loop.
/// </summary>
public class Animation
{
    public SpriteSheet Sheet;
    public float FrameDuration;
    public bool Loop;

    public int FrameCount => Sheet.FrameCount;

    public Animation(SpriteSheet sheet, float frameDuration, bool loop)
    {
        Sheet = sheet;
        FrameDuration = frameDuration;
        Loop = loop;
    }
}

/// <summary>
/// Toca UMA animação por vez. Avança frames por TEMPO (não por FPS).
/// </summary>
public class AnimationPlayer
{
    private Animation? _current;
    private float _timer;
    private int _frame;

    public string CurrentName { get; private set; } = "";

    /// Índice do frame atual (0..FrameCount-1)
    public int CurrentFrameIndex => _frame;

    public void Play(string name, Animation animation, bool restart = false)
    {
        if (_current == animation && !restart) return;

        _current = animation;
        CurrentName = name;
        _timer = 0f;
        _frame = 0;
    }

    public void Update(float dt)
    {
        if (_current == null) return;

        _timer += dt;
        while (_timer >= _current.FrameDuration)
        {
            _timer -= _current.FrameDuration;
            _frame++;

            if (_frame >= _current.FrameCount)
            {
                if (_current.Loop)
                    _frame = 0;
                else
                    _frame = _current.FrameCount - 1;
            }
        }
    }

    public bool IsFinished
        => _current != null && !_current.Loop && _frame >= _current.FrameCount - 1;

    public void DrawCentered(Vector2 center, bool flipX, Color tint)
    {
        if (_current == null) return;

        var topLeft = new Vector2(
            center.X - _current.Sheet.FrameWidth / 2f,
            center.Y - _current.Sheet.FrameHeight / 2f);

        _current.Sheet.Draw(_frame, topLeft, flipX, tint);
    }
}