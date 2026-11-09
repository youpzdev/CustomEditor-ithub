using UnityEditor;
using UnityEngine;

internal static class AudioEditorStyles
{
    public const float FieldGap = 4f;
    public const float SectionGap = 12f;
    public const float RowPadding = 6f;
    public const float LabelWidth = 90f;
    public const int TitleSize = 16;

    public static float LineHeight => EditorGUIUtility.singleLineHeight;
    public static float ButtonHeight => LineHeight + RowPadding;
    public static float EntryHeight => LineHeight * 3f + FieldGap * 2f + RowPadding * 2f;

    public static GUIStyle Title => title ??= new GUIStyle(EditorStyles.boldLabel)
    {
        fontSize = TitleSize,
        wordWrap = true
    };

    private static GUIStyle title;
}
