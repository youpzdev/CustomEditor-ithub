using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GoofyTweenDemoFocus : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        var scroll = GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.viewport == null || scroll.content == null) return;
        Canvas.ForceUpdateCanvases();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
        var viewport = scroll.viewport.rect;
        float offset = 0;
        if (bounds.min.y < viewport.yMin) offset = viewport.yMin - bounds.min.y;
        else if (bounds.max.y > viewport.yMax) offset = viewport.yMax - bounds.max.y;
        if (Mathf.Approximately(offset, 0)) return;
        scroll.StopMovement();
        scroll.content.anchoredPosition += Vector2.up * offset;
    }
}
