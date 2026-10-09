using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquesta la partida completa: valida jugadas, gestiona puntuacion,
/// administra turnos via TurnStateMachine y coordina la IA.
///
/// FLUJO NORMAL:
///   DominoPiece.OnMouseDown -> TryPlayPiece(piece)
///     |-- LeftOnly/RightOnly  --> CommitPlay --> NotifyPiecePlayed --> turno siguiente
///     `-- BothEnds            --> OnEndAmbiguity --> espera ResolveAmbiguity()
///
/// FLUJO IA:
///   TurnStateMachine.OnTurnChanged (isAI=true) --> InvokeAITurn()
///   EliteAIController.ProcesarTurnoIA()        --> llama TryPlayPiece o PassTurn
/// </summary>
public class MatchController : MonoBehaviour
{
    // ── Eventos Observer ──────────────────────────────────────────────────────

    /// <summary>Ficha colocada. Params: ficha, playerIndex.</summary>
    public event Action<DominoPiece, int> OnPiecePlayed;

    /// <summary>Puntuacion actualizada. Params: playerIndex, totalScore.</summary>
    public event Action<int, int> OnScoreUpdated;

    /// <summary>
    /// Ambiguedad de extremo detectada. Params: pieza, LeftEnd, RightEnd.
    /// El flujo se pausa hasta que alguien llame ResolveAmbiguity().
    /// </summary>
    public event Action<DominoPiece, int, int> OnEndAmbiguity;

    /// <summary>Ambiguedad resuelta o cancelada.</summary>
    public event Action OnAmbiguityResolved;

    /// <summary>El turno cambio. Params: playerIndex, isAI.</summary>
    public event Action<int, bool> OnTurnChanged;

    /// <summary>La partida termino. Param: winnerIndex (-1 = tranca).</summary>
    public event Action<int> OnMatchOver;

    // ── Configuracion Inspector ───────────────────────────────────────────────

    [Header("Configuracion de jugadores")]
    [SerializeField] private int playerCount = 2;
    [Tooltip("Indices de jugadores humanos. El resto se tratan como IA.")]
    [SerializeField] private List<int> humanPlayerIndices = new List<int> { 0 };

    [Header("IA")]
    [SerializeField] private EliteAIController aiController;
    [SerializeField] private float aiTurnDelay = 0.5f; // segundos antes de que la IA actue

    // ── Estado interno ────────────────────────────────────────────────────────

    private MatchState matchState;
    private TurnStateMachine turnMachine;
    private readonly Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private DominoPiece pendingAmbiguousPiece;

    // ── Propiedades publicas ──────────────────────────────────────────────────

    public MatchState State            => matchState;
    public TurnStateMachine TurnMachine => turnMachine;
    public int CurrentPlayerIndex      => turnMachine?.CurrentPlayer ?? 0;
    public bool IsAwaitingEndChoice    => pendingAmbiguousPiece != null;
    public bool IsCurrentPlayerAI      => turnMachine?.CurrentIsAI ?? false;

    // ── Ciclo de vida Unity ───────────────────────────────────────────────────

    private void Awake()
    {
        matchState  = new MatchState();
        turnMachine = new TurnStateMachine(playerCount, humanPlayerIndices, startingPlayerIndex: 0);

        // Suscribir eventos de la maquina de estados
        turnMachine.OnTurnChanged  += HandleTurnChanged;
        turnMachine.OnStateChanged += HandleStateChanged;
        turnMachine.OnGameOver     += HandleGameOver;

        // Auto-agregar TableInputManager si no existe
        if (GetComponent<TableInputManager>() == null)
            gameObject.AddComponent<TableInputManager>();

        // Resolver referencia a la IA si no esta asignada en el inspector
        if (aiController == null)
            aiController = FindAnyObjectByType<EliteAIController>();
    }

    private void Start()
    {
        // Disparar el primer turno (puede ser IA si humanPlayerIndices esta vacio)
        OnTurnChanged?.Invoke(CurrentPlayerIndex, IsCurrentPlayerAI);
        if (IsCurrentPlayerAI)
            StartCoroutine(InvokeAITurn());
    }

