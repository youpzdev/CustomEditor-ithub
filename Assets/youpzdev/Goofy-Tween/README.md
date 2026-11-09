# Goofy Tween

Shared animation presets and scene components for Unity with DOTween Free.
Developed against Unity 6000.1.17f1 and DOTween 1.3.045. DOTween Pro and other
Goofy libraries are not required. This repository does not include DOTween.

Install DOTween from https://dotween.demigiant.com/download.php, then copy this
library to Assets/youpzdev/Goofy-Tween. The editor menus use that location.
Runtime needs Scripts and the presets you use. The editor menus also need
Demo/Scripts, which contains their generated scene components. Saved demo scenes,
prefabs, art, screenshots and Tests are optional. Tests require Unity Test Framework.
The saved demo uses Unity Input System, already installed in Goofy Systems. For a
legacy input project, keep Demo/Scripts, omit Demo/Scenes and Demo/Prefabs, then
create a fresh scene from Tools/Goofy/Tween. The generator chooses the enabled
input backend. Runtime UI components use uGUI; the counter uses
UnityEngine.UI.Text and exposes an event for other renderers.

## Shared presets

Create a preset with Assets/Create/Goofy/Tween Preset. The Tools/Goofy/Tween menu
creates a starter library of presets, a demo scene, or opens the existing demo.
Existing presets and the demo are preserved when those commands are repeated.

Presets define the effect, duration, delay, ease, clock and loops. They remain
shared assets: starting or stopping playback never modifies them. Relative Move
and Rotate use offsets; relative Scale uses a multiplier of the target's scale.
LocalSpace chooses local or world coordinates. UI panel offsets use anchored
coordinates. The supplied assets cover all fourteen effects:

Move, Rotate, Scale, Fade, Color, PunchScale, ShakePosition, Jump, Path, Pulse,
Float, Spin, Wobble and Heartbeat. Path points are copied before use. Fade
requires CanvasGroup, Graphic or SpriteRenderer; Color requires Graphic or
SpriteRenderer. These animations write Transform properties directly.

Relative effects capture their baseline when playback starts, including delayed
sequence steps. Zero-duration Punch and Shake finish at the unchanged baseline;
zero-duration Jump reaches its endpoint without retaining jump height.

## Code and scene playback

```csharp
Tween tween = GoofyTween.Play(transform, movePreset);
tween.Pause();
tween.Play();
tween.Kill();

Tween paused = GoofyTween.Create(transform, movePreset);
DOTween.Sequence().Append(paused).Play();
```

Create returns a paused tween without a lifetime link, ready to compose.
Play starts it with a KillOnDisable link to its target GameObject. The raw API
returns native DOTween objects; the caller owns competing raw animations on
the same property. It never kills unrelated tweens or changes DOTween defaults.
Use TweenPlayer for an Inspector-assigned preset and owned playback.

```csharp
player.Play();
player.Stop(TweenStopMode.KeepCurrent);
player.Stop(TweenStopMode.RestoreInitial);
player.Stop(TweenStopMode.Complete);
```

TweenPlayer replaces its own playback from the current state. CaptureInitialState
records a new restoration baseline, useful after setting up a pooled instance.
Disabling the component stops playback and restores the affected property.
ReducedMotion finishes active and new transitions immediately. Infinite idle
playback finishes its current effect cycle when explicitly completed.

TweenSequence combines entries with Target and Preset. JoinPrevious starts a
step alongside the previous step; Delay offsets that entry, and Stagger adds
spacing to subsequent entries. Child loops must be finite. The sequence owns
its clock through UnscaledTime because DOTween sequences control child updates.
Invalid entries leave existing playback intact. Stop modes match TweenPlayer.

## Panels and buttons

TweenPanel owns a RectTransform and CanvasGroup. ShowPreset and HidePreset can
use Fade, Scale or Move. Show and Hide continue from the visible state when
interrupted. Hidden panels remain active with alpha zero and raycasts disabled;
the component does not own a navigation stack. Interaction is blocked during
transitions and enabled after showing. OnShown and OnHidden describe completed
transitions, not cancelled ones. OnDisable restores the captured initial state.
Content and Group can reference a child RectTransform and a separate CanvasGroup.

TweenButton works alongside a Selectable. HoverPreset, PressPreset and
ClickPreset use Scale: relative presets multiply the captured initial scale,
while absolute presets use Vector directly. Pointer and keyboard selection
share one owned scale transition. Disabled controls ignore activation and return
to their initial scale. Button and panel transitions are finite single passes;
loop settings are unused. They use preset duration, delay, ease and clock.
Null presets on these two base components apply states immediately.
ReducedMotion also completes transitions immediately.

UI components explicitly start their owned animations, enable AutoKill and
disable recycling for retained tween handles. Changing DOTween's global defaults
does not turn completed handles into unrelated animations or leave them active
without an owner. The components do not change those global settings.

### AnimatedButton

AnimatedButton extends TweenButton with the full button feedback scenario.
It preserves PlayClickAnimation() and PlayShakeAnimation() from the original
component. Pointer click and keyboard submit already call PlayClickAnimation;
do not also bind that same method to Button.onClick for the same activation.

Assign inherited HoverPreset, PressPreset and ClickPreset for scale states.
PunchPreset uses PunchScale, and ShakePreset uses ShakePosition. ClickPreset
replaces punch feedback when assigned. Scale and punch share one owned channel;
position shake has its own channel. Disable restores the captured non-unit
scale and position and stops only those owned animations. Shared presets remain
unchanged, including when the component limits feedback to a finite pass.

The original press, hover, punch and shake fields remain as legacy defaults
when the corresponding preset is absent. These use raw DOTween without creating
runtime preset assets. New scenes can use shared preset assets instead.

ClickSfx and MixerGroup are optional. Configure SoundSource with an existing
AudioSource when using sound. Missing or inactive sources are diagnosed;
the component neither creates an audio source nor requires Goofy Audio.

### AnimatedPanel and UIPanel

AnimatedPanel extends the same TweenPanel engine with navigation and the original
UIPanel animation choices. Its nested PanelAnimationType contains all fifteen
original values:

FadeOnly, FadeScale, SlideFromBottom, SlideFromTop, SlideFromLeft,
SlideFromRight, PopBounce, ElasticFromBottom, ElasticFromTop, FlipHorizontal,
FlipVertical, RotateFadeIn, ZoomOut, ShakeAppear and SwingIn.

AnimationType chooses the geometry. ShowPreset and HidePreset supply shared
timing. A Scale preset supplies the hidden scale, a Move preset the anchored
offset or absolute position, and a Rotate preset the rotation. Relative values
are measured from the captured baseline. Other combinations keep the selected
style's legacy geometry. Without presets, Duration and the original style
values provide the transition; null does not disable AnimatedPanel animation.

```csharp
panel.AnimationType = AnimatedPanel.PanelAnimationType.FlipVertical;
panel.Show();
panel.Hide();
AnimatedPanel.BackTopmost();
```

Show reactivates an inactive panel and brings it to the top without duplicate
stack entries. Hide removes it from the stack immediately and deactivates its
GameObject only after completion. Reopening during Hide cancels that old
completion; an OnHidden listener can reopen the panel safely. Disable and
destruction remove stack entries. The static stack resets for a new Play session,
including when domain reload is disabled.

Topmost and CountOpen expose the current stack. Back only acts on the top panel,
invokes OnBackPressed once for that request and then hides it. Recursive Back
from that callback is ignored. A UI back button can invoke BackTopmost or Back.
Escape uses the enabled input backend, with no hard dependency on Input System.
ShowSfx and Mixer use an explicitly assigned SoundSource, like AnimatedButton.

UIPanel is the compatibility component for the old class name and nested enum.
It maps UIPanel.PanelAnimationType to AnimatedPanel.PanelAnimationType and runs
the same engine. Existing Show and Hide calls remain valid. When migrating,
replace the original scripts instead of keeping two classes with the same name.
Check Content, Group and the optional audio source references after import.

## Animated icons

AnimatedIcon retains IdleAnimation, PlayAnimation(IdleAnimation) and
StopAnimation(). It delegates owned playback to its required TweenPlayer.
Configure Bindings with one shared TweenPreset per non-None selection:

```csharp
icon.Bindings = new[]
{
    new AnimatedIcon.AnimationBinding
    {
        Animation = AnimatedIcon.IdleAnimation.Pulse,
        Preset = pulsePreset
    }
};
icon.PlayAnimation(AnimatedIcon.IdleAnimation.Pulse);
```

The choices are None, Bounce, Pulse, Wobble, Float, Shake, Spin and Heartbeat.
The binding supplies the effect and timing; use looping presets for idle motion.
Speed changes this icon's playback rate without modifying the shared asset.
Target defaults to the icon transform. PlayOnEnable uses SelectedAnimation;
None stops playback. StopAnimation and disabling restore the captured state.
Use the player's ReducedMotion setting when reduced motion is required.

The original icon duration, strength and ease fields are replaced by shared
bindings. Select the matching preset during migration; those old scalar values
do not create or modify preset assets automatically.

## Values

```csharp
health.SetImmediate(1);
health.SetValue(0.35f);
slider.SetValue(75);
counter.SetValue(1250);
```

TweenSlider, TweenFill and TweenCounter replace their own transition from the
displayed value. Preset supplies timing, ease and the clock; its effect and loop
settings are unused for value transitions. SetImmediate initializes or resets
without a preset. Stop leaves the visible value, while Stop(true) completes it.
Disabling stops playback. ReducedMotion applies final values immediately.

TweenSlider clamps to the Slider range and uses SetValueWithoutNotify by default.
Enable NotifyValueChanged when animation updates should invoke gameplay listeners.
TweenFill clamps to 0..1. Its optional SecondaryTarget trails damage after
SecondaryDelay; healing immediately updates that secondary target. Use Filled
Image mode and a fill-capable sprite. Retargeting preserves both displayed values.

TweenCounter owns Value and formats its Text with NumberFormat and InvariantCulture.
It does not parse pre-existing text. Initialize it with SetImmediate. OnValueChanged
can drive another text component. Invalid formats, missing references and invalid
numbers are diagnosed before replacing a valid transition.

## Verification and portability

Tests use native DOTween against real Unity transforms and UI components.
The demo is saved at Demo/Scenes/GoofyTweenDemo.unity. Open it with the menu,
enter Play Mode and exercise interruption, values and Reduced Motion. All APIs
are main-thread only. Invalid core arguments throw; scene components log setup
errors with their object and field. No fallback animation engine is included.

MIT license applies to this library. DOTween has its own license:
https://dotween.demigiant.com/license.php.
