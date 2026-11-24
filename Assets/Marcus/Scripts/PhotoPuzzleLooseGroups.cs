using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class PhotoPuzzleLooseGroups
{
    private const float SnapDistance = 55f;
    private const float HintJoinGap = 42f;
    private const float OverlapTolerance = .25f;

    private readonly Rect board;
    private readonly Rect table;
    private readonly Rect[] workAreas;
    private readonly Action<string> showJoinGuide;
    private readonly PhotoPuzzlePiece[] pieces = new PhotoPuzzlePiece[PhotoPuzzleRound.PieceCount];
    private readonly List<PhotoPuzzlePiece>[] groups = new List<PhotoPuzzlePiece>[PhotoPuzzleRound.PieceCount];
    private PhotoPuzzlePiece anchor;
    private PhotoPuzzlePiece[] dragged;
    private PhotoPuzzlePiece matchingPiece;
    private PhotoPuzzlePiece matchingNeighbor;
    private PhotoPuzzlePiece hintedPiece;
    private PhotoPuzzlePiece hintedNeighbor;
    private Vector2 targetPosition;
    private Vector2 neighborShift;
    private bool canJoin;

    public PhotoPuzzleLooseGroups(Vector2 boardSize, Rect tableBounds, Action<string> showJoinGuide)
    {
        board = new Rect(-boardSize / 2f, boardSize);
        table = tableBounds;
        this.showJoinGuide = showJoinGuide;
        workAreas = new[]
        {
            Rect.MinMaxRect(table.xMin, table.yMin, board.xMin, table.yMax),
            Rect.MinMaxRect(board.xMax, table.yMin, table.xMax, table.yMax),
            Rect.MinMaxRect(table.xMin, board.yMax, table.xMax, table.yMax),
            Rect.MinMaxRect(table.xMin, table.yMin, table.xMax, board.yMin)
        };
    }

    public bool IsDragging => anchor != null;
    public bool HasHint => hintedPiece != null;

    public void Register(PhotoPuzzlePiece piece)
    {
        pieces[piece.Index] = piece;
        AssignGroup(new List<PhotoPuzzlePiece> { piece });
    }

    public void Reset()
    {
        CancelDrag(anchor);
        ClearHint();
        foreach (var piece in pieces) Register(piece);
    }

    public void SetHint(int piece, int neighbor)
    {
        hintedPiece = pieces[piece];
        hintedNeighbor = pieces[neighbor];
        Vector2Int edge = PhotoOffset(hintedPiece, hintedNeighbor);
        hintedPiece.SetHintEdge(edge);
        hintedNeighbor.SetHintEdge(-edge);
        showJoinGuide(null);
    }

    private void ClearHint()
    {
        if (!HasHint) return;
        hintedPiece.SetHintEdge(Vector2Int.zero);
        hintedNeighbor.SetHintEdge(Vector2Int.zero);
        hintedPiece = null;
        hintedNeighbor = null;
        showJoinGuide(null);
    }

    public int CountFor(PhotoPuzzlePiece piece) => groups[piece.Index].Count;

    public int[] GetLinkedIndices(PhotoPuzzlePiece piece)
    {
        var group = groups[piece.Index];
        var indices = new int[group.Count];
        for (int i = 0; i < group.Count; i++) indices[i] = group[i].Index;
        return indices;
    }

    public bool CanSubmit(PhotoPuzzlePiece piece) => anchor == null || anchor == piece;

    public bool BeginDrag(PhotoPuzzlePiece piece)
    {
        if (anchor != null) return false;
        showJoinGuide(null);
        anchor = piece;
        dragged = groups[piece.Index].ToArray();
        foreach (var member in dragged) member.SetDragPresentation(true);
        return true;
    }

    public void MoveDrag(PhotoPuzzlePiece piece, Vector2 position, Vector2 pointer)
    {
        if (anchor != piece) return;
        MovePieces(position - piece.TablePosition);
        bool overBoard = board.Contains(pointer);
        if (overBoard && IsOwnSlot(piece, pointer))
            ClearMatch();
        else
            UpdateMatch(overBoard);
    }

    public void EndDrag(PhotoPuzzlePiece piece, Vector2 pointer)
    {
        if (anchor != piece) return;

        foreach (var member in dragged)
        {
            if (!member.IsPlaced) continue;
            CancelDrag(piece);
            return;
        }

        if (canJoin)
        {
            var target = groups[matchingNeighbor.Index];
            MovePieces(targetPosition - matchingPiece.TablePosition);
            foreach (var member in target) member.TablePosition += neighborShift;
            target.AddRange(dragged);
            AssignGroup(target);
            foreach (var member in target) member.RememberTablePosition();
            if (HasHint && groups[hintedPiece.Index] == groups[hintedNeighbor.Index]) ClearHint();
            FinishDrag();
            showJoinGuide("Pieces joined. Drag the group onto the grid together.");
            AudioManager.Instance?.PlayPlacement();
            return;
        }

        bool needsSpace = matchingNeighbor != null;
        if (!board.Contains(pointer) && FitsOnTable(Vector2.zero))
        {
            foreach (var member in dragged) member.RememberTablePosition();
            FinishDrag();
        }
        else
            CancelDrag(piece);
        if (needsSpace) showJoinGuide("This group needs more space. Move one piece to an open area first.");
    }

    public void CancelDrag(PhotoPuzzlePiece piece)
    {
        if (anchor == null || anchor != piece) return;
        foreach (var member in dragged)
            if (member != null && !member.IsPlaced) member.ReturnToTablePosition();
        FinishDrag();
    }

    public void Detach(PhotoPuzzlePiece piece)
    {
        if (piece == hintedPiece || piece == hintedNeighbor) ClearHint();
        var previous = groups[piece.Index];
        if (previous.Count == 1) return;

        previous.Remove(piece);
        AssignGroup(new List<PhotoPuzzlePiece> { piece });

        // Removing a piece can leave two disconnected clusters on the table.
        while (previous.Count > 0)
        {
            var connected = new List<PhotoPuzzlePiece> { previous[0] };
            previous.RemoveAt(0);
            for (int i = 0; i < connected.Count; i++)
            {
                for (int j = previous.Count - 1; j >= 0; j--)
                {
                    if (!AreNeighbors(connected[i], previous[j])) continue;
                    connected.Add(previous[j]);
                    previous.RemoveAt(j);
                }
            }
            AssignGroup(connected);
        }
    }

    private bool IsOwnSlot(PhotoPuzzlePiece piece, Vector2 pointer)
    {
        int side = PhotoPuzzleRound.GridSize;
        int column = Mathf.FloorToInt((pointer.x - board.xMin) * side / board.width);
        int row = Mathf.Min(side - 1, Mathf.FloorToInt((board.yMax - pointer.y) * side / board.height));
        return piece.Index == row * side + column;
    }

    private void UpdateMatch(bool pointerOverBoard)
    {
        if (!pointerOverBoard && TryGetNearbyHint(out var hintedMoving, out var hintedTarget))
        {
            PreviewJoin(hintedMoving, hintedTarget);
            return;
        }

        PhotoPuzzlePiece newPiece = null;
        PhotoPuzzlePiece newNeighbor = null;
        float bestDistance = SnapDistance * SnapDistance;

        foreach (var moving in dragged)
        {
            Rect bounds = PieceBounds(moving, Vector2.zero);
            if (pointerOverBoard && bounds.xMin >= board.xMin && bounds.xMax <= board.xMax &&
                bounds.yMin >= board.yMin && bounds.yMax <= board.yMax) continue;
            foreach (var other in pieces)
            {
                if (!other.CanArrange || IsInDrag(other) || !AreNeighbors(moving, other)) continue;

                float distance = (AlignedPosition(moving, other) - moving.TablePosition).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                newPiece = moving;
                newNeighbor = other;
            }
        }

        PreviewJoin(newPiece, newNeighbor);
    }

    private bool TryGetNearbyHint(out PhotoPuzzlePiece moving, out PhotoPuzzlePiece target)
    {
        moving = null;
        target = null;
        if (!HasHint) return false;

        bool movingFirst = IsInDrag(hintedPiece);
        if (movingFirst == IsInDrag(hintedNeighbor)) return false;
        moving = movingFirst ? hintedPiece : hintedNeighbor;
        target = movingFirst ? hintedNeighbor : hintedPiece;
        Vector2 distance = moving.TablePosition - target.TablePosition;
        Vector2 reach = (moving.TileSize + target.TileSize) / 2f + Vector2.one * HintJoinGap;
        return Mathf.Abs(distance.x) <= reach.x && Mathf.Abs(distance.y) <= reach.y;
    }

    private static Vector2 AlignedPosition(PhotoPuzzlePiece moving, PhotoPuzzlePiece neighbor) =>
        neighbor.TablePosition - Vector2.Scale(PhotoOffset(moving, neighbor), moving.TileSize);

    private void PreviewJoin(PhotoPuzzlePiece moving, PhotoPuzzlePiece neighbor)
    {
        // The two groups' aligned shape stays fixed until the candidate changes.
        if (matchingPiece == moving && matchingNeighbor == neighbor) return;
        ClearMatch();
        if (neighbor == null) return;
        matchingPiece = moving;
        matchingNeighbor = neighbor;
        Vector2 alignment = AlignedPosition(moving, neighbor);
        Vector2 adjustment = alignment - moving.TablePosition;
        neighborShift = Vector2.zero;
        canJoin = FitsOnTable(adjustment) || FindSpaceTogether(adjustment, groups[neighbor.Index], out neighborShift);
        targetPosition = alignment + neighborShift;
        if (canJoin)
        {
            matchingPiece.SetMatchPreview(true);
            matchingNeighbor.SetMatchPreview(true);
            showJoinGuide("Matching edges. Release to join.");
        }
        else
            showJoinGuide("Matching edges. The group needs more room here.");
    }

    private bool FindSpaceTogether(Vector2 alignment, List<PhotoPuzzlePiece> neighborGroup, out Vector2 shift)
    {
        // Try a nearby parking spot when the correct join would cross the grid or
        // another tile. The shape stays fixed and both groups move together.
        Rect footprint = PieceBounds(dragged[0], alignment);
        foreach (var tile in dragged) footprint = Include(footprint, PieceBounds(tile, alignment));
        foreach (var tile in neighborGroup) footprint = Include(footprint, PieceBounds(tile, Vector2.zero));

        shift = Vector2.zero;
        float bestDistance = float.PositiveInfinity;
        foreach (Rect area in workAreas)
        {
            Vector2 freeSpace = area.size - footprint.size;
            if (freeSpace.x < 0 || freeSpace.y < 0) continue;
            Vector2 nearest = new Vector2(Mathf.Clamp(footprint.xMin, area.xMin, area.xMin + freeSpace.x),
                Mathf.Clamp(footprint.yMin, area.yMin, area.yMin + freeSpace.y));
            if (CheckParkingSpot(nearest, footprint, neighborGroup, ref shift, ref bestDistance)) continue;

            int columns = Mathf.Max(1, Mathf.CeilToInt(freeSpace.x / 24f));
            int rows = Mathf.Max(1, Mathf.CeilToInt(freeSpace.y / 24f));
            for (int row = 0; row <= rows; row++)
                for (int column = 0; column <= columns; column++)
                {
                    Vector2 spot = area.min + new Vector2(freeSpace.x * column / columns, freeSpace.y * row / rows);
                    CheckParkingSpot(spot, footprint, neighborGroup, ref shift, ref bestDistance);
                }
        }
        return !float.IsPositiveInfinity(bestDistance);
    }

    private bool CheckParkingSpot(Vector2 spot, Rect footprint, List<PhotoPuzzlePiece> neighborGroup,
        ref Vector2 bestShift, ref float bestDistance)
    {
        Vector2 shift = spot - footprint.min;
        float distance = shift.sqrMagnitude;
        if (distance >= bestDistance) return false;
        Vector2 margin = Vector2.one * OverlapTolerance;
        Rect occupied = new Rect(spot + margin, footprint.size - margin * 2f);
        foreach (var piece in pieces)
            if (!piece.IsPlaced && !IsInDrag(piece) && !neighborGroup.Contains(piece) &&
                occupied.Overlaps(PieceBounds(piece, Vector2.zero))) return false;
        bestShift = shift;
        bestDistance = distance;
        return true;
    }

    private static Rect Include(Rect first, Rect second)
    {
        return Rect.MinMaxRect(Mathf.Min(first.xMin, second.xMin), Mathf.Min(first.yMin, second.yMin),
            Mathf.Max(first.xMax, second.xMax), Mathf.Max(first.yMax, second.yMax));
    }

    private void ClearMatch()
    {
        if (matchingNeighbor == null) return;
        matchingPiece.SetMatchPreview(false);
        matchingNeighbor.SetMatchPreview(false);
        matchingPiece = null;
        matchingNeighbor = null;
        canJoin = false;
        showJoinGuide(null);
    }

    private bool FitsOnTable(Vector2 translation)
    {
        foreach (var moving in dragged)
        {
            Rect bounds = PieceBounds(moving, translation);
            if (bounds.xMin < table.xMin || bounds.xMax > table.xMax ||
                bounds.yMin < table.yMin || bounds.yMax > table.yMax || bounds.Overlaps(board)) return false;

            // Small tolerance allows pieces to touch without treating float rounding as overlap.
            bounds = Rect.MinMaxRect(bounds.xMin + OverlapTolerance, bounds.yMin + OverlapTolerance,
                bounds.xMax - OverlapTolerance, bounds.yMax - OverlapTolerance);
            foreach (var other in pieces)
                if (!other.IsPlaced && !IsInDrag(other) && bounds.Overlaps(PieceBounds(other, Vector2.zero))) return false;
        }

        return true;
    }

    private static Rect PieceBounds(PhotoPuzzlePiece piece, Vector2 translation) =>
        new Rect(piece.TablePosition + translation - piece.TileSize / 2f, piece.TileSize);

    private static Vector2Int PhotoOffset(PhotoPuzzlePiece from, PhotoPuzzlePiece to)
    {
        int side = PhotoPuzzleRound.GridSize;
        return new Vector2Int(to.Index % side - from.Index % side, from.Index / side - to.Index / side);
    }

    private static bool AreNeighbors(PhotoPuzzlePiece first, PhotoPuzzlePiece second)
    {
        Vector2Int offset = PhotoOffset(first, second);
        return Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 1;
    }

    private bool IsInDrag(PhotoPuzzlePiece piece) => System.Array.IndexOf(dragged, piece) >= 0;

    private void AssignGroup(List<PhotoPuzzlePiece> group)
    {
        foreach (var piece in group)
        {
            groups[piece.Index] = group;
            piece.SetLinked(group.Count > 1);
        }
    }

    private void MovePieces(Vector2 delta)
    {
        foreach (var member in dragged)
            if (!member.IsPlaced) member.TablePosition += delta;
    }

    private void FinishDrag()
    {
        ClearMatch();
        foreach (var member in dragged)
            if (member != null) member.SetDragPresentation(false);
        anchor = null;
        dragged = null;
    }
}
