using Raylib_cs;
using System.Numerics;
using ResetyKile.Core;

namespace ResetyKile.UI;

public class MapMenu
{
    public class MapPoint
    {
        public string LevelName = "";
        public string DisplayName = "";
        public Vector2 Position;
        public Vector2[] Neighbors = Array.Empty<Vector2>();
    }

    public bool IsOpen { get; private set; }
    public int SelectedIndex { get; private set; } = 0;

    private List<MapPoint> _points = new();
    private float _fadeAlpha = 0f;
    private float _time = 0f;

    public void AddPoint(string levelName, string displayName, Vector2 pos)
    {
        _points.Add(new MapPoint
        {
            LevelName = levelName,
            DisplayName = displayName,
            Position = pos,
        });
    }

    public void Open()
    {
        IsOpen = true;
        _fadeAlpha = 0f;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void Update(float dt)
    {
        _time += dt;

        // Fade in/out
        float target = IsOpen ? 1f : 0f;
        float speed = 8f;
        if (_fadeAlpha < target) _fadeAlpha = MathF.Min(_fadeAlpha + speed * dt, target);
        else if (_fadeAlpha > target) _fadeAlpha = MathF.Max(_fadeAlpha - speed * dt, target);
    }

    /// Retorna o level escolhido, ou null se nada foi confirmado.
    public string? HandleInput(InputState input, string currentLevel)
    {
        if (!IsOpen) return null;

        // Navegação
        if (input.MapNavigateX != 0 || input.MapNavigateY != 0)
        {
            int bestIndex = SelectedIndex;
            float bestDist = float.MaxValue;

            Vector2 current = _points[SelectedIndex].Position;

            for (int i = 0; i < _points.Count; i++)
            {
                if (i == SelectedIndex) continue;

                Vector2 diff = _points[i].Position - current;

                // Filtra pela direção
                if (input.MapNavigateX > 0 && diff.X <= 0) continue;
                if (input.MapNavigateX < 0 && diff.X >= 0) continue;
                if (input.MapNavigateY > 0 && diff.Y <= 0) continue;
                if (input.MapNavigateY < 0 && diff.Y >= 0) continue;

                float dist = diff.Length();
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            SelectedIndex = bestIndex;
        }

        // Confirma
        if (input.MapConfirmPressed)
        {
            string chosen = _points[SelectedIndex].LevelName;
            Close();
            if (chosen != currentLevel)
                return chosen;
        }

        return null;
    }

    public void Draw(string currentLevel)
    {
        if (_fadeAlpha <= 0.01f) return;

        byte alpha = (byte)(_fadeAlpha * 240);

        // Fundo escuro
        Raylib.DrawRectangle(0, 0, Renderer.InternalW, Renderer.InternalH,
            new Color((byte)15, (byte)12, (byte)25, alpha));

        // Grade decorativa (placeholder de mapa)
        Color gridColor = new Color((byte)40, (byte)35, (byte)55, alpha);
        for (int x = 0; x < Renderer.InternalW; x += 20)
            Raylib.DrawRectangle(x, 0, 1, Renderer.InternalH, gridColor);
        for (int y = 0; y < Renderer.InternalH; y += 20)
            Raylib.DrawRectangle(0, y, Renderer.InternalW, 1, gridColor);

        // Título
        string title = "MAPA";
        int titleSize = 14;
        int titleW = Raylib.MeasureText(title, titleSize);
        Raylib.DrawText(title, Renderer.InternalW / 2 - titleW / 2, 8, titleSize,
            new Color((byte)240, (byte)240, (byte)250, alpha));

        // Desenha conexões (linhas entre pontos próximos)
        for (int i = 0; i < _points.Count; i++)
        {
            for (int j = i + 1; j < _points.Count; j++)
            {
                if (Vector2.Distance(_points[i].Position, _points[j].Position) < 80f)
                {
                    Raylib.DrawLineEx(_points[i].Position, _points[j].Position, 1f,
                        new Color((byte)80, (byte)80, (byte)110, alpha));
                }
            }
        }

        // Desenha pontos
        for (int i = 0; i < _points.Count; i++)
        {
            var p = _points[i];
            bool selected = i == SelectedIndex;
            bool isCurrent = p.LevelName == currentLevel;

            // Cor: selecionado = destaque, atual = verde, resto = branco
            Color baseColor;
            if (isCurrent) baseColor = new Color((byte)120, (byte)220, (byte)130, (byte)255);
            else           baseColor = new Color((byte)200, (byte)200, (byte)220, (byte)255);

            if (selected)
            {
                // Pulso
                float pulse = (MathF.Sin(_time * 6f) + 1f) * 0.5f;
                byte extraA = (byte)(100 + 100 * pulse);

                // Moldura animada
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(p.Position.X - 5, p.Position.Y - 5, 10, 10),
                    1f,
                    new Color((byte)255, (byte)220, (byte)100, extraA));
            }

            // Ponto
            Raylib.DrawRectangle((int)p.Position.X - 3, (int)p.Position.Y - 3, 6, 6,
                new Color(baseColor.R, baseColor.G, baseColor.B, alpha));

            // Contorno
            Raylib.DrawRectangleLinesEx(
                new Rectangle(p.Position.X - 3, p.Position.Y - 3, 6, 6),
                1f,
                new Color((byte)15, (byte)12, (byte)22, alpha));
        }

        // Nome do ponto selecionado
        if (SelectedIndex < _points.Count)
        {
            var sel = _points[SelectedIndex];
            string name = sel.DisplayName;
            int nameSize = 10;
            int nameW = Raylib.MeasureText(name, nameSize);
            Raylib.DrawText(name, Renderer.InternalW / 2 - nameW / 2, Renderer.InternalH - 30,
                nameSize, new Color((byte)240, (byte)240, (byte)250, alpha));
        }

        // Hint
        string hint = "Setas: navegar  |  Enter/A: ir  |  Tab/Select: fechar";
        int hintSize = 7;
        int hintW = Raylib.MeasureText(hint, hintSize);
        Raylib.DrawText(hint, Renderer.InternalW / 2 - hintW / 2, Renderer.InternalH - 12,
            hintSize, new Color((byte)150, (byte)150, (byte)180, alpha));
    }
}