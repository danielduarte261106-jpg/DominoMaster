using System;
using System.Collections.Generic;
using UnityEngine;

public class DominoSet : MonoBehaviour
{
    [Header("Configuracion del Set")]
    public GameObject dominoPrefab;

    [Header("Texturas de Frutas (0 al 6)")]
    public Sprite[] fruitSprites;

    [Header("Integracion con HandManager")]
    [Tooltip("Si esta asignado, DominoSet llama DealHands() automaticamente al generar.")]
    [SerializeField] private HandManager handManager;
    [SerializeField] private int playerCountForDeal = 2;

    /// <summary>Disparado cuando GenerateFullSet termina. Param: lista de fichas generadas.</summary>
    public event Action<IReadOnlyList<DominoPiece>> OnSetGenerated;

    private readonly List<DominoPiece> pieces = new List<DominoPiece>();

    private void Start()
    {
        GenerateFullSet();
    }

    public void GenerateFullSet()
    {
        pieces.Clear();
        
        if (dominoPrefab == null || fruitSprites == null || fruitSprites.Length < 7)
        {
            Debug.LogError("¡Falta el Prefab del Dominó o los Sprites en el Inspector!");
            return;
        }

        int index = 0;
        for (int left = 0; left <= 6; left++)
        {
            for (int right = left; right <= 6; right++)
            {
                // Colocar fichas iniciales en la reserva lateral fuera del centro del tapete
                float x = -6.8f + (index % 4) * 0.45f; 
                float y = 2.5f - (index / 4) * 0.4f;
                Vector3 pos = new Vector3(x, y, 0);

                GameObject pieceObj = Instantiate(dominoPrefab, pos, Quaternion.identity);
                pieceObj.name = $"Ficha_{left}_{right}";
                pieceObj.transform.SetParent(this.transform);

                DominoPiece pieceScript = pieceObj.GetComponent<DominoPiece>();
                if (pieceScript != null)
                {
                    pieces.Add(pieceScript);
                    pieceScript.SetupVisuals(left, right, fruitSprites[left], fruitSprites[right]);
                    pieceScript.SetFaceDown(true);
                    pieceScript.SetPlayerInputEnabled(false);
                }
                
                index++;
            }
        }
        Debug.Log($"[DominoSet] Se generaron {pieces.Count} fichas.");

        // Notificar a HandManager si esta asignado
        OnSetGenerated?.Invoke(pieces.AsReadOnly());
        if (handManager == null)
            handManager = FindAnyObjectByType<HandManager>();
        if (handManager != null)
            handManager.DealHands(playerCountForDeal);
    }

    // --- LÓGICA DE GESTIÓN DEL MAZO ---
    public void AddPiece(DominoPiece piece) { if (piece != null) pieces.Add(piece); }
    public DominoPiece GetRandomPiece() {
        if (pieces.Count == 0) return null;
        int index = UnityEngine.Random.Range(0, pieces.Count);
        DominoPiece selected = pieces[index];
        pieces.RemoveAt(index);
        return selected;
    }
    public int GetPieceCount() { return pieces.Count; }
    public bool IsEmpty() { return pieces.Count == 0; }
    public IReadOnlyList<DominoPiece> GetPieces() { return pieces.AsReadOnly(); }
}