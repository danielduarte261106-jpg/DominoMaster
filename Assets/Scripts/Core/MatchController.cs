using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquesta el estado de mesa y las reglas de jugada legal.
/// No conoce HUD ni anima fichas: solo valida, puntúa y notifica por eventos.
/// </summary>
public class MatchController : MonoBehaviour
{
    public event Action<DominoPiece, int> OnPiecePlayed;
    public event Action<int, int> OnScoreUpdated;

    private MatchState matchState;
    private readonly Dictionary<int, int> playerScores = new Dictionary<int, int>();

    [SerializeField] private int currentPlayerIndex;

    public MatchState State => matchState;
    public int CurrentPlayerIndex => currentPlayerIndex;

    private void Awake()
    {
        matchState = new MatchState();
        if (GetComponent<TableInputManager>() == null)
            gameObject.AddComponent<TableInputManager>();
    }

    public void SetCurrentPlayer(int playerIndex)
    {
        currentPlayerIndex = playerIndex;
    }

    public int GetScore(int playerIndex)
    {
        return playerScores.TryGetValue(playerIndex, out int score) ? score : 0;
    }

    public bool CanPlayPiece(DominoPiece piece)
    {
        EnsureState();
        return matchState.CanPlace(piece);
    }

    /// <summary>
    /// Valida si la ficha encaja en algún extremo libre. Si es legal, actualiza la cadena,
    /// suma el Comodín Combinatorio al jugador actual y dispara eventos.
    /// </summary>
    public bool TryPlayPiece(DominoPiece piece)
    {
        EnsureState();

        if (!matchState.TryPlace(piece, out _))
            return false;

        int points = piece.GetCombinatorialPlayValue();
        int newTotal = AddScore(currentPlayerIndex, points);

        OnPiecePlayed?.Invoke(piece, currentPlayerIndex);
        OnScoreUpdated?.Invoke(currentPlayerIndex, newTotal);
        return true;
    }

    public void ResetMatch()
    {
        EnsureState();
        matchState.Reset();
        playerScores.Clear();
        currentPlayerIndex = 0;
    }

    private int AddScore(int playerIndex, int points)
    {
        int total = GetScore(playerIndex) + points;
        playerScores[playerIndex] = total;
        return total;
    }

    private void EnsureState()
    {
        if (matchState == null)
            matchState = new MatchState();
    }
}
