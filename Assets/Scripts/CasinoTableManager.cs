using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CasinoGameState
{
    Betting,      // Fase de apuestas
    Setup,        // Se reparten las fichas a los jugadores
    Playing,      // Juego en curso
    Blocked,      // Mesa cerrada por falta de movimientos
    RoundEnded,   // Terminó la ronda (alguien vació su mano)
    MatchFinished // Terminó la partida completa (mejor puntaje acumulado)
}

public enum GameMode
{
    PlayerVsAI,     // 1 Jugador vs IA
    FourPlayersAI,  // 4 jugadores contra IA
    TeamVsTeam,     // 2 equipos de 2 (jugador vs IA)
    PlayerVsPlayer  // 1v1 humano (local o red)
}

public enum PlayerType
{
    Human,
    AI
}

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public PlayerType type;
    public int handCount;
    public int roundScore;
    public int cumulativeScore;
    public Vector3 seatingPosition; // Posición cardinal para UI
    public Color seatColor;
}

public class CasinoTableManager : MonoBehaviour
{
    #region Singleton
    private static CasinoTableManager instance;
    public static CasinoTableManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject singletonObj = new GameObject("[CasinoTableManager]");
                instance = singletonObj.AddComponent<CasinoTableManager>();
                DontDestroyOnLoad(singletonObj);
                Debug.Log("[CasinoTableManager] Instancia Singleton creada.");
            }
            return instance;
        }
    }
    #endregion

    #region Eventos
    public delegate void GamePhaseHandler(CasinoGameState previous, CasinoGameState current);
    public delegate void TurnHandler(int playerIndex);
    public delegate void ScoreHandler(int playerIndex, int points);

    public static event GamePhaseHandler OnGameStateChange;
    public static event TurnHandler OnCurrentTurnChange;
    public static event ScoreHandler OnRoundScoreAdded;
    #endregion

    #region Variables
    [Header("Configuración General")]
    public GameMode currentMode = GameMode.PlayerVsAI;
    public int pointsToWinMatch = 1000; // Puntos necesarios para ganar la partida

    [Header("Configuración de Partida")]
    public int baseChipsPerPlayer = 1000;
    public int chipsInPlayPerRound = 500;

    [Header("Reflexión de IA")]
    public float minThinkingTime = 1.0f;
    public float maxThinkingTime = 2.5f;

    private CasinoGameState currentGameState;
    private int currentPlayerIndex;
    private int playersInMatch = 4;

    private List<PlayerData> players = new List<PlayerData>();
    private Stack<DominoSet> activeDeckPiles = new Stack<Stack<DominoSet>>();

    [SerializeField] private float cameraShakeDuration = 0.5f;
    [SerializeField] private float cameraShakeMagnitude = 0.2f;
    #endregion

    #region Lifecycle
    private void Start()
    {
        InitializePlayers();
        ChangeGameState(CasinoGameState.Betting);
    }

    private void InitializePlayers()
    {
        players.Clear();
        switch (currentMode)
        {
            case GameMode.PlayerVsAI:
                playersInMatch = 2;
                CreatePlayer(0, "Tú", PlayerType.Human, Vector3.left, Color.green);
                CreatePlayer(1, "IA", PlayerType.AI, Vector3.right, Color.red);
                break;
            case GameMode.FourPlayersAI:
                playersInMatch = 4;
                CreatePlayer(0, "Jugador 1", PlayerType.Human, Vector3.left, Color.green);
                CreatePlayer(1, "IA Arriba", PlayerType.AI, Vector3.up, Color.purple);
                CreatePlayer(2, "Jugador 3", PlayerType.Human, Vector3.right, Color.blue);
                CreatePlayer(3, "IA Abajo", PlayerType.AI, Vector3.down, Color.yellow);
                break;
            case GameMode.TeamVsTeam:
                playersInMatch = 4;
                CreateTeam(0, "Equipo Verde", Color.green, new[] { 0, 2 });
                CreateTeam(1, "Equipo Rojo", Color.red, new[] { 1, 3 });
                break;
            case GameMode.PlayerVsPlayer:
                playersInMatch = 2;
                CreatePlayer(0, "Jugador 1", PlayerType.Human, Vector3.left, Color.green);
                CreatePlayer(1, "Jugador 2", PlayerType.Human, Vector3.right, Color.cyan);
                break;
        }
    }

    private void CreatePlayer(int index, string name, PlayerType type, Vector3 pos, Color seatColor)
    {
        players.Add(new PlayerData
        {
            playerName = name,
            type = type,
            handCount = 0,
            roundScore = 0,
            cumulativeScore = baseChipsPerPlayer,
            seatingPosition = pos,
            seatColor = seatColor
        });
    }

    private void CreateTeam(int teamIndex, string name, Color color, int[] memberIndices)
    {
        for (int i = 0; i < memberIndices.Length; i++)
        {
            int idx = memberIndices[i];
            string typeStr = (i == 0) ? "Tú" : "Socio";
            CreatePlayer(idx, $"{name} - {typeStr}", (i == 0) ? PlayerType.Human : PlayerType.AI, Vector3.up * (teamIndex * 2 - 1), color);
        }
    }
    #endregion

    #region Machine de Estados
    public void ChangeGameState(CasinoGameState newState)
    {
        CasinoGameState previous = currentGameState;
        currentGameState = newState;
        Debug.Log($"[Estado] {previous} -> {newState}");
        OnGameStateChange?.Invoke(previous, newState);
        StartCoroutine(GamePhaseTransitionCoroutine(previous, newState));
    }

    private IEnumerator GamePhaseTransitionCoroutine(CasinoGameState prev, CasinoGameState next)
    {
        // Lógica de transición específica según el estado.
        // Aquí puedes conectar animaciones de transición del Canvas o efectos.
        switch (next)
        {
            case CasinoGameState.Betting:
                yield return new WaitForSeconds(1f);
                StartCoroutine(SwitchToNextPhase());
                break;
            case CasinoGameState.Setup:
                yield return new WaitForSeconds(1f);
                StartCoroutine(SwitchToNextPhase());
                break;
            case CasinoGameState.Playing:
                yield return new WaitForSeconds(1f);
                StartCoroutine(SwitchToNextPhase());
                break;
            case CasinoGameState.Blocked:
                Debug.Log("Mesa bloqueada. Resolviendo ronda por puntaje.");
                ResolveBlockedRound();
                break;
            case CasinoGameState.RoundEnded:
                Debug.Log("Ronda terminada.");
                yield return new WaitForSeconds(2f);
                // Reiniciar para siguiente ronda o terminar partido
                StartCoroutine(SwitchToNextPhase());
                break;
            case CasinoGameState.MatchFinished:
                Debug.Log("Partida finalizada. Mostrando pantalla de resultados.");
                yield return new WaitForSeconds(3f);
                ChangeGameState(CasinoGameState.Betting);
                break;
        }
    }

    private IEnumerator SwitchToNextPhase()
    {
        CasinoGameState nextPhase = GetNextPhase(currentGameState);
        ChangeGameState(nextPhase);
        yield break;
    }

    private CasinoGameState GetNextPhase(CasinoGameState current)
    {
        switch (current)
        {
            case CasinoGameState.Betting: return CasinoGameState.Setup;
            case CasinoGameState.Setup: return CasinoGameState.Playing;
            case CasinoGameState.Playing: return CasinoGameState.RoundEnded;
            case CasinoGameState.RoundEnded: return CasinoGameState.Betting;
            default: return CasinoGameState.MatchFinished;
        }
    }
    #endregion

    #region Turnos
    private void SwitchTurn()
    {
        int nextIndex = (currentPlayerIndex + 1) % playersInMatch;
        // Buscar el siguiente turno válido (en 1v1 solo alterna; en 4 jugadores también)
        currentPlayerIndex = nextIndex;
        OnCurrentTurnChange?.Invoke(currentPlayerIndex);

        if (players[currentPlayerIndex].type == PlayerType.AI)
        {
            StartCoroutine(TakeAITurn());
        }
    }

    private IEnumerator TakeAITurn()
    {
        // Simula "pensamiento"
        float thinkingTime = Random.Range(minThinkingTime, maxThinkingTime);
        Debug.Log($"{players[currentPlayerIndex].playerName} está pensando ({thinkingTime:F2}s)...");
        yield return new WaitForSeconds(thinkingTime);
        // Nota: La IA ejecuta su movimiento llamando a CasinoTableManager.HandlePlayerMove o similar,
        // o directamente a través del EliteAIController. Aquí delegamos.
        // Para compilar sin EliteAIController, usamos un placeholder.
        Debug.Log($"[Placeholder IA] Turno de {players[currentPlayerIndex].playerName}.");
        ChangeGameState(CasinoGameState.Playing);
    }

    #endregion

    #region Sistema de Puntuación Casino
    public void AddScore(int playerIndex, int points)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;

        players[playerIndex].roundScore += points;
        players[playerIndex].cumulativeScore += points;
        OnRoundScoreAdded?.Invoke(playerIndex, points);
        TriggerCameraShake();
        Debug.Log($"{players[playerIndex].playerName} suma {points} puntos. Total ronda: {players[playerIndex].roundScore}");
    }

    public void DeductChips(int playerIndex, int chips)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        players[playerIndex].cumulativeScore -= chips;
    }

    public void CheckMatchWinner()
    {
        // Determina el ganador de la partida
        PlayerData winner = null;
        int highestScore = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].cumulativeScore > highestScore)
            {
                highestScore = players[i].cumulativeScore;
                winner = players[i];
            }
        }

        if (highestScore >= pointsToWinMatch && winner != null)
        {
            Debug.Log($"¡{winner.playerName} gana la partida con {winner.cumulativeScore} puntos!");
            ChangeGameState(CasinoGameState.MatchFinished);
        }
    }

    public void ResetRoundScores()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].roundScore = 0;
        }
    }
    #endregion

    #region Control de Juego (Placeholder - para conectar con UI)
    // El sistema de juego real (tirar fichas, validar movimientos) se manejará
    // en otros scripts, pero aquí delegamos las llamadas externas.
    public void OnPlayerPlayedPiece(int playerIndex)
    {
        // Validar si el juego puede continuar
        if (HasAvailableMoves(playerIndex))
        {
            SwitchTurn();
        }
        else
        {
            // Si no tiene movimientos, el juego pasa al siguiente jugador.
            // Si nadie tiene movimientos, la mesa se bloquea.
            bool anyMoves = HasAvailableMovesAny();
            if (anyMoves)
            {
                SwitchTurn();
            }
            else
            {
                ChangeGameState(CasinoGameState.Blocked);
            }
        }
    }

    public void OnPlayerPasses(int playerIndex)
    {
        // Jugador pasa turno
        SwitchTurn();
    }

    private bool HasAvailableMovesAny()
    {
        // Placeholder. Debe analizar el estado de la mesa.
        return true; 
    }

    private bool HasAvailableMoves(int playerIndex)
    {
        // Placeholder. Debe analizar las fichas del jugador y la mesa.
        return true;
    }

    private void ResolveBlockedRound()
    {
        // Al bloquearse, gana quien tenga menos puntos sumados en su mano
        int minPoints = int.MaxValue;
        int winnerIdx = -1;
        for (int i = 0; i < players.Count; i++)
        {
            // En una implementación real, restaríamos los puntos de las fichas en mano.
            // Aquí simulamos con un cálculo.
            int playerPoints = players[i].cumulativeScore; // Placeholder
            if (playerPoints < minPoints)
            {
                minPoints = playerPoints;
                winnerIdx = i;
            }
        }
        Debug.Log($"Mesa bloqueada. Gana {players[winnerIdx].playerName}.");
        ChangeGameState(CasinoGameState.RoundEnded);
    }
    #endregion

    #region Efectos Visuales
    private void TriggerCameraShake()
    {
        // Placeholder. En la versión final, conectar con el sistema de cámara del CasinoSceneBuilder.
        Debug.Log("[Efecto] Temblor de cámara");
    }
    #endregion

    #region Getters
    public CasinoGameState GetGameState() => currentGameState;
    public int GetCurrentPlayerIndex() => currentPlayerIndex;
    public PlayerData GetCurrentPlayer() => (currentPlayerIndex >= 0 && currentPlayerIndex < players.Count) ? players[currentPlayerIndex] : null;
    public List<PlayerData> GetAllPlayers() => players;
    public int GetPlayersCount() => playersInMatch;
    #endregion
}
