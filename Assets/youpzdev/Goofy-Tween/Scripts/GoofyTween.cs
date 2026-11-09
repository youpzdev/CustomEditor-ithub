using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class GoofyTween
{
    internal static bool IsFinite(float value) {return !float.IsNaN(value) && !float.IsInfinity(value);}

    public static Tween Create(Transform target, TweenPreset preset)
    {
        Validate(target, preset);
        Tween tween = Build(target, preset);
        tween.SetDelay(preset.Delay).SetEase(preset.Ease).SetLoops(preset.Loops, preset.LoopType);
        tween.SetUpdate(preset.UnscaledTime).SetTarget(target).SetAutoKill(true).SetRecyclable(false).Pause();

        return tween;
    }

    public static Tween Play(Transform target, TweenPreset preset)
    {
        Tween tween = Create(target, preset);
        tween.SetLink(target.gameObject, LinkBehaviour.KillOnDisable).Play();

        return tween;
    }

    internal static void ValidateTiming(TweenPreset preset)
    {
        if (preset == null) throw new ArgumentNullException(nameof(preset), "GoofyTween: пресет не задан");
        CheckFinite(preset.Duration, nameof(preset.Duration));
        CheckFinite(preset.Delay, nameof(preset.Delay));
        if (preset.Duration < 0) throw Invalid(nameof(preset.Duration), "ожидается значение не меньше нуля");
        if (preset.Delay < 0) throw Invalid(nameof(preset.Delay), "ожидается значение не меньше нуля");
        CheckEnum(preset.Ease, nameof(preset.Ease));
        if (preset.Ease == Ease.INTERNAL_Custom || preset.Ease == Ease.INTERNAL_Zero) throw Invalid(nameof(preset.Ease), "служебный режим DOTween нельзя задать в пресете");
    }

    private static Tween Build(Transform target, TweenPreset preset)
    {
        Vector3 vector = preset.Vector;
        float duration = preset.Duration;
        if (duration == 0)
        {
            if (preset.Effect == TweenEffect.PunchScale || preset.Effect == TweenEffect.ShakePosition) return DOTween.To(() => 0f, value => {}, 1f, 0);
            if (preset.Effect == TweenEffect.Jump) return Move(target, vector, 0, preset.LocalSpace).SetRelative(preset.Relative);
        }

        switch (preset.Effect)
        {
            case TweenEffect.Move: return Move(target, vector, duration, preset.LocalSpace).SetRelative(preset.Relative);
            case TweenEffect.Rotate:
            case TweenEffect.Spin: return Rotate(target, vector, duration, preset.LocalSpace).SetRelative(preset.Relative);
            case TweenEffect.Scale:
                if (!preset.Relative) return target.DOScale(vector, duration);
                Vector3 baseline = Vector3.one;

                return DOTween.To(() => {baseline = target.localScale; return Vector3.one;}, factor => target.localScale = Vector3.Scale(baseline, factor), vector, duration);
            case TweenEffect.Fade: return Fade(target, preset.Amount, duration);
            case TweenEffect.Color: return Color(target, preset.Color, duration);
            case TweenEffect.PunchScale: return target.DOPunchScale(vector, duration, preset.Vibrato, preset.Elasticity);
            case TweenEffect.ShakePosition:
                return preset.LocalSpace ? target.DOShakePosition(duration, vector, preset.Vibrato, preset.Randomness, false, preset.FadeOut) : DOTween.Shake(() => target.position, value => target.position = value, duration, vector, preset.Vibrato, preset.Randomness, preset.FadeOut, ShakeRandomnessMode.Full);
            case TweenEffect.Jump:
                Tween jump = preset.LocalSpace ? target.DOLocalJump(vector, preset.Amount, preset.Jumps, duration) : target.DOJump(vector, preset.Amount, preset.Jumps, duration);

                return jump.SetRelative(preset.Relative);
            case TweenEffect.Path:
                var points = (Vector3[])preset.Points.Clone();
                Tween path = preset.LocalSpace ? target.DOLocalPath(points, duration, preset.PathType, preset.PathMode) : target.DOPath(points, duration, preset.PathType, preset.PathMode);

                return path.SetRelative(preset.Relative);
            case TweenEffect.Pulse:
            case TweenEffect.Float:
            case TweenEffect.Wobble:
            case TweenEffect.Heartbeat: return Cycle(target, preset);
            default: throw Invalid(nameof(preset.Effect), "неизвестный эффект");
        }
    }

    private static Tween Cycle(Transform target, TweenPreset preset)
    {
        TweenEffect effect = preset.Effect;
        Vector3 vector = preset.Vector;
        bool relative = preset.Relative;
        bool localSpace = preset.LocalSpace;
        bool scale = effect == TweenEffect.Pulse || effect == TweenEffect.Heartbeat;
        Vector3 baseline = Vector3.zero;
        Vector3 destination = Vector3.zero;

        return DOTween.To(() =>
        {
            baseline = scale ? target.localScale : effect == TweenEffect.Float ? localSpace ? target.localPosition : target.position : localSpace ? target.localEulerAngles : target.eulerAngles;
            destination = relative ? scale ? Vector3.Scale(baseline, vector) : baseline + vector : vector;

            return 0f;
        }, progress =>
        {
            Vector3 value = Vector3.LerpUnclamped(baseline, destination, CycleWeight(effect, progress));
            if (scale) target.localScale = value;
            else if (effect == TweenEffect.Float)
            {
                if (localSpace) target.localPosition = value;
                else target.position = value;
            }
            else if (localSpace) target.localRotation = Quaternion.Euler(value);
            else target.rotation = Quaternion.Euler(value);
        }, 1f, preset.Duration);
    }

    private static float CycleWeight(TweenEffect effect, float progress)
    {
        if (effect == TweenEffect.Wobble)
        {
            if (progress <= 0.25f) return progress * 4;
            if (progress <= 0.75f) return 2 - progress * 4;

            return progress * 4 - 4;
        }
        if (effect == TweenEffect.Heartbeat)
        {
            if (progress <= 0.2f) return progress / 0.2f;
            if (progress <= 0.35f) return Mathf.LerpUnclamped(1, 0.3f, (progress - 0.2f) / 0.15f);
            if (progress <= 0.55f) return Mathf.LerpUnclamped(0.3f, 1, (progress - 0.35f) / 0.2f);

            return (1 - progress) / 0.45f;
        }

        return progress <= 0.5f ? progress * 2 : (1 - progress) * 2;
    }

    private static Tween Move(Transform target, Vector3 value, float duration, bool localSpace) {return localSpace ? target.DOLocalMove(value, duration) : target.DOMove(value, duration);}
    private static Tween Rotate(Transform target, Vector3 value, float duration, bool localSpace) {return localSpace ? target.DOLocalRotate(value, duration, RotateMode.FastBeyond360) : target.DORotate(value, duration, RotateMode.FastBeyond360);}

    private static Tween Fade(Transform target, float alpha, float duration)
    {
        var group = target.GetComponent<CanvasGroup>();
        if (group != null) return DOTween.To(() => group.alpha, value => group.alpha = value, alpha, duration);
        var graphic = target.GetComponent<Graphic>();
        if (graphic != null) return DOTween.ToAlpha(() => graphic.color, value => graphic.color = value, alpha, duration);
        var sprite = target.GetComponent<SpriteRenderer>();

        return DOTween.ToAlpha(() => sprite.color, value => sprite.color = value, alpha, duration);
    }

    private static Tween Color(Transform target, UnityEngine.Color color, float duration)
    {
        var graphic = target.GetComponent<Graphic>();
        if (graphic != null) return DOTween.To(() => graphic.color, value => graphic.color = value, color, duration);
        var sprite = target.GetComponent<SpriteRenderer>();

        return DOTween.To(() => sprite.color, value => sprite.color = value, color, duration);
    }

    internal static void Validate(Transform target, TweenPreset preset)
    {
        if (target == null) throw new ArgumentNullException(nameof(target), "GoofyTween: объект не задан");
        ValidateTiming(preset);
        CheckEnum(preset.Effect, nameof(preset.Effect));
        CheckEnum(preset.LoopType, nameof(preset.LoopType));
        CheckEnum(preset.PathType, nameof(preset.PathType));
        CheckEnum(preset.PathMode, nameof(preset.PathMode));
        if (preset.Loops != -1 && preset.Loops < 1) throw Invalid(nameof(preset.Loops), "ожидается -1 или положительное число");
        if (preset.Loops == -1 && preset.Duration == 0) throw Invalid(nameof(preset.Loops), "бесконечный цикл требует Duration больше нуля");
        CheckFinite(preset.Vector, nameof(preset.Vector));
        CheckFinite(preset.Amount, nameof(preset.Amount));
        CheckFinite(preset.Color.r, "Color.r");
        CheckFinite(preset.Color.g, "Color.g");
        CheckFinite(preset.Color.b, "Color.b");
        CheckFinite(preset.Color.a, "Color.a");
        CheckFinite(preset.Elasticity, nameof(preset.Elasticity));
        CheckFinite(preset.Randomness, nameof(preset.Randomness));
        if (preset.Vibrato < 1) throw Invalid(nameof(preset.Vibrato), "ожидается положительное число");
        if (preset.Jumps < 1) throw Invalid(nameof(preset.Jumps), "ожидается положительное число");
        if (preset.Elasticity < 0 || preset.Elasticity > 1) throw Invalid(nameof(preset.Elasticity), "ожидается значение от нуля до единицы");
        if (preset.Randomness < 0 || preset.Randomness > 180) throw Invalid(nameof(preset.Randomness), "ожидается значение от нуля до 180");
        if (preset.Effect == TweenEffect.Fade && (preset.Amount < 0 || preset.Amount > 1)) throw Invalid(nameof(preset.Amount), "прозрачность должна быть от нуля до единицы");
        if (preset.Effect == TweenEffect.Jump && preset.Amount < 0) throw Invalid(nameof(preset.Amount), "высота прыжка не может быть отрицательной");
        if (preset.Effect == TweenEffect.Fade && target.GetComponent<CanvasGroup>() == null && target.GetComponent<Graphic>() == null && target.GetComponent<SpriteRenderer>() == null)
            throw Invalid(nameof(target), "для Fade нужен CanvasGroup, Graphic или SpriteRenderer");
        if (preset.Effect == TweenEffect.Color && target.GetComponent<Graphic>() == null && target.GetComponent<SpriteRenderer>() == null)
            throw Invalid(nameof(target), "для Color нужен Graphic или SpriteRenderer");
        if (preset.Effect == TweenEffect.Path && (preset.Points == null || preset.Points.Length == 0)) throw Invalid(nameof(preset.Points), "путь не может быть пустым");
        if (preset.Effect == TweenEffect.Path && preset.PathType == PathType.CubicBezier && preset.Points.Length % 3 != 0)
            throw Invalid(nameof(preset.Points), "CubicBezier требует группы по три точки");
        if (preset.Points != null)
            for (int i = 0; i < preset.Points.Length; i++) CheckFinite(preset.Points[i], $"Points[{i}]");
        bool sequence = preset.Effect == TweenEffect.Jump || preset.Effect == TweenEffect.Pulse || preset.Effect == TweenEffect.Float || preset.Effect == TweenEffect.Wobble || preset.Effect == TweenEffect.Heartbeat;
        if (sequence && preset.LoopType == LoopType.Incremental) throw Invalid(nameof(preset.LoopType), "Incremental не поддерживается составным эффектом");
    }

    private static void CheckEnum<T>(T value, string field) where T : Enum
    {
        if (!Enum.IsDefined(typeof(T), value)) throw Invalid(field, "неизвестное значение");
    }

    private static void CheckFinite(Vector3 value, string field)
    {
        CheckFinite(value.x, field + ".x");
        CheckFinite(value.y, field + ".y");
        CheckFinite(value.z, field + ".z");
    }

    private static void CheckFinite(float value, string field)
    {
        if (!IsFinite(value)) throw Invalid(field, "ожидается конечное число");
    }

    private static ArgumentException Invalid(string field, string reason) {return new ArgumentException($"GoofyTween: {field}, {reason}", field);}
}
