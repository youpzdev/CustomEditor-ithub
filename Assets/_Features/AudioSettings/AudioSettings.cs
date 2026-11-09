using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettings", menuName = "Audio/Settings")]
public class AudioSettings : ScriptableObject
{
    [SerializeField, Tooltip("Уникальный идентификатор этих настроек звука.")]
    private string id;
    [SerializeField] private AudioContentType contentType;
    [SerializeField] private List<AudioEntry> dangerous = new List<AudioEntry>();
    [SerializeField] private List<AudioEntry> friendly = new List<AudioEntry>();
    [SerializeField] private List<AudioEntry> neutral = new List<AudioEntry>();
    [SerializeField, TextArea(6, 15)] private string panelText;

    public string Id => id;
    public AudioContentType ContentType => contentType;
    public string PanelText => panelText;
    public IReadOnlyList<AudioEntry> Clips => contentType switch
    {
        AudioContentType.Dangerous => dangerous,
        AudioContentType.Friendly => friendly,
        AudioContentType.Neutral => neutral,
        _ => throw new System.ArgumentOutOfRangeException(nameof(contentType), contentType, "Unknown audio content type.")
    };
}
