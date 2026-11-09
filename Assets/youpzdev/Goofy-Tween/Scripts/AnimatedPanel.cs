using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class AnimatedPanel : TweenPanel
{
    public enum PanelAnimationType
    {
        FadeOnly, FadeScale,
        SlideFromBottom, SlideFromTop, SlideFromLeft, SlideFromRight,
        PopBounce, ElasticFromBottom, ElasticFromTop,
        FlipHorizontal, FlipVertical,
        RotateFadeIn, ZoomOut, ShakeAppear, SwingIn
    }

    [Header("Legacy transition")]
    [SerializeField] private PanelAnimationType _animationType = PanelAnimationType.FadeScale;
    [SerializeField] private float _duration = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioSource soundSource;
    [SerializeField] private AudioClip showSfx;
    [SerializeField] private AudioMixerGroup mixer;

    [Header("Navigation")]
    [SerializeField] private UnityEvent onBackPressed = new();

    private static readonly List<AnimatedPanel> panelStack = new();
    private static int lastBackFrame = -1;

    private bool handlingBack;

    public PanelAnimationType AnimationType {get => _animationType; set => _animationType = value;}
    public float Duration {get => _duration; set => _duration = value;}
    public AudioSource SoundSource {get => soundSource; set => soundSource = value;}
    public AudioClip ShowSfx {get => showSfx; set => showSfx = value;}
    public AudioMixerGroup Mixer {get => mixer; set => mixer = value;}
    public UnityEvent OnBackPressed => onBackPressed;
    public static int CountOpen {get {PruneStack(); return panelStack.Count;}}
    public static AnimatedPanel Topmost {get {PruneStack(); return panelStack.Count == 0 ? null : panelStack[^1];}}

    public override void Show()
    {
        if (!ValidStyle()) return;
        if (!enabled) {Debug.LogWarning($"Панель {name}: компонент выключен", this); return;}
        if (!ValidTransition(true)) return;
        bool inactive = !gameObject.activeSelf;
        if (inactive) gameObject.SetActive(true);
        if (IsShown && !IsTransitioning && !inactive) {PushToStack(); return;}
        TransitionTo(true, inactive);
    }

    public override void Hide()
    {
        if (!ValidStyle()) return;
        base.Hide();
    }

    public bool Back()
    {
        if (Topmost != this || handlingBack) return false;
        handlingBack = true;
        try
        {
            onBackPressed.Invoke();
            if (IsShown) Hide();
        }
        finally {handlingBack = false;}

        return true;
    }

    public static bool BackTopmost()
    {
        var panel = Topmost;

        return panel != null && panel.Back();
    }

    private void Update()
    {
        if (Topmost != this || lastBackFrame == Time.frameCount || !BackPressed()) return;
        lastBackFrame = Time.frameCount;
        Back();
    }

    protected override void OnDisable()
    {
        RemoveFromStack();
        base.OnDisable();
        if (!isActiveAndEnabled) IsShown = false;
    }

    private void OnDestroy()
    {
        RemoveFromStack();
    }

    protected override float DurationFor(TweenPreset preset) {return preset == null ? _duration : preset.Duration;}

    protected override Ease EaseFor(TweenPreset preset, bool shown)
    {
        if (preset != null) return preset.Ease;
        if (!shown) return Ease.InCubic;
        if (_animationType == PanelAnimationType.PopBounce || _animationType == PanelAnimationType.ElasticFromBottom || _animationType == PanelAnimationType.ElasticFromTop) return Ease.OutElastic;
        if (_animationType == PanelAnimationType.SwingIn) return Ease.OutBounce;
        if (_animationType == PanelAnimationType.FadeScale || _animationType == PanelAnimationType.FlipHorizontal || _animationType == PanelAnimationType.FlipVertical) return Ease.OutBack;

        return Ease.OutCubic;
    }

    protected override void ConfigureHidden(TweenPreset preset, bool showing, ref Vector3 scale, ref Vector2 position, ref Vector3 rotation)
    {
        Vector3 scaleFactor = preset != null && preset.Effect == TweenEffect.Scale ? preset.Vector : Vector3.one * 0.8f;
        Vector2 offset = Vector2.zero;
        switch (_animationType)
        {
            case PanelAnimationType.FadeScale: scale = (preset != null && preset.Effect == TweenEffect.Scale && !preset.Relative ? preset.Vector : Vector3.Scale(InitialScale, scaleFactor)); break;
            case PanelAnimationType.SlideFromBottom:
            case PanelAnimationType.ElasticFromBottom: offset = new Vector2(0, -Screen.height); break;
            case PanelAnimationType.SlideFromTop:
            case PanelAnimationType.ElasticFromTop: offset = new Vector2(0, Screen.height); break;
            case PanelAnimationType.SlideFromLeft: offset = new Vector2(-Screen.width, 0); break;
            case PanelAnimationType.SlideFromRight: offset = new Vector2(Screen.width, 0); break;
            case PanelAnimationType.PopBounce: scale = ScaleTarget(preset, 0.5f); break;
            case PanelAnimationType.FlipHorizontal: rotation = RotationTarget(preset, new Vector3(0, 90, 0)); break;
            case PanelAnimationType.FlipVertical: rotation = RotationTarget(preset, new Vector3(90, 0, 0)); break;
            case PanelAnimationType.RotateFadeIn:
                scale = ScaleTarget(preset, 0.85f);
                rotation = RotationTarget(preset, new Vector3(0, 0, showing ? -15 : 15));
                break;
            case PanelAnimationType.ZoomOut: scale = ScaleTarget(preset, 1.3f); break;
            case PanelAnimationType.ShakeAppear: if (showing) scale = InitialScale * 0.9f; break;
            case PanelAnimationType.SwingIn:
                rotation = RotationTarget(preset, new Vector3(0, 0, 25));
                offset = new Vector2(0, Screen.height * 0.3f);
                break;
        }
        if (offset != Vector2.zero) position = preset != null && preset.Effect == TweenEffect.Move ? (preset.Relative ? InitialPosition + (Vector2)preset.Vector : (Vector2)preset.Vector) : InitialPosition + offset;
    }

    private Vector3 ScaleTarget(TweenPreset preset, float factor)
    {
        if (preset != null && preset.Effect == TweenEffect.Scale) return preset.Relative ? Vector3.Scale(InitialScale, preset.Vector) : preset.Vector;

        return InitialScale * factor;
    }

    private Vector3 RotationTarget(TweenPreset preset, Vector3 offset)
    {
        if (preset != null && preset.Effect == TweenEffect.Rotate) return preset.Relative ? InitialRotation + preset.Vector : preset.Vector;

        return InitialRotation + offset;
    }

    private bool ValidStyle()
    {
        if (System.Enum.IsDefined(typeof(PanelAnimationType), _animationType)) return true;
        Debug.LogError($"Панель {name}: неизвестный AnimationType={_animationType}", this);

        return false;
    }

    protected override void AddMotion(Sequence sequence, bool shown, TweenPreset preset, float duration)
    {
        if (!shown || _animationType != PanelAnimationType.ShakeAppear) return;
        Vector3 strength = preset != null && preset.Effect == TweenEffect.ShakePosition ? preset.Vector : new Vector3(18, 18, 0);
        int vibrato = preset == null ? 10 : preset.Vibrato;
        sequence.Append(Content.DOShakeAnchorPos(duration * 0.6f, strength, vibrato, 45));
    }

    protected override bool ValidPreset(TweenPreset preset)
    {
        try {GoofyTween.ValidateTiming(preset);}
        catch (System.ArgumentException exception) {Debug.LogError($"Панель {name}: preset {preset.name}, {exception.Message}", this); return false;}
        if (!System.Enum.IsDefined(typeof(PanelAnimationType), _animationType) || !System.Enum.IsDefined(typeof(TweenEffect), preset.Effect) || float.IsNaN(preset.Vector.sqrMagnitude) || float.IsInfinity(preset.Vector.sqrMagnitude) || preset.Vibrato < 1)
        {Debug.LogError($"Панель {name}: AnimationType={_animationType}, Vector={preset.Vector}, Vibrato={preset.Vibrato}", this); return false;}

        return true;
    }

    protected override void OnTransitionStarted(bool shown)
    {
        if (!shown) {RemoveFromStack(); return;}
        PushToStack();
        if (showSfx == null) return;
        if (soundSource == null || !soundSource.isActiveAndEnabled) {Debug.LogWarning($"Панель {name}: showSfx задан, активный SoundSource не задан", this); return;}
        soundSource.outputAudioMixerGroup = mixer;
        soundSource.PlayOneShot(showSfx);
    }

    protected override void OnTransitionCompleted(bool shown)
    {
        if (!shown && !IsShown) gameObject.SetActive(false);
    }

    private void PushToStack()
    {
        RemoveFromStack();
        panelStack.Add(this);
    }

    private void RemoveFromStack() {panelStack.Remove(this);}

    private static void PruneStack()
    {
        for (int i = panelStack.Count - 1; i >= 0; i--)
            if (panelStack[i] == null || !panelStack[i].isActiveAndEnabled || !panelStack[i].IsShown) panelStack.RemoveAt(i);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStack()
    {
        panelStack.Clear();
        lastBackFrame = -1;
    }

    private static bool BackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
