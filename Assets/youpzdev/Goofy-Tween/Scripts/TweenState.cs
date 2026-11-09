using UnityEngine;
using UnityEngine.UI;

internal class TweenState
{
    internal Transform Target => target;

    private readonly Transform target;
    private readonly Vector3 localPosition;
    private readonly Vector3 position;
    private readonly Quaternion localRotation;
    private readonly Quaternion rotation;
    private readonly Vector3 scale;

    private readonly CanvasGroup group;
    private readonly Graphic graphic;
    private readonly SpriteRenderer sprite;
    private readonly float alpha;
    private readonly Color graphicColor;
    private readonly Color spriteColor;

    internal TweenState(Transform target)
    {
        this.target = target;
        localPosition = target.localPosition;
        position = target.position;
        localRotation = target.localRotation;
        rotation = target.rotation;
        scale = target.localScale;
        group = target.GetComponent<CanvasGroup>();
        graphic = target.GetComponent<Graphic>();
        sprite = target.GetComponent<SpriteRenderer>();
        if (group != null) alpha = group.alpha;
        if (graphic != null) graphicColor = graphic.color;
        if (sprite != null) spriteColor = sprite.color;
    }

    internal void Restore(TweenEffect effect, bool local)
    {
        if (target == null) return;
        switch (effect)
        {
            case TweenEffect.Move:
            case TweenEffect.Float:
            case TweenEffect.Jump:
            case TweenEffect.Path:
            case TweenEffect.ShakePosition:
                if (local) target.localPosition = localPosition;
                else target.position = position;
                break;
            case TweenEffect.Rotate:
            case TweenEffect.Spin:
            case TweenEffect.Wobble:
                if (local) target.localRotation = localRotation;
                else target.rotation = rotation;
                break;
            case TweenEffect.Scale:
            case TweenEffect.Pulse:
            case TweenEffect.Heartbeat:
            case TweenEffect.PunchScale:
                target.localScale = scale;
                break;
            case TweenEffect.Fade:
                if (group != null) group.alpha = alpha;
                else if (graphic != null) {var color = graphic.color; color.a = graphicColor.a; graphic.color = color;}
                else if (sprite != null) {var color = sprite.color; color.a = spriteColor.a; sprite.color = color;}
                break;
            case TweenEffect.Color:
                if (graphic != null) graphic.color = graphicColor;
                else if (sprite != null) sprite.color = spriteColor;
                break;
        }
    }
}
