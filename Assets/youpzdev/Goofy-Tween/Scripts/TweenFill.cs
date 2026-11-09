using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TweenFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image target;
    [SerializeField] private Image secondaryTarget;
    [SerializeField] private TweenPreset preset;

    [Header("Settings")]
    [SerializeField] [Min(0)] private float secondaryDelay = 0.25f;
    [SerializeField] private bool reducedMotion;

    private Tween tween;

    public Image Target {get => target; set {Stop(); target = value;}}
    public Image SecondaryTarget {get => secondaryTarget; set {Stop(); secondaryTarget = value;}}
    public TweenPreset Preset {get => preset; set => preset = value;}
    public float SecondaryDelay {get => secondaryDelay; set => secondaryDelay = value;}
    public bool ReducedMotion {get => reducedMotion; set {reducedMotion = value; if (value) Stop(true);}}

    public Tween SetValue(float value)
    {
        if (!Valid(value, true)) return null;
        Stop();
        value = Mathf.Clamp01(value);
        if (reducedMotion || preset.Duration == 0) {WriteImmediate(value); return null;}
        var damage = value < target.fillAmount;
        if (secondaryTarget && !damage) secondaryTarget.fillAmount = value;
        var primary = DOTween.To(() => target.fillAmount, amount => target.fillAmount = amount, value, preset.Duration).SetEase(preset.Ease);
        var sequence = DOTween.Sequence().Append(primary);
        if (secondaryTarget && damage)
            sequence.Insert(secondaryDelay, DOTween.To(() => secondaryTarget.fillAmount, amount => secondaryTarget.fillAmount = amount, value, preset.Duration).SetEase(preset.Ease));
        tween = sequence.SetDelay(preset.Delay).SetUpdate(preset.UnscaledTime).SetTarget(this).SetAutoKill(true).SetRecyclable(false).Play();

        return tween;
    }

    public void SetImmediate(float value)
    {
        if (!Valid(value, false)) return;
        Stop();
        WriteImmediate(Mathf.Clamp01(value));
    }

    public void Stop(bool complete = false)
    {
        var previous = tween;
        tween = null;
        if (previous != null && previous.IsActive()) previous.Kill(complete);
    }

    private void WriteImmediate(float value)
    {
        target.fillAmount = value;
        if (secondaryTarget) secondaryTarget.fillAmount = value;
    }

    private bool Valid(float value, bool needsPreset)
    {
        if (!target) {Debug.LogError($"TweenFill {name}: target не задан", this); return false;}
        if (target.type != Image.Type.Filled) {Debug.LogError($"TweenFill {name}: target.type={target.type}, нужен Filled", this); return false;}
        if (secondaryTarget && secondaryTarget.type != Image.Type.Filled) {Debug.LogError($"TweenFill {name}: secondaryTarget.type={secondaryTarget.type}, нужен Filled", this); return false;}
        if (!isActiveAndEnabled) {Debug.LogError($"TweenFill {name}: компонент выключен", this); return false;}
        if (float.IsNaN(value) || float.IsInfinity(value)) {Debug.LogError($"TweenFill {name}: неверное value={value.ToString(CultureInfo.InvariantCulture)}", this); return false;}
        if (secondaryTarget == target) {Debug.LogError($"TweenFill {name}: target и secondaryTarget совпадают", this); return false;}
        if (float.IsNaN(secondaryDelay) || float.IsInfinity(secondaryDelay) || secondaryDelay < 0) {Debug.LogError($"TweenFill {name}: неверное secondaryDelay={secondaryDelay.ToString(CultureInfo.InvariantCulture)}", this); return false;}
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
