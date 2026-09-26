using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;

namespace ResetyKile.World;

public static class LevelLoader
{
    public class LoadResult
    {
        public Level Level = null!;
        public Vector2 PlayerSpawn;
    }

    // ============================================================
    // MAPEAMENTO DE TILES (por ID)
    // ============================================================
    private static TileType MapTile(int id) => id switch
    {
        // ---- Placeholder (IDs 1-32) ----
        0  => TileType.Empty,
        1  => TileType.Solid,      // chão marrom
        2  => TileType.Solid,      // parede cinza
        3  => TileType.Platform,   // plataforma bege
        4  => TileType.Platform,   // verde
        5  => TileType.Solid,      // teto roxo
        6  => TileType.Empty,      // porta (passa)
        7  => TileType.Empty,      // vazio
        8  => TileType.Empty,      // reserva

        // ---- Couch (sofá — IDs 33-37) — decorativo ----
        >= 33 and <= 37 => TileType.Empty,

        // ---- Paint (fundo — ID 38) — decorativo ----
        38 => TileType.Empty,

        // ---- Bed (cama — IDs 39-41) — plataforma ----
        >= 39 and <= 41 => TileType.Platform,

        _ => TileType.Empty,
    };

    // ============================================================
    // LOAD
    // ============================================================
    public static LoadResult Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Mapa não encontrado: {path}");

        string json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var map = JsonSerializer.Deserialize<TiledMap>(json, opts)
            ?? throw new Exception("Falha ao ler o JSON do Tiled");

        int w = map.Width;
        int h = map.Height;

        var grid = new TileType[w, h];

        TiledLayer? tileLayer = null;
        TiledLayer? objectLayer = null;

        foreach (var layer in map.Layers)
        {
            if (layer.Type == "tilelayer") tileLayer = layer;
            if (layer.Type == "objectgroup") objectLayer = layer;
        }

        if (tileLayer == null)
            throw new Exception("Nenhuma camada de tiles encontrada");

        // ---- Preencher grid ----
        var data = tileLayer.Data;   // long[] agora
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                if (idx >= data.Length) continue;

                long rawId = data[idx];

                // Limpa flip/rotação (bits 31, 30, 29) e converte pra int
                int tileId = (int)(rawId & 0x1FFFFFFF);

                grid[x, y] = MapTile(tileId);
            }
        }

        // ---- Spawn do player ----
        Vector2 spawn = new Vector2(64, 64);

        if (objectLayer != null)
        {
            foreach (var obj in objectLayer.Objects)
            {
                if (obj.Name == "player_spawn")
                {
                    spawn = new Vector2(obj.X, obj.Y);
                    break;
                }
            }
        }

        return new LoadResult
        {
            Level = new Level(grid),
            PlayerSpawn = spawn,
        };
    }

    // ============================================================
    // ESTRUTURAS
    // ============================================================
    private class TiledMap
    {
        [JsonPropertyName("width")]  public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
        [JsonPropertyName("layers")] public List<TiledLayer> Layers { get; set; } = new();
    }

    private class TiledLayer
    {
        [JsonPropertyName("type")]    public string Type { get; set; } = "";
        [JsonPropertyName("name")]    public string Name { get; set; } = "";
        [JsonPropertyName("data")]    public long[] Data { get; set; } = Array.Empty<long>();  // ← long
        [JsonPropertyName("objects")] public List<TiledObject> Objects { get; set; } = new();
    }

    private class TiledObject
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("x")]    public float X { get; set; }
        [JsonPropertyName("y")]    public float Y { get; set; }
    }
}