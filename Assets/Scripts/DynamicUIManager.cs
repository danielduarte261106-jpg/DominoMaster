using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestor de Interfaz Inmersiva para Dominó Frutas
/// Maneja avatares visuales, paneles de victoria/derrota, notificaciones de Comodín Combinatorio y efectos de confeti.
/// </summary>
public class DynamicUIManager : MonoBehaviour
{
    #region Referencias a Avatares Visuales
    [Header("Avatares de Jugadores")]
    [SerializeField] private Transform playerAvatarLeft;    // Jugador 1 (Izquierda)
    [SerializeField] private Transform playerAvatarRight;  // Jugador 2 (Derecha)
    [SerializeField] private Transform playerAvatarTop;   // Jugador 3 (Arriba)
    [SerializeField] private Transform playerAvatarBottom; // Jugador 4 (Abajo)
    
    [Header("Panel de Victoria/Derrota")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private Text victoryLabel;
    [SerializeField] private Text defeatLabel;
    [SerializeField] private Image winConfettiEffect;
    [SerializeField] private Image loseConfettiEffect;
    #endregion

    #region Notificaciones de Comodín Combinatorio
    [Header("Notificaciones de Comodín Combinatorio")]
    public GameObject notificationPrefab;
    [SerializeField] private float notificationFloatSpeed = 10f; 
    [SerializeField] private float notificationScaleRange = 0.5f; 
    
    private float lastNotificationTime;
    private float notificationTimer;
    #endregion

    #region Estado de la Interfaz
    private int currentVictoryPanel = 0; 
    private bool showCombinationDoublesNotification = false;
    #endregion

    #region Variables
    private List<PlayerData> players = new List<PlayerData>();
    private Dictionary<int, Sprite> playerSprites = new Dictionary<int, Sprite>();
    #endregion

    [Header("Nucleo de partida (bajo acoplamiento)")]
    [SerializeField] private MatchController matchController;
    [SerializeField] private Text scoreLabel;
    [SerializeField] private Text turnLabel;   // (opcional) muestra de quien es el turno

    [Header("Panel de eleccion de extremo (opcional)")]
    [Tooltip("Asigna el EndSelectionUI de escena. Si esta vacio se crea en runtime.")]
    [SerializeField] private EndSelectionUI endSelectionUI;

    private const float PlayedPieceSpacing = 0.65f;
    private static readonly Color PlayedTint = new Color(0.82f, 0.82f, 0.82f, 0.9f);

    #region Initialization
    private void Awake()
    {
        // Limpieza automática de textos heredados o círculos que ensucien la mesa
        for (int i = 1; i <= 4; i++)
        {
            var strayText = GameObject.Find($"PlayerName_Jugador {i}");
            if (strayText != null) Destroy(strayText);
        }

        foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t != null && (t.name.StartsWith("Seat_") || t.name.StartsWith("SeatCircle_")))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (playerAvatarLeft != null) playerAvatarLeft.gameObject.SetActive(true);
        if (playerAvatarRight != null) playerAvatarRight.gameObject.SetActive(true);
        if (playerAvatarTop != null) playerAvatarTop.gameObject.SetActive(true);
        if (playerAvatarBottom != null) playerAvatarBottom.gameObject.SetActive(true);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
            victoryLabel.text = "";
            winConfettiEffect.gameObject.SetActive(false);
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
            defeatLabel.text = "";
            loseConfettiEffect.gameObject.SetActive(false);
        }

        lastNotificationTime = -Mathf.Infinity;
        notificationTimer = 0f;

