using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour que gestiona el reparto de fichas y las manos de todos los jugadores.
///
/// Responsabilidades:
///   1. Recibir el set completo de fichas de DominoSet.
///   2. Barajar y repartir N fichas a cada jugador.
///   3. Mantener PlayerHand[] para acceso rapido.
///   4. Escuchar OnPiecePlayed de MatchController para quitar la ficha de la mano.
///   5. Detectar mano vacia -> llamar MatchController.DeclareWinner().
///   6. Posicionar visualmente las fichas de la mano del humano en la parte inferior.
///   7. Ocultar las fichas de las manos de la IA.
///
/// SETUP en Unity:
///   - Asigna dominoSet, matchController en el Inspector.
///   - Llama a DealHands() desde el inicio de la partida (o desde CasinoTableManager.Setup).
/// </summary>
public class HandManager : MonoBehaviour
{
    // ── Eventos ───────────────────────────────────────────────────────────────

    /// <summary>Una mano fue repartida. Params: playerIndex, cantidad de fichas.</summary>
    public event Action<int, int> OnHandDealt;

    /// <summary>Una ficha fue quitada de una mano. Params: playerIndex, fichas restantes.</summary>
    public event Action<int, int> OnPieceRemovedFromHand;

    /// <summary>Un jugador se quedo sin fichas (gana la ronda).</summary>
    public event Action<int> OnPlayerHandEmpty;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Referencias")]
    [SerializeField] private DominoSet dominoSet;
    [SerializeField] private MatchController matchController;

    [Header("Reglas de reparto")]
    [Tooltip("Fichas por jugador. Estandar: 7 para 2 jugadores, 5 para 4.")]
    [SerializeField] private int piecesPerPlayer = 7;

    [Header("Layout de mano del humano (J0)")]
    [Tooltip("Centro de la fila de mano en coordenadas de mundo.")]
    [SerializeField] private Vector3 humanHandOrigin = new Vector3(0f, -3.75f, 0f);
    [SerializeField] private float humanHandSpacing  = 1.05f;

    // ── Estado interno ────────────────────────────────────────────────────────

    private PlayerHand[] hands;
    private int playerCount;

    // ── Propiedades publicas ──────────────────────────────────────────────────

    public int PlayerCount => playerCount;
    public PlayerHand GetHand(int playerIndex) =>
        (playerIndex >= 0 && playerIndex < playerCount) ? hands[playerIndex] : null;

