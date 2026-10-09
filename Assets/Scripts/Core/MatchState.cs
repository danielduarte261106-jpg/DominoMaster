using System.Collections.Generic;

public enum BoardEnd
{
    Left,
    Right
}

/// <summary>
/// Resultado devuelto por MatchState.CheckAmbiguity.
/// </summary>
public enum PlacementAmbiguity
{
    /// La ficha solo encaja en Left (o la mesa está vacía).
    LeftOnly,
    /// La ficha solo encaja en Right.
    RightOnly,
    /// La ficha encaja en ambos extremos: hay que preguntar al jugador.
    BothEnds,
    /// La ficha no encaja en ninguno.
    None
}

/// <summary>
/// Estado puro de la cadena de fichas en mesa. Sin MonoBehaviour ni UI.
/// LeftEnd / RightEnd son null hasta la primera ficha.
/// </summary>
public sealed class MatchState
{
    private readonly List<DominoPiece> placedPieces = new List<DominoPiece>();

    public int? LeftEnd { get; private set; }
    public int? RightEnd { get; private set; }

    public bool IsEmpty => placedPieces.Count == 0;
    public IReadOnlyList<DominoPiece> PlacedPieces => placedPieces;

    public bool Contains(DominoPiece piece) => piece != null && placedPieces.Contains(piece);

    public bool CanPlaceOnLeft(DominoPiece piece)
    {
        if (piece == null || Contains(piece)) return false;
        if (IsEmpty) return true;
        return LeftEnd.HasValue && piece.MatchesValue(LeftEnd.Value);
    }

    public bool CanPlaceOnRight(DominoPiece piece)
    {
        if (piece == null || Contains(piece)) return false;
        if (IsEmpty) return true;
        return RightEnd.HasValue && piece.MatchesValue(RightEnd.Value);
    }

    public bool CanPlace(DominoPiece piece) => CanPlaceOnLeft(piece) || CanPlaceOnRight(piece);

    /// <summary>
    /// Determina si la ficha tiene colocación ambigua (encaja en los dos extremos
    /// a la vez) o si solo encaja en uno. Puro dominio: no toca Unity.
    /// </summary>
    public PlacementAmbiguity CheckAmbiguity(DominoPiece piece)
    {
        bool onLeft  = CanPlaceOnLeft(piece);
        bool onRight = CanPlaceOnRight(piece);

        if (!onLeft && !onRight) return PlacementAmbiguity.None;
        if (IsEmpty)             return PlacementAmbiguity.LeftOnly; // primera ficha: sin ambigüedad
        if (onLeft && onRight)   return PlacementAmbiguity.BothEnds;
        return onLeft ? PlacementAmbiguity.LeftOnly : PlacementAmbiguity.RightOnly;
    }

    /// <summary>
    /// Coloca la ficha en el primer extremo legal no ambiguo.
    /// Si ambos extremos aceptan la ficha devuelve FALSE sin modificar el estado;
    /// el controlador debe pedir al jugador que elija y luego llamar a
    /// TryPlace(piece, chosenEnd, out usedEnd).
    /// </summary>
    public bool TryPlace(DominoPiece piece, out BoardEnd usedEnd)
    {
        usedEnd = BoardEnd.Left;

        if (piece == null || Contains(piece)) return false;

        if (IsEmpty)
        {
            PlaceFirst(piece);
            return true;
        }

        // Si hay ambigüedad, rechazamos para que el controlador gestione la elección.
        if (CanPlaceOnLeft(piece) && CanPlaceOnRight(piece))
            return false;

        if (CanPlaceOnLeft(piece))
        {
            PlaceOnLeft(piece);
            usedEnd = BoardEnd.Left;
            return true;
        }

        if (CanPlaceOnRight(piece))
        {
            PlaceOnRight(piece);
            usedEnd = BoardEnd.Right;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Overload con extremo explícito: úsalo cuando el jugador ya eligió
    /// en qué extremo colocar la ficha (resuelve la ambigüedad).
    /// </summary>
    public bool TryPlace(DominoPiece piece, BoardEnd chosenEnd, out BoardEnd usedEnd)
    {
        usedEnd = chosenEnd;

        if (piece == null || Contains(piece)) return false;

        bool canChosen = chosenEnd == BoardEnd.Left
            ? CanPlaceOnLeft(piece)
            : CanPlaceOnRight(piece);

        if (!canChosen) return false;

        if (IsEmpty)
        {
            PlaceFirst(piece);
            return true;
        }

        if (chosenEnd == BoardEnd.Left)
            PlaceOnLeft(piece);
        else
            PlaceOnRight(piece);

        return true;
    }

    public void Reset()
    {
        placedPieces.Clear();
        LeftEnd = null;
        RightEnd = null;
    }

    private void PlaceFirst(DominoPiece piece)
    {
        LeftEnd = piece.SideA;
        RightEnd = piece.SideB;
        placedPieces.Add(piece);
    }

    private void PlaceOnLeft(DominoPiece piece)
    {
        LeftEnd = GetExposedValue(piece, LeftEnd.Value);
        placedPieces.Add(piece);
    }

    private void PlaceOnRight(DominoPiece piece)
    {
        RightEnd = GetExposedValue(piece, RightEnd.Value);
        placedPieces.Add(piece);
    }

    /// <summary>
    /// El lado que coincide se une al extremo; el otro queda expuesto.
    /// </summary>
    private static int GetExposedValue(DominoPiece piece, int attachedEnd)
    {
        return piece.SideA == attachedEnd ? piece.SideB : piece.SideA;
    }
}
