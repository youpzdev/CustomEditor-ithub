using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class GoofyTweenDemoLayout : MonoBehaviour
{
    [SerializeField] private float twoColumnWidth = 860;
    [SerializeField] private float sectionHeight = 330;

    private GridLayoutGroup grid;
    private RectTransform rect;
    private float previousWidth = -1;

    private void OnEnable()
    {
        grid = GetComponent<GridLayoutGroup>();
        rect = GetComponent<RectTransform>();
        RefreshLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (grid != null && rect != null) RefreshLayout();
    }

    private void RefreshLayout()
    {
        float width = rect.rect.width;
        if (width <= 0 || Mathf.Abs(width - previousWidth) < 0.5f) return;
        previousWidth = width;
        int columns = width >= twoColumnWidth ? 2 : 1;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        float available = width - grid.padding.horizontal - grid.spacing.x * (columns - 1);
        grid.cellSize = new Vector2(available / columns, sectionHeight);
    }
}
