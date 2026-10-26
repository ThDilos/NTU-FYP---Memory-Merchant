
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueBehaviour : MonoBehaviour
{
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private LineAdvancer lineAdvancer;

    [Header("Buttons")]
    [SerializeField] private Button speedButton;
    [SerializeField] private Button autoButton;

    [Header("Typewriter Speed")]
    [SerializeField] private int normalSpeed = 60;
    [SerializeField] private int fastSpeed = 120;

    [Header("Button Color")]
    [SerializeField] private Color normalColor = Color.beige;
    [SerializeField] private Color selectedColor = Color.orange;

    private bool isAutoAdvance;
    private bool isFastSpeed;

    private void Start()
    {
        if (dialogueRunner == null || linePresenter == null || lineAdvancer == null)
        {
            Debug.LogError("Please assign DialogueRunner, LinePresenter and LineAdvancer");
            return;
        }

        isFastSpeed = false;
        isAutoAdvance = linePresenter.autoAdvance;
        // Set initial values.
        linePresenter.lettersPerSecond = normalSpeed;

        UpdateButtonColors();
        SetButtonsVisible(false);

        dialogueRunner.onDialogueStart.AddListener(
            OnDialogueStarted
        );

        dialogueRunner.onDialogueComplete.AddListener(
            OnDialogueEnded
        );
        speedButton.onClick.AddListener(ToggleSpeed);
        autoButton.onClick.AddListener(ToggleAutoAdvance);
    }

    private void ToggleAutoAdvance()
    {
        isAutoAdvance = !isAutoAdvance;
        linePresenter.autoAdvance = isAutoAdvance;

         if (isAutoAdvance)
        {
            // Finish typing the current line if it is still typing.
            lineAdvancer.RequestNextLine();
        }

        UpdateButtonColors();

        Debug.Log("Auto advance: " + isAutoAdvance);
    }

    private void ToggleSpeed()
    {
        isFastSpeed = !isFastSpeed;

        linePresenter.lettersPerSecond =
            isFastSpeed ? fastSpeed : normalSpeed;

        UpdateButtonColors();

        Debug.Log(
            $"Typewriter speed: {linePresenter.lettersPerSecond} letters/sec"
        );
    }

    private void UpdateButtonColors()
    {
        SetButtonColor(speedButton, isFastSpeed);
        SetButtonColor(autoButton, isAutoAdvance);
    }

    private void SetButtonColor(Button button, bool selected)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;

        if (image != null)
        {
            image.color = selected ? selectedColor : normalColor;
        }
    }

    private void OnDialogueStarted()
    {
        SetButtonsVisible(true);
    }

    private void OnDialogueEnded()
    {
        SetButtonsVisible(false);
    }

    private void SetButtonsVisible(bool visible)
    {
        if (speedButton != null)
            speedButton.gameObject.SetActive(visible);

        if (autoButton != null)
            autoButton.gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (speedButton != null)
            speedButton.onClick.RemoveListener(ToggleSpeed);

        if (autoButton != null)
            autoButton.onClick.RemoveListener(ToggleAutoAdvance);

        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(
                OnDialogueStarted
            );

            dialogueRunner.onDialogueComplete.RemoveListener(
                OnDialogueEnded
            );
        }
    }
}
