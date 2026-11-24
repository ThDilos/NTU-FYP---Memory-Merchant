using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialVisualStateController : MonoBehaviour
{
    [SerializeField] private TutorialMemoryState memoryState;
    [Header("Cabinet")]
    public GameObject openCabinetWithRecorder;
    public GameObject recorderFragment;

    [Header("Fragments")]
    public GameObject motherFragment;
    public GameObject studyFragment;

    [Header("Echoes")]
    public GameObject lockEcho;

    [Header("Study")]
    public GameObject studyFog;
    public bool StudyRestored { get; private set; }
    private TutorialMemoryEffects.StudyPeel studyPeel;
    private Coroutine studyPeelRoutine;

    void Start()
    {
        ResetVisuals();
    }

    void Update()
    {
        // Editor reset aid; key 1 is reserved for using the stored Key Echo.
        if (Keyboard.current != null &&
            (Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame))
            ResetVisuals();
    }
    public void OpenCabinet()
    {

        // Show the open cabinet + recorder artwork
        openCabinetWithRecorder.SetActive(true);

        // Show the recorder fragment
        recorderFragment.SetActive(true);

        // Hide the lock echo
        lockEcho.SetActive(false);
    }

    public void CollectMotherFragment()
    {
        if (memoryState != null) memoryState.CollectFragment(0);
        motherFragment.SetActive(false);
    }

    public void CollectRecorderFragment()
    {
        if (recorderFragment == null || !recorderFragment.activeSelf) return;
        if (memoryState != null) memoryState.CollectFragment(1);
        recorderFragment.SetActive(false);
    }

    public void CollectStudyFragment()
    {
        if (memoryState != null) memoryState.CollectFragment(2);
        studyFragment.SetActive(false);
    }

    public void RestoreStudy()
    {
        if (StudyRestored) return;
        StudyRestored = true;
        if (studyFog == null) return;
        // The Manual suspends this component's input handling, while the GameObject remains active.
        if (gameObject.activeInHierarchy)
            studyPeel = TutorialMemoryEffects.StudyPeel.Create(transform, studyFog.GetComponent<SpriteRenderer>());
        // Unlock the actual Study immediately. Only the collider-free haze waits for the player to see it.
        studyFog.SetActive(false);
        if (studyPeel != null) studyPeelRoutine = StartCoroutine(PlayStudyPeel());
    }

    public void ResetVisuals()
    {
        StopStudyPeel();
        StudyRestored = false;
        if (memoryState != null) memoryState.ResetProgress();
        // Cabinet starts closed
        openCabinetWithRecorder.SetActive(false);

        // Recorder fragment only appears after cabinet opens
        recorderFragment.SetActive(false);

        // These fragments start visible
        motherFragment.SetActive(true);
        studyFragment.SetActive(true);

        // Lock Echo starts visible
        lockEcho.SetActive(true);

        // Study starts covered by fog
        studyFog.SetActive(true);
    }

    private IEnumerator PlayStudyPeel()
    {
        Camera camera = Camera.main;
        // A correct reconstruction usually happens with the Manual paused and open.
        while (Time.deltaTime <= 0f || !studyPeel.DoorwayVisible(camera)) yield return null;
        for (float time = 0f; time < TutorialMemoryEffects.StudyPeel.Duration;)
        {
            yield return null;
            if (Time.deltaTime <= 0f) continue;
            time += Time.deltaTime;
            studyPeel.Draw(time);
        }
        studyPeel.Remove();
        studyPeel = null;
        studyPeelRoutine = null;
    }

    private void StopStudyPeel()
    {
        if (studyPeelRoutine != null) StopCoroutine(studyPeelRoutine);
        studyPeelRoutine = null;
        studyPeel?.Remove();
        studyPeel = null;
    }

    private void OnDisable()
    {
        if (!gameObject.activeInHierarchy) StopStudyPeel();
    }

    private void OnDestroy() => StopStudyPeel();
}
