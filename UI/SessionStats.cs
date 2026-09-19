using Raylib_cs;
using ResetyKile.Core;

namespace ResetyKile.UI;

/// <summary>
/// Stats temporárias da sessão: tempo decorrido + pontos.
/// Futuramente isso vira tela de estatísticas. Por enquanto, HUD.
/// </summary>
public class SessionStats
{
    // Pontuação
    public int Points { get; private set; }
    public int Kills { get; private set; }
    public int DashKills { get; private set; }

    // Constantes
    private const int KillPoints = 5;
    private const int DashKillPoints = 10;

    public void RegisterKill(bool wasDashAttack)
    {
        Kills++;
        if (wasDashAttack)
        {
            DashKills++;
            Points += DashKillPoints;
        }
        else
        {
            Points += KillPoints;
        }
    }

    /// Retorna quantos pontos ganhou (pra mostrar floating text)
    public int GetLastKillPoints(bool wasDashAttack)
        => wasDashAttack ? DashKillPoints : KillPoints;

    public void ResetPoints()
    {
        Points = 0;
        Kills = 0;
        DashKills = 0;
    }

    public static void Draw(SessionStats stats, float sessionTime)
    {
        // ---- Tempo no canto superior direito ----
        int minutes = (int)(sessionTime / 60f);
        int seconds = (int)(sessionTime % 60f);
        string timeStr = $"{minutes:00}:{seconds:00}";

        int timeFontSize = 10;
        int timeW = Raylib.MeasureText(timeStr, timeFontSize);
        int timeX = Renderer.InternalW - timeW - 4;
        int timeY = 4;

        // Sombra
        Raylib.DrawText(timeStr, timeX + 1, timeY + 1, timeFontSize, new Color((byte)0, (byte)0, (byte)0, (byte)180));
        Raylib.DrawText(timeStr, timeX, timeY, timeFontSize, new Color((byte)220, (byte)220, (byte)240, (byte)255));

        // ---- Pontos logo abaixo ----
        string pointStr = $"{stats.Points} pts";
        int pointFontSize = 10;
        int pointW = Raylib.MeasureText(pointStr, pointFontSize);
        int pointX = Renderer.InternalW - pointW - 4;
        int pointY = timeY + 12;

        Color pointColor = stats.Points > 0
            ? new Color((byte)255, (byte)220, (byte)100, (byte)255)
            : new Color((byte)140, (byte)140, (byte)160, (byte)255);

        Raylib.DrawText(pointStr, pointX + 1, pointY + 1, pointFontSize, new Color((byte)0, (byte)0, (byte)0, (byte)180));
        Raylib.DrawText(pointStr, pointX, pointY, pointFontSize, pointColor);
    }
}