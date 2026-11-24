using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Keeps the existing scene/component identity and return callback.
public class MinigamePlaceholderController : MonoBehaviour
{
    [SerializeField] private string shopSceneName = "Marcus_TutorialEnvironment";
    [SerializeField] private Texture2D puzzlePhoto;
    [SerializeField] private Texture2D[] alternatePhotos;
    [SerializeField] private Sprite buttonArtwork;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite coinBalanceArtwork;
    private bool returning;
    private RectTransform window;
    private TMP_Text statusText;
    private TMP_Text balanceText;
    private Button replayButton;
    private Button clearFogButton;
    private TMP_Text clearFogLabel;
    private TMP_Text fogProgress;
    private TMP_Text joinGuide;
    private Button matchEdgesButton;
    private Button findRowButton;
    private TMP_Text bonusHint;
    private readonly RectTransform[] bonusHighlights = new RectTransform[PhotoPuzzleRound.GridSize];
    private Coroutine bonusEffect;
    private string bonusResult;
    private Texture2D fogTexture;
    private readonly PhotoPuzzlePiece[] pieces = new PhotoPuzzlePiece[PhotoPuzzleRound.PieceCount];
    private readonly RectTransform[] pieceArtwork = new RectTransform[PhotoPuzzleRound.PieceCount];
    private readonly RectTransform[] slots = new RectTransform[PhotoPuzzleRound.PieceCount];
    private RawImage restoredPhoto;
    private RectTransform photoShine;
    private RectTransform snappingArtwork;
    private Coroutine completionEffect;
    private Coroutine coinBounce;
    private CanvasGroup rewardCoin;
    private RectTransform rewardCoinRect;
    private RawImage rewardCoinImage;
    private GameEffectAtlas rewardCoinAtlas;
    private PhotoPuzzleRound round;
    private PhotoPuzzleLooseGroups looseGroups;
    private static int nextPhotoIndex;
    private int selectedPiece = -1;
    private long rewardToken;
    private static readonly Color Ink = new Color(.20f, .12f, .06f);
    private static readonly Color Jade = new Color(.15f, .28f, .22f);
    private static readonly Color Paper = new Color(.98f, .89f, .73f);
    private static readonly Vector2 LayoutSize = new Vector2(1840, 1000);
    private static readonly Vector2 BoardSize = new Vector2(960, 540);
    private const int GridSize = PhotoPuzzleRound.GridSize;
    private static readonly Vector2 SlotSize = BoardSize / GridSize;
    private static readonly Vector2 LoosePieceSize = SlotSize * .87f;
    private static readonly Vector2 CoinBalancePosition = new Vector2(682f, 456f);
    private const string JoinInstructions = "To join loose pieces, line up matching edges until they glow, then release.";
    private const string HintJoinInstructions = "Bring the gold-marked pieces close beside the grid. Release when both glow.";
    public RectTransform DragRoot => window;
    public bool Completed => round != null && round.Complete;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPhotoSequence() => nextPhotoIndex = 0;

