using System;
using System.Globalization;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TweenCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text target;
    [SerializeField] private TweenPreset preset;

    [Header("Settings")]
    [SerializeField] private string numberFormat = "0";
    [SerializeField] private bool reducedMotion;
    [SerializeField] private float value;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onValueChanged = new();

    private Tween tween;

    public Text Target {get => target; set {Stop(); target = value;}}
    public TweenPreset Preset {get => preset; set => preset = value;}
    public string NumberFormat {get => numberFormat; set {if (ValidFormat(value)) numberFormat = value;}}
    public bool ReducedMotion {get => reducedMotion; set {reducedMotion = value; if (value) Stop(true);}}
    public float Value => value;
    public UnityEvent<float> OnValueChanged => onValueChanged;

    public Tween SetValue(float destination)
    {
        if (!Valid(destination, true)) return null;
        Stop();
        if (reducedMotion || preset.Duration == 0) {Write(destination, numberFormat); return null;}
        var format = numberFormat;
        tween = DOTween.To(() => value, amount => Write(amount, format), destination, preset.Duration)
            .SetDelay(preset.Delay).SetEase(preset.Ease).SetUpdate(preset.UnscaledTime).SetTarget(this).SetAutoKill(true).SetRecyclable(false).Play();

        return tween;
    }

    public void SetImmediate(float destination)
    {
        if (!Valid(destination, false)) return;
        Stop();
        Write(destination, numberFormat);
    }

    public void Stop(bool complete = false)
    {
        var previous = tween;
        tween = null;
        if (previous != null && previous.IsActive()) previous.Kill(complete);
    }

    private void Write(float amount, string format)
    {
        value = amount;
        target.text = amount.ToString(format, CultureInfo.InvariantCulture);
        onValueChanged.Invoke(amount);
    }

    private bool Valid(float destination, bool needsPreset)
    {
        if (!target) {Debug.LogError($"TweenCounter {name}: target не задан", this); return false;}
        if (!isActiveAndEnabled) {Debug.LogError($"TweenCounter {name}: компонент выключен", this); return false;}
        if (float.IsNaN(destination) || float.IsInfinity(destination)) {Debug.LogError($"TweenCounter {name}: неверное value={destination.ToString(CultureInfo.InvariantCulture)}", this); return false;}
        if (!ValidFormat(numberFormat)) return false;
        if (needsPreset && !TweenValueSettings.Validate(preset, this)) return false;

        return true;
    }

    private bool ValidFormat(string format)
    {
        try {value.ToString(format, CultureInfo.InvariantCulture);}
        catch (FormatException) {Debug.LogError($"TweenCounter {name}: неверный numberFormat={format}", this); return false;}

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

internal static class TweenValueSettings
{
    public static bool Validate(TweenPreset preset, Component owner)
    {
        try {GoofyTween.ValidateTiming(preset);}
        catch (ArgumentException error) {Debug.LogError($"{owner.GetType().Name} {owner.name}: preset {error.Message}", owner); return false;}

        return true;
    }
}