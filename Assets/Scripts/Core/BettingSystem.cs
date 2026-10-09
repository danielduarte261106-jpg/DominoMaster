using System;
using System.Collections.Generic;

/// <summary>
/// Sistema de apuestas del casino. Clase de dominio puro (sin MonoBehaviour).
///
/// FLUJO DE APUESTA POR RONDA:
///   1. PlaceBet(playerIndex, amount)  -> cada jugador aporta su apuesta al bote.
///   2. LockBets()                     -> cierra la fase de apuestas.
///   3. AwardPot(winnerIndex)          -> entrega el bote al ganador.
///      AwardPotSplit(winners)         -> reparte el bote en caso de empate.
///   4. ResetRound()                   -> prepara la siguiente ronda.
///
/// DESEMPATE POR TRANCA:
///   GetLowestResiduaPlayer(values)    -> devuelve el indice del jugador
///   con menor valor de fichas en mano (segun PlayerHand.ResiduaValue).
/// </summary>
public sealed class BettingSystem
{
    // ── Configuracion ──────────────────────────────────────────────────────────

    private readonly int playerCount;
    private readonly int startingChips;

    // ── Estado ─────────────────────────────────────────────────────────────────

    private readonly int[]  chips;        // fichas actuales de cada jugador
    private readonly int[]  currentBets;  // apuesta de la ronda en curso
    private int pot;                      // bote acumulado de la ronda
    private bool betsLocked;

    // ── Eventos ────────────────────────────────────────────────────────────────

    /// <summary>Un jugador hizo su apuesta. Params: playerIndex, amount, nuevoChips.</summary>
    public event Action<int, int, int> OnBetPlaced;

    /// <summary>Las apuestas estan cerradas y el juego puede comenzar.</summary>
    public event Action<int> OnBetsLocked;   // param: bote total

    /// <summary>El bote fue entregado. Params: winnerIndex, amount.</summary>
    public event Action<int, int> OnPotAwarded;

    /// <summary>Un jugador se quedo sin fichas (eliminado o necesita recompra).</summary>
    public event Action<int> OnPlayerBroke;

    // ── Propiedades publicas ───────────────────────────────────────────────────

    public int Pot               => pot;
    public int PlayerCount       => playerCount;
    public bool AreBetsLocked    => betsLocked;

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <param name="playerCount">Numero de jugadores (2-4).</param>
    /// <param name="startingChips">Fichas iniciales por jugador.</param>
    public BettingSystem(int playerCount, int startingChips = 1000)
    {
        this.playerCount   = playerCount;
        this.startingChips = startingChips;
        chips              = new int[playerCount];
        currentBets        = new int[playerCount];

        for (int i = 0; i < playerCount; i++)
            chips[i] = startingChips;
    }

    // ── Consultas ──────────────────────────────────────────────────────────────

    public int GetChips(int playerIndex)   => InRange(playerIndex) ? chips[playerIndex]       : 0;
    public int GetBet(int playerIndex)     => InRange(playerIndex) ? currentBets[playerIndex]  : 0;
    public bool IsEliminated(int playerIndex) => InRange(playerIndex) && chips[playerIndex] <= 0;

    // ── API de apuesta ─────────────────────────────────────────────────────────

    /// <summary>
    /// El jugador hace su apuesta para la ronda.
    /// Retorna false si no tiene fondos suficientes o las apuestas ya estan cerradas.
    /// </summary>
    public bool PlaceBet(int playerIndex, int amount)
    {
        if (!InRange(playerIndex) || betsLocked) return false;
        if (amount <= 0) return false;

        // All-in automatico si no tiene suficiente
        int actual = Math.Min(amount, chips[playerIndex]);
        if (actual <= 0) return false;

        chips[playerIndex]       -= actual;
        currentBets[playerIndex] += actual;
        pot                      += actual;

        OnBetPlaced?.Invoke(playerIndex, actual, chips[playerIndex]);
        if (chips[playerIndex] == 0) OnPlayerBroke?.Invoke(playerIndex);
        return true;
    }

    /// <summary>
    /// Apuesta minima obligatoria: todos los jugadores que no han apostado
    /// aportan el minimum automaticamente (util para la IA).
    /// </summary>
    public void PlaceMinimumBets(int minimumBet)
    {
        for (int i = 0; i < playerCount; i++)
            if (currentBets[i] == 0)
                PlaceBet(i, minimumBet);
    }

    /// <summary>Cierra la fase de apuestas e impide nuevas apuestas.</summary>
    public void LockBets()
    {
        betsLocked = true;
        OnBetsLocked?.Invoke(pot);
    }

    /// <summary>
    /// Entrega todo el bote al ganador.
    /// </summary>
    public void AwardPot(int winnerIndex)
    {
        if (!InRange(winnerIndex)) return;
        chips[winnerIndex] += pot;
        OnPotAwarded?.Invoke(winnerIndex, pot);
        pot = 0;
    }

    /// <summary>
    /// Reparte el bote equitativamente entre varios ganadores (empate).
    /// El sobrante (si el bote no es divisible) va al jugador de menor indice.
    /// </summary>
    public void AwardPotSplit(IReadOnlyList<int> winnerIndices)
    {
        if (winnerIndices == null || winnerIndices.Count == 0) return;

        int share     = pot / winnerIndices.Count;
        int remainder = pot % winnerIndices.Count;

        for (int i = 0; i < winnerIndices.Count; i++)
        {
            int idx    = winnerIndices[i];
            int amount = share + (i == 0 ? remainder : 0);
            chips[idx] += amount;
            OnPotAwarded?.Invoke(idx, amount);
        }
        pot = 0;
    }

    /// <summary>
    /// Dado un diccionario playerIndex -> valorResidual (fichas en mano),
    /// devuelve el indice del jugador que gana la tranca (menor valor residual).
    /// Si hay empate, devuelve los indices de todos los empatados.
    /// </summary>
    public List<int> GetLowestResiduaPlayers(Dictionary<int, int> residualValues)
    {
        int min     = int.MaxValue;
        var winners = new List<int>();

        foreach (var kv in residualValues)
            if (kv.Value < min) min = kv.Value;

        foreach (var kv in residualValues)
            if (kv.Value == min) winners.Add(kv.Key);

        return winners;
    }

    /// <summary>Reinicia las apuestas de la ronda (no los chips acumulados).</summary>
    public void ResetRound()
    {
        for (int i = 0; i < playerCount; i++)
            currentBets[i] = 0;
        pot        = 0;
        betsLocked = false;
    }

    /// <summary>Reinicia todo (nueva partida).</summary>
    public void ResetMatch()
    {
        for (int i = 0; i < playerCount; i++)
        {
            chips[i]       = startingChips;
            currentBets[i] = 0;
        }
        pot        = 0;
        betsLocked = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private bool InRange(int i) => i >= 0 && i < playerCount;
}
