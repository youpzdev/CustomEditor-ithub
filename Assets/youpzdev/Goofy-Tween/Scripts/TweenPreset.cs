using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(menuName = "Goofy/Tween Preset", fileName = "TweenPreset")]
public class TweenPreset : ScriptableObject
{
    [Header("Timing")]
    [SerializeField] [Min(0)] private float duration = 0.25f;
    [SerializeField] [Min(0)] private float delay;
    [SerializeField] private Ease ease = Ease.OutCubic;
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private int loops = 1;
    [SerializeField] private LoopType loopType = LoopType.Restart;

    [Space(5)]
    [Header("Effect")]
    [SerializeField] private TweenEffect effect;
    [SerializeField] private Vector3 vector = Vector3.one;
    [SerializeField] private float amount = 1;
    [SerializeField] private bool relative = true;
    [SerializeField] private bool localSpace = true;
    [SerializeField] private Color color = UnityEngine.Color.white;
    [SerializeField] private Vector3[] points = System.Array.Empty<Vector3>();

    [Space(5)]
    [Header("Shape")]
    [SerializeField] [Min(1)] private int vibrato = 10;
    [SerializeField] [Range(0, 1)] private float elasticity = 1;
    [SerializeField] [Range(0, 180)] private float randomness = 90;
    [SerializeField] private bool fadeOut = true;
    [SerializeField] [Min(1)] private int jumps = 1;
    [SerializeField] private PathType pathType = PathType.CatmullRom;
    [SerializeField] private PathMode pathMode = PathMode.Full3D;

    public float Duration {get => duration; set => duration = value;}
    public float Delay {get => delay; set => delay = value;}
    public Ease Ease {get => ease; set => ease = value;}
    public bool UnscaledTime {get => unscaledTime; set => unscaledTime = value;}
    public int Loops {get => loops; set => loops = value;}
    public LoopType LoopType {get => loopType; set => loopType = value;}
    public TweenEffect Effect {get => effect; set => effect = value;}
    public Vector3 Vector {get => vector; set => vector = value;}
    public float Amount {get => amount; set => amount = value;}
    public bool Relative {get => relative; set => relative = value;}
    public bool LocalSpace {get => localSpace; set => localSpace = value;}
    public Color Color {get => color; set => color = value;}
    public Vector3[] Points {get => points; set => points = value;}
    public int Vibrato {get => vibrato; set => vibrato = value;}
    public float Elasticity {get => elasticity; set => elasticity = value;}
    public float Randomness {get => randomness; set => randomness = value;}
    public bool FadeOut {get => fadeOut; set => fadeOut = value;}
    public int Jumps {get => jumps; set => jumps = value;}
    public PathType PathType {get => pathType; set => pathType = value;}
    public PathMode PathMode {get => pathMode; set => pathMode = value;}
}