        ResolveMatchController();
        EnsureScoreLabel();
        SubscribeToMatchEvents();
        RefreshScoreLabel(matchController != null ? matchController.CurrentPlayerIndex : 0,
            matchController != null ? matchController.GetScore(matchController.CurrentPlayerIndex) : 0);
    }

    private void OnDestroy()
    {
        if (matchController == null) return;

        matchController.OnScoreUpdated      -= HandleScoreUpdated;
        matchController.OnPiecePlayed       -= HandlePiecePlayed;
        matchController.OnEndAmbiguity      -= HandleAmbiguityStarted;
        matchController.OnAmbiguityResolved -= HandleAmbiguityEnded;
        matchController.OnTurnChanged       -= HandleTurnChanged;
        matchController.OnMatchOver         -= HandleMatchOver;
    }
    #endregion

    #region Suscripción a MatchController
    private void ResolveMatchController()
    {
        if (matchController != null)
            return;

        matchController = GetComponent<MatchController>();
        if (matchController == null)
            matchController = FindAnyObjectByType<MatchController>();
        if (matchController == null)
            matchController = gameObject.AddComponent<MatchController>();
    }

    private void SubscribeToMatchEvents()
    {
        if (matchController == null) return;

        matchController.OnScoreUpdated      += HandleScoreUpdated;
        matchController.OnPiecePlayed       += HandlePiecePlayed;
        matchController.OnEndAmbiguity      += HandleAmbiguityStarted;
        matchController.OnAmbiguityResolved += HandleAmbiguityEnded;
        matchController.OnTurnChanged       += HandleTurnChanged;
        matchController.OnMatchOver         += HandleMatchOver;

        if (endSelectionUI == null)
            endSelectionUI = FindAnyObjectByType<EndSelectionUI>();
    }

    // ── Handlers de ambiguedad ────────────────────────────────────────────────
    private void HandleAmbiguityStarted(DominoPiece piece, int leftEnd, int rightEnd)
    {
        if (endSelectionUI == null)
            Debug.LogWarning("[DynamicUIManager] OnEndAmbiguity recibido pero no hay EndSelectionUI.");
    }

    private void HandleAmbiguityEnded()
    {
        // EndSelectionUI gestiona su propio panel; aqui podemos anadir efectos extra.
    }

    // ── Handlers de turno y fin de partida ─────────────────────────────────
    private void HandleTurnChanged(int playerIndex, bool isAI)
    {
        if (turnLabel == null) return;
        string who = isAI ? "IA" : "Tu";
        turnLabel.text = $"Turno: J{playerIndex + 1} ({who})";
    }

    private void HandleMatchOver(int winnerIndex)
    {
        if (winnerIndex == -1)
        {
            // Tranca: mostrar panel de derrota con mensaje especial
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
                if (defeatLabel != null)
                    defeatLabel.text = "Mesa trancada.\nGana quien tenga menos puntos.";
            }
        }
        else if (winnerIndex == 0) // asumimos que J1 (index 0) es el jugador humano principal
        {
            ShowVictoryPanel();
        }
        else
        {
            ShowDefeatPanel();
        }
    }

    private void HandleScoreUpdated(int playerIndex, int totalScore)
    {
        RefreshScoreLabel(playerIndex, totalScore);
    }

    private void HandlePiecePlayed(DominoPiece piece, int playerIndex)
    {
        if (piece == null)
            return;

        piece.SetFaceDown(false);
        piece.SetPlayerInputEnabled(false);
        ApplyPlayedVisuals(piece);
        MovePieceToTableCenter(piece);
    }

    private void RefreshScoreLabel(int playerIndex, int totalScore)
    {
        if (scoreLabel == null)
            EnsureScoreLabel();
        if (scoreLabel == null)
            return;

        scoreLabel.text = $"Puntos J{playerIndex + 1}: {totalScore}";
    }

    private void ApplyPlayedVisuals(DominoPiece piece)
    {
        SpriteRenderer[] renderers = piece.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = PlayedTint;
        }
    }

    private void MovePieceToTableCenter(DominoPiece piece)
    {
        int placedIndex = 0;
        if (matchController != null && matchController.State != null)
            placedIndex = Mathf.Max(0, matchController.State.PlacedPieces.Count - 1);

        Vector3 chainOffset = new Vector3((placedIndex - 3) * PlayedPieceSpacing, 0f, -0.2f);
        piece.transform.SetParent(null);
        piece.transform.position = chainOffset;
    }

    private void EnsureScoreLabel()
    {
        if (scoreLabel != null)
            return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUDCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject scoreObj = new GameObject("ScoreLabel");
        scoreObj.transform.SetParent(canvas.transform, false);

        scoreLabel = scoreObj.AddComponent<Text>();
        scoreLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (scoreLabel.font == null)
            scoreLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        scoreLabel.fontSize = 36;
        scoreLabel.alignment = TextAnchor.UpperLeft;
        scoreLabel.color = Color.white;
        scoreLabel.raycastTarget = false;
        scoreLabel.text = "Puntos J1: 0";

        RectTransform rect = scoreLabel.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(32f, -24f);
        rect.sizeDelta = new Vector2(520f, 64f);
    }
    #endregion

    #region Notificaciones de Comodín Combinatorio
    public void ShowCombinationDoublesNotification(int playerIndex)
    {
        if (showCombinationDoublesNotification)
        {
            victoryLabel.gameObject.SetActive(false);
            defeatLabel.gameObject.SetActive(false);
            
            lastNotificationTime = Time.time;
            notificationTimer = 0f;
            
            if (notificationPrefab != null) 
            {
                GameObject notificationObj = Instantiate(notificationPrefab, transform);
                notificationObj.transform.localPosition = new Vector3(
                    (playerIndex % 2) * 2 - 1, 
                    (playerIndex / 2) * 2 - 1, 
                    0
                );
                notificationObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
                
                // CORRECCIÓN APLICADA AQUÍ: Se cambió Vector3.Scale por new Vector3
                float scale = 1f + (Mathf.Sin(Time.time * notificationFloatSpeed) * 0.5f) * (notificationScaleRange * 2 - 1);
                notificationObj.transform.localScale = new Vector3(scale, scale, scale);
            }
            
            string message = $"¡{players[playerIndex].playerName} ha colocado un Comodín Combinatorio!";
            victoryLabel.text = message;
            victoryLabel.gameObject.SetActive(true);
        }
    }

    public void HideCombinationDoublesNotification()
    {
        if (showCombinationDoublesNotification)
        {
            victoryLabel.gameObject.SetActive(false);
            defeatLabel.gameObject.SetActive(false);
            showCombinationDoublesNotification = false;
        }
    }
    #endregion

    #region Panel de Victoria/Derrota
    public void ShowVictoryPanel()
    {
        currentVictoryPanel = 0;
        victoryPanel.SetActive(true);
        if (players.Count > currentVictoryPanel) 
        {
            victoryLabel.text = $"{players[currentVictoryPanel].playerName} ¡GANA!";
        }
        
        if (winConfettiEffect != null)
        {
            winConfettiEffect.gameObject.SetActive(true);
        }
    }

    public void ShowDefeatPanel()
    {
        currentVictoryPanel = 1;
        defeatPanel.SetActive(true);
        defeatLabel.text = "El oponente ha ganado";
        
        if (loseConfettiEffect != null)
        {
            loseConfettiEffect.gameObject.SetActive(true);
        }
    }
    #endregion

    #region Notificaciones Flotantes
    public IEnumerator ShowFloatingNotification(string message, float duration = 2f)
    {
        if (notificationPrefab == null) yield break;

        float x = Random.Range(-1f, 1f);
        float z = Random.Range(0f, 1f);
        
        GameObject notification = Instantiate(notificationPrefab, transform);
        notification.transform.localPosition = new Vector3(x, z, 0);
        notification.transform.localRotation = Quaternion.identity;
        
        Text text = notification.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = message;
            text.alignment = TextAnchor.MiddleLeft; 
        }
        
        float entryDelay = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(entryDelay);
        
        float exitDelay = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(exitDelay);
        
        notification.gameObject.SetActive(false);
    }
    #endregion

    #region Actualización de Avatares
    public void UpdatePlayerAvatars(int playerIndex, Vector3 position, Color color)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        
        if (playerAvatarLeft != null)
        {
            playerAvatarLeft.transform.position = position;
            if (playerAvatarLeft.GetComponent<Image>() != null)
                playerAvatarLeft.GetComponent<Image>().color = color;
        }
        if (playerAvatarRight != null)
        {
            playerAvatarRight.transform.position = position;
            if (playerAvatarRight.GetComponent<Image>() != null)
                playerAvatarRight.GetComponent<Image>().color = color;
        }
        if (playerAvatarTop != null)
        {
            playerAvatarTop.transform.position = position;
            if (playerAvatarTop.GetComponent<Image>() != null)
                playerAvatarTop.GetComponent<Image>().color = color;
        }
        if (playerAvatarBottom != null)
        {
            playerAvatarBottom.transform.position = position;
            if (playerAvatarBottom.GetComponent<Image>() != null)
                playerAvatarBottom.GetComponent<Image>().color = color;
        }
    }
    #endregion

    #region Callbacks para el Sistema de Juego
    public void OnPlayerPlaysCombinationDoubles(int playerIndex)
    {
        if (showCombinationDoublesNotification)
        {
            ShowCombinationDoublesNotification(playerIndex);
        }
    }

    public void OnPlayerPlaysStandardMove(int playerIndex)
    {
        Color avatarColor = Color.white;
        if (playerAvatarLeft != null && playerAvatarLeft.GetComponent<Image>() != null)
        {
            avatarColor = playerAvatarLeft.GetComponent<Image>().color;
        }
        UpdatePlayerAvatars(playerIndex, playerAvatarLeft?.position ?? Vector3.zero, avatarColor);
    }
    #endregion

    #region Corrutinas de Efectos
    private IEnumerator ConfettiAnimation(float duration, Color color)
    {
        float startTime = Time.time;
        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;
            float intensity = Mathf.SmoothStep(0f, 1f, progress);
            Color tintedColor = Color.Lerp(Color.white, color, intensity);
            yield return null;
        }
        yield break;
    }
    #endregion
}