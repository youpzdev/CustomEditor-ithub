using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class TweenPanel : MonoBehaviour
{
    [Header("Transitions")]
    [SerializeField] private TweenPreset showPreset;
    [SerializeField] private TweenPreset hidePreset;
    [SerializeField] private bool reducedMotion;

    [Space(5)]
    [Header("Events")]
    [SerializeField] private UnityEvent onShown = new();
    [SerializeField] private UnityEvent onHidden = new();

    [SerializeField] [FormerlySerializedAs("_content")] private RectTransform rect;
    [SerializeField] [FormerlySerializedAs("_canvasGroup")] private CanvasGroup group;
    private Vector3 initialScale;
    private Vector2 initialPosition;
    private Vector3 initialRotation;
    private float initialAlpha;
    private bool initialInteractable;
    private bool initialBlocksRaycasts;
    private bool initialized;

    private Sequence transition;
    private int revision;

    public TweenPreset ShowPreset {get => showPreset; set => showPreset = value;}
    public TweenPreset HidePreset {get => hidePreset; set => hidePreset = value;}
    public bool ReducedMotion
    {
        get => reducedMotion;
        set
        {
            reducedMotion = value;
            if (value && IsTransitioning) transition.Complete();
        }
    }
    public bool IsShown {get; protected set;}
    public bool IsTransitioning => transition != null && transition.IsActive();
    public Tween CurrentTween => transition;
    public UnityEvent OnShown => onShown;
    public UnityEvent OnHidden => onHidden;

    protected virtual void Awake()
    {
        CaptureInitial();
    }

    protected virtual void OnDisable()
    {
        if (!initialized) return;
        int request = StopTransition();
        if (request != revision) return;
        rect.localScale = initialScale;
        rect.anchoredPosition = initialPosition;
        rect.localEulerAngles = initialRotation;
        group.alpha = initialAlpha;
        group.interactable = initialInteractable;
        group.blocksRaycasts = initialBlocksRaycasts;
        IsShown = initialAlpha > 0;
    }

    public virtual void Show() {TransitionTo(true);}
    public virtual void Hide() {TransitionTo(false);}

    public RectTransform Content {get => rect; set {int request = StopTransition(); if (request != revision) return; initialized = false; rect = value;}}
    public CanvasGroup Group {get => group; set {int request = StopTransition(); if (request != revision) return; initialized = false; group = value;}}
    protected Vector3 InitialScale => initialScale;
    protected Vector2 InitialPosition => initialPosition;
    protected Vector3 InitialRotation => initialRotation;

    protected virtual TweenPreset ResolvePreset(bool shown) {return shown ? showPreset : hidePreset;}
    protected virtual float DurationFor(TweenPreset preset) {return preset == null ? 0 : preset.Duration;}
    protected virtual Ease EaseFor(TweenPreset preset, bool shown) {return preset == null ? Ease.OutCubic : preset.Ease;}
    protected virtual void OnTransitionStarted(bool shown) { }
    protected virtual void OnTransitionCompleted(bool shown) { }
    protected virtual void AddMotion(Sequence sequence, bool shown, TweenPreset preset, float duration) { }

    protected virtual void ConfigureHidden(TweenPreset preset, bool showing, ref Vector3 scale, ref Vector2 position, ref Vector3 rotation)
    {
        if (preset == null) return;
        if (preset.Effect == TweenEffect.Scale) scale = preset.Relative ? Vector3.Scale(initialScale, preset.Vector) : preset.Vector;
        if (preset.Effect == TweenEffect.Move) position = preset.Relative ? initialPosition + (Vector2)preset.Vector : (Vector2)preset.Vector;
    }

    private void CaptureInitial()
    {
        if (initialized) return;
        if (rect == null) rect = GetComponent<RectTransform>();
        if (group == null) group = GetComponent<CanvasGroup>();
        initialScale = rect.localScale;
        initialPosition = rect.anchoredPosition;
        initialRotation = rect.localEulerAngles;
        initialAlpha = group.alpha;
        initialInteractable = group.interactable;
        initialBlocksRaycasts = group.blocksRaycasts;
        IsShown = initialAlpha > 0;
        initialized = true;
    }

    protected void TransitionTo(bool shown, bool force = false)
    {
        if (!isActiveAndEnabled) return;
        CaptureInitial();
        if (!force && IsShown == shown && !IsTransitioning) return;
        var preset = ResolvePreset(shown);
        if (!ValidTransition(shown)) return;
        float duration = DurationFor(preset);
        bool wasTransitioning = IsTransitioning;
        int currentRevision = StopTransition();
        if (currentRevision != revision) return;
        IsShown = shown;
        group.interactable = false;
        group.blocksRaycasts = false;
        Vector3 scale = initialScale;
        Vector2 position = initialPosition;
        Vector3 rotation = initialRotation;
        if (!shown) ConfigureHidden(preset, false, ref scale, ref position, ref rotation);
        float alpha = shown ? (initialAlpha > 0 ? initialAlpha : 1) : 0;
        OnTransitionStarted(shown);
        if (currentRevision != revision) return;
        if (reducedMotion || duration == 0)
        {
            ApplyFinal(shown, scale, position, rotation, alpha, currentRevision);
            return;
        }
        if (shown && !wasTransitioning)
        {
            Vector3 startScale = initialScale;
            Vector2 startPosition = initialPosition;
            Vector3 startRotation = initialRotation;
            ConfigureHidden(preset, true, ref startScale, ref startPosition, ref startRotation);
            rect.localScale = startScale;
            rect.anchoredPosition = startPosition;
            rect.localEulerAngles = startRotation;
            if (force) group.alpha = 0;
        }
        transition = DOTween.Sequence().SetAutoKill(true).SetRecyclable(false);
        transition.SetUpdate(preset == null || preset.UnscaledTime);
        transition.SetDelay(preset == null ? 0 : preset.Delay);
        transition.Join(group.DOFade(alpha, duration).SetEase(EaseFor(preset, shown)));
        transition.Join(rect.DOScale(scale, duration).SetEase(EaseFor(preset, shown)));
        transition.Join(rect.DOAnchorPos(position, duration).SetEase(EaseFor(preset, shown)));
        transition.Join(rect.DOLocalRotate(rotation, duration).SetEase(EaseFor(preset, shown)));
        AddMotion(transition, shown, preset, duration);
        transition.OnComplete(() => ApplyFinal(shown, scale, position, rotation, alpha, currentRevision));
        transition.SetLink(gameObject, LinkBehaviour.KillOnDestroy).Play();
    }

    protected bool ValidTransition(bool shown)
    {
        var preset = ResolvePreset(shown);
        if (preset != null && !ValidPreset(preset)) return false;
        float duration = DurationFor(preset);
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0)
        {Debug.LogError($"Панель {name}: неверное Duration={duration}", this); return false;}

        return true;
    }

    protected virtual bool ValidPreset(TweenPreset preset)
    {
        try {GoofyTween.ValidateTiming(preset);}
        catch (System.ArgumentException error)
        {Debug.LogError($"Панель {name}: preset {preset.name}, {error.Message}, Ease={preset.Ease}, Duration={preset.Duration}, Delay={preset.Delay}", this); return false;}
        if (float.IsNaN(preset.Vector.sqrMagnitude) || float.IsInfinity(preset.Vector.sqrMagnitude))
        {
            Debug.LogError($"Панель {name}: неверный preset {preset.name}, Vector={preset.Vector}, Ease={preset.Ease}, Duration={preset.Duration}, Delay={preset.Delay}", this);
            return false;
        }
        if (preset.Effect != TweenEffect.Fade && preset.Effect != TweenEffect.Scale && preset.Effect != TweenEffect.Move)
        {
            Debug.LogError($"Панель {name}: preset {preset.name} использует неподдерживаемый Effect={preset.Effect}", this);
            return false;
        }

        return true;
    }

    protected int StopTransition()
    {
        int request = ++revision;
        var previous = transition;
        transition = null;
        if (previous != null && previous.IsActive()) previous.Kill();

        return request;
    }

    private void ApplyFinal(bool shown, Vector3 scale, Vector2 position, Vector3 rotation, float alpha, int currentRevision)
    {
        if (currentRevision != revision) return;
        transition = null;
        rect.localScale = scale;
        rect.anchoredPosition = position;
        rect.localEulerAngles = rotation;
        group.alpha = alpha;
        group.interactable = shown && initialInteractable;
        group.blocksRaycasts = shown && initialBlocksRaycasts;
        if (shown) onShown.Invoke();
        else onHidden.Invoke();
        if (currentRevision == revision) OnTransitionCompleted(shown);
    }
}
