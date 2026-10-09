using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modelo puro de la mano de un jugador. Sin MonoBehaviour, sin Unity.
/// Contiene las fichas que le fueron repartidas, permite quitarlas
/// al jugarlas y calcula el valor residual para el desempate por tranca.
/// </summary>
public sealed class PlayerHand
{
    // ── Estado ─────────────────────────────────────────────────────────────────

    private readonly List<DominoPiece> pieces = new List<DominoPiece>();

    // ── Propiedades publicas ───────────────────────────────────────────────────

    public int PlayerIndex { get; }
    public int Count       => pieces.Count;
    public bool IsEmpty    => pieces.Count == 0;

    /// Suma de (SideA + SideB) de todas las fichas que quedan en mano.
    /// Se usa para el desempate cuando la mesa esta trancada.
    public int ResiduaValue
    {
        get
        {
            int total = 0;
            foreach (var p in pieces)
                total += p.SideA + p.SideB;
            return total;
        }
    }

    public IReadOnlyList<DominoPiece> Pieces => pieces;

    // ── Constructor ────────────────────────────────────────────────────────────

    public PlayerHand(int playerIndex)
    {
        PlayerIndex = playerIndex;
    }

    // ── API publica ────────────────────────────────────────────────────────────

    /// <summary>Agrega una ficha a la mano (durante el reparto).</summary>
    public void AddPiece(DominoPiece piece)
    {
        if (piece != null && !pieces.Contains(piece))
            pieces.Add(piece);
    }

    /// <summary>
    /// Quita la ficha de la mano. Devuelve true si estaba y fue quitada.
    /// Llamar antes de confirmar la jugada en MatchController.
    /// </summary>
    public bool RemovePiece(DominoPiece piece) => pieces.Remove(piece);

    /// <summary>Limpia la mano (inicio de nueva ronda).</summary>
    public void Clear() => pieces.Clear();

    /// <summary>Devuelve la ficha con mayor valor del Comodin Combinatorio que encaje en la mesa.</summary>
    public DominoPiece GetBestPlayable(MatchState state)
    {
        DominoPiece best = null;
        int bestValue    = int.MinValue;

        foreach (var piece in pieces)
        {
            if (!state.CanPlace(piece)) continue;
            int value = piece.GetCombinatorialPlayValue();
            if (value > bestValue)
            {
                bestValue = value;
                best      = piece;
            }
        }
        return best; // null si no hay jugada valida
    }

    /// <summary>Indica si el jugador puede realizar algun movimiento con el estado actual de la mesa.</summary>
    public bool HasAnyPlayable(MatchState state)
    {
        foreach (var piece in pieces)
            if (state.CanPlace(piece)) return true;
        return false;
    }
}
