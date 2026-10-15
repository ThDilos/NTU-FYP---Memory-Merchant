using UnityEngine;

// Visual layout only; each scene keeps its existing Settings controller and callbacks.
public sealed class SettingsPanelLayout : MonoBehaviour
{
    [SerializeField] private RectTransform window;
    [Min(0f)] [SerializeField] private float viewportPadding = 48f;
    private Vector2 previousSize = new Vector2(-1f, -1f);
    private void OnEnable() { previousSize = new Vector2(-1f, -1f); Fit(); }
    private void LateUpdate() => Fit();
    private void Fit()
    {
        RectTransform overlay = transform as RectTransform;
        if (window == null || overlay == null) return;
        Vector2 available = overlay.rect.size;
        if (available.x <= 0f || available.y <= 0f || available == previousSize) return;
        previousSize = available;
        float scale = Mathf.Min(1f, (available.x - viewportPadding * 2f) / window.sizeDelta.x,
            (available.y - viewportPadding * 2f) / window.sizeDelta.y);
        window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }
}
