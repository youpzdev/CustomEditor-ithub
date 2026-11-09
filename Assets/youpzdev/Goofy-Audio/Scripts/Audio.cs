using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

public static class Audio
{
    private static readonly Queue<AudioSource> pool = new();
    private static readonly List<AudioHandle> active = new();

    private static AudioRunner runner;

    private static int maxInactive = 32;

    public static int CountActive {get {Prune(); return active.Count;}}
    public static int CountInactive {get {Prune(); return pool.Count;}}

    public static int MaxInactive
    {
        get => maxInactive;
        set
        {
            if (value < 0) {Debug.LogError($"Audio: MaxInactive должен быть >= 0, получено {value}"); return;}
            maxInactive = value;
            Prune();
            while (pool.Count > value) Dispose(pool.Dequeue());
        }
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f, AudioMixerGroup mixerGroup = null) {PlayTracked(clip, volume, pitch, mixerGroup);}

    public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, AudioMixerGroup mixerGroup = null) {PlayTracked(clip, position, volume, pitch, mixerGroup);}

    public static AudioHandle PlayTracked(AudioClip clip, float volume = 1f, float pitch = 1f,
        AudioMixerGroup mixerGroup = null, bool loop = false, Object owner = null) {return Start(clip, Vector3.zero, false, volume, pitch, mixerGroup, loop, owner);}

    public static AudioHandle PlayTracked(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
        AudioMixerGroup mixerGroup = null, bool loop = false, Object owner = null) {return Start(clip, position, true, volume, pitch, mixerGroup, loop, owner);}

    public static void Prewarm(int count)
    {
        if (count < 0) {Debug.LogError($"Audio: Prewarm count должен быть >= 0, получено {count}"); return;}
        Prune();
        int target = Mathf.Min(count, maxInactive);
        while (pool.Count < target)
        {
            var source = Create();
            source.gameObject.SetActive(false);
            pool.Enqueue(source);
        }
    }

    public static void StopAll() {for (int i = active.Count - 1; i >= 0; i--) Release(active[i]);}

    public static void PauseAll() {foreach (var handle in active) handle.Pause();}

    public static void ResumeAll() {foreach (var handle in active) handle.Resume();}

    public static void Clear()
    {
        StopAll();
        while (pool.Count > 0) Dispose(pool.Dequeue());
    }

    internal static void Tick()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var handle = active[i];
            if (!handle.IsValid || !handle.OwnerAlive || (!handle.IsPaused && !handle.Source.isPlaying)) Release(handle);
            else handle.Tick(Time.unscaledDeltaTime);
        }
    }

    internal static void Release(AudioHandle handle) {if (handle.Active) Return(handle.Source);}

    internal static void OnRunnerDestroyed(AudioRunner destroyed)
    {
        if (runner != destroyed) return;
        foreach (var handle in active) handle.Invalidate();
        active.Clear();
        pool.Clear();
        runner = null;
    }

    internal static bool ValidNumber(float value) {return !float.IsNaN(value) && !float.IsInfinity(value);}

    internal static bool ValidPitch(float value)
    {
        if (ValidNumber(value) && Mathf.Abs(value) >= 0.01f && Mathf.Abs(value) <= 3f) return true;
        Debug.LogError($"Audio: pitch должен быть от -3 до 3, кроме (-0.01, 0.01), получено {value}");

        return false;
    }

    private static AudioHandle Start(AudioClip clip, Vector3 position, bool spatial, float volume, float pitch,
        AudioMixerGroup mixerGroup, bool loop, Object owner)
    {
        if (clip == null) return null;
        if (!ReferenceEquals(owner, null) && owner == null) return null;
        if (!ValidNumber(volume)) {Debug.LogError($"Audio: volume должен быть конечным, получено {volume}"); return null;}
        if (!ValidPitch(pitch)) return null;
        if (!ValidNumber(position.x) || !ValidNumber(position.y) || !ValidNumber(position.z))
        {Debug.LogError($"Audio: position должен быть конечным, получено {position}"); return null;}

        var source = Get();
        source.transform.position = position;
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = pitch;
        source.spatialBlend = spatial ? 1f : 0f;
        source.loop = loop;
        source.outputAudioMixerGroup = mixerGroup;
        var handle = new AudioHandle(source, owner);
        active.Add(handle);
        source.Play();

        return handle;
    }

    private static AudioSource Get()
    {
        EnsureRunner();
        while (pool.Count > 0)
        {
            var source = pool.Dequeue();
            if (source == null) continue;
            source.gameObject.SetActive(true);

            return source;
        }

        return Create();
    }

    private static AudioSource Create()
    {
        EnsureRunner();
        var instance = new GameObject("AudioSource");
        instance.transform.SetParent(runner.transform);
        var source = instance.AddComponent<AudioSource>();
        source.playOnAwake = false;

        return source;
    }

    private static void EnsureRunner()
    {
        if (runner != null) return;
        var container = new GameObject("AudioPool");
        runner = container.AddComponent<AudioRunner>();
        if (Application.isPlaying) Object.DontDestroyOnLoad(container);
    }

    private static void Return(AudioSource source)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(active[i].Source, source)) continue;
            active[i].Invalidate();
            active.RemoveAt(i);
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.outputAudioMixerGroup = null;
            source.gameObject.SetActive(false);
            if (pool.Count < maxInactive && runner != null) pool.Enqueue(source);
            else Dispose(source);

            return;
        }
    }

    private static void Prune()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (!active[i].IsValid || !active[i].OwnerAlive) Release(active[i]);
        }

        int count = pool.Count;
        for (int i = 0; i < count; i++)
        {
            var source = pool.Dequeue();
            if (source != null) pool.Enqueue(source);
        }
    }

    private static void Dispose(AudioSource source)
    {
        if (source == null) return;
        if (Application.isPlaying) Object.Destroy(source.gameObject);
        else Object.DestroyImmediate(source.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Clear();
        maxInactive = 32;
    }
}
