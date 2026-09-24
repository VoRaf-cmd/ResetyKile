using System.Numerics;
using ResetyKile.Entities;

namespace ResetyKile.World;

public class LevelState
{
    public string Name { get; }
    public List<Enemy> Enemies { get; set; } = new();
    public HashSet<int> DeadEnemyIds { get; } = new();
    public HashSet<int> CollectedItemIds { get; } = new();
    public Dictionary<string, bool> Flags { get; } = new();
    public Vector2 SpawnPosition { get; set; }
    public bool Initialized { get; set; } = false;

    public LevelState(string name) { Name = name; }

    public int AliveCount
    {
        get
        {
            int c = 0;
            foreach (var e in Enemies) if (e.IsAlive) c++;
            return c;
        }
    }

    public int TotalCount => Enemies.Count;
    public bool IsCleared => AliveCount == 0 && TotalCount > 0;

    public void MarkEnemyDead(int enemyId) => DeadEnemyIds.Add(enemyId);
    public bool IsEnemyDead(int enemyId) => DeadEnemyIds.Contains(enemyId);
    public void MarkItemCollected(int itemId) => CollectedItemIds.Add(itemId);
    public bool IsItemCollected(int itemId) => CollectedItemIds.Contains(itemId);

    public void Reset()
    {
        DeadEnemyIds.Clear();
        CollectedItemIds.Clear();
        Flags.Clear();
        foreach (var e in Enemies) e.Reset();
    }
}