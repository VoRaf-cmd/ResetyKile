using Raylib_cs;
using System.Numerics;

namespace ResetyKile.Core;

/// <summary>
/// Controla o zoom da câmera, com transições suaves.
/// </summary>
public class CameraController
{
    public float Zoom { get; private set; } = 1.0f;

    private float _targetZoom = 1.0f;
    private const float LerpSpeed = 6f;

    /// Define o zoom alvo (a câmera converge pra ele suavemente).
    public void SetTargetZoom(float target)
    {
        _targetZoom = target;
    }

    public void Update(float dt)
    {
        Zoom = MathUtil.ExpLerp(Zoom, _targetZoom, LerpSpeed, dt);
    }

    public void Reset()
    {
        Zoom = 1.0f;
        _targetZoom = 1.0f;
    }
}