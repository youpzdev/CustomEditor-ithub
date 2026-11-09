using UnityEngine;
using UnityEngine.UI;

public class GoofyTweenDemo : MonoBehaviour
{
    [Header("Transitions")]
    [SerializeField] private TweenPanel panel;
    [SerializeField] private TweenPlayer movement;
    [SerializeField] private TweenPlayer idle;
    [SerializeField] private AnimatedIcon animatedIcon;
    [SerializeField] private TweenSequence sequence;
    [SerializeField] private TweenButton[] buttons;

    [Space(5)]
    [Header("Panel style presets")]
    [SerializeField] private TweenPreset standardPanelShow;
    [SerializeField] private TweenPreset elasticPanelShow;
    [SerializeField] private TweenPreset popPanelShow;
    [SerializeField] private TweenPreset scalePanelShow;
    [SerializeField] private TweenPreset panelStyleHide;

    [Space(5)]
    [Header("Values")]
    [SerializeField] private TweenFill health;
    [SerializeField] private TweenSlider slider;
    [SerializeField] private TweenCounter counter;
    [SerializeField] private Text healthLabel;
    [SerializeField] private Text status;
    [SerializeField] private Text panelStyleLabel;
    [SerializeField] private Text idleLabel;

    private float healthTarget = 1;
    private float scoreTarget = 1250;

    private void Start()
    {
        if (!panel || !movement || !idle || !sequence || !health || !slider || !counter || !healthLabel || !status)
        {Debug.LogError($"Demo {name}: не назначены ссылки компонентов", this); enabled = false; return;}
        health.SetImmediate(healthTarget);
        slider.SetImmediate(healthTarget * 100);
        counter.SetImmediate(scoreTarget);
        ApplyPanelProfile();
        RefreshLabels();
        status.text = "Choose a demo. Click again to interrupt.";
    }

    public void ShowPanel() {panel.Show(); status.text = "Panel shown";}
    public void HidePanel() {panel.Hide(); status.text = "Panel hidden. Show brings it back.";}

    public void NextPanelStyle()
    {
        if (!(panel is AnimatedPanel animated)) {Debug.LogError($"Demo {name}: panel не содержит AnimatedPanel", this); return;}
        int count = System.Enum.GetValues(typeof(AnimatedPanel.PanelAnimationType)).Length;
        animated.AnimationType = (AnimatedPanel.PanelAnimationType)(((int)animated.AnimationType + 1) % count);
        ApplyPanelProfile();
        RefreshLabels();
        status.text = $"Selected {animated.AnimationType}. Use Show / Hide.";
    }

    public void ApplyPanelProfile()
    {
        if (!(panel is AnimatedPanel animated)) {Debug.LogError($"Demo {name}: panel не содержит AnimatedPanel", this); return;}
        var show = standardPanelShow;
        if (animated.AnimationType == AnimatedPanel.PanelAnimationType.FadeScale) show = scalePanelShow;
        else if (animated.AnimationType == AnimatedPanel.PanelAnimationType.PopBounce) show = popPanelShow;
        else if (animated.AnimationType == AnimatedPanel.PanelAnimationType.ElasticFromBottom || animated.AnimationType == AnimatedPanel.PanelAnimationType.ElasticFromTop) show = elasticPanelShow;
        if (show == null || panelStyleHide == null)
        {Debug.LogError($"Demo {name}: не назначены style presets для {animated.AnimationType}", this); return;}
        animated.ShowPreset = show;
        animated.HidePreset = panelStyleHide;
    }

    public void NextIdle()
    {
        if (animatedIcon == null) {Debug.LogError($"Demo {name}: animatedIcon не задан", this); return;}
        int count = System.Enum.GetValues(typeof(AnimatedIcon.IdleAnimation)).Length;
        var animation = (AnimatedIcon.IdleAnimation)(((int)animatedIcon.SelectedAnimation + 1) % count);
        animatedIcon.PlayAnimation(animation);
        RefreshLabels();
        status.text = $"Idle: {animation}";
    }

    public void MoveObject()
    {
        movement.Stop(TweenStopMode.RestoreInitial);
        movement.Play();
        status.text = "Playing the shared Move preset";
    }

    public void ResetMovement() {movement.Stop(TweenStopMode.RestoreInitial); status.text = "Movement reset";}

    public void PlaySequence()
    {
        sequence.Stop(TweenStopMode.RestoreInitial);
        sequence.Play();
        status.text = "Move, then rotate and scale together";
    }

    public void ResetSequence() {sequence.Stop(TweenStopMode.RestoreInitial); status.text = "Sequence reset";}

    public void TakeDamage()
    {
        healthTarget = healthTarget > 0.4f ? 0.35f : 0.15f;
        scoreTarget += 250;
        UpdateValues();
        status.text = "Damage applied. The trail catches up.";
    }

    public void RestoreValues()
    {
        healthTarget = 1;
        scoreTarget = 1250;
        UpdateValues();
        status.text = "Health and score restored";
    }

    public void SetReducedMotion(bool reduced)
    {
        panel.ReducedMotion = reduced;
        movement.ReducedMotion = reduced;
        idle.ReducedMotion = reduced;
        sequence.ReducedMotion = reduced;
        health.ReducedMotion = reduced;
        slider.ReducedMotion = reduced;
        counter.ReducedMotion = reduced;
        foreach (var button in buttons)
            if (button != null) button.ReducedMotion = reduced;
        idle.Stop(TweenStopMode.RestoreInitial);
        if (reduced)
        {
            movement.Stop(TweenStopMode.Complete);
            sequence.Stop(TweenStopMode.Complete);
            if (panel.IsShown) panel.Show();
            else panel.Hide();
            UpdateValues();
        }
        else if (animatedIcon != null) animatedIcon.PlayAnimation(animatedIcon.SelectedAnimation);
        else idle.Play();
        status.text = reduced ? "Reduced motion on. Actions keep their result." : "Reduced motion off";
    }

    private void UpdateValues()
    {
        health.SetValue(healthTarget);
        slider.SetValue(healthTarget * 100);
        counter.SetValue(scoreTarget);
        healthLabel.text = $"HEALTH  {Mathf.RoundToInt(healthTarget * 100)} / 100";
    }

    private void RefreshLabels()
    {
        if (panelStyleLabel != null && panel is AnimatedPanel animated) panelStyleLabel.text = $"Style {(int)animated.AnimationType + 1}/15: {animated.AnimationType}";
        if (idleLabel != null && animatedIcon != null) idleLabel.text = $"Idle {(int)animatedIcon.SelectedAnimation + 1}/8: {animatedIcon.SelectedAnimation}";
    }
}
