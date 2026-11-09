using UnityEngine;

public class AudioHandle
{
    private readonly AudioSource source;
    private readonly Object owner;

    private readonly bool hasOwner;
    private bool active = true;
    private bool paused;
    private bool fading;
    private bool stopAfterFade;

    private float fadeFrom;
    private float fadeTarget;
    private float fadeDuration;
    private float fadeElapsed;

    private Transform followTarget;

    internal AudioSource Source => source;
    internal bool Active => active;
    internal bool OwnerAlive => !hasOwner || owner != null;

    public bool IsValid => active && source != null;
    public bool IsPlaying => IsValid && !paused && source.isPlaying;
    public bool IsPaused => IsValid && paused;

    public float Volume
    {
        get => IsValid ? source.volume : 0;
        set
        {
            if (!IsValid) return;
            if (!Audio.ValidNumber(value)) {Debug.LogError($"Audio: volume должен быть конечным, получено {value}"); return;}
            fading = false;
            stopAfterFade = false;
            source.volume = Mathf.Clamp01(value);
        }
    }

    public float Pitch
    {
        get => IsValid ? source.pitch : 0;
        set
        {if (IsValid && Audio.ValidPitch(value)) source.pitch = value;}
    }

    internal AudioHandle(AudioSource source, Object owner)
    {
        this.source = source;
        this.owner = owner;
        hasOwner = owner != null;
    }

    public void Stop() {Audio.Release(this);}

    public void Stop(float fadeOut) {BeginFade(0, fadeOut, true);}

    public void FadeTo(float volume, float duration) {BeginFade(volume, duration, false);}

    public void Follow(Transform target)
    {
        if (!IsValid) return;
        followTarget = target;
        if (target == null) return;
        source.spatialBlend = 1;
        source.transform.position = target.position;
    }

    public void Pause()
    {
        if (!IsValid || paused) return;
        paused = true;
        source.Pause();
    }

    public void Resume()
    {
        if (!IsValid || !paused) return;
        paused = false;
        source.UnPause();
    }

    internal void Invalidate()
    {
        active = false;
        paused = false;
        fading = false;
        stopAfterFade = false;
        followTarget = null;
    }

    internal void Tick(float deltaTime)
    {
        if (!IsValid) return;
        if (followTarget != null) source.transform.position = followTarget.position;
        if (!fading || paused) return;
        fadeElapsed += deltaTime;
        source.volume = Mathf.Lerp(fadeFrom, fadeTarget, fadeElapsed / fadeDuration);
        if (fadeElapsed < fadeDuration) return;
        fading = false;
        if (stopAfterFade) Stop();
    }

    private void BeginFade(float volume, float duration, bool stop)
    {
        if (!IsValid) return;
        if (!Audio.ValidNumber(volume)) {Debug.LogError($"Audio: FadeTo volume должен быть конечным, получено {volume}"); return;}
        if (!Audio.ValidNumber(duration) || duration < 0)
        {Debug.LogError($"Audio: fade duration должен быть конечным и >= 0, получено {duration}"); return;}
        fadeFrom = source.volume;
        fadeTarget = Mathf.Clamp01(volume);
        fadeDuration = duration;
        fadeElapsed = 0;
        stopAfterFade = stop;
        fading = duration > 0;
        if (fading) return;
        source.volume = fadeTarget;
        if (stop) Stop();
    }
}
