namespace ResetyKile.Render;

/// <summary>
/// Congela o jogo por alguns milissegundos pra dar "peso" ao impacto.
/// Enquanto ativo, o Update não roda (mas o Render sim).
/// </summary>
public class HitStop
{
    public float Timer { get; private set; }

    public bool Active => Timer > 0f;

    public void Trigger(float duration) => Timer = duration;

    public void Update(float dt) => Timer -= dt;
}