    private void Awake()
    {
        if (window != null) return;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        var backdrop = MakeImage("Puzzle_Backdrop", transform, Vector2.zero, Vector2.zero, new Color(.08f,.06f,.035f));
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.rectTransform.sizeDelta = Vector2.zero;
        var frame = MakeImage("Photo_Puzzle_Window", transform, Vector2.zero, LayoutSize, Color.clear);
        window = frame.rectTransform;
        MakeImage("Photo_Frame", window, Vector2.zero, BoardSize + new Vector2(12,12), Jade);
        MakeImage("Blank_Canvas", window, Vector2.zero, BoardSize, new Color(.94f,.83f,.63f));
        BuildGrid();
        fogTexture = CreateFogTexture();
        looseGroups = new PhotoPuzzleLooseGroups(BoardSize, Rect.MinMaxRect(-915f, -418f, 915f, 418f), ShowJoinGuide);
        for (int i = 0; i < PhotoPuzzleRound.PieceCount; i++)
        {
            int index = i;
            int column = i % GridSize;
            int row = i / GridSize;
            Vector2 position = new Vector2((column + .5f) * SlotSize.x - BoardSize.x / 2f,
                BoardSize.y / 2f - (row + .5f) * SlotSize.y);
            Image area = MakeImage("Photo_Slot_"+i, window, position, SlotSize, Color.clear);
            area.raycastTarget = true;
            var drop = area.gameObject.AddComponent<PhotoPuzzleSlot>(); drop.Owner = this; drop.Index = i;
            var button = area.gameObject.AddComponent<Button>(); button.targetGraphic = area;
            button.onClick.AddListener(() => PlaceSelected(index));
            slots[i] = area.rectTransform;
            Image tile = MakeImage("Photo_Piece_"+i, window, Vector2.zero, new Vector2(160,90), new Color(.62f,.41f,.20f));
            tile.raycastTarget = true;
            const float uvCell = 1f / GridSize;
            var photo = MakePhoto("Photo", tile.transform, Vector2.zero, Vector2.zero,
                new Rect(column * uvCell, 1f - (row + 1) * uvCell, uvCell, uvCell));
            FitTileArtwork(photo.rectTransform);
            pieceArtwork[i] = photo.rectTransform;
            RawImage cover = MakePhoto("Piece_Fog", tile.transform, Vector2.zero, Vector2.zero, new Rect(0, 0, 1, 1));
            cover.texture = fogTexture;
            FitTileArtwork(cover.rectTransform);
            var tileButton = tile.gameObject.AddComponent<Button>(); tileButton.targetGraphic = tile;
            tileButton.transition = Selectable.Transition.None;
            tileButton.onClick.AddListener(() => SelectPiece(index));
            var piece = tile.gameObject.AddComponent<PhotoPuzzlePiece>();
            piece.Initialize(this, i, tile, tileButton, cover, looseGroups);
            pieces[i] = piece;
        }
        BuildCompletionArtwork();
        BuildRewardArtwork();
        BuildInstructions();
        statusText = Label("Status", "", new Vector2(0,-450), new Vector2(600,48), 26, true);
        statusText.color = Paper;
        clearFogButton = MakeButton("Clear_Fog", "Clear Fog", new Vector2(-471, -450), ClearFog, 233);
        clearFogLabel = clearFogButton.GetComponentInChildren<TMP_Text>();
        fogProgress = Label("Fog_Progress", "", new Vector2(-471, -485), new Vector2(252, 22), 18f);
        fogProgress.color = Paper;
        matchEdgesButton = MakeBonusButton("Match_Edges", "Match Edges", "Show a matching tile", -121, MatchEdges);
        findRowButton = MakeBonusButton("Find_Row", "Find the Row", "Show the correct row", 121, FindRow);
        bonusHint = Label("Bonus_Hint", "", new Vector2(0, -485), new Vector2(640, 22), 18f);
        bonusHint.color = Paper;
        replayButton = MakeButton("Play_Again", "Next Photo", new Vector2(-650,-450), StartRound, 220);
        MakeButton("Return_To_Shop", "Back", new Vector2(650,-450), ReturnToShop, 220);
        StartRound();
        Fit();
    }

    private void BuildInstructions()
    {
        const string guide = "Drag to a slot, or click a piece then a slot. Link 3 and 6 on the grid for Clear Fog, and 9 for a hint.";
        var instructions = Label("Puzzle_Instructions", guide, new Vector2(0, 469), new Vector2(1140, 30), 23f);
        instructions.color = Paper;
        joinGuide = Label("Puzzle_Join_Guide", JoinInstructions, new Vector2(0, 443), new Vector2(1140, 30), 23f);
        joinGuide.color = Paper;
    }

    private void ShowJoinGuide(string message)
    {
        if (joinGuide == null) return;
        bool hintActive = looseGroups.HasHint;
        joinGuide.text = message ?? (hintActive ? HintJoinInstructions : JoinInstructions);
        joinGuide.color = hintActive || message != null ? new Color(1f, .86f, .53f) : Paper;
    }

    private static void FitTileArtwork(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * 3f;
        rect.offsetMax = Vector2.one * -3f;
    }

    private static Texture2D CreateFogTexture()
    {
        const int width = 64, height = 36;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Photo puzzle fog", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[width * height];
        Color deep = new Color(.20f, .29f, .29f);
        Color mist = new Color(.69f, .77f, .73f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float broad = Mathf.PerlinNoise(3f + x * .035f, 6f + y * .105f);
                float detail = Mathf.PerlinNoise(5f + x * .078f, 2f + y * .19f);
                float haze = Mathf.Round(Mathf.Clamp01((broad * .78f + detail * .22f - .17f) * 1.52f) * 8f) / 8f;
                pixels[y * width + x] = Color.Lerp(deep, mist, haze);
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void BuildCompletionArtwork()
    {
        restoredPhoto = MakePhoto("Restored_Photo", window, Vector2.zero, BoardSize, new Rect(0, 0, 1, 1));
        restoredPhoto.gameObject.AddComponent<RectMask2D>();
        var shine = new GameObject("Photo_Shine", typeof(RectTransform));
        shine.layer = window.gameObject.layer;
        photoShine = shine.GetComponent<RectTransform>();
        photoShine.SetParent(restoredPhoto.rectTransform, false);
        Place(photoShine, Vector2.zero, new Vector2(144, BoardSize.y * 1.3f));
        photoShine.localRotation = Quaternion.Euler(0, 0, -14f);
        for (int strip = -4; strip <= 4; strip++)
        {
            float brightness = 1f - Mathf.Abs(strip) / 5f;
            var tint = new Color(1f, .92f, .71f, .23f * brightness * brightness);
            MakeImage("Shine_Band", photoShine, new Vector2(strip * 16f, 0),
                new Vector2(16f, photoShine.sizeDelta.y), tint);
        }
        restoredPhoto.gameObject.SetActive(false);
    }

    private void BuildRewardArtwork()
    {
        Image balance = MakeImage("Puzzle_Coin_Balance", window, CoinBalancePosition, new Vector2(168f, 74f),
            coinBalanceArtwork != null ? Color.white : new Color(.21f, .14f, .07f));
        balance.sprite = coinBalanceArtwork;
        balance.preserveAspect = true;
        balanceText = Label("Puzzle_Coin_Total", "", Vector2.zero, new Vector2(105f, 44f), 24f, true);
        balanceText.rectTransform.SetParent(balance.rectTransform, false);
        balanceText.rectTransform.anchoredPosition = coinBalanceArtwork != null ? new Vector2(17f, 0f) : Vector2.zero;
        balanceText.enableAutoSizing = true;
        balanceText.fontSizeMin = 16f;
        balanceText.fontSizeMax = 24f;
        balanceText.color = new Color(.98f, .90f, .68f);

        var coin = new GameObject("Puzzle_Coin_Reward", typeof(RectTransform), typeof(CanvasGroup));
        coin.layer = window.gameObject.layer;
        rewardCoinRect = (RectTransform)coin.transform;
        rewardCoinRect.SetParent(window, false);
        Place(rewardCoinRect, Vector2.zero, new Vector2(60f, 60f));
        rewardCoin = coin.GetComponent<CanvasGroup>();
        rewardCoin.interactable = false;
        rewardCoin.blocksRaycasts = false;
        rewardCoinAtlas = GameEffectAtlas.Load("PhotoRewardCoin");
        if (rewardCoinAtlas != null)
        {
            rewardCoinImage = coin.AddComponent<RawImage>();
            rewardCoinImage.raycastTarget = false;
        }
        else
        {
            MakeImage("Coin_Outline", coin.transform, Vector2.zero, new Vector2(39f, 27f), new Color(.41f, .19f, .06f));
            MakeImage("Coin_Outline", coin.transform, Vector2.zero, new Vector2(27f, 39f), new Color(.41f, .19f, .06f));
            MakeImage("Coin_Face", coin.transform, Vector2.zero, new Vector2(31f, 23f), new Color(.87f, .51f, .12f));
            MakeImage("Coin_Face", coin.transform, Vector2.up, new Vector2(23f, 31f), new Color(.99f, .73f, .27f));
            MakeImage("Coin_Glint", coin.transform, new Vector2(-4f, 5f), new Vector2(7f, 12f), new Color(1f, .91f, .58f));
        }
        coin.SetActive(false);
    }

    private void RefreshCoinBalance()
    {
        balanceText.text = coinBalanceArtwork != null ? TutorialRewardBridge.Coins.ToString()
            : "Coins " + TutorialRewardBridge.Coins;
    }

    private void BuildGrid()
    {
        var grid = new GameObject("Photo_Grid", typeof(RectTransform));
        grid.layer = 5;
        var rect = grid.GetComponent<RectTransform>();
        rect.SetParent(window, false);
        Place(rect, Vector2.zero, BoardSize);
        var colour = new Color(.28f, .24f, .15f, .25f);
        // Built before slots and pieces so restored photograph tiles cover the
        // guide. MakeImage keeps the lines decorative rather than input targets.
        for (int division = 1; division < GridSize; division++)
        {
            float x = -BoardSize.x * .5f + SlotSize.x * division;
            float y = -BoardSize.y * .5f + SlotSize.y * division;
            MakeImage("Grid_Column_" + division, rect, new Vector2(x, 0f), new Vector2(3f, BoardSize.y), colour);
            MakeImage("Grid_Row_" + division, rect, new Vector2(0f, y), new Vector2(BoardSize.x, 3f), colour);
        }
    }

    private void StartRound()
    {
        if (returning) return;
        ClearCompletionEffect();
        ClearBonusHighlight();
        bonusResult = null;
        looseGroups.Reset();
        ShowJoinGuide(null);
        AdvancePhoto();
        TutorialRewardBridge.CancelPuzzleRound(rewardToken);
        rewardToken = TutorialRewardBridge.BeginPuzzleRound();
        selectedPiece = -1;
        int[] order = new int[PhotoPuzzleRound.PieceCount];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        for (int i=order.Length-1; i>0; i--)
        { int j=Random.Range(0,i+1); int temp=order[i]; order[i]=order[j]; order[j]=temp; }
        round = new PhotoPuzzleRound(order, Random.Range(0, GridSize - 1), Random.Range(0, GridSize - 2));
        for (int i = 0; i < order.Length; i++)
            pieces[order[i]].ResetPiece(PerimeterPosition(i), LoosePieceSize, round.IsFogged(order[i]));
        replayButton.gameObject.SetActive(false);
        statusText.text = "";
        RefreshCoinBalance();
        RefreshPowers();
    }

    private void AdvancePhoto()
    {
        int alternateCount = alternatePhotos?.Length ?? 0;
        int index = nextPhotoIndex % (alternateCount + 1);
        nextPhotoIndex = (index + 1) % (alternateCount + 1);
        Texture2D photo = index == 0 ? puzzlePhoto : alternatePhotos[index - 1];
        if (photo == null) photo = puzzlePhoto;
        foreach (RectTransform artwork in pieceArtwork) artwork.GetComponent<RawImage>().texture = photo;
        restoredPhoto.texture = photo;
    }

    private static Vector2 PerimeterPosition(int index)
    {
        int side = index / GridSize;
        float centered = index % GridSize + .5f - GridSize / 2f;
        float direction = (side & 1) == 0 ? 1f : -1f;
        return side < 2 ? new Vector2(centered * SlotSize.x, direction * 352f)
            : new Vector2(direction * 804f, centered * SlotSize.y);
    }

    public void SelectPiece(int index)
    {
        if (returning || round == null || index < 0 || index >= pieces.Length || round.IsPlaced(index) || round.IsFogged(index)) return;
        selectedPiece = index;
        for (int i=0; i<pieces.Length; i++) pieces[i].SetSelected(i==index);
        RefreshBonusPower();
    }
    private void PlaceSelected(int slot)
    {
        if (selectedPiece >= 0) pieces[selectedPiece].TryPlaceAt(slot);
    }
    public bool TryPlacePiece(int piece, int slot)
    {
        if (returning || round == null || !round.TryPlace(piece, slot)) return false;
        SnapPiece(piece);
        FinishPlacement(piece);
        return true;
    }

    public bool TryPlaceGroup(int[] members, int anchorPiece, int anchorSlot)
    {
        if (returning || round == null || !round.TryPlaceGroup(members, anchorPiece, anchorSlot)) return false;
        foreach (int member in members) SnapPiece(member);
        FinishPlacement(anchorPiece);
        return true;
    }

    private void SnapPiece(int piece) => pieces[piece].SnapTo(slots[piece].anchoredPosition, SlotSize);

    private void FinishPlacement(int focusPiece)
    {
        ShowJoinGuide(null);
        AudioManager.Instance?.PlayPlacement();
        selectedPiece = -1;
        foreach (var tile in pieces) tile.SetSelected(false);
        RefreshPowers();
        if (round.Complete)
        {
            ClearBonusHighlight();
            int oldBalance = TutorialRewardBridge.Coins;
            bool paid = TutorialRewardBridge.CompletePuzzleRound(rewardToken);
            rewardToken = 0;
            int credited = paid ? TutorialRewardBridge.Coins - oldBalance : 0;
            statusText.text = credited > 0 ? "+" + credited + " Coins" : "Complete";
            RefreshCoinBalance();
            replayButton.gameObject.SetActive(true);
            if (isActiveAndEnabled)
            {
                completionEffect = StartCoroutine(PlayCompletionEffect(pieceArtwork[focusPiece]));
                if (credited > 0) coinBounce = StartCoroutine(PlayCoinBounce());
            }
        }
    }

    public void ClearFog()
    {
        if (returning || round == null || !round.TryClearFog(out int first, out int second)) return;
        pieces[first].RevealFog();
        pieces[second].RevealFog();
        AudioManager.Instance?.PlayInteraction();
        RefreshPowers();
    }

    private void RefreshPowers()
    {
        bool hasFog = round.FoggedCount > 0;
        clearFogButton.interactable = round.CanClearFog;
        clearFogButton.gameObject.SetActive(hasFog);
        fogProgress.gameObject.SetActive(hasFog);
        if (hasFog)
        {
            clearFogLabel.text = round.CanClearFog ? "Clear Fog  ×" + round.ClearFogCharges : "Clear Fog";
            int linked = round.LargestConnectedGroup;
            int target = (linked / PhotoPuzzleRound.LinksPerCharge + 1) * PhotoPuzzleRound.LinksPerCharge;
            fogProgress.text = round.CanClearFog ? "Reveal 2 pieces" : linked + "/" + target + " linked for Clear Fog";
        }
        RefreshBonusPower();
    }

    private void RefreshBonusPower()
    {
        bool available = round.BonusAvailable;
        bool hasSelection = selectedPiece >= 0;
        matchEdgesButton.gameObject.SetActive(available);
        findRowButton.gameObject.SetActive(available);
        matchEdgesButton.interactable = hasSelection;
        findRowButton.interactable = hasSelection;
        bonusHint.gameObject.SetActive(!round.Complete);
        bonusHint.rectTransform.anchoredPosition = new Vector2(0, available ? -485 : -450);
        if (available)
            bonusHint.text = hasSelection ? "Choose one hint for this piece." : "Select a clear, loose piece to use a hint.";
        else if (round.LargestConnectedGroup < PhotoPuzzleRound.BonusLinkTarget)
            bonusHint.text = round.LargestConnectedGroup + "/" + PhotoPuzzleRound.BonusLinkTarget + " linked for a hint";
        else
            bonusHint.text = bonusResult ?? "Hint used";
    }

    private void MatchEdges()
    {
        if (returning || looseGroups.IsDragging || !round.BonusAvailable || selectedPiece < 0) return;
        int[] linkedPieces = looseGroups.GetLinkedIndices(pieces[selectedPiece]);
        if (!round.TryMatchNeighbor(selectedPiece, out int neighbor, linkedPieces))
        {
            bonusHint.text = "Select another piece or reveal more pieces with Clear Fog.";
            return;
        }
        RectTransform tile = (RectTransform)pieces[neighbor].transform;
        ShowBonusHighlight(0, tile, Vector2.zero, tile.rect.size);
        if (round.IsPlaced(neighbor))
        {
            PlayBonusFeedback("Use the highlighted piece on the grid as a guide.");
            return;
        }
        looseGroups.SetHint(selectedPiece, neighbor);
        PlayBonusFeedback("The gold marks show which edges join.");
    }

    private void FindRow()
    {
        if (returning || looseGroups.IsDragging || !round.TryFindRow(selectedPiece, out int row)) return;
        for (int column = 0; column < GridSize; column++)
        {
            RectTransform slot = slots[row * GridSize + column];
            ShowBonusHighlight(column, window, slot.anchoredPosition, SlotSize);
        }
        PlayBonusFeedback("The selected piece belongs in the highlighted row.");
    }

    private void ShowBonusHighlight(int index, RectTransform parent, Vector2 position, Vector2 size)
    {
        if (bonusHighlights[index] == null)
        {
            var overlay = new GameObject("Bonus_Highlight_" + index, typeof(RectTransform), typeof(CanvasGroup));
            overlay.layer = window.gameObject.layer;
            bonusHighlights[index] = (RectTransform)overlay.transform;
            var group = overlay.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var fill = MakeImage("Glow", overlay.transform, Vector2.zero, Vector2.zero, new Color(1f, .77f, .28f, .21f));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            AddBonusEdge(overlay.transform, new Vector2(0f, 0f), Vector2.right, Vector2.zero, Vector2.up * 4f);
            AddBonusEdge(overlay.transform, Vector2.up, Vector2.one, Vector2.down * 4f, Vector2.zero);
            AddBonusEdge(overlay.transform, Vector2.zero, Vector2.up, Vector2.zero, Vector2.right * 4f);
            AddBonusEdge(overlay.transform, Vector2.right, Vector2.one, Vector2.left * 4f, Vector2.zero);
        }
        RectTransform highlight = bonusHighlights[index];
        highlight.SetParent(parent, false);
        highlight.SetAsLastSibling();
        Place(highlight, position, size - Vector2.one * 9f);
        highlight.gameObject.SetActive(true);
    }

    private void AddBonusEdge(Transform parent, Vector2 minimum, Vector2 maximum, Vector2 insetMin, Vector2 insetMax)
    {
        var edge = MakeImage("Edge", parent, Vector2.zero, Vector2.zero, new Color(1f, .84f, .40f));
        edge.rectTransform.anchorMin = minimum;
        edge.rectTransform.anchorMax = maximum;
        edge.rectTransform.offsetMin = insetMin;
        edge.rectTransform.offsetMax = insetMax;
    }

    private void PlayBonusFeedback(string message)
    {
        bonusResult = message;
        AudioManager.Instance?.PlayInteraction();
        RefreshBonusPower();
        bonusEffect = StartCoroutine(FadeBonusHighlight());
    }

    private IEnumerator FadeBonusHighlight()
    {
        const float duration = 2.4f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float alpha = Mathf.Clamp01((duration - elapsed) / .5f);
            foreach (RectTransform highlight in bonusHighlights)
                if (highlight != null && highlight.gameObject.activeSelf)
                    highlight.GetComponent<CanvasGroup>().alpha = alpha;
            yield return null;
        }
        bonusEffect = null;
        ClearBonusHighlight();
        bonusResult = null;
        RefreshBonusPower();
    }

    private void ClearBonusHighlight()
    {
        if (bonusEffect != null) StopCoroutine(bonusEffect);
        bonusEffect = null;
        foreach (RectTransform highlight in bonusHighlights)
            if (highlight != null) highlight.gameObject.SetActive(false);
    }

    private IEnumerator PlayCompletionEffect(RectTransform artwork)
    {
        // The piece and drop slot are already locked. Animate only its photograph.
        snappingArtwork = artwork;
        const float snapDuration = .18f;
        for (float elapsed = 0f; elapsed < snapDuration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / snapDuration;
            float scale = progress < .6f ? Mathf.Lerp(.91f, 1.018f, progress / .6f)
                : Mathf.Lerp(1.018f, 1f, (progress - .6f) / .4f);
            artwork.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        artwork.localScale = Vector3.one;
        snappingArtwork = null;

        // Show the finished photograph with a single sweep clipped to its edges.
        restoredPhoto.transform.SetAsLastSibling();
        restoredPhoto.gameObject.SetActive(true);
        photoShine.gameObject.SetActive(true);
        const float shineDuration = .85f;
        float travel = BoardSize.x * .5f + 180f;
        for (float elapsed = 0f; elapsed < shineDuration; elapsed += Time.unscaledDeltaTime)
        {
            photoShine.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, elapsed / shineDuration), 0f);
            yield return null;
        }
        photoShine.gameObject.SetActive(false);
        completionEffect = null;
    }

