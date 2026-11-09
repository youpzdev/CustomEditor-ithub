using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TweenPlayer))]
public class TweenPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var player = (TweenPlayer)target;
                if (GUILayout.Button("Play")) player.Play();
                if (GUILayout.Button("Stop")) player.Stop(TweenStopMode.KeepCurrent);
                if (GUILayout.Button("Restore")) player.Stop(TweenStopMode.RestoreInitial);
            }
        }
        if (!EditorApplication.isPlaying) EditorGUILayout.HelpBox("Preview controls are available in Play Mode", MessageType.Info);
    }
}
