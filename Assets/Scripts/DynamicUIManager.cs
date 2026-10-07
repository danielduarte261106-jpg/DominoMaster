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
    [SerializeField] private float notificationFloatSpeed = 10f; // Velocidad de aparición de notificación
    [SerializeField] private float notificationScaleRange = 0.5f; // Rango de escala de la notificación
    
    private float lastNotificationTime;
    private float notificationTimer;
    #endregion

    #region Estado de la Interfaz
    private int currentVictoryPanel = 0; // 0 = victoria, 1 = derrota
    private bool showCombinationDoublesNotification = false;
    #endregion

    #region Variables
    private List<PlayerData> players = new List<PlayerData>();
    private Dictionary<int, Sprite> playerSprites = new Dictionary<int, Sprite>(); // Mapa de índice de jugador a sprite
    #endregion

    #region Initialization
    private void Start()
    {
        // Inicializar referencias a avatares
        if (playerAvatarLeft != null) playerAvatarLeft.gameObject.SetActive(true);
        if (playerAvatarRight != null) playerAvatarRight.gameObject.SetActive(true);
        if (playerAvatarTop != null) playerAvatarTop.gameObject.SetActive(true);
        if (playerAvatarBottom != null) playerAvatarBottom.gameObject.SetActive(true);

        // Crear panel de victoria
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
            victoryLabel.text = "";
            winConfettiEffect.gameObject.SetActive(false);
        }

        // Crear panel de derrota
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
            defeatLabel.text = "";
            loseConfettiEffect.gameObject.SetActive(false);
        }

        // Inicializar notificaciones
        lastNotificationTime = -Mathf.Infinity;
        notificationTimer = 0f;
    }

    #endregion

    #region Notificaciones de Comodín Combinatorio
    /// <summary>
    /// Dispara una notificación de Comodín Combinatorio (doble o valor de riesgo).
    /// </summary>
    public void ShowCombinationDoublesNotification(int playerIndex)
    {
        if (showCombinationDoublesNotification)
        {
            // Ocultar notificación anterior
            victoryLabel.gameObject.SetActive(false);
            defeatLabel.gameObject.SetActive(false);
            
            // Activar nueva notificación
            lastNotificationTime = Time.time;
            notificationTimer = 0f;
            
            // Crear objeto de notificación
            GameObject notificationObj = Instantiate(notificationPrefab, transform);
            notificationObj.transform.localPosition = new Vector3(
                (playerIndex % 2) * 2 - 1, 
                (playerIndex / 2) * 2 - 1, 
                0
            );
            notificationObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            // Configurar texto de la notificación
            string message = $"¡{players[playerIndex].playerName} ha colocado un Comodín Combinatorio!";
            victoryLabel.text = message;
            victoryLabel.gameObject.SetActive(true);
            
            // Animar la notificación
            float scale = 1f + (Mathf.Sin(Time.time * notificationFloatSpeed) * 0.5f) * (notificationScaleRange * 2 - 1);
            notificationObj.transform.localScale = Vector3.Scale(scale, scale, scale);
        }
    }

    /// <summary>
    /// Desactivar la notificación de Comodín Combinatorio.
    /// </summary>
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
    /// <summary>
    /// Cambia el panel de victoria o derrota según el resultado.
    /// </summary>
    public void ShowVictoryPanel()
    {
        currentVictoryPanel = 0;
        victoryPanel.SetActive(true);
        victoryLabel.text = $"{players[currentVictoryPanel].playerName} ¡GANA!";
        
        // Activar efectos de confeti
        if (winConfettiEffect != null)
        {
            winConfettiEffect.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Cambia el panel de derrota.
    /// </summary>
    public void ShowDefeatPanel()
    {
        currentVictoryPanel = 1;
        defeatPanel.SetActive(true);
        defeatLabel.text = "El oponente ha ganado";
        
        // Activar efectos de confeti negativo
        if (loseConfettiEffect != null)
        {
            loseConfettiEffect.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Notificaciones Flotantes
    /// <summary>
    /// Emite una notificación flotante de alto impacto cuando ocurre un evento importante.
    /// </summary>
    public void ShowFloatingNotification(string message, float duration = 2f)
    {
        // Calcular posición aleatoria en la pantalla
        float x = Random.Range(-1f, 1f);
        float z = Random.Range(0f, 1f);
        
        GameObject notification = Instantiate(notificationPrefab, transform);
        notification.transform.localPosition = new Vector3(x, z, 0);
        notification.transform.localRotation = Quaternion.identity;
        
        // Texto de la notificación
        Text text = notification.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = message;
            text.alignment = TextAlignment.Left;
        }
        
        // Animar entrada
        float entryDelay = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(entryDelay);
        
        // Animar salida
        float exitDelay = Random.Range(0.5f, 1.5f);
        yield return new WaitForSeconds(exitDelay);
        
        // Desactivar
        notification.gameObject.SetActive(false);
    }

    #endregion

    #region Actualización de Avatares
    /// <summary>
    /// Actualiza la posición y apariencia de los avatares visuales de los jugadores.
    /// </summary>
    public void UpdatePlayerAvatars(int playerIndex, Vector3 position, Color color)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;
        
        var player = players[playerIndex];
        if (playerAvatarLeft != null)
        {
            playerAvatarLeft.transform.position = position;
            playerAvatarLeft.GetComponent<SpriteRenderer>().color = color;
        }
        if (playerAvatarRight != null)
        {
            playerAvatarRight.transform.position = position;
            playerAvatarRight.GetComponent<SpriteRenderer>().color = color;
        }
        if (playerAvatarTop != null)
        {
            playerAvatarTop.transform.position = position;
            playerAvatarTop.GetComponent<SpriteRenderer>().color = color;
        }
        if (playerAvatarBottom != null)
        {
            playerAvatarBottom.transform.position = position;
            playerAvatarBottom.GetComponent<SpriteRenderer>().color = color;
        }
    }

    #endregion

    #region Callbacks para el Sistema de Juego
    /// <summary>
    /// Llamar cuando un jugador juega una ficha y se genera un Comodín Combinatorio.
    /// </summary>
    public void OnPlayerPlaysCombinationDoubles(int playerIndex)
    {
        if (showCombinationDoublesNotification)
        {
            ShowCombinationDoublesNotification(playerIndex);
        }
    }

    /// <summary>
    /// Llamar cuando se resuelve una combinación de dominó.
    /// </summary>
    public void OnPlayerPlaysStandardMove(int playerIndex)
    {
        // Actualizar posición del avatar
        UpdatePlayerAvatars(playerIndex, playerAvatarLeft?.position ?? Vector3.zero, playerAvatarLeft?.color ?? Color.white);
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
            // Modificar el color del confeti según el progreso
            Color tintedColor = Color.Lerp(Color.white, color, intensity);
            yield return null;
        }
        yield break;
    }

    #endregion
}
