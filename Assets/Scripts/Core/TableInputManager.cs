using UnityEngine;

/// <summary>
/// Detecta clics en mundo 2D y prioriza fichas aunque la mesa tenga un collider debajo.
/// </summary>
public class TableInputManager : MonoBehaviour
{
    [SerializeField] private MatchController matchController;
    [SerializeField] private Camera worldCamera;

    private void Awake()
    {
        if (matchController == null)
            matchController = GetComponent<MatchController>();
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        Vector2 worldPoint = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

        for (int i = 0; i < hits.Length; i++)
        {
            DominoPiece piece = hits[i].GetComponent<DominoPiece>();
            if (piece == null)
                continue;

            piece.TryPlayFromInput();
            return;
        }
    }
}
