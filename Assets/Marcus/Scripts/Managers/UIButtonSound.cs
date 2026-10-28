using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UIButtonSound : MonoBehaviour
{
    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Play);
    }
    private void Play() => AudioManager.Instance?.PlayButton();
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Play); }
}
