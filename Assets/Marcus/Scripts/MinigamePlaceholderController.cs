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
    [SerializeField] private Sprite buttonArtwork;
    [SerializeField] private TMP_FontAsset font;
    private bool returning;
    private RectTransform window;
    private TMP_Text statusText;
    private Button replayButton;
    private readonly PhotoPuzzlePiece[] pieces = new PhotoPuzzlePiece[PhotoPuzzleRound.PieceCount];
    private readonly RectTransform[] slots = new RectTransform[PhotoPuzzleRound.PieceCount];
    private PhotoPuzzleRound round;
    private int selectedPiece = -1;
    private long rewardToken;
    private static readonly Color Ink = new Color(.20f, .12f, .06f);
    private static readonly Color Jade = new Color(.15f, .28f, .22f);
    private static readonly Vector2 LayoutSize = new Vector2(1640, 1000);
    private static readonly Vector2 BoardSize = new Vector2(960, 540);
    private static readonly Vector2 SlotSize = BoardSize / 3f;
    private static readonly Vector2 LoosePieceSize = new Vector2(220, 123.75f);
    private static readonly Vector2[] LoosePositions =
    {
        new Vector2(-320, 360), new Vector2(0, 360), new Vector2(320, 360),
        new Vector2(-650, 140), new Vector2(-650, -140),
        new Vector2(650, 140), new Vector2(650, -140),
        new Vector2(-240, -360), new Vector2(240, -360)
    };
    public RectTransform DragRoot => window;
    public bool Completed => round != null && round.Complete;

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
        for (int i = 0; i < PhotoPuzzleRound.PieceCount; i++)
        {
            int index = i;
            Vector2 position = new Vector2((i%3-1)*SlotSize.x,(1-i/3)*SlotSize.y);
            Image area = MakeImage("Photo_Slot_"+i, window, position, SlotSize, Color.clear);
            area.raycastTarget = true;
            var drop = area.gameObject.AddComponent<PhotoPuzzleSlot>(); drop.Owner = this; drop.Index = i;
            var button = area.gameObject.AddComponent<Button>(); button.targetGraphic = area;
            button.onClick.AddListener(() => PlaceSelected(index));
            slots[i] = area.rectTransform;
            Image tile = MakeImage("Photo_Piece_"+i, window, Vector2.zero, new Vector2(160,90), new Color(.62f,.41f,.20f));
            tile.raycastTarget = true;
            var photo = MakePhoto("Photo", tile.transform, Vector2.zero, new Vector2(154,84),
                new Rect((i%3)/3f,(2-i/3)/3f,1f/3f,1f/3f));
            photo.rectTransform.anchorMin = Vector2.zero; photo.rectTransform.anchorMax = Vector2.one;
            photo.rectTransform.sizeDelta = new Vector2(-6,-6);
            var piece = tile.gameObject.AddComponent<PhotoPuzzlePiece>();
            piece.Initialize(this,i,tile);
            var tileButton = tile.gameObject.AddComponent<Button>(); tileButton.targetGraphic = tile;
            tileButton.transition = Selectable.Transition.None;
            tileButton.onClick.AddListener(() => SelectPiece(index));
            pieces[i] = piece;
        }
        statusText = Label("Status", "", new Vector2(0,-450), new Vector2(600,48), 26, true);
        statusText.color = new Color(.94f,.83f,.63f);
        replayButton = MakeButton("Play_Again", "Play Again", new Vector2(-650,-450), StartRound);
        MakeButton("Return_To_Shop", "Back", new Vector2(650,-450), ReturnToShop);
        StartRound();
        Fit();
    }

    private void StartRound()
    {
        if (returning) return;
        TutorialRewardBridge.CancelPuzzleRound(rewardToken);
        rewardToken = TutorialRewardBridge.BeginPuzzleRound();
        round = new PhotoPuzzleRound(); selectedPiece = -1;
        int[] order = {0,1,2,3,4,5,6,7,8};
        for (int i=order.Length-1; i>0; i--)
        { int j=Random.Range(0,i+1); int temp=order[i]; order[i]=order[j]; order[j]=temp; }
        for (int i=0; i<order.Length; i++)
            pieces[order[i]].ResetPiece(LoosePositions[i], LoosePieceSize);
        replayButton.gameObject.SetActive(false);
        statusText.text = "";
    }

    public void SelectPiece(int index)
    {
        if (returning || round == null || round.IsPlaced(index) || index < 0 || index >= pieces.Length) return;
        selectedPiece = index;
        for (int i=0; i<pieces.Length; i++) pieces[i].SetSelected(i==index);
    }
    private void PlaceSelected(int slot)
    {
        if (selectedPiece >= 0) TryPlacePiece(selectedPiece,slot);
    }
    public bool TryPlacePiece(int piece, int slot)
    {
        if (returning || round == null || round.Complete) return false;
        if (!round.TryPlace(piece,slot)) return false;
        pieces[piece].SnapTo(slots[slot].anchoredPosition,SlotSize);
        selectedPiece = -1;
        foreach (var tile in pieces) tile.SetSelected(false);
        if (round.Complete)
        {
            bool paid = TutorialRewardBridge.CompletePuzzleRound(rewardToken);
            rewardToken = 0;
            statusText.text = paid ? "+"+ShopCatalogue.PuzzleReward+" Coins" : "Complete";
            replayButton.gameObject.SetActive(true);
        }
        return true;
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
    private void OnDisable() => TutorialRewardBridge.CancelPuzzleRound(rewardToken);
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
    private Button MakeButton(string name, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        Image image = MakeImage(name,window,position,new Vector2(290,62),Color.white);
        image.sprite = buttonArtwork; image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var label = Label(name+"_Label",text,Vector2.zero,new Vector2(266,48),26,true);
        label.transform.SetParent(image.transform,false);
        return button;
    }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