    // ── Ciclo de vida Unity ───────────────────────────────────────────────────

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        if (matchController != null)
            matchController.OnPiecePlayed -= HandlePiecePlayed;
    }

    // ── API publica ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa las manos y reparte fichas. Llamar una vez por ronda,
    /// despues de que DominoSet haya generado el set completo.
    /// </summary>
    public void DealHands(int numPlayers)
    {
        playerCount = Mathf.Clamp(numPlayers, 2, 4);
        hands       = new PlayerHand[playerCount];

        for (int i = 0; i < playerCount; i++)
            hands[i] = new PlayerHand(i);

        // Ajuste automatico: si el set no tiene fichas suficientes, reducimos
        int available = dominoSet != null ? dominoSet.GetPieceCount() : 0;
        int needed    = piecesPerPlayer * playerCount;
        if (available < needed)
        {
            Debug.LogWarning($"[HandManager] Solo hay {available} fichas para {needed} necesarias. " +
                             "Reduciendo fichas por jugador.");
            piecesPerPlayer = Mathf.FloorToInt((float)available / playerCount);
        }

        // Reparto circular
        for (int card = 0; card < piecesPerPlayer; card++)
        {
            for (int player = 0; player < playerCount; player++)
            {
                DominoPiece piece = dominoSet?.GetRandomPiece();
                if (piece == null) break;

                hands[player].AddPiece(piece);
            }
        }

        // Post-reparto: layout visual y eventos
        for (int i = 0; i < playerCount; i++)
        {
            if (i == 0)
                LayoutHumanHand();
            else
                HideAIHand(i);

            OnHandDealt?.Invoke(i, hands[i].Count);
            Debug.Log($"[HandManager] J{i + 1} recibio {hands[i].Count} fichas.");
        }
    }

    /// <summary>
    /// Devuelve la mejor ficha jugable para un jugador (usada por la IA).
    /// </summary>
    public DominoPiece GetBestPlayableForAI(int playerIndex)
    {
        var hand = GetHand(playerIndex);
        if (hand == null || matchController == null) return null;
        return hand.GetBestPlayable(matchController.State);
    }

    /// <summary>
    /// Verifica si un jugador tiene al menos un movimiento legal.
    /// </summary>
    public bool PlayerHasPlayable(int playerIndex)
    {
        var hand = GetHand(playerIndex);
        if (hand == null || matchController == null) return false;
        return hand.HasAnyPlayable(matchController.State);
    }

    /// <summary>
    /// Reposiciona las fichas de la mano humana tras jugar una
    /// (para cerrar el hueco visual).
    /// </summary>
    public void RefreshHumanHandLayout() => LayoutHumanHand();

    /// <summary>
    /// Reinicia todas las manos para una nueva ronda.
    /// </summary>
    public void ResetHands()
    {
        if (hands == null) return;
        foreach (var hand in hands)
            hand?.Clear();
    }

    // ── Handlers de eventos ───────────────────────────────────────────────────

    private void HandlePiecePlayed(DominoPiece piece, int playerIndex)
    {
        var hand = GetHand(playerIndex);
        if (hand == null) return;

        hand.RemovePiece(piece);
        OnPieceRemovedFromHand?.Invoke(playerIndex, hand.Count);

        Debug.Log($"[HandManager] J{playerIndex + 1} jugo ficha. Mano restante: {hand.Count}");

        if (hand.IsEmpty)
        {
            Debug.Log($"[HandManager] J{playerIndex + 1} se quedo sin fichas. GANA LA RONDA.");
            OnPlayerHandEmpty?.Invoke(playerIndex);
            matchController?.DeclareWinner(playerIndex);
        }
        else if (playerIndex == 0)
        {
            // Actualizar layout visual de la mano humana
            LayoutHumanHand();
        }
    }

    // ── Layout visual ─────────────────────────────────────────────────────────

    /// <summary>
    /// Coloca las fichas del jugador humano (index 0) en una fila centrada
    /// en la parte inferior de la pantalla. Las fichas son interactivas.
    /// </summary>
    private void LayoutHumanHand()
    {
        var hand = GetHand(0);
        if (hand == null) return;

        int count      = hand.Count;
        float spacing  = Mathf.Max(humanHandSpacing, 1.3f);
        float totalW   = (count - 1) * spacing;
        float startX   = humanHandOrigin.x - totalW * 0.5f;

        for (int i = 0; i < count; i++)
        {
            DominoPiece piece = hand.Pieces[i];
            if (piece == null) continue;

            Vector3 target = new Vector3(startX + i * spacing,
                                         humanHandOrigin.y,
                                         humanHandOrigin.z);
            piece.transform.position = target;
            piece.transform.SetParent(null);
            piece.SetFaceDown(false);           // Mostrar frutas al jugador humano
            piece.SetPlayerInputEnabled(true);   // Asegurar que sea clickeable
        }
    }

    /// <summary>
    /// Desactiva la interaccion y mueve las fichas de IA fuera de vista.
    /// </summary>
    private void HideAIHand(int playerIndex)
    {
        var hand = GetHand(playerIndex);
        if (hand == null) return;

        foreach (var piece in hand.Pieces)
        {
            if (piece == null) continue;
            piece.SetPlayerInputEnabled(false);

            // Mover fuera del frustum hasta que sea su turno de jugar
            // (una posicion con Z alto para que quede detras del tapete)
            piece.transform.position = new Vector3(
                1000f + playerIndex * 10f, 1000f, 10f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (dominoSet == null)
            dominoSet = FindAnyObjectByType<DominoSet>();

        if (matchController == null)
            matchController = FindAnyObjectByType<MatchController>();
    }

    private void SubscribeEvents()
    {
        if (matchController != null)
            matchController.OnPiecePlayed += HandlePiecePlayed;
    }
}
