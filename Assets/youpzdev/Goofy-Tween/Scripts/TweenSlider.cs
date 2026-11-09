using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TweenSlider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider target;
    [SerializeField] private TweenPreset preset;

    [Header("Settings")]
    [SerializeField] private bool notifyValueChanged;
    [SerializeField] private bool reducedMotion;

    private Tween tween;

    public Slider Target {get => target; set {Stop(); target = value;}}
    public TweenPreset Preset {get => preset; set => preset = value;}
    public bool NotifyValueChanged {get => notifyValueChanged; set => notifyValueChanged = value;}
    public bool ReducedMotion {get => reducedMotion; set {reducedMotion = value; if (value) Stop(true);}}

    public Tween SetValue(float value)
    {
        if (!Valid(value, true)) return null;
        Stop();
        value = Mathf.Clamp(value, target.minValue, target.maxValue);
        if (reducedMotion || preset.Duration == 0) {Write(value); return null;}
        tween = DOTween.To(() => target.value, Write, value, preset.Duration)
            .SetDelay(preset.Delay).SetEase(preset.Ease).SetUpdate(preset.UnscaledTime).SetTarget(this).SetAutoKill(true).SetRecyclable(false).Play();

        return tween;
    }

    public void SetImmediate(float value)
    {
        if (!Valid(value, false)) return;
        Stop();
        Write(Mathf.Clamp(value, target.minValue, target.maxValue));
    }

    public void Stop(bool complete = false)
    {
        var previous = tween;
        tween = null;
        if (previous != null && previous.IsActive()) previous.Kill(complete);
    }

    private void Write(float value)
    {
        if (notifyValueChanged) target.value = value;
        else target.SetValueWithoutNotify(value);
    }

    private bool Valid(float value, bool needsPreset)
    {
        if (!target) {Debug.LogError($"TweenSlider {name}: target не задан", this); return false;}
        if (!isActiveAndEnabled) {Debug.LogError($"TweenSlider {name}: компонент выключен", this); return false;}
        if (float.IsNaN(value) || float.IsInfinity(value)) {Debug.LogError($"TweenSlider {name}: неверное value={value.ToString(CultureInfo.InvariantCulture)}", this); return false;}
        if (needsPreset && !TweenValueSettings.Validate(preset, this)) return false;

        return true;
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }
}
