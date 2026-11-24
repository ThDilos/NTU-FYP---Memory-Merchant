using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

// Keep the HUD anchored to the screen and teach movement once per tutorial run.
public sealed class MemoryWorldHUDGuidance : MonoBehaviour
{
    [SerializeField] private RectTransform settingsButton;
    [SerializeField] private RectTransform manualButton;
    [SerializeField] private RectTransform objectivePanel;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private PlayerController player;
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private DialogueRunner dialogueRunner;

    private static bool movementLearned;
    private GameObject movementHint;
    private Vector3 previousPosition;
    private float travelled;
    private float readyAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => movementLearned = false;

    private void Start()
    {
        Anchor(settingsButton, new Vector2(64f, -48f));
        Anchor(manualButton, new Vector2(234f, -48f));
        Anchor(objectivePanel, new Vector2(64f, -222f));
        if (!TutorialRewardBridge.MemoryWorldIntroSeen && !TutorialRewardBridge.TutorialCompleted &&
            !TutorialRewardBridge.IsArchiveReplay) movementLearned = false;
        if (TutorialRewardBridge.TutorialCompleted || TutorialRewardBridge.IsArchiveReplay) movementLearned = true;
        previousPosition = player != null ? player.transform.position : Vector3.zero;
        readyAt = Time.unscaledTime + .5f;
        BuildHint();
    }

    private static void Anchor(RectTransform rect, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
    }

    private void BuildHint()
    {
        Image border = MakeImage("Memory_Movement_Hint", transform, new Color(.65f, .45f, .23f, .96f));
        movementHint = border.gameObject;
        RectTransform rect = border.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
        rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 36f);
        rect.sizeDelta = new Vector2(500f, 104f);
        Image paper = MakeImage("Face", rect, new Color(.11f, .10f, .07f, .97f));
        paper.rectTransform.anchorMin = Vector2.zero;
        paper.rectTransform.anchorMax = Vector2.one;
        paper.rectTransform.offsetMin = new Vector2(3f, 3f);
        paper.rectTransform.offsetMax = new Vector2(-3f, -3f);
        Label("Movement_Keys", rect, "WASD / Arrow Keys", 27f, FontStyles.Bold, 19f);
        Label("Movement_Instruction", rect, "Move to explore", 23f, FontStyles.Normal, -20f);
        movementHint.SetActive(false);
    }

    private static Image MakeImage(string name, Transform parent, Color colour)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    private void Label(string name, RectTransform parent, string text, float size, FontStyles style, float y)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        TMP_Text label = item.GetComponent<TMP_Text>();
        if (objectiveText != null) label.font = objectiveText.font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, .92f, .73f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.anchoredPosition = new Vector2(0f, y);
        label.rectTransform.sizeDelta = new Vector2(470f, 42f);
    }

    private void LateUpdate()
    {
        if (movementHint == null || player == null) return;
        bool ready = Time.timeScale > 0f && Time.unscaledTime >= readyAt && player.isActiveAndEnabled &&
            (dialogueRunner == null || !dialogueRunner.IsDialogueRunning) &&
            (tutorialUI == null || (!tutorialUI.ExternalInputBlocked &&
            (tutorialUI.manualPanel == null || !tutorialUI.manualPanel.activeSelf) &&
            (tutorialUI.settingsPanel == null || !tutorialUI.settingsPanel.activeSelf)));
        float distance = Vector2.Distance(player.transform.position, previousPosition);
        // A key pressed into a wall is not evidence that the player has moved.
        // Ignore large teleports used by scene/cutscene positioning.
        if (!movementLearned && ready && player.MovementInput.sqrMagnitude > .01f && distance < .5f)
        {
            travelled += distance;
            if (travelled >= .12f) movementLearned = true;
        }
        previousPosition = player.transform.position;
        movementHint.SetActive(ready && !movementLearned);
    }
}
