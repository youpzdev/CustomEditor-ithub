using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(TweenPlayer))]
public class AnimatedIcon : MonoBehaviour
{
    public enum IdleAnimation
    {
        None,
        Bounce,
        Pulse,
        Wobble,
        Float,
        Shake,
        Spin,
        Heartbeat
    }

    [Serializable]
    public class AnimationBinding
    {
        [SerializeField] private IdleAnimation animation;
        [SerializeField] private TweenPreset preset;

        public IdleAnimation Animation {get => animation; set => animation = value;}
        public TweenPreset Preset {get => preset; set => preset = value;}
    }

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private AnimationBinding[] bindings = Array.Empty<AnimationBinding>();

    [Space(5)]
    [Header("Settings")]
    [SerializeField] private IdleAnimation idleAnimation;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] [Min(0.01f)] private float speed = 1;

    private TweenPlayer player;
    private int revision;

    public AnimationBinding[] Bindings {get => bindings; set => bindings = value;}
    public IdleAnimation SelectedAnimation {get => idleAnimation; set => idleAnimation = value;}
    public bool PlayOnEnable {get => playOnEnable; set => playOnEnable = value;}
    public TweenPlayer Player => player != null ? player : GetComponent<TweenPlayer>();
    public Tween CurrentTween => Player.CurrentTween;
    public Transform Target
    {
        get => ReferenceEquals(target, null) ? transform : target;
        set
        {
            if (!ReferenceEquals(value, null) && value == null) throw new ArgumentException("AnimatedIcon: Target уничтожен", nameof(value));
            StopAnimation();
            target = value;
            Player.Target = Target;
        }
    }
    public float Speed
    {
        get => speed;
        set
        {
            ValidateSpeed(value);
            speed = value;
            if (CurrentTween != null && CurrentTween.IsActive()) CurrentTween.timeScale = speed;
        }
    }

    private void Awake()
    {
        player = GetComponent<TweenPlayer>();
        player.PlayOnEnable = false;
        player.Target = Target;
    }

    private void OnEnable()
    {
        if (playOnEnable) PlayAnimation(idleAnimation);
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    public void PlayAnimation(IdleAnimation animation)
    {
        if (animation == IdleAnimation.None) {idleAnimation = animation; StopAnimation(); return;}
        try
        {
            if (!Enum.IsDefined(typeof(IdleAnimation), animation)) throw new ArgumentOutOfRangeException(nameof(animation), animation, "AnimatedIcon: неизвестная анимация");
            if (!isActiveAndEnabled) throw new ArgumentException("AnimatedIcon: компонент выключен", nameof(animation));
            ValidateSpeed(speed);
            TweenPreset preset = FindPreset(animation);
            GoofyTween.Validate(Target, preset);
            int request = ++revision;
            Player.Stop(TweenStopMode.RestoreInitial);
            if (request != revision) return;
            Player.Preset = preset;
            Tween tween = Player.Play();
            if (request != revision) return;
            idleAnimation = animation;
            if (tween != null && tween.IsActive()) tween.timeScale = speed;
        }
        catch (ArgumentException error) {Debug.LogError($"AnimatedIcon {name}: {error.Message}", this);}
    }

    public void StopAnimation()
    {
        revision++;
        Player.Stop(TweenStopMode.RestoreInitial);
    }

    private TweenPreset FindPreset(IdleAnimation animation)
    {
        TweenPreset preset = null;
        bool found = false;
        if (bindings != null)
            foreach (AnimationBinding binding in bindings)
            {
                if (binding == null || binding.Animation != animation) continue;
                if (found) throw new ArgumentException($"AnimatedIcon: Bindings содержит повтор {animation}", nameof(Bindings));
                found = true;
                preset = binding.Preset;
            }
        if (!found || preset == null) throw new ArgumentException($"AnimatedIcon: Bindings не содержит пресет {animation}", nameof(Bindings));

        return preset;
    }

    private static void ValidateSpeed(float value)
    {
        if (!GoofyTween.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(Speed), value, "AnimatedIcon: Speed должен быть конечным и больше нуля");
    }
}
