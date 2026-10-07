using System.Collections.Generic;

public enum BoardEnd
{
    Left,
    Right
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
    /// Coloca la ficha en el primer extremo legal (mesa vacía, luego Left, luego Right).
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
