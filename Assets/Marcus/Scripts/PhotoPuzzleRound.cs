// Correct pieces lock in place; mistakes never remove progress.
public sealed class PhotoPuzzleRound
{
    public const int PieceCount = 9;
    private readonly bool[] placed = new bool[PieceCount];
    public int PlacedCount { get; private set; }
    public bool Complete => PlacedCount == PieceCount;
    public bool IsPlaced(int piece) => piece >= 0 && piece < PieceCount && placed[piece];
    public bool TryPlace(int piece, int slot)
    {
        if (Complete || piece < 0 || piece >= PieceCount || piece != slot || placed[piece]) return false;
        placed[piece] = true;
        PlacedCount++;
        return true;
    }
}
