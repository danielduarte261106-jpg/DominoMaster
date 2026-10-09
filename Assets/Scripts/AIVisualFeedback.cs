using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Proporciona feedback visual inmersivo para las acciones de la Inteligencia Artificial.
/// Muestra un indicador de pensamiento (burbuja o texto) y una animación suave de revelación
/// al colocar la ficha en el tapete de juego.
/// </summary>
public class AIVisualFeedback : MonoBehaviour
{
    [Header("Referencias de Escena")]
    [SerializeField] private MatchController matchController;
    [SerializeField] private Transform aiAvatarTransform;

    [Header("Indicador de Pensamiento")]
    [SerializeField] private GameObject thinkingBubble;
    [SerializeField] private Text thinkingText;
    [SerializeField] private float pulseSpeed = 4f;

    [Header("Efecto de Jugada")]
    [Tooltip("Tiempo en segundos que se resalta la ficha antes de colocarla en la mesa")]
    [SerializeField] private float pieceHighlightDuration = 0.4f;
    [SerializeField] private Color aiPieceHighlightColor = new Color(1f, 0.85f, 0.3f, 1f);

    private Coroutine thinkingCoroutine;

    private void Awake()
    {
        if (thinkingBubble != null)
            thinkingBubble.SetActive(false);
    }

    private void Start()
    {
        if (matchController == null)
            matchController = FindAnyObjectByType<MatchController>();

        if (matchController != null)
        {
            matchController.OnTurnChanged += HandleTurnChanged;
            matchController.OnPiecePlayed += HandlePiecePlayed;
        }
    }

    private void OnDestroy()
    {
        if (matchController != null)
        {
            matchController.OnTurnChanged -= HandleTurnChanged;
            matchController.OnPiecePlayed -= HandlePiecePlayed;
        }
    }

    private void HandleTurnChanged(int playerIndex, bool isAI)
    {
        if (isAI)
        {
            StartThinkingFeedback();
        }
        else
        {
            StopThinkingFeedback();
        }
    }

    private void HandlePiecePlayed(DominoPiece piece, int playerIndex)
    {
        if (matchController != null && matchController.IsCurrentPlayerAI && playerIndex != 0)
        {
            StopThinkingFeedback();
            if (piece != null)
                StartCoroutine(AnimateAIPiecePlacement(piece));
        }
    }

    public void StartThinkingFeedback()
    {
        if (thinkingCoroutine != null)
            StopCoroutine(thinkingCoroutine);

        if (thinkingBubble != null)
            thinkingBubble.SetActive(true);

        thinkingCoroutine = StartCoroutine(PulseThinkingIndicator());
    }

    public void StopThinkingFeedback()
    {
        if (thinkingCoroutine != null)
        {
            StopCoroutine(thinkingCoroutine);
            thinkingCoroutine = null;
        }

        if (thinkingBubble != null)
            thinkingBubble.SetActive(false);
    }

    private IEnumerator PulseThinkingIndicator()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime * pulseSpeed;
            float scale = 1f + Mathf.Sin(timer) * 0.1f;
            if (thinkingBubble != null)
                thinkingBubble.transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }
    }

    private IEnumerator AnimateAIPiecePlacement(DominoPiece piece)
    {
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        if (sr != null)
            sr.color = aiPieceHighlightColor;

        Vector3 originalScale = piece.transform.localScale;
        piece.transform.localScale = originalScale * 1.15f;

        yield return new WaitForSeconds(pieceHighlightDuration);

        if (sr != null)
            sr.color = originalColor;

        piece.transform.localScale = originalScale;
    }
}
