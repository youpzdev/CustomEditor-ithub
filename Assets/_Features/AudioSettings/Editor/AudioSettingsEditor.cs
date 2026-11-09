using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(AudioSettings))]
[CanEditMultipleObjects]
public class AudioSettingsEditor : Editor
{
    private enum Section { Sounds, Text, Hidden }

    private static readonly string[] SectionNames = { "Звуки", "Текст", "Скрыть" };
    private static readonly string[] ListNames = { "dangerous", "friendly", "neutral" };
    private SerializedProperty id;
    private SerializedProperty contentType;
    private SerializedProperty panelText;
    private ReorderableList[] lists;
    private Section section;

    private void OnEnable()
    {
        id = serializedObject.FindProperty("id");
        contentType = serializedObject.FindProperty("contentType");
        panelText = serializedObject.FindProperty("panelText");
        lists = new ReorderableList[ListNames.Length];
        for (int i = 0; i < lists.Length; i++)
            lists[i] = CreateList(serializedObject.FindProperty(ListNames[i]));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Настройки звуков", AudioEditorStyles.Title);
        EditorGUILayout.Space(AudioEditorStyles.FieldGap);
        EditorGUILayout.PropertyField(id, new GUIContent("ID", "Уникальный идентификатор ассета. Видим во всех разделах."));
        EditorGUILayout.Space(AudioEditorStyles.SectionGap);
        section = (Section)GUILayout.Toolbar((int)section, SectionNames, GUILayout.Height(AudioEditorStyles.ButtonHeight));
        EditorGUILayout.Space(AudioEditorStyles.SectionGap);

        switch (section)
        {
            case Section.Sounds:
                DrawSounds();
                break;
            case Section.Text:
                EditorGUILayout.LabelField("Текст панели", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(panelText, GUIContent.none);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSounds()
    {
        EditorGUILayout.PropertyField(contentType, new GUIContent("Тип аудиоконтента"));
        EditorGUILayout.Space(AudioEditorStyles.FieldGap);
        if (contentType.hasMultipleDifferentValues)
        {
            EditorGUILayout.HelpBox("У выбранных ассетов разные типы. Выбери общий тип, чтобы редактировать список вместе.", MessageType.Info);
            return;
        }

        int index = contentType.intValue;
        if (index < 0 || index >= lists.Length)
        {
            EditorGUILayout.HelpBox("Неизвестный тип аудиоконтента. Выбери тип из списка выше.", MessageType.Error);
            return;
        }

        lists[index].DoLayoutList();
    }

    private ReorderableList CreateList(SerializedProperty property)
    {
        var list = new ReorderableList(serializedObject, property, true, true, true, true)
        {
            elementHeight = AudioEditorStyles.EntryHeight
        };
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Звуки: {property.arraySize}", EditorStyles.boldLabel);
        list.drawNoneElementCallback = rect => EditorGUI.LabelField(rect, "Список пуст. Нажми +, чтобы добавить звук.");
        list.drawElementCallback = (rect, index, active, focused) => DrawEntry(rect, property.GetArrayElementAtIndex(index), index);
        list.onAddCallback = addedList =>
        {
            int index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            var entry = property.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("clip").objectReferenceValue = null;
            entry.FindPropertyRelative("volume").floatValue = 1f;
            addedList.index = index;
        };
        return list;
    }

    private static void DrawEntry(Rect rect, SerializedProperty entry, int index)
    {
        var clip = entry.FindPropertyRelative("clip");
        rect.y += AudioEditorStyles.RowPadding;
        rect.height = AudioEditorStyles.LineHeight;
        string title = clip.hasMultipleDifferentValues ? "Разные клипы" : clip.objectReferenceValue == null ? "Новый звук" : clip.objectReferenceValue.name;
        EditorGUI.LabelField(rect, $"{index + 1}. {title}", EditorStyles.boldLabel);

        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = AudioEditorStyles.LabelWidth;
        try
        {
            rect.y += AudioEditorStyles.LineHeight + AudioEditorStyles.FieldGap;
            EditorGUI.PropertyField(rect, clip, new GUIContent("Аудиоклип"));
            rect.y += AudioEditorStyles.LineHeight + AudioEditorStyles.FieldGap;
            EditorGUI.PropertyField(rect, entry.FindPropertyRelative("volume"), new GUIContent("Громкость"));
        }
        finally
        {
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
