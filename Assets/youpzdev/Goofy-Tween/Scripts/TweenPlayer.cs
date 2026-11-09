using System;
using DG.Tweening;
using UnityEngine;

public class TweenPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private TweenPreset preset;

    [Header("Settings")]
    [SerializeField] private bool playOnEnable;
    [SerializeField] private bool reducedMotion;

    private Tween currentTween;
    private TweenState initial;
    private TweenEffect activeEffect;
    private bool activeLocal;
    private bool hasPlayback;
    private int revision;

    public Transform Target
    {
        get => target != null ? target : transform;
        set {Stop(TweenStopMode.RestoreInitial); target = value; CaptureInitialState();}
    }
    public TweenPreset Preset {get => preset; set => preset = value;}
    public bool PlayOnEnable {get => playOnEnable; set => playOnEnable = value;}
    public bool ReducedMotion {get => reducedMotion; set {reducedMotion = value; if (value) Finish();}}
    public Tween CurrentTween => currentTween;

    private void Awake()
    {
        CaptureInitialState();
    }

    private void OnEnable()
    {
        if (!playOnEnable) return;
        try {Play();}
        catch (ArgumentException error) {Debug.LogError($"TweenPlayer {name}: {error.Message}", this);}
    }

    private void OnDisable()
    {
        Stop(TweenStopMode.RestoreInitial);
    }

    public void CaptureInitialState() {initial = new TweenState(Target);}

    public Tween Play()
    {
        var next = GoofyTween.Create(Target, preset);
        int request = ++revision;
        var previous = currentTween;
        currentTween = null;
        previous?.Kill();
        if (request != revision) {next.Kill(); return currentTween;}
        initial ??= new TweenState(Target);
        activeEffect = preset.Effect;
        activeLocal = preset.LocalSpace;
        hasPlayback = true;
        currentTween = next.SetRecyclable(false).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        if (reducedMotion) Finish();
        else currentTween.Play();

        return next;
    }

    public void Stop(TweenStopMode mode = TweenStopMode.KeepCurrent)
    {
        if (!Enum.IsDefined(typeof(TweenStopMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode), mode, "TweenPlayer: неизвестный режим остановки");
        int request = ++revision;
        var previous = currentTween;
        currentTween = null;
        if (mode == TweenStopMode.Complete) Finish(previous);
        else previous?.Kill();
        if (request == revision && mode == TweenStopMode.RestoreInitial && hasPlayback) initial?.Restore(activeEffect, activeLocal);
    }

    private void Finish()
    {
        var previous = currentTween;
        currentTween = null;
        Finish(previous);
    }

    private static void Finish(Tween tween)
    {
        if (tween == null || !tween.IsActive()) return;
        if (tween.Loops() < 0) tween.Goto(tween.Duration(false), false);
        else tween.Complete(true);
        tween.Kill();
    }
}
