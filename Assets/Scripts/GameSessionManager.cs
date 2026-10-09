using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquestador Maestro de la sesión de juego DominoMaster estilo Casino.
/// Conecta el ciclo completo de vida de las partidas:
///   1. Fase de apuestas inicial (BettingSystem + CasinoChipsUI).
///   2. Generación y reparto de fichas (DominoSet + HandManager).
///   3. Conducción de la partida y turnos (MatchController).
///   4. Resolución de victoria o tranca y entrega del bote (AwardPot).
///   5. Transición a una nueva ronda.
/// </summary>
public class GameSessionManager : MonoBehaviour
{
    [Header("Referencias Principales")]
    [SerializeField] private MatchController matchController;
    [SerializeField] private HandManager handManager;
    [SerializeField] private DominoSet dominoSet;
    [SerializeField] private CasinoChipsUI chipsUI;
    [SerializeField] private DynamicUIManager dynamicUIManager;

    [Header("Configuración de Casino")]
    [SerializeField] private int playerCount = 2;
    [SerializeField] private int startingChips = 1000;
    [SerializeField] private int minimumBet = 20;

    private BettingSystem bettingSystem;
    private int currentRoundNumber = 1;

    public BettingSystem Betting => bettingSystem;
    public int CurrentRound => currentRoundNumber;

    private void Awake()
    {
        bettingSystem = new BettingSystem(playerCount, startingChips);

        if (chipsUI == null)
            chipsUI = FindAnyObjectByType<CasinoChipsUI>();

        if (chipsUI != null)
            chipsUI.BindBettingSystem(bettingSystem);
    }

    private void Start()
    {
        ResolveDependencies();
        SubscribeEvents();
        StartNewRound();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void ResolveDependencies()
    {
        if (matchController == null)
            matchController = FindAnyObjectByType<MatchController>();

        if (handManager == null)
            handManager = FindAnyObjectByType<HandManager>();

        if (dominoSet == null)
            dominoSet = FindAnyObjectByType<DominoSet>();

        if (dynamicUIManager == null)
            dynamicUIManager = FindAnyObjectByType<DynamicUIManager>();
    }

    private void SubscribeEvents()
    {
        if (matchController != null)
            matchController.OnMatchOver += HandleMatchOver;

        if (bettingSystem != null)
            bettingSystem.OnBetsLocked += HandleBetsLocked;
    }

    private void UnsubscribeEvents()
    {
        if (matchController != null)
            matchController.OnMatchOver -= HandleMatchOver;

        if (bettingSystem != null)
            bettingSystem.OnBetsLocked -= HandleBetsLocked;
    }

    /// <summary>
    /// Inicia una nueva ronda de apuestas.
    /// </summary>
    public void StartNewRound()
    {
        Debug.Log($"[GameSessionManager] Iniciando Ronda {currentRoundNumber}.");
        bettingSystem.ResetRound();

        // La IA pone su apuesta automáticamente (apuesta mínima o escalonada)
        for (int i = 1; i < playerCount; i++)
        {
            bettingSystem.PlaceBet(i, minimumBet);
        }

        // Mostrar fase de apuesta para el humano
        if (chipsUI != null)
            chipsUI.ShowBettingPhase(minimumBet);
        else
        {
            // Fallback si no hay UI asignada: apuesta directa
            bettingSystem.PlaceBet(0, minimumBet);
            bettingSystem.LockBets();
        }
    }

    private void HandleBetsLocked(int totalPot)
    {
        Debug.Log($"[GameSessionManager] Apuestas cerradas con un bote de ${totalPot}. Repartiendo fichas...");

        // Iniciar la ronda de dominó
        if (handManager != null)
            handManager.ResetHands();

        if (dominoSet != null)
            dominoSet.GenerateFullSet();
    }

    private void HandleMatchOver(int winnerIndex)
    {
        if (winnerIndex >= 0)
        {
            Debug.Log($"[GameSessionManager] Ronda ganada por J{winnerIndex + 1}. Entregando bote de ${bettingSystem.Pot}.");
            bettingSystem.AwardPot(winnerIndex);
        }
        else
        {
            // Tranca: resolvemos por menor puntuación residual en mano
            var residualValues = new Dictionary<int, int>();
            for (int i = 0; i < playerCount; i++)
            {
                var hand = handManager != null ? handManager.GetHand(i) : null;
                int res = hand != null ? hand.ResiduaValue : 0;
                residualValues[i] = res;
                Debug.Log($"[GameSessionManager] Tranca - J{i + 1} valor residual en mano: {res}");
            }

            var winners = bettingSystem.GetLowestResiduaPlayers(residualValues);
            if (winners.Count == 1)
            {
                int winner = winners[0];
                Debug.Log($"[GameSessionManager] Tranca decidida a favor de J{winner + 1}.");
                bettingSystem.AwardPot(winner);
            }
            else
            {
                Debug.Log($"[GameSessionManager] Tranca empatada entre {winners.Count} jugadores. Bote dividido.");
                bettingSystem.AwardPotSplit(winners);
            }
        }

        currentRoundNumber++;
    }
}
