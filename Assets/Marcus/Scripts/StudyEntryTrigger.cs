using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class StudyEntryTrigger : MonoBehaviour
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TutorialMemoryCompletionController completion;
    [Min(0f)] [SerializeField] private float dialogueDelay = 1f;
    [Tooltip("Left edge of the restored Study, in world coordinates.")]
    [SerializeField] private float studyBoundaryX = 1.18f;
    private bool fired;
    private Coroutine pendingDialogue;
    private void OnTriggerEnter2D(Collider2D other) => TryEnter(other);
    private void OnTriggerStay2D(Collider2D other) => TryEnter(other);
    private void TryEnter(Collider2D other)
    {
        if (fired || pendingDialogue != null || state == null || !state.StudyRestored || completion == null) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        pendingDialogue = StartCoroutine(BeginAfterEntry(player));
    }
    private IEnumerator BeginAfterEntry(PlayerController player)
    {
        // Keep movement available during the entry delay; the completion controller locks it when Yarn starts.
        float elapsed = 0f;
        while (elapsed < dialogueDelay)
        {
            if (!StillInStudy(player)) { pendingDialogue = null; yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
        while (StillInStudy(player))
        {
            if (completion.TryBeginMotherMessage())
            {
                fired = true; pendingDialogue = null; yield break;
            }
            yield return null;
        }
        pendingDialogue = null;
    }
    private bool StillInStudy(PlayerController player) => player != null && state.StudyRestored &&
        player.transform.position.x >= studyBoundaryX;
    private void OnDisable()
    {
        if (pendingDialogue != null) StopCoroutine(pendingDialogue);
        pendingDialogue = null;
    }
}
