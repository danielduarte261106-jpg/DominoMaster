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
    [Header("Configuración de IA")]
    [SerializeField] private CasinoTableManager tableManager;
    [SerializeField] private List<DominoPiece> hand; // Fichas actuales de la IA

    [SerializeField] private float minThinkTime = 1.0f;
    [SerializeField] private float maxThinkTime = 2.5f;

    private int currentPlayerIndex => tableManager?.GetCurrentPlayerIndex() ?? -1;

    private void OnEnable()
    {
        // Suscripción opcional a eventos del gestor de mesa si es necesario
    }

    /// <summary>
    /// Llama este método al inicio del turno de la IA para iniciar el ciclo de pensamiento.
    /// </summary>
    public void IniciarTurnoIA()
    {
        if (tableManager == null) tableManager = FindObjectOfType<CasinoTableManager>();
        if (hand == null || hand.Count == 0) return;

        StartCoroutine(CorrutinaPensamientoIA());
    }

    /// <summary>
    /// Corrutina que simula 1 a 2.5 segundos de "pensamiento" antes de tomar una decisión.
    /// </summary>
    private IEnumerator CorrutinaPensamientoIA()
    {
        // 1. Tiempo de "pensamiento" aleatorio
        float thinkingDuration = Random.Range(minThinkTime, maxThinkTime);
        Debug.Log($"[IA] Pensando por {thinkingDuration:F2} segundos...");
        yield return new WaitForSeconds(thinkingDuration);

        // 2. Evaluar todas las fichas de la mano
        DominoPiece mejorFicha = null;
        int mejorPuntuacion = int.MinValue;
        bool puedeTrancarMesa = false;

        foreach (var ficha in hand)
        {
            // Prioridad 1: ¿Es un Comodín Combinatorio (doble o con 0/6)?
            bool esComodin = ficha.IsDouble || ficha.HasRiskValue;

            if (esComodin)
            {
                // 3. Calcular valor de jugada combinatorio
                int valorJugada = ficha.GetCombinatorialPlayValue();

                // 4. Verificar si jugar esta ficha tranca la mesa
                // Lógica simplificada: si el valor de la jugada es alto y la ficha tiene valores de riesgo (0 o 6),
                // probabilidad alta de bloqueo.
                if (puedeTrancarAlJugar(ficha))
                {
                    puedeTrancarMesa = true;
                    mejorFicha = ficha;
                    mejorPuntuacion = valorJugada;
                    break; // La IA prioriza trancar la mesa
                }
                // 5. Si no tranca inmediatamente, busca el mayor puntaje posible
                else if (valorJugada > mejorPuntuacion)
                {
                    mejorPuntuacion = valorJugada;
                    mejorFicha = ficha;
                }
            }
            // Si no es comodín, la IA puede elegir una ficha segura para mantener el flujo,
            // pero el enfoque principal es el comodín según la instrucción.
        }

        // 6. Si no encontró comodín jugable, elige una ficha aleatoria o la de menor impacto
        if (mejorFicha == null && hand.Count > 0)
        {
            mejorFicha = hand[Random.Range(0, hand.Count)];
            Debug.Log("[IA] No encontró comodín, juega ficha aleatoria.");
        }

        // 7. Ejecutar movimiento
        if (mejorFicha != null)
        {
            Debug.Log($"[IA] Jugando ficha: {mejorFicha.gameObject.name} (Valor Combinatorio: {mejorFicha.GetCombinatorialPlayValue()})");
            // Notificar al gestor de mesa que la IA jugó esta ficha.
            // En una integración real, se pasaría el índice del jugador o la ficha específica.
            tableManager.OnPlayerPlayedPiece(currentPlayerIndex);
        }
        else
        {
            Debug.Log("[IA] No hay movimientos válidos o decide pasar.");
            tableManager.OnPlayerPasses(currentPlayerIndex);
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
