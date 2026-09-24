using System.Numerics;

namespace ResetyKile.World;

/// <summary>
/// Estado global do jogo — qual fase ativa, progresso, pontos desbloqueados.
/// </summary>
public class GameState
{
    /// Fases disponíveis (nome → LevelState)
    private readonly Dictionary<string, LevelState> _levelStates = new();

    /// Ordem das fases pra navegação
    private readonly List<string> _levelOrder = new();

    /// Fase atualmente ativa
    public string CurrentLevelName { get; private set; } = "";

    /// Custo de entrar na Casa do Tio
    public const float UncleHouseStaminaCost = 1f;
    public const int   UncleHouseSoulCost    = 1;
    public const int   UncleHouseHealAmount  = 10;   // metade de 20

    public void RegisterLevel(string name, Vector2 spawn)
    {
        if (!_levelStates.ContainsKey(name))
        {
            _levelStates[name] = new LevelState(name) { SpawnPosition = spawn };
            _levelOrder.Add(name);
        }
    }

    public LevelState GetLevelState(string name)
    {
        if (!_levelStates.TryGetValue(name, out var state))
            throw new Exception($"Fase '{name}' não registrada");
        return state;
    }

    public IReadOnlyList<string> AllLevels => _levelOrder;

    public void SetCurrentLevel(string name)
    {
        CurrentLevelName = name;
    }

    public string GetNextLevel(string name)
    {
        int idx = _levelOrder.IndexOf(name);
        if (idx < 0 || idx + 1 >= _levelOrder.Count) return _levelOrder[0];
        return _levelOrder[idx + 1];
    }

    public string GetPrevLevel(string name)
    {
        int idx = _levelOrder.IndexOf(name);
        if (idx <= 0) return _levelOrder[^1];
        return _levelOrder[idx - 1];
    }
}