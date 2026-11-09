public class UIPanel : AnimatedPanel
{
    public new enum PanelAnimationType
    {
        FadeOnly, FadeScale,
        SlideFromBottom, SlideFromTop, SlideFromLeft, SlideFromRight,
        PopBounce, ElasticFromBottom, ElasticFromTop,
        FlipHorizontal, FlipVertical,
        RotateFadeIn, ZoomOut, ShakeAppear, SwingIn
    }

    public new PanelAnimationType AnimationType
    {
        get => (PanelAnimationType)base.AnimationType;
        set => base.AnimationType = (AnimatedPanel.PanelAnimationType)value;
    }
}
