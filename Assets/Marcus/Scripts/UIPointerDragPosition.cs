using UnityEngine;
using UnityEngine.EventSystems;

// Keep the exact point originally pressed under the cursor, including the movement
// before Unity crosses its drag threshold. Works with world-space and scaled UI.
internal sealed class UIPointerDragPosition
{
    private RectTransform rect;
    private Vector2 grabbedLocalPoint;
    private Vector2 screenPosition;
    private Camera pressCamera;

    public bool Begin(RectTransform target, PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(target,
            data.pressPosition, data.pressEventCamera, out grabbedLocalPoint)) return false;
        rect = target;
        // InputSystemUIInputModule reuses the event for right/middle buttons after
        // OnDrag. Never retain it: its press raycast (and camera) can be cleared
        // before LateUpdate, making world-space UI jump to overlay coordinates.
        pressCamera = data.pressEventCamera;
        screenPosition = data.position;
        return true;
    }

    public void Move(PointerEventData data)
    {
        screenPosition = data.position;
        Refresh();
    }

    public void Refresh()
    {
        if (rect == null) return;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rect,
            screenPosition, pressCamera, out Vector3 point))
            rect.position = point - rect.TransformVector(grabbedLocalPoint);
    }

    public void Clear() { rect = null; pressCamera = null; }
}
