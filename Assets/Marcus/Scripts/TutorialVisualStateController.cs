using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialVisualStateController : MonoBehaviour
{
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

    void Start()
    {
        ResetVisuals();
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // KEY 1 - Collect Mother Fragment
        if (Keyboard.current.digit1Key.wasPressedThisFrame ||
            Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            Debug.Log("KEY 1: Mother fragment collected");
            CollectMotherFragment();
        }

        // KEY 2 - Open Cabinet
        if (Keyboard.current.digit2Key.wasPressedThisFrame ||
            Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            Debug.Log("KEY 2: Cabinet opened");
            OpenCabinet();
        }

        // KEY 3 - Collect Recorder Fragment
        if (Keyboard.current.digit3Key.wasPressedThisFrame ||
            Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            Debug.Log("KEY 3: Recorder fragment collected");
            CollectRecorderFragment();
        }

        // KEY 4 - Collect Study Fragment
        if (Keyboard.current.digit4Key.wasPressedThisFrame ||
            Keyboard.current.numpad4Key.wasPressedThisFrame)
        {
            Debug.Log("KEY 4: Study fragment collected");
            CollectStudyFragment();
        }

        // KEY 5 - Restore Study
        if (Keyboard.current.digit5Key.wasPressedThisFrame ||
            Keyboard.current.numpad5Key.wasPressedThisFrame)
        {
            Debug.Log("KEY 5: Study restored");
            RestoreStudy();
        }

        // KEY 6 - Reset Everything
        if (Keyboard.current.digit6Key.wasPressedThisFrame ||
            Keyboard.current.numpad6Key.wasPressedThisFrame)
        {
            Debug.Log("KEY 6: Reset visuals");
            ResetVisuals();
        }
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
        motherFragment.SetActive(false);
    }

    public void CollectRecorderFragment()
    {
        recorderFragment.SetActive(false);
    }

    public void CollectStudyFragment()
    {
        studyFragment.SetActive(false);
    }

    public void RestoreStudy()
    {
        studyFog.SetActive(false);
    }

    public void ResetVisuals()
    {
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