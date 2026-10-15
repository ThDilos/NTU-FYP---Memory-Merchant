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
        StudyRestored = true;
        studyFog.SetActive(false);
    }

    public void ResetVisuals()
    {
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
}