    private void ClearCompletionEffect()
    {
        if (completionEffect != null) StopCoroutine(completionEffect);
        completionEffect = null;
        if (coinBounce != null) StopCoroutine(coinBounce);
        coinBounce = null;
        if (rewardCoin != null) rewardCoin.gameObject.SetActive(false);
        if (snappingArtwork != null) snappingArtwork.localScale = Vector3.one;
        snappingArtwork = null;
        if (restoredPhoto != null) restoredPhoto.gameObject.SetActive(false);
    }

    private IEnumerator PlayCoinBounce()
    {
        rewardCoinRect.SetAsLastSibling();
        rewardCoin.gameObject.SetActive(true);
        const float duration = .46f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float lift = Mathf.Sin(progress * Mathf.PI);
            rewardCoinRect.anchoredPosition = CoinBalancePosition + new Vector2(Mathf.Lerp(-132f, -100f, progress), 36f * lift);
            rewardCoinRect.localScale = new Vector3(Mathf.Lerp(.72f, 1.08f, lift), Mathf.Lerp(.72f, 1.08f, lift), 1f);
            rewardCoin.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(progress * 8f, (1f - progress) * 4f));
            rewardCoinAtlas?.Sample(rewardCoinImage, progress);
            yield return null;
        }
        rewardCoin.gameObject.SetActive(false);
        coinBounce = null;
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ReturnToShop();
    }
    private void LateUpdate() => Fit();
    private void Fit()
    {
        if (window == null) return;
        Vector2 available = ((RectTransform)transform).rect.size;
        window.localScale = Vector3.one*Mathf.Max(.1f,Mathf.Min(1f,(available.x-80)/LayoutSize.x,(available.y-80)/LayoutSize.y));
    }
    private void OnDisable()
    {
        ClearCompletionEffect();
        ClearBonusHighlight();
        TutorialRewardBridge.CancelPuzzleRound(rewardToken);
    }
    private void OnDestroy()
    {
        if (fogTexture != null) Destroy(fogTexture);
    }
    public void ReturnToShop()
    {
        if (returning) return;
        if (!Application.CanStreamedLevelBeLoaded(shopSceneName))
        { statusText.text = "The shop scene is missing from the build."; return; }
        returning = true;
        TutorialRewardBridge.CancelPuzzleRound(rewardToken);
        SceneManager.LoadScene(shopSceneName);
    }

    private Image MakeImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        go.layer = 5; go.transform.SetParent(parent,false);
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
        Place(image.rectTransform,position,size); return image;
    }
    private RawImage MakePhoto(string name, Transform parent, Vector2 position, Vector2 size, Rect uv)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        go.layer = 5; go.transform.SetParent(parent,false);
        var image = go.GetComponent<RawImage>(); image.texture = puzzlePhoto; image.uvRect = uv; image.raycastTarget = false;
        Place(image.rectTransform,position,size); return image;
    }
    private TMP_Text Label(string name, string value, Vector2 position, Vector2 size, float fontSize, bool bold=false)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        go.layer = 5; go.transform.SetParent(window,false);
        var text = go.GetComponent<TMP_Text>(); text.font = font; text.text = value; text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; text.color = Ink;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        Place(text.rectTransform,position,size); return text;
    }
    private Button MakeButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction action, float width)
    {
        Image image = MakeImage(name,window,position,new Vector2(width,50),Color.white);
        image.sprite = buttonArtwork; image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var label = Label(name+"_Label",text,Vector2.zero,new Vector2(width-24,44),23,true);
        label.transform.SetParent(image.transform,false);
        return button;
    }
    private Button MakeBonusButton(string name, string title, string effect, float x, UnityEngine.Events.UnityAction action)
    {
        var button = MakeButton(name, title, new Vector2(x, -450), action, 218);
        var heading = button.GetComponentInChildren<TMP_Text>();
        heading.fontSize = 20f;
        heading.rectTransform.anchoredPosition = Vector2.up * 8f;
        heading.rectTransform.sizeDelta = new Vector2(188f, 24f);
        var description = Label(name + "_Effect", effect, Vector2.zero, new Vector2(184f, 18f), 13f);
        description.transform.SetParent(button.transform, false);
        description.rectTransform.anchoredPosition = Vector2.down * 11f;
        return button;
    }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
