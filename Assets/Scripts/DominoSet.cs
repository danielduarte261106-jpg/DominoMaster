using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un conjunto (mazo) de fichas de dominó.
/// Contiene una lista de DominoPiece y permite operaciones básicas como
/// obtener una ficha aleatoria y saber cuántas quedan.
/// </summary>
public class DominoSet
{
    // Lista interna de fichas
    private readonly List<DominoPiece> pieces = new List<DominoPiece>();

    /// <summary>
    /// Añade una ficha al set.
    /// </summary>
    public void AddPiece(DominoPiece piece)
    {
        if (piece != null)
        {
            pieces.Add(piece);
        }
    }

    /// <summary>
    /// Obtiene una ficha aleatoria del set y la elimina de él.
    /// Devuelve null si el set está vacío.
    /// </summary>
    public DominoPiece GetRandomPiece()
    {
        if (pieces.Count == 0)
            return null;

        int index = Random.Range(0, pieces.Count);
        DominoPiece selected = pieces[index];
        pieces.RemoveAt(index);
        return selected;
    }

    /// <summary>
    /// Devuelve el número de fichas restantes en el set.
    /// </summary>
    public int GetPieceCount()
    {
        return pieces.Count;
    }

    /// <summary>
    /// Indica si el set está vacío.
    /// </summary>
    public bool IsEmpty()
    {
        return pieces.Count == 0;
    }

    /// <summary>
    /// Opcional: devuelve una copia de la lista actual (solo lectura).
    /// </summary>
    public IReadOnlyList<DominoPiece> GetPieces()
    {
        return pieces.AsReadOnly();
    }
}
