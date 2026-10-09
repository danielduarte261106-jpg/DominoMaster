using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista del sistema de apuestas y fichas de casino.
/// Conecta la lógica matemática pura de BettingSystem con la UI de Unity.
/// 
/// Elementos visuales:
///   - Visualización del bote acumulado (Pot Label).
///   - Fichas del jugador humano y de la IA.
///   - Botones de apuesta rápida (+10, +50, +100, All-In).
///   - Notificación de entrega de bote al ganador de la ronda.
/// </summary>
public class CasinoChipsUI : MonoBehaviour
{
    [Header("Referencias de UI - Bote")]
    [SerializeField] private Text potLabel;
    [SerializeField] private GameObject potContainer;

    [Header("Referencias de UI - Jugador Humano (J1)")]
    [SerializeField] private Text humanChipsLabel;
    [SerializeField] private Text humanCurrentBetLabel;

    [Header("Referencias de UI - Oponente IA (J2)")]
    [SerializeField] private Text aiChipsLabel;
    [SerializeField] private Text aiCurrentBetLabel;

    [Header("Controles de Apuesta (Fase de Apuestas)")]
    [SerializeField] private GameObject bettingPanel;
    [SerializeField] private Button bet10Button;
    [SerializeField] private Button bet50Button;
    [SerializeField] private Button bet100Button;
    [SerializeField] private Button allInButton;
    [SerializeField] private Button confirmBetButton;

    [Header("Efectos y Notificaciones")]
    [SerializeField] private Text potWonBannerLabel;
    [SerializeField] private float bannerDisplayDuration = 2.5f;

    // Referencia al orquestador o sistema de apuestas
    private BettingSystem bettingSystem;
    private int pendingHumanBet = 10;

    public void BindBettingSystem(BettingSystem system)
    {
        if (bettingSystem != null)
            UnbindEvents();

        bettingSystem = system;
        if (bettingSystem != null)
        {
            bettingSystem.OnBetPlaced  += HandleBetPlaced;
            bettingSystem.OnBetsLocked += HandleBetsLocked;
            bettingSystem.OnPotAwarded += HandlePotAwarded;
            bettingSystem.OnPlayerBroke += HandlePlayerBroke;
            RefreshAllUI();
        }
    }

    private void Awake()
    {
        SetupButtons();
        if (potWonBannerLabel != null)
            potWonBannerLabel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void UnbindEvents()
    {
        if (bettingSystem == null) return;
        bettingSystem.OnBetPlaced   -= HandleBetPlaced;
        bettingSystem.OnBetsLocked  -= HandleBetsLocked;
        bettingSystem.OnPotAwarded  -= HandlePotAwarded;
        bettingSystem.OnPlayerBroke -= HandlePlayerBroke;
    }

    private void SetupButtons()
    {
        if (bet10Button != null)
            bet10Button.onClick.AddListener(() => AddPendingBet(10));
        if (bet50Button != null)
            bet50Button.onClick.AddListener(() => AddPendingBet(50));
        if (bet100Button != null)
            bet100Button.onClick.AddListener(() => AddPendingBet(100));
        if (allInButton != null)
            allInButton.onClick.AddListener(SetAllInBet);
        if (confirmBetButton != null)
            confirmBetButton.onClick.AddListener(ConfirmHumanBet);
    }

    public void ShowBettingPhase(int minimumBet = 10)
    {
        pendingHumanBet = minimumBet;
        if (bettingPanel != null)
            bettingPanel.SetActive(true);

        RefreshAllUI();
    }

    public void HideBettingPhase()
    {
        if (bettingPanel != null)
            bettingPanel.SetActive(false);
    }

    private void AddPendingBet(int amount)
    {
        if (bettingSystem == null) return;
        int maxAvailable = bettingSystem.GetChips(0);
        pendingHumanBet = Mathf.Clamp(pendingHumanBet + amount, 0, maxAvailable);
        UpdatePendingBetDisplay();
    }

    private void SetAllInBet()
    {
        if (bettingSystem == null) return;
        pendingHumanBet = bettingSystem.GetChips(0);
        UpdatePendingBetDisplay();
    }

    private void ConfirmHumanBet()
    {
        if (bettingSystem == null) return;
        bettingSystem.PlaceBet(0, pendingHumanBet);
        HideBettingPhase();
    }

    private void UpdatePendingBetDisplay()
    {
        if (humanCurrentBetLabel != null)
            humanCurrentBetLabel.text = $"Apuesta: ${pendingHumanBet}";
    }

    public void RefreshAllUI()
    {
        if (bettingSystem == null) return;

        if (potLabel != null)
            potLabel.text = $"Bote: ${bettingSystem.Pot}";

        if (humanChipsLabel != null)
            humanChipsLabel.text = $"Fichas: ${bettingSystem.GetChips(0)}";

        if (humanCurrentBetLabel != null)
            humanCurrentBetLabel.text = $"Apuesta: ${bettingSystem.GetBet(0)}";

        if (aiChipsLabel != null)
            aiChipsLabel.text = $"IA Fichas: ${bettingSystem.GetChips(1)}";

        if (aiCurrentBetLabel != null)
            aiCurrentBetLabel.text = $"IA Apuesta: ${bettingSystem.GetBet(1)}";
    }

    private void HandleBetPlaced(int playerIndex, int amount, int remainingChips)
    {
        RefreshAllUI();
    }

    private void HandleBetsLocked(int totalPot)
    {
        HideBettingPhase();
        RefreshAllUI();
    }

    private void HandlePotAwarded(int winnerIndex, int amount)
    {
        RefreshAllUI();
        if (potWonBannerLabel != null)
        {
            potWonBannerLabel.gameObject.SetActive(true);
            string winnerName = winnerIndex == 0 ? "¡Tú ganaste" : "¡La IA ganó";
            potWonBannerLabel.text = $"{winnerName} el Bote de ${amount}!";
            CancelInvoke(nameof(HideBanner));
            Invoke(nameof(HideBanner), bannerDisplayDuration);
        }
    }

    private void HandlePlayerBroke(int playerIndex)
    {
        string name = playerIndex == 0 ? "Te has quedado sin fichas." : "La IA se quedó sin fichas.";
        Debug.Log($"[CasinoChipsUI] J{playerIndex + 1} en bancarrota: {name}");
    }

    private void HideBanner()
    {
        if (potWonBannerLabel != null)
            potWonBannerLabel.gameObject.SetActive(false);
    }
}
