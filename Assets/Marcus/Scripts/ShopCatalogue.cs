public enum ShopProduct
{
    EchoSlotII, EchoSlotIII, DualEchoReplay, MemoryStabiliser,
    FragmentHint, ReconstructionHint, EchoHint
}

// Permanent upgrades cost Memory Points; consumables cost Coins.
public static class ShopCatalogue
{
    public const int PuzzleReward = 20;
    public const int ItemLimit = 99;
    public const int ProductCount = 7;
    private static readonly int[] Prices = { 20, 40, 50, 10, 10, 15, 10 };
    public static bool IsValid(ShopProduct product) => (int)product >= 0 && (int)product < ProductCount;
    public static bool IsUpgrade(ShopProduct product) => IsValid(product) && (int)product < 3;
    public static int Price(ShopProduct product) => IsValid(product) ? Prices[(int)product] : 0;
    public static string CurrencyName(ShopProduct product) => IsUpgrade(product) ? "Memory Points" : "Coins";
    public static string PriceText(ShopProduct product) => Price(product) + (IsUpgrade(product) ? " MP" : " Coins");
}
