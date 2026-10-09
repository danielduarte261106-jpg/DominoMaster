using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vista del dialogo de eleccion de extremo.
/// Se activa cuando MatchController detecta ambiguedad (OnEndAmbiguity)
/// y ofrece dos botones: "Izquierda" y "Derecha".
/// Tambien expone un boton "Cancelar" opcional para deshacer la seleccion.
///
/// SETUP en Unity Editor:
///   1. Crea un Panel en el Canvas (llamalo "EndSelectionPanel").
///   2. Agrega este script al Panel.
///   3. Asigna los campos en el Inspector:
///      - matchController  -> el GameObject que tiene MatchController.
///      - panelRoot        -> este mismo Panel (o el que quieras ocultar/mostrar).
///      - leftButton       -> boton "Izquierda".
///      - rightButton      -> boton "Derecha".
///      - cancelButton     -> boton "Cancelar" (opcional).
///      - infoLabel        -> Text que muestra los valores de los extremos.
/// </summary>
public class EndSelectionUI : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private MatchController matchController;

    [Header("Elementos del panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Text infoLabel;

    // Ficha en espera mientras el panel esta visible
    private DominoPiece currentPiece;

    // ── Ciclo de vida ──────────────────────────────────────────────────────────
    private void Awake()
    {
        ResolveController();
        SetupButtons();
        HidePanel();
    }

    private void Start()
    {
        ResolveController();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        if (matchController == null) return;
        matchController.OnEndAmbiguity    -= HandleAmbiguity;
        matchController.OnAmbiguityResolved -= HandleResolved;
    }

    // ── Logica de panel ────────────────────────────────────────────────────────
    private void HandleAmbiguity(DominoPiece piece, int leftEndValue, int rightEndValue)
    {
        currentPiece = piece;

        // Actualizar etiqueta informativa con los nombres de frutas
        if (infoLabel != null)
        {
            string leftFruit  = FruitName(leftEndValue);
            string rightFruit = FruitName(rightEndValue);
            infoLabel.text = $"Esta ficha encaja en ambos lados.\n" +
                             $"Izquierda: {leftFruit} ({leftEndValue})   " +
                             $"Derecha: {rightFruit} ({rightEndValue})";
        }

        ShowPanel();
    }

    private void HandleResolved()
    {
        currentPiece = null;
        HidePanel();
    }

    private void OnLeftChosen()
    {
        if (currentPiece == null || matchController == null) return;
        matchController.ResolveAmbiguity(currentPiece, BoardEnd.Left);
        // HidePanel() lo llama HandleResolved via evento
    }

    private void OnRightChosen()
    {
        if (currentPiece == null || matchController == null) return;
        matchController.ResolveAmbiguity(currentPiece, BoardEnd.Right);
    }

    private void OnCancelChosen()
    {
        if (matchController == null) return;
        matchController.CancelAmbiguity();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ResolveController()
    {
        if (matchController != null) return;
        matchController = FindAnyObjectByType<MatchController>();
    }

    private void SubscribeEvents()
    {
        if (matchController == null) return;
        matchController.OnEndAmbiguity     += HandleAmbiguity;
        matchController.OnAmbiguityResolved += HandleResolved;
    }

    private void SetupButtons()
    {
        if (leftButton   != null) leftButton.onClick.AddListener(OnLeftChosen);
        if (rightButton  != null) rightButton.onClick.AddListener(OnRightChosen);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelChosen);
    }

    /// <summary>Convierte el valor numerico en el nombre de fruta del juego.</summary>
    private static string FruitName(int value) => value switch
    {
        0 => "Semilla",
        1 => "Uva",
        2 => "Fresa",
        3 => "Lima",
        4 => "Manzana",
        5 => "Pina",
        6 => "Sandia",
        _ => value.ToString()
    };
}
