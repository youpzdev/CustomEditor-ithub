using System;
using DG.Tweening;
using UnityEngine;

public class TweenSequence : MonoBehaviour
{
    [SerializeField] private TweenSequenceEntry[] entries = Array.Empty<TweenSequenceEntry>();
    [SerializeField] [Min(0)] private float stagger;
    [SerializeField] private bool unscaledTime = true;
    [SerializeField] private bool playOnEnable;
    [SerializeField] private bool reducedMotion;

    private Sequence current;
    private TweenState[] initial;
    private TweenEffect[] effects;
    private bool[] localSpaces;
    private int revision;

    public TweenSequenceEntry[] Entries {get => entries; set => entries = value;}
    public float Stagger {get => stagger; set => stagger = value;}
    public bool UnscaledTime {get => unscaledTime; set => unscaledTime = value;}
    public bool PlayOnEnable {get => playOnEnable; set => playOnEnable = value;}
    public bool ReducedMotion {get => reducedMotion; set {reducedMotion = value; if (value) current?.Complete(true);}}
    public Sequence CurrentSequence => current;

    private void OnEnable()
    {
        if (!playOnEnable) return;
        try {Play();}
        catch (ArgumentException error) {Debug.LogError($"TweenSequence {name}: {error.Message}", this);}
    }

    private void OnDisable()
    {
        Stop(TweenStopMode.RestoreInitial);
    }

    public Sequence Play()
    {
        if (entries == null || entries.Length == 0) throw new ArgumentException("TweenSequence: entries не заданы", nameof(Entries));
        if (!GoofyTween.IsFinite(stagger) || stagger < 0) throw new ArgumentOutOfRangeException(nameof(Stagger), stagger, "TweenSequence: stagger должен быть конечным и >= 0");
        var next = DOTween.Sequence().Pause().SetRecyclable(false);
        var states = new TweenState[entries.Length];
        var nextEffects = new TweenEffect[entries.Length];
        var nextSpaces = new bool[entries.Length];
        try
        {
            float previousStart = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.Target == null || entry.Preset == null) throw new ArgumentException($"TweenSequence: entries[{i}] требует target и preset", nameof(Entries));
                if (!GoofyTween.IsFinite(entry.Delay) || entry.Delay < 0) throw new ArgumentException($"TweenSequence: entries[{i}].Delay должен быть конечным и >= 0", nameof(Entries));
                if (entry.Preset.Loops < 0) throw new ArgumentException($"TweenSequence: entries[{i}].Loops должен быть конечным", nameof(Entries));
                var child = GoofyTween.Create(entry.Target, entry.Preset);
                states[i] = initial != null && i < initial.Length && initial[i].Target == entry.Target ? initial[i] : new TweenState(entry.Target);
                nextEffects[i] = entry.Preset.Effect;
                nextSpaces[i] = entry.Preset.LocalSpace;
                float start = (entry.JoinPrevious && i > 0 ? previousStart : next.Duration(false)) + entry.Delay + (i > 0 ? stagger : 0);
                next.Insert(start, child);
                previousStart = start;
            }
        }
        catch
        {
            next.Kill();
            throw;
        }
        int request = ++revision;
        var previous = current;
        current = null;
        previous?.Kill();
        if (request != revision) {next.Kill(); return current;}
        initial = states;
        effects = nextEffects;
        localSpaces = nextSpaces;
        current = next.SetUpdate(unscaledTime).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        if (reducedMotion) current.Complete(true);
        else current.Play();

        return current;
    }

    public void Stop(TweenStopMode mode = TweenStopMode.KeepCurrent)
    {
        if (!Enum.IsDefined(typeof(TweenStopMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode), mode, "TweenSequence: неизвестный режим остановки");
        int request = ++revision;
        var previous = current;
        current = null;
        if (mode == TweenStopMode.Complete) previous?.Complete(true);
        previous?.Kill();
        if (request != revision || mode != TweenStopMode.RestoreInitial || initial == null) return;
        for (int i = initial.Length - 1; i >= 0; i--) initial[i].Restore(effects[i], localSpaces[i]);
        initial = null;
    }
}