    private void OnDestroy()
    {
        if (turnMachine == null) return;
        turnMachine.OnTurnChanged  -= HandleTurnChanged;
        turnMachine.OnStateChanged -= HandleStateChanged;
        turnMachine.OnGameOver     -= HandleGameOver;
    }

    // ── API publica ───────────────────────────────────────────────────────────

    public int GetScore(int playerIndex) =>
        playerScores.TryGetValue(playerIndex, out int s) ? s : 0;

    public bool CanPlayPiece(DominoPiece piece)
    {
        EnsureState();
        return matchState.CanPlace(piece);
    }

    /// <summary>
    /// Punto de entrada para jugadores humanos y la IA.
    /// Determina ambiguedad, coloca directamente o pausa el flujo.
    /// Solo acepta input del jugador activo.
    /// </summary>
    public bool TryPlayPiece(DominoPiece piece)
    {
        EnsureState();

        if (turnMachine.IsGameOver) return false;
        if (IsAwaitingEndChoice)
        {
            Debug.Log("[MatchController] Eleccion de extremo pendiente. Clic ignorado.");
            return false;
        }
        if (turnMachine.State != TurnState.WaitingForInput) return false;

        PlacementAmbiguity ambiguity = matchState.CheckAmbiguity(piece);

        switch (ambiguity)
        {
            case PlacementAmbiguity.None:
                Debug.Log($"[MatchController] ({piece.SideA}|{piece.SideB}) no encaja.");
                return false;

            case PlacementAmbiguity.LeftOnly:
            case PlacementAmbiguity.RightOnly:
                BoardEnd direct = ambiguity == PlacementAmbiguity.LeftOnly
                    ? BoardEnd.Left : BoardEnd.Right;
                return CommitPlay(piece, direct);

            case PlacementAmbiguity.BothEnds:
                if (IsCurrentPlayerAI)
                {
                    BoardEnd aiChosenEnd = UnityEngine.Random.value > 0.5f ? BoardEnd.Left : BoardEnd.Right;
                    Debug.Log($"[MatchController] Ambiguedad de IA resuelta automaticamente hacia {aiChosenEnd}.");
                    return CommitPlay(piece, aiChosenEnd);
                }

                pendingAmbiguousPiece = piece;
                piece.SetPlayerInputEnabled(false);
                turnMachine.NotifyAmbiguityStarted();
                Debug.Log($"[MatchController] Ambiguedad ({piece.SideA}|{piece.SideB}) " +
                          $"L={matchState.LeftEnd} R={matchState.RightEnd}");
                OnEndAmbiguity?.Invoke(piece, matchState.LeftEnd.Value, matchState.RightEnd.Value);
                return true;

            default: return false;
        }
    }

    /// <summary>
    /// Resuelve la ambiguedad de extremo. Llamado por EndSelectionUI o por la IA.
    /// </summary>
    public bool ResolveAmbiguity(DominoPiece piece, BoardEnd chosenEnd)
    {
        if (pendingAmbiguousPiece == null || pendingAmbiguousPiece != piece)
        {
            Debug.LogWarning("[MatchController] ResolveAmbiguity sin pieza pendiente valida.");
            return false;
        }

        pendingAmbiguousPiece = null;
        turnMachine.NotifyAmbiguityResolved();
        OnAmbiguityResolved?.Invoke();
        return CommitPlay(piece, chosenEnd);
    }

    /// <summary>
    /// Cancela la eleccion de extremo. La ficha vuelve a ser clickeable.
    /// </summary>
    public void CancelAmbiguity()
    {
        if (pendingAmbiguousPiece == null) return;

        pendingAmbiguousPiece.SetPlayerInputEnabled(true);
        pendingAmbiguousPiece = null;
        turnMachine.NotifyAmbiguityCancelled();
        OnAmbiguityResolved?.Invoke();
        Debug.Log("[MatchController] Ambiguedad cancelada.");
    }

