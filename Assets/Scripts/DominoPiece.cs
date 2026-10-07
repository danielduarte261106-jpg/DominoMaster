using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class DominoPiece : MonoBehaviour
{
    // Valores de los lados del dominó (0-6)
    [SerializeField] private int sideA = 0;
    [SerializeField] private int sideB = 0;

    // Referencias a componentes
    private Rigidbody2D rb;
    private BoxCollider2D bc;
    private SpriteRenderer spriteRenderer;

    // Configuración de física
    [Header("Física")]
    [Tooltip("Gravedad aplicada al caer")]
    public float fallGravity = 1.5f;
    [Tooltip("Fuerza de rebote al impactar")]
    public float bounceForce = 0.2f;
    [Tooltip("Tiempo para ajustarse a la posición final")]
    public float settleTime = 0.3f;

    // Referencia a los sprites de frutas (0=Semilla,1=Manzana,2=Uva,3=Limón,4=Piña,5=Sandía,6=Fresa)
    private Sprite[] fruitSprites;

    // Estado interno
    private bool isSettled = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // Eventos (opcional para otros sistemas)
    public System.Action<DominoPiece> OnSettled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Configuración inicial de física
        rb.gravityScale = fallGravity;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        bc.isTrigger = false;

        // Cargar sprites de frutas desde Resources/Frutas/
        fruitSprites = Resources.LoadAll<Sprite>("Frutas");
        if (fruitSprites == null || fruitSprites.Length == 0)
        {
            Debug.LogError("[DominoPiece] No se encontraron sprites en Resources/Frutas/. Asegúrate de que los assets estén allí.");
        }
        else
        {
            UpdateSprite();
        }
    }

    private void Start()
    {
        // Al iniciar, la pieza cae libremente gracias a la gravedad del Rigidbody2D.
        // Cuando colisione con algo (mesa u otra ficha), OnCollisionEnter2D manejará el rebote y el asentamiento.
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSettled) return;

        // Simular un pequeño rebote al impactar
        rb.velocity = Vector2.zero; // Detener velocidad lineal para evitar acumulación
        rb.angularVelocity = 0f;   // Detener rotación

        // Aplicar un impulso upward pequeño para el efecto de rebote
        Vector2 bounceDir = collision.GetContact(0).normal;
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        // Después de un breve momento, iniciar el asentamiento a la posición final
        StartCoroutine(SettleToPosition(transform.position, transform.rotation));
    }

    /// <summary>
    /// Corrutina que interpola posición y rotación a los valores objetivo usando Lerp.
    /// </summary>
    private IEnumerator SettleToPosition(Vector3 targetPos, Quaternion targetRot)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < settleTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / settleTime;
            // Suavizado (ease out)
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        // Asegurar valores exactos
        transform.position = targetPos;
        transform.rotation = targetRot;
        isSettled = true;
        rb.isKinematic = true; // Desactivar física una vez asentado
        bc.enabled = false;    // Opcional: desactivar colisión para evitar interferencias

        OnSettled?.Invoke(this);
    }

    /// <summary>
    /// Llama a este método desde el sistema de juego para indicar la posición final donde debe quedar la ficha.
    /// </summary>
    public void SetTargetTransform(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
        // Si ya está quieto (por ejemplo, se coloca directamente sin caer), asentamos inmediatamente.
        if (!rb.isKinematic && rb.velocity.sqrMagnitude < 0.01f)
        {
            StartCoroutine(SettleToPosition(targetPosition, targetRotation));
        }
    }

    /// <summary>
    /// Actualiza el sprite según los valores de los lados.
    /// Actualmente muestra el sprite del lado A (puedes extenderlo para mostrar ambos).
    /// </summary>
    private void UpdateSprite()
    {
        if (spriteRenderer == null || fruitSprites == null) return;

        int index = Mathf.Clamp(sideA, 0, fruitSprites.Length - 1);
        spriteRenderer.sprite = fruitSprites[index];
        // Opcional: cambiar nombre del GameObject para depuración
        gameObject.name = $"Domino_{sideA}_{sideB}";
    }

    // ======================
    // Lógica de Comodín Combinatorio
    // ======================

    /// <summary>
    /// Determina si la ficha es un doble (comodín básico).
    /// </summary>
    public bool IsDouble => sideA == sideB;

    /// <summary>
    /// Determina si la ficha contiene un 0 o un 6 (bono de riesgo).
    /// </summary>
    public bool HasRiskValue => sideA == 0 || sideA == 6 || sideB == 0 || sideB == 6;

    /// <summary>
    /// Calcula el valor de jugada base (suma de los lados).
    /// </summary>
    public int BasePlayValue => sideA + sideB;

    /// <summary>
    /// Calcula el valor final de la jugada aplicando las reglas del Comodín Combinatorio:
    /// - Si es doble, el valor se multiplica por 2.
    /// - Además, si contiene un 0 o un 6, se suma un bono de riesgo (por ejemplo, +3 puntos).
    /// Puedes ajustar el bono según tu diseño de puntuación.
    /// </summary>
    public int GetCombinatorialPlayValue()
    {
        int value = BasePlayValue;
        if (IsDouble)
        {
            value *= 2; // Doble multiplica el valor
        }
        if (HasRiskValue)
        {
            value += 3; // Bono de riesgo (ajustable)
        }
        return value;
    }

    // ======================
    // Métodos públicos para configurar los lados (usado por el generador o el pool)
    // ======================
    public void SetSides(int a, int b)
    {
        sideA = Mathf.Clamp(a, 0, 6);
        sideB = Mathf.Clamp(b, 0, 6);
        UpdateSprite();
    }

    // ======================
    // Gizmos para depuración en editor (opcional)
    // ======================
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f,
            $"{sideA}|{sideB} (Valor: {GetCombinatorialPlayValue()})");
    }
#endif
}
