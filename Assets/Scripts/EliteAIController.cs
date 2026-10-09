using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador de IA competitiva para el juego Dominó Frutas.
/// Analiza el estado de la mesa, evalúa la conveniencia de jugar un Comodín Combinatorio
/// y busca trancar la mesa cuando tiene ventaja matemática.
/// </summary>
public class EliteAIController : MonoBehaviour
{
    [Header("Configuracion de IA")]
    [SerializeField] private CasinoTableManager tableManager;
    [SerializeField] private HandManager handManager;  // fuente de verdad de la mano
    [SerializeField] private List<DominoPiece> hand;   // fallback/debug manual

    [SerializeField] private float minThinkTime = 1.0f;
    [SerializeField] private float maxThinkTime = 2.5f;

    // Indice de jugador que controla esta IA (se inyecta en ExecuteTurn)
    private int aiPlayerIndex = 1;

    // Referencia al controlador activo (se inyecta en ExecuteTurn)
    private MatchController matchController;

    private int legacyPlayerIndex => tableManager?.GetCurrentPlayerIndex() ?? -1;

    private void OnEnable()
    {
        // Suscripcion opcional a eventos del gestor de mesa si es necesario
    }

    // ── NUEVA API: punto de entrada desde MatchController ─────────────────────

    /// <summary>
    /// MatchController llama este metodo cuando es el turno de la IA.
    /// Inyecta la referencia al controlador y arranca la corrutina de pensamiento.
    /// </summary>
    public void ExecuteTurn(MatchController controller)
    {
        matchController  = controller;
        aiPlayerIndex    = controller.CurrentPlayerIndex;

        // Resolver HandManager si no esta asignado
        if (handManager == null)
            handManager = FindAnyObjectByType<HandManager>();

        bool hasCards = handManager != null
            ? handManager.PlayerHasPlayable(aiPlayerIndex)
            : (hand != null && hand.Count > 0);

        if (!hasCards)
        {
            Debug.Log("[IA] Sin fichas jugables. Pasa automaticamente.");
            matchController.PassTurn();
            return;
        }
        StartCoroutine(CorrutinaPensamientoIA());
    }

    // ── API legacy (compatible con CasinoTableManager) ────────────────────────

    /// <summary>Llamado desde CasinoTableManager (sistema legado).</summary>
    public void IniciarTurnoIA()
    {
        if (tableManager == null) tableManager = FindObjectOfType<CasinoTableManager>();
        if (hand == null || hand.Count == 0) return;
        StartCoroutine(CorrutinaPensamientoIA());
    }

    /// <summary>
    /// Corrutina que simula pensamiento de IA antes de tomar una decision.
    /// Prioridad: Comodin con bloqueo > mayor valor combinatorio > mejor jugable.
    /// </summary>
    private IEnumerator CorrutinaPensamientoIA()
    {
        float thinkingDuration = Random.Range(minThinkTime, maxThinkTime);
        Debug.Log($"[IA] Pensando por {thinkingDuration:F2} segundos...");
        yield return new WaitForSeconds(thinkingDuration);

        // Obtener mano efectiva: HandManager (preferido) o lista manual
        IReadOnlyList<DominoPiece> efectiveHand = null;
        if (handManager != null)
        {
            var playerHand = handManager.GetHand(aiPlayerIndex);
            efectiveHand   = playerHand?.Pieces;
        }
        if (efectiveHand == null)
            efectiveHand = hand;

        DominoPiece mejorFicha = null;
        int mejorPuntuacion    = int.MinValue;

        if (efectiveHand != null)
        {
            foreach (var ficha in efectiveHand)
            {
                if (ficha == null) continue;
                if (matchController != null && !matchController.CanPlayPiece(ficha)) continue;

                bool esComodin  = ficha.IsDouble || ficha.HasRiskValue;
                int valorJugada = ficha.GetCombinatorialPlayValue();

                if (esComodin && puedeTrancarAlJugar(ficha))
                {
                    mejorFicha      = ficha;
                    mejorPuntuacion = valorJugada;
                    break; // prioridad maxima
                }

                if (valorJugada > mejorPuntuacion)
                {
                    mejorPuntuacion = valorJugada;
                    mejorFicha      = ficha;
                }
            }
        }

        // Fallback: mejor jugable segun HandManager
        if (mejorFicha == null && handManager != null)
        {
            mejorFicha = handManager.GetBestPlayableForAI(aiPlayerIndex);
            if (mejorFicha != null)
                Debug.Log("[IA] Fallback: juega la mejor ficha disponible.");
        }

        // 7. Ejecutar movimiento via MatchController (nueva API) o CasinoTableManager (legado)
        if (mejorFicha != null)
        {
            Debug.Log($"[IA] Jugando ficha: {mejorFicha.gameObject.name} " +
                      $"(Valor Combinatorio: {mejorFicha.GetCombinatorialPlayValue()})");

            if (matchController != null)
            {
                // Nueva API: la IA juega directamente a traves del controlador
                bool played = matchController.TryPlayPiece(mejorFicha);
                if (!played)
                {
                    Debug.Log("[IA] TryPlayPiece fallo. La IA pasa.");
                    matchController.PassTurn();
                }
            }
            else if (tableManager != null)
            {
                // Legado: compatible con CasinoTableManager
                tableManager.OnPlayerPlayedPiece(legacyPlayerIndex);
            }
        }
        else
        {
            Debug.Log("[IA] No hay movimientos validos. Pasa.");
            if (matchController != null)
                matchController.PassTurn();
            else if (tableManager != null)
                tableManager.OnPlayerPasses(legacyPlayerIndex);
        }

        // Turno terminado
        yield break;
    }

    /// <summary>
    /// Evalúa si jugar la ficha dada tiene alta probabilidad de trancar la mesa (bloquear movimientos futuros).
    /// </summary>
    private bool puedeTrancarAlJugar(DominoPiece ficha)
    {
        // Lógica heurística:
        // - Si la ficha es doble (comodín base)
        // - O si contiene un 0 o un 6 (valores de riesgo)
        // - Y si la suma de sus lados es alta (mayor probabilidad de cerrar combinaciones)
        if (ficha.IsDouble) return true;
        if (ficha.HasRiskValue)
        {
            // Valor base > 8 sugiere que es una ficha "pesada" que probablemente cierre opciones
            return ficha.BasePlayValue > 8;
        }
        return false;
    }

    /// <summary>
    /// Método público para que el Table Manager llame cuando es el turno de la IA.
    /// </summary>
    public void ProcesarTurnoIA()
    {
        IniciarTurnoIA();
    }
}