    /// <summary>
    /// El jugador actual pasa (no puede jugar ninguna ficha).
    /// Llamado por la UI o por la IA cuando no tiene movimientos validos.
    /// </summary>
    public void PassTurn()
    {
        if (turnMachine.IsGameOver) return;
        if (turnMachine.State != TurnState.WaitingForInput) return;

        Debug.Log($"[MatchController] Jugador {CurrentPlayerIndex} pasa.");
        turnMachine.NotifyPlayerPassed();
    }

    /// <summary>
    /// Declara al jugador ganador porque se quedo sin fichas.
    /// Llamado por el gestor de mano cuando la mano de un jugador queda vacia.
    /// </summary>
    public void DeclareWinner(int playerIndex)
    {
        turnMachine.NotifyPlayerWon(playerIndex);
    }

    public void ResetMatch()
    {
        EnsureState();
        matchState.Reset();
        playerScores.Clear();
        pendingAmbiguousPiece = null;
        turnMachine.Reset(startingPlayerIndex: 0);

        OnTurnChanged?.Invoke(CurrentPlayerIndex, IsCurrentPlayerAI);
        if (IsCurrentPlayerAI)
            StartCoroutine(InvokeAITurn());
    }

    // ── Handlers de TurnStateMachine ──────────────────────────────────────────

    private void HandleTurnChanged(int newPlayer, bool isAI)
    {
        Debug.Log($"[MatchController] Turno -> J{newPlayer + 1} ({(isAI ? "IA" : "Humano")})");
        OnTurnChanged?.Invoke(newPlayer, isAI);

        if (isAI)
            StartCoroutine(InvokeAITurn());
    }

    private void HandleStateChanged(TurnState newState)
    {
        Debug.Log($"[MatchController] Estado -> {newState}");
    }

    private void HandleGameOver(int winnerIndex)
    {
        string msg = winnerIndex == -1
            ? "Mesa trancada. Gana quien tenga menos puntos en mano."
            : $"Gana J{winnerIndex + 1}!";
        Debug.Log($"[MatchController] GAME OVER: {msg}");
        OnMatchOver?.Invoke(winnerIndex);
    }

    // ── Integracion IA ────────────────────────────────────────────────────────

    /// <summary>
    /// Espera aiTurnDelay segundos y luego delega en EliteAIController.
    /// La IA puede llamar TryPlayPiece() o PassTurn() en su corrutina interna.
    /// </summary>
    private IEnumerator InvokeAITurn()
    {
        yield return new WaitForSeconds(aiTurnDelay);

        if (aiController == null)
        {
            Debug.LogWarning("[MatchController] No hay EliteAIController asignado. La IA pasa.");
            PassTurn();
            yield break;
        }

        // Pasamos referencia al controlador para que la IA pueda llamar TryPlayPiece
        aiController.ExecuteTurn(this);
    }

    // ── Metodos privados ──────────────────────────────────────────────────────

    private bool CommitPlay(DominoPiece piece, BoardEnd chosenEnd)
    {
        if (!matchState.TryPlace(piece, chosenEnd, out _))
        {
            Debug.LogWarning($"[MatchController] CommitPlay fallo ({piece.SideA}|{piece.SideB}) en {chosenEnd}.");
            return false;
        }

        int points   = piece.GetCombinatorialPlayValue();
        int newTotal = AddScore(CurrentPlayerIndex, points);

        OnPiecePlayed?.Invoke(piece, CurrentPlayerIndex);
        OnScoreUpdated?.Invoke(CurrentPlayerIndex, newTotal);

        Debug.Log($"[MatchController] ({piece.SideA}|{piece.SideB}) en {chosenEnd}. " +
                  $"+{points} -> Total J{CurrentPlayerIndex + 1}: {newTotal}");

        turnMachine.NotifyPiecePlayed();
        return true;
    }

    private int AddScore(int playerIndex, int points)
    {
        int total = GetScore(playerIndex) + points;
        playerScores[playerIndex] = total;
        return total;
    }

    private void EnsureState()
    {
        if (matchState == null) matchState = new MatchState();
    }
}
