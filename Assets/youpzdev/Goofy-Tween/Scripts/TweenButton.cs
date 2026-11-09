using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Selectable))]
public class TweenButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Transitions")]
    [SerializeField] private TweenPreset hoverPreset;
    [SerializeField] private TweenPreset pressPreset;
    [SerializeField] private TweenPreset clickPreset;
    [SerializeField] private bool reducedMotion;

    private Selectable selectable;
    private Vector3 initialScale;
    private bool initialized;
    private bool hovered;
    private bool selected;
    private bool pressed;
    private bool wasInteractable;

    protected Tween scaleTween;
    private int scaleRevision;

    public TweenPreset HoverPreset {get => hoverPreset; set => hoverPreset = value;}
    public TweenPreset PressPreset {get => pressPreset; set => pressPreset = value;}
    public TweenPreset ClickPreset {get => clickPreset; set => clickPreset = value;}
    public virtual bool ReducedMotion
    {
        get => reducedMotion;
        set
        {
            reducedMotion = value;
            if (initialized && value) RefreshState();
        }
    }
    public Tween CurrentTween => scaleTween;

    protected virtual void Awake()
    {
        selectable = GetComponent<Selectable>();
        initialScale = transform.localScale;
        initialized = true;
        wasInteractable = CanInteract;
    }

    protected virtual void OnDisable()
    {
        if (!initialized) return;
        int request = StopAnimation();
        if (request != scaleRevision) return;
        transform.localScale = initialScale;
        hovered = false;
        selected = false;
        pressed = false;
    }

    protected virtual void Update()
    {
        bool interactable = CanInteract;
        if (interactable == wasInteractable) return;
        wasInteractable = interactable;
        if (!interactable) pressed = false;
        RefreshState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        RefreshState();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        RefreshState();
    }
    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        RefreshState();
    }
    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        pressed = false;
        RefreshState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !CanInteract) return;
        pressed = true;
        RefreshState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        pressed = false;
        RefreshState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClick();
    }

    protected bool CanInteract => isActiveAndEnabled && selectable != null && selectable.IsActive() && selectable.IsInteractable();

    protected int ScaleRevision => scaleRevision;
    protected Vector3 InitialScale => initialScale;
    protected bool Pressed => pressed;
    protected bool Highlighted => hovered || selected;
    protected TweenPreset StatePreset => pressed ? pressPreset : (Highlighted ? hoverPreset : null);
    protected virtual Vector3 StateScale(TweenPreset preset) {return TargetScale(preset);}
    protected virtual float StateDuration(TweenPreset preset) {return preset == null ? 0 : preset.Duration;}
    protected virtual Ease StateEase(TweenPreset preset) {return preset == null ? Ease.OutQuad : preset.Ease;}

    protected void RefreshState()
    {
        if (!CanInteract) {int stopped = StopAnimation(); if (stopped == scaleRevision) transform.localScale = initialScale; return;}
        var preset = StatePreset;
        if (preset != null && !ValidPreset(preset)) return;
        Vector3 target = StateScale(preset);
        float duration = StateDuration(preset);
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0 || float.IsNaN(target.sqrMagnitude) || float.IsInfinity(target.sqrMagnitude))
        {Debug.LogError($"Кнопка {name}: неверное состояние Scale={target}, Duration={duration}", this); return;}
        int request = StopAnimation();
        if (request != scaleRevision) return;
        if (reducedMotion || duration == 0) {transform.localScale = target; return;}
        scaleTween = transform.DOScale(target, duration).SetEase(StateEase(preset)).SetDelay(preset == null ? 0 : preset.Delay).SetUpdate(preset == null || preset.UnscaledTime).SetAutoKill(true).SetRecyclable(false);
        scaleTween.SetLink(gameObject, LinkBehaviour.KillOnDestroy).Play();
    }

    protected virtual void PlayClick()
    {
        if (!CanInteract) {RefreshState(); return;}
        if (clickPreset == null || reducedMotion) {RefreshState(); return;}
        if (!ValidPreset(clickPreset)) return;
        int request = StopAnimation();
        if (request != scaleRevision) return;
        Vector3 resting = StateScale(StatePreset);
        if (clickPreset.Duration == 0) {transform.localScale = resting; return;}
        var sequence = DOTween.Sequence().SetAutoKill(true).SetRecyclable(false);
        sequence.SetUpdate(clickPreset.UnscaledTime);
        sequence.SetDelay(clickPreset.Delay);
        sequence.Append(transform.DOScale(TargetScale(clickPreset), clickPreset.Duration * 0.5f).SetEase(clickPreset.Ease));
        sequence.Append(transform.DOScale(resting, clickPreset.Duration * 0.5f).SetEase(clickPreset.Ease));
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        scaleTween = sequence;
        sequence.Play();
    }

    protected Vector3 TargetScale(TweenPreset preset)
    {
        if (preset == null) return initialScale;

        return preset.Relative ? Vector3.Scale(initialScale, preset.Vector) : preset.Vector;
    }

    protected bool ValidPreset(TweenPreset preset)
    {
        try {GoofyTween.ValidateTiming(preset);}
        catch (System.ArgumentException error)
        {Debug.LogError($"Кнопка {name}: preset {preset.name}, {error.Message}, Ease={preset.Ease}, Duration={preset.Duration}, Delay={preset.Delay}", this); return false;}
        if (preset.Effect != TweenEffect.Scale || float.IsNaN(preset.Vector.sqrMagnitude) || float.IsInfinity(preset.Vector.sqrMagnitude))
        {
            Debug.LogError($"Кнопка {name}: проверь preset {preset.name}, Effect={preset.Effect}, Vector={preset.Vector}, Ease={preset.Ease}, Duration={preset.Duration}, Delay={preset.Delay}", this);
            return false;
        }

        return true;
    }

    protected int StopAnimation()
    {
        int request = ++scaleRevision;
        var previous = scaleTween;
        scaleTween = null;
        if (previous != null && previous.IsActive()) previous.Kill();

        return request;
    }
}
