using System;
using System.Collections.Generic;

// Correct pieces lock in place. Connecting them earns fog clears and one hint.
public sealed class PhotoPuzzleRound
{
    public const int GridSize = 4;
    public const int PieceCount = GridSize * GridSize;
    public const int FogPieceCount = 4;
    public const int LinksPerCharge = 3;
    public const int PiecesPerClear = 2;
    public const int BonusLinkTarget = 9;
    private const int StarterRows = 2;
    private const int StarterColumns = 3;

    private readonly int[] fogOrder = new int[FogPieceCount];
    private readonly int initialFogCount;
    private int placed;
    private int fogged;
    private int spentCharges;
    private bool bonusSpent;

    public int PlacedCount { get; private set; }
    public int LargestConnectedGroup { get; private set; }
    public bool Complete => PlacedCount == PieceCount;
    public int FoggedCount => initialFogCount - spentCharges * PiecesPerClear;
    public int ClearFogCharges => Math.Min(initialFogCount / PiecesPerClear, LargestConnectedGroup / LinksPerCharge) - spentCharges;
    public bool CanClearFog => ClearFogCharges > 0;
    public bool BonusAvailable => !Complete && !bonusSpent && LargestConnectedGroup >= BonusLinkTarget;

    public PhotoPuzzleRound() { }

    public PhotoPuzzleRound(IReadOnlyList<int> pieceOrder, int starterTopRow, int starterLeftColumn)
    {
        if (pieceOrder == null) throw new ArgumentNullException(nameof(pieceOrder));
        if (pieceOrder.Count != PieceCount) throw new ArgumentException("Supply each piece once.", nameof(pieceOrder));
        if (starterTopRow < 0 || starterTopRow > GridSize - StarterRows) throw new ArgumentOutOfRangeException(nameof(starterTopRow));
        if (starterLeftColumn < 0 || starterLeftColumn > GridSize - StarterColumns) throw new ArgumentOutOfRangeException(nameof(starterLeftColumn));

        // Keep one six-piece rectangle visible so any fog layout can earn both charges.
        int seen = 0;
        foreach (int piece in pieceOrder)
        {
            if (!IsValidPiece(piece) || (seen & (1 << piece)) != 0)
                throw new ArgumentException("Supply each piece once.", nameof(pieceOrder));
            seen |= 1 << piece;
            int row = piece / GridSize;
            int column = piece % GridSize;
            bool isInStarter = row >= starterTopRow && row < starterTopRow + StarterRows &&
                column >= starterLeftColumn && column < starterLeftColumn + StarterColumns;
            if (isInStarter || initialFogCount == FogPieceCount) continue;
            fogOrder[initialFogCount++] = piece;
            fogged |= 1 << piece;
        }
    }

    public bool IsPlaced(int piece) => IsValidPiece(piece) && (placed & (1 << piece)) != 0;
    public bool IsFogged(int piece) => IsValidPiece(piece) && (fogged & (1 << piece)) != 0;

    public bool TryPlace(int piece, int slot)
    {
        if (piece != slot || !IsRevealedAndLoose(piece)) return false;
        PlacePieces(1 << piece, piece, 1);
        return true;
    }

    public bool TryPlaceGroup(int[] members, int anchorPiece, int anchorSlot)
    {
        // The loose group uses the pieces' original relative positions. A matching
        // anchor places every member in its own slot, including at the board edges.
        if (members == null || !IsValidPiece(anchorPiece) || anchorPiece != anchorSlot) return false;
        int group = 0;
        foreach (int piece in members)
        {
            if (!IsRevealedAndLoose(piece)) return false;
            int bit = 1 << piece;
            if ((group & bit) != 0) return false;
            group |= bit;
        }
        if ((group & (1 << anchorPiece)) == 0 || CountConnectedFrom(anchorPiece, group) != members.Length) return false;

        PlacePieces(group, anchorPiece, members.Length);
        return true;
    }

    public bool TryMatchNeighbor(int piece, out int neighbor, int[] excludedPieces = null)
    {
        neighbor = -1;
        if (!BonusAvailable || !IsRevealedAndLoose(piece)) return false;

        Span<int> neighbors = stackalloc int[4];
        int count = GetNeighbors(piece, neighbors);
        for (int i = 0; i < count; i++)
        {
            int candidate = neighbors[i];
            if (IsFogged(candidate) || (excludedPieces != null && Array.IndexOf(excludedPieces, candidate) >= 0)) continue;
            if (!IsPlaced(candidate))
            {
                neighbor = candidate;
                break;
            }
            if (neighbor < 0) neighbor = candidate;
        }
        if (neighbor < 0) return false;
        bonusSpent = true;
        return true;
    }

    public bool TryFindRow(int piece, out int row)
    {
        row = -1;
        if (!BonusAvailable || !IsRevealedAndLoose(piece)) return false;
        row = piece / GridSize;
        bonusSpent = true;
        return true;
    }

    public bool TryClearFog(out int first, out int second)
    {
        first = second = -1;
        if (!CanClearFog) return false;

        int index = spentCharges * PiecesPerClear;
        first = fogOrder[index];
        second = fogOrder[index + 1];
        fogged &= ~((1 << first) | (1 << second));
        spentCharges++;
        return true;
    }

    private bool IsRevealedAndLoose(int piece) => IsValidPiece(piece) && !IsPlaced(piece) && !IsFogged(piece);

    private void PlacePieces(int group, int anchor, int count)
    {
        placed |= group;
        PlacedCount += count;
        LargestConnectedGroup = Math.Max(LargestConnectedGroup, CountConnectedFrom(anchor, placed));
    }

    private static int CountConnectedFrom(int start, int pieces)
    {
        // Only the group containing the new piece can have grown.
        Span<int> pending = stackalloc int[PieceCount];
        Span<int> neighbors = stackalloc int[4];
        pending[0] = start;
        int visited = 1 << start;
        int size = 1;
        for (int head = 0; head < size; head++)
        {
            int count = GetNeighbors(pending[head], neighbors);
            for (int i = 0; i < count; i++)
            {
                int neighbor = neighbors[i];
                int bit = 1 << neighbor;
                if ((pieces & bit) == 0 || (visited & bit) != 0) continue;
                visited |= bit;
                pending[size++] = neighbor;
            }
        }
        return size;
    }

    private static int GetNeighbors(int piece, Span<int> neighbors)
    {
        int count = 0;
        int column = piece % GridSize;
        if (column > 0) neighbors[count++] = piece - 1;
        if (column < GridSize - 1) neighbors[count++] = piece + 1;
        if (piece >= GridSize) neighbors[count++] = piece - GridSize;
        if (piece < PieceCount - GridSize) neighbors[count++] = piece + GridSize;
        return count;
    }

    private static bool IsValidPiece(int piece) => piece >= 0 && piece < PieceCount;
}
