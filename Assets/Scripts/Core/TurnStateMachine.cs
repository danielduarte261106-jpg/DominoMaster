using System;
using System.Collections.Generic;

/// <summary>
/// Estados posibles del turno en curso.
/// </summary>
public enum TurnState
{
    /// Esperando que el jugador humano (o la IA) elija una ficha.
    WaitingForInput,
    /// Jugada en progreso: ambiguedad de extremo activa.
    ResolvingAmbiguity,
    /// La jugada se confirmo; animaciones/efectos en curso antes de pasar el turno.
    CommittingPlay,
    /// El jugador no pudo jugar y paso. Se aplica logica de robo/paso.
    PlayerPassed,
    /// La partida ha terminado (alguien se quedo sin fichas o la mesa esta trancada).
    GameOver
}

/// <summary>
/// Clase de dominio puro (sin MonoBehaviour) que gestiona la rotacion de turnos
/// y la maquina de estados del flujo de juego.
///
/// Responsabilidades:
///   - Rotar el turno entre N jugadores.
///   - Trackear quien es humano y quien es IA.
///   - Detectar bloqueo de mesa (todos pasaron consecutivamente).
///   - Exponer eventos para que el controlador y la vista reaccionen.
///
/// NO contiene logica de reglas (eso es MatchState).
/// NO contiene nada de Unity (MonoBehaviour, GameObject, etc.).
/// </summary>
public sealed class TurnStateMachine
{
    // ── Configuracion ──────────────────────────────────────────────────────────

    private readonly int playerCount;
    private readonly HashSet<int> aiPlayerIndices;

    // ── Estado ─────────────────────────────────────────────────────────────────

    private TurnState currentState;
    private int currentPlayerIndex;

    /// Cuantos jugadores consecutivos han pasado sin jugar.
    /// Si llega a playerCount, la mesa esta trancada.
    private int consecutivePasses;

    // ── Eventos ────────────────────────────────────────────────────────────────

    /// <summary>El turno cambio al jugador indicado. bool = es IA.</summary>
    public event Action<int, bool> OnTurnChanged;

    /// <summary>El estado interno de la maquina cambio.</summary>
    public event Action<TurnState> OnStateChanged;

    /// <summary>El jugador (humano o IA) paso su turno.</summary>
    public event Action<int> OnPlayerPassed;

    /// <summary>La partida termino. int = indice del ganador (-1 si es empate/tranca).</summary>
    public event Action<int> OnGameOver;

    // ── Propiedades ────────────────────────────────────────────────────────────

    public TurnState State        => currentState;
    public int CurrentPlayer      => currentPlayerIndex;
    public bool CurrentIsAI       => aiPlayerIndices.Contains(currentPlayerIndex);
    public bool IsGameOver        => currentState == TurnState.GameOver;

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <param name="playerCount">Numero total de jugadores (2-4).</param>
    /// <param name="humanPlayerIndices">Indices que son humanos; el resto se tratan como IA.</param>
    /// <param name="startingPlayerIndex">Indice del jugador que abre la partida.</param>
    public TurnStateMachine(int playerCount, IEnumerable<int> humanPlayerIndices, int startingPlayerIndex = 0)
    {
        if (playerCount < 2 || playerCount > 4)
            throw new ArgumentOutOfRangeException(nameof(playerCount), "El domino soporta 2-4 jugadores.");

        this.playerCount = playerCount;

        // Construimos el conjunto de IAs a partir de los humanos
        aiPlayerIndices = new HashSet<int>();
        var humans = new HashSet<int>(humanPlayerIndices);
        for (int i = 0; i < playerCount; i++)
            if (!humans.Contains(i))
                aiPlayerIndices.Add(i);

        currentPlayerIndex = Clamp(startingPlayerIndex, 0, playerCount - 1);
        consecutivePasses  = 0;
        SetState(TurnState.WaitingForInput);
    }

    // ── API publica ────────────────────────────────────────────────────────────

    /// <summary>
    /// Notifica que el jugador actual jugo una ficha con exito.
    /// Avanza al siguiente turno (a menos que ya sea GameOver).
    /// </summary>
    public void NotifyPiecePlayed()
    {
        AssertState(TurnState.WaitingForInput, TurnState.ResolvingAmbiguity);
        consecutivePasses = 0;
        SetState(TurnState.CommittingPlay);
        AdvanceTurn();
    }

    /// <summary>
    /// Notifica que el jugador actual no pudo jugar (paso).
    /// Si todos los jugadores pasan consecutivamente, declara tranca.
    /// </summary>
    public void NotifyPlayerPassed()
    {
        AssertState(TurnState.WaitingForInput);
        consecutivePasses++;
        OnPlayerPassed?.Invoke(currentPlayerIndex);

        if (consecutivePasses >= playerCount)
        {
            // Mesa trancada: nadie puede jugar
            SetState(TurnState.GameOver);
            OnGameOver?.Invoke(-1); // -1 = tranca, el ganador se determina por puntos externos
            return;
        }

        AdvanceTurn();
    }

    /// <summary>
    /// Notifica que el jugador actual gano (se quedo sin fichas).
    /// </summary>
    public void NotifyPlayerWon(int winnerIndex)
    {
        SetState(TurnState.GameOver);
        OnGameOver?.Invoke(winnerIndex);
    }

    /// <summary>
    /// Activa el estado de resolucion de ambiguedad para el turno actual.
    /// Llamado por MatchController cuando detecta BothEnds.
    /// </summary>
    public void NotifyAmbiguityStarted()
    {
        AssertState(TurnState.WaitingForInput);
        SetState(TurnState.ResolvingAmbiguity);
    }

    /// <summary>
    /// La ambiguedad se resolvio. Vuelve a WaitingForInput para que
    /// CommitPlay pueda ejecutarse en MatchController.
    /// </summary>
    public void NotifyAmbiguityResolved()
    {
        AssertState(TurnState.ResolvingAmbiguity);
        SetState(TurnState.WaitingForInput);
    }

    /// <summary>
    /// La ambiguedad fue cancelada. Regresa a WaitingForInput
    /// sin avanzar el turno.
    /// </summary>
    public void NotifyAmbiguityCancelled()
    {
        AssertState(TurnState.ResolvingAmbiguity);
        SetState(TurnState.WaitingForInput);
    }

    /// <summary>
    /// Reinicia la maquina al estado inicial.
    /// </summary>
    public void Reset(int startingPlayerIndex = 0)
    {
        currentPlayerIndex = Clamp(startingPlayerIndex, 0, playerCount - 1);
        consecutivePasses  = 0;
        SetState(TurnState.WaitingForInput);
        OnTurnChanged?.Invoke(currentPlayerIndex, CurrentIsAI);
    }

    // ── Metodos privados ───────────────────────────────────────────────────────

    private void AdvanceTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % playerCount;
        consecutivePasses  = consecutivePasses > 0 ? consecutivePasses : 0; // mantiene el contador si todos pasan
        SetState(TurnState.WaitingForInput);
        OnTurnChanged?.Invoke(currentPlayerIndex, CurrentIsAI);
    }

    private void SetState(TurnState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    private void AssertState(params TurnState[] validStates)
    {
        foreach (var s in validStates)
            if (currentState == s) return;

        throw new InvalidOperationException(
            $"[TurnStateMachine] Operacion invalida en estado '{currentState}'. " +
            $"Se esperaba: {string.Join(" o ", validStates)}");
    }

    private static int Clamp(int value, int min, int max) =>
        value < min ? min : value > max ? max : value;
}
