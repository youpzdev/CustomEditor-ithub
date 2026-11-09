using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class AnimatedButton : TweenButton
{
    [Header("Feedback presets")]
    [SerializeField] private TweenPreset punchPreset;
    [SerializeField] private TweenPreset shakePreset;

    [Header("Audio")]
    [SerializeField] private AudioSource soundSource;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("Legacy press")]
    [SerializeField] private bool _enablePressAnimation = true;
    [SerializeField] private float _pressScaleFactor = 0.92f;
    [SerializeField] private float _pressDuration = 0.25f;

    [Header("Legacy hover")]
    [SerializeField] private bool _enableHoverAnimation = true;
    [SerializeField] private float _hoverScaleFactor = 1.06f;
    [SerializeField] private float _hoverDuration = 0.2f;

    [Header("Legacy punch")]
    [SerializeField] private bool _enablePunchAnimation = true;
    [SerializeField] private Vector3 _punchScale = new(0.15f, 0.15f, 0);
    [SerializeField] private float _punchDuration = 0.35f;
    [SerializeField] private int _punchVibrato = 6;

    [Header("Legacy shake")]
    [SerializeField] private bool _enableShakeAnimation;
    [SerializeField] private Vector3 _shakeStrength = new(5, 0, 0);
    [SerializeField] private float _shakeDuration = 0.4f;
    [SerializeField] private int _shakeVibrato = 10;

    private Vector3 initialPosition;
    private Tween shakeTween;
    private int shakeRevision;

    public TweenPreset PunchPreset {get => punchPreset; set => punchPreset = value;}
    public TweenPreset ShakePreset {get => shakePreset; set => shakePreset = value;}
    public AudioSource SoundSource {get => soundSource; set => soundSource = value;}
    public AudioClip ClickSfx {get => clickSfx; set => clickSfx = value;}
    public AudioMixerGroup MixerGroup {get => mixerGroup; set => mixerGroup = value;}
    public Tween CurrentShake => shakeTween;
    public override bool ReducedMotion
    {
        get => base.ReducedMotion;
        set
        {
            base.ReducedMotion = value;
            if (value) StopShake();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        initialPosition = transform.localPosition;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopShake();
    }

    protected override void Update()
    {
        base.Update();
        if (!CanInteract) StopShake();
    }

    protected override Vector3 StateScale(TweenPreset preset)
    {
        if (preset != null) return base.StateScale(preset);
        float factor = Pressed && _enablePressAnimation ? _pressScaleFactor : Highlighted && _enableHoverAnimation ? _hoverScaleFactor : 1;

        return InitialScale * factor;
    }

    protected override float StateDuration(TweenPreset preset)
    {
        if (preset != null) return base.StateDuration(preset);

        return Pressed && _enablePressAnimation ? _pressDuration : _hoverDuration;
    }

    protected override Ease StateEase(TweenPreset preset)
    {
        if (preset != null) return base.StateEase(preset);

        return Pressed ? Ease.OutQuad : Ease.OutBack;
    }

    protected override void PlayClick()
    {
        PlayClickAnimation();
    }

    public void PlayClickAnimation()
    {
        if (!CanInteract) {RefreshState(); return;}
        if (ClickPreset != null) {if (!ValidPreset(ClickPreset)) return; base.PlayClick(); PlaySound(); return;}
        if (ReducedMotion || (punchPreset == null && !_enablePunchAnimation)) {RefreshState(); PlaySound(); return;}
        Tween candidate;
        if (punchPreset != null)
        {
            candidate = CreateFeedback(punchPreset, TweenEffect.PunchScale);
            if (candidate == null) return;
        }
        else
        {
            if (!ValidLegacy(_punchDuration, _punchScale, _punchVibrato, "Punch")) return;
            candidate = transform.DOPunchScale(Vector3.Scale(InitialScale, _punchScale), _punchDuration, _punchVibrato).Pause().SetUpdate(true).SetAutoKill(true).SetRecyclable(false);
        }
        Vector3 resting = StateScale(StatePreset);
        int request = StopAnimation();
        if (request != ScaleRevision) {candidate.Kill(); return;}
        scaleTween = candidate;
        candidate.OnComplete(() => {if (request != ScaleRevision) return; scaleTween = null; transform.localScale = resting;});
        candidate.SetLink(gameObject, LinkBehaviour.KillOnDestroy).Play();
        PlaySound();
    }

    public void PlayShakeAnimation()
    {
        if (!CanInteract || ReducedMotion) return;
        Tween candidate;
        if (shakePreset != null)
        {
            candidate = CreateFeedback(shakePreset, TweenEffect.ShakePosition);
            if (candidate == null) return;
        }
        else
        {
            if (!_enableShakeAnimation) return;
            if (!ValidLegacy(_shakeDuration, _shakeStrength, _shakeVibrato, "Shake")) return;
            candidate = transform.DOShakePosition(_shakeDuration, _shakeStrength, _shakeVibrato).Pause().SetUpdate(true).SetAutoKill(true).SetRecyclable(false);
        }
        int request = StopShake();
        if (request != shakeRevision) {candidate.Kill(); return;}
        shakeTween = candidate;
        candidate.OnComplete(() => {if (request != shakeRevision) return; shakeTween = null; transform.localPosition = initialPosition;});
        candidate.SetLink(gameObject, LinkBehaviour.KillOnDestroy).Play();
    }

    private Tween CreateFeedback(TweenPreset preset, TweenEffect expected)
    {
        if (preset.Effect != expected) {Debug.LogError($"AnimatedButton {name}: preset {preset.name}, нужен Effect={expected}, получен {preset.Effect}", this); return null;}
        try {return GoofyTween.Create(transform, preset).SetLoops(1);}
        catch (ArgumentException exception) {Debug.LogError($"AnimatedButton {name}: preset {preset.name}, {exception.Message}", this); return null;}
    }

    private bool ValidLegacy(float duration, Vector3 vector, int vibrato, string operation)
    {
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0 || float.IsNaN(vector.sqrMagnitude) || float.IsInfinity(vector.sqrMagnitude) || vibrato < 1)
        {Debug.LogError($"AnimatedButton {name}: {operation}, Duration={duration}, Vector={vector}, Vibrato={vibrato}", this); return false;}

        return true;
    }

    private int StopShake()
    {
        int request = ++shakeRevision;
        var previous = shakeTween;
        shakeTween = null;
        if (previous == null) return request;
        if (previous.IsActive()) previous.Kill();
        if (request == shakeRevision) transform.localPosition = initialPosition;

        return request;
    }

    private void PlaySound()
    {
        if (clickSfx == null) return;
        if (soundSource == null || !soundSource.isActiveAndEnabled) {Debug.LogWarning($"AnimatedButton {name}: clickSfx задан, активный SoundSource не задан", this); return;}
        soundSource.outputAudioMixerGroup = mixerGroup;
        soundSource.PlayOneShot(clickSfx);
    }
}
