using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GoofyTweenDemoBuilder
{
    public const string Root = "Assets/youpzdev/Goofy-Tween";
    public const string ScenePath = Root + "/Demo/Scenes/GoofyTweenDemo.unity";
    private const string ButtonPath = Root + "/Demo/Prefabs/ActionButton.prefab";

    private static Font font;

    [MenuItem("Tools/Goofy/Tween/Create Preset Library")]
    public static void CreatePresetLibrary()
    {
        EnsureFolder(Root + "/Presets");
        Preset("ButtonHover", TweenEffect.Scale, GoofyTweenDemoTokens.PressDuration, new Vector3(1.035f, 1.035f, 1));
        Preset("ButtonPress", TweenEffect.Scale, GoofyTweenDemoTokens.PressDuration, new Vector3(0.96f, 0.96f, 1));
        Preset("ButtonClick", TweenEffect.Scale, 0.18f, new Vector3(1.045f, 1.045f, 1));
        Preset("PanelShow", TweenEffect.Scale, GoofyTweenDemoTokens.StateDuration, new Vector3(0.95f, 0.95f, 1));
        Preset("PanelHide", TweenEffect.Scale, 0.2f, new Vector3(0.95f, 0.95f, 1));
        Preset("MoveRight", TweenEffect.Move, 0.7f, new Vector3(120, 0, 0), Ease.InOutCubic);
        Preset("RotateQuarter", TweenEffect.Rotate, 0.45f, new Vector3(0, 0, 90), Ease.InOutCubic);
        Preset("SequenceScale", TweenEffect.Scale, 0.45f, new Vector3(1.18f, 1.18f, 1));
        Preset("IdlePulse", TweenEffect.Pulse, 1.8f, new Vector3(1.07f, 1.07f, 1), Ease.InOutSine, -1);
        Preset("IdleFloat", TweenEffect.Float, 2, new Vector3(0, 8, 0), Ease.InOutSine, -1);
        Preset("IdleSpin", TweenEffect.Spin, 4, new Vector3(0, 0, 360), Ease.Linear, -1, LoopType.Incremental);
        Preset("ValuesSmooth", TweenEffect.Scale, GoofyTweenDemoTokens.ValueDuration, Vector3.one);
        Preset("FadeIn", TweenEffect.Fade, GoofyTweenDemoTokens.StateDuration, Vector3.one, amount: 1);
        Preset("FadeOut", TweenEffect.Fade, 0.18f, Vector3.one, amount: 0);
        Preset("ColorAccent", TweenEffect.Color, GoofyTweenDemoTokens.StateDuration, Vector3.one, color: GoofyTweenDemoTokens.Accent);
        Preset("PunchScale", TweenEffect.PunchScale, 0.4f, new Vector3(0.1f, 0.1f, 0));
        Preset("ShakeSmall", TweenEffect.ShakePosition, 0.35f, new Vector3(0.15f, 0.15f, 0));
        Preset("JumpRight", TweenEffect.Jump, 0.7f, new Vector3(2, 0, 0), amount: 1);
        Preset("PathArc", TweenEffect.Path, 1.5f, Vector3.zero, Ease.InOutSine,
            points: new[] {new Vector3(1, 0, 0), new Vector3(2, 1, 0), new Vector3(3, 0, 0)});
        Preset("IdleWobble", TweenEffect.Wobble, 1.4f, new Vector3(0, 0, 7), Ease.InOutSine, -1);
        Preset("IdleHeartbeat", TweenEffect.Heartbeat, 1.3f, new Vector3(1.08f, 1.08f, 1), Ease.Linear, -1);
        Preset("IdleBounce", TweenEffect.Jump, 0.8f, Vector3.zero, Ease.Linear, -1, amount: 8);
        Preset("IdleShake", TweenEffect.ShakePosition, 0.65f, new Vector3(1.5f, 1.5f, 0), Ease.Linear, -1);
        Preset("PanelStyleStandard", TweenEffect.Fade, 0.35f, Vector3.one, Ease.OutCubic, amount: 1);
        Preset("PanelStyleElastic", TweenEffect.Fade, 0.55f, Vector3.one, Ease.OutElastic, amount: 1);
        Preset("PanelStylePop", TweenEffect.Fade, 0.4f, Vector3.one, Ease.OutBack, amount: 1);
        Preset("PanelStyleScale", TweenEffect.Fade, GoofyTweenDemoTokens.StateDuration, Vector3.one, Ease.OutBack, amount: 1);
        Preset("PanelStyleHide", TweenEffect.Fade, 0.2f, Vector3.one, Ease.InCubic, amount: 0);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Goofy/Tween/Create Demo")]
    public static void CreateDemo()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Demo нельзя создавать в Play Mode");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (!existing.IsValid() || !existing.isLoaded) existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(existing);
            return;
        }
        CreatePresetLibrary();
        EnsureFolder(Root + "/Demo/Scenes");
        EnsureFolder(Root + "/Demo/Prefabs");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var current = SceneManager.GetActiveScene();
        bool emptyUntitled = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(current.path) && !current.isDirty;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, emptyUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var camera = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = GoofyTweenDemoTokens.Paper;
        camera.orthographic = true;
        var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        events.AddComponent<InputSystemUIInputModule>();
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        events.AddComponent<StandaloneInputModule>();
#endif

        CreateButtonPrefab();
        var canvas = UI("DemoCanvas", null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1;
        var background = Fill("Paper", canvas.transform, GoofyTweenDemoTokens.Paper);
        var controller = new GameObject("DemoControls").AddComponent<GoofyTweenDemo>();
        var buttons = new List<TweenButton>();

        BuildHeader(background.transform, controller);
        var content = BuildScroll(background.transform);
        var panel = BuildPanel(content, controller, buttons);
        var movement = BuildMovement(content, controller, buttons, out var idle, out var animatedIcon);
        BuildValues(content, controller, buttons, out var health, out var slider, out var counter, out var healthLabel);
        var sequence = BuildSequence(content, controller, buttons);
        events.GetComponent<EventSystem>().firstSelectedGameObject = buttons[0].gameObject;
        var status = Text("Status", background.transform, "Choose a demo. Click again to interrupt.", GoofyTweenDemoTokens.Label, GoofyTweenDemoTokens.Muted);
        var statusRect = status.rectTransform;
        statusRect.anchorMin = new Vector2(0, 0);
        statusRect.anchorMax = new Vector2(1, 0);
        statusRect.pivot = new Vector2(0.5f, 0);
        statusRect.offsetMin = new Vector2(GoofyTweenDemoTokens.PageInset, 4);
        statusRect.offsetMax = new Vector2(-GoofyTweenDemoTokens.PageInset, GoofyTweenDemoTokens.FooterHeight);
        SetReferences(controller, panel, movement, idle, sequence, health, slider, counter, healthLabel, status, buttons, animatedIcon,
            FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Panel/PanelStyleLabel").GetComponent<Text>(),
            FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Movement/MovementStage/IdleLabel").GetComponent<Text>());
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException($"Не удалось сохранить Demo: {ScenePath}");
        AssetDatabase.SaveAssets();
        Selection.activeObject = controller.gameObject;
    }

    [MenuItem("Tools/Goofy/Tween/Open Demo")]
    public static void OpenDemo()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Сначала останови Play Mode");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) {CreateDemo(); return;}
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Goofy/Tween/Upgrade Demo Components")]
    public static void UpgradeDemoComponents()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Сначала останови Play Mode перед обновлением Demo");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) {CreateDemo(); return;}
        CreatePresetLibrary();
        UpgradeButtonPrefab();
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var canvas = FindInScene(scene, "DemoCanvas");
        var demo = FindInScene(scene, "DemoControls").GetComponent<GoofyTweenDemo>();
        var preview = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Panel/InventoryPanel");
        var panel = preview.GetComponent<AnimatedPanel>();
        if (panel == null)
        {
            var previous = preview.GetComponent<TweenPanel>();
            if (previous == null) throw new InvalidOperationException("Upgrade Demo: InventoryPanel не содержит TweenPanel");
            var show = previous.ShowPreset;
            var hide = previous.HidePreset;
            bool reduced = previous.ReducedMotion;
            Object.DestroyImmediate(previous);
            panel = preview.gameObject.AddComponent<AnimatedPanel>();
            panel.ShowPreset = show;
            panel.HidePreset = hide;
            panel.ReducedMotion = reduced;
        }
        var idle = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Movement/MovementStage/IdleObject").GetComponent<TweenPlayer>();
        var icon = idle.GetComponent<AnimatedIcon>();
        if (icon == null) {icon = idle.gameObject.AddComponent<AnimatedIcon>(); ConfigureIcon(icon);}
        idle.PlayOnEnable = false;
        var buttons = new List<TweenButton>();
        foreach (var existing in canvas.GetComponentsInChildren<TweenButton>(true)) buttons.Add(UpgradeButton(existing));
        var panelActions = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Panel/Actions");
        if (!HasChild(panelActions, "NextstyleButton")) Button("Next style", panelActions, demo.NextPanelStyle, buttons);
        var movementActions = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Movement/Actions");
        if (!HasChild(movementActions, "NextidleButton")) Button("Next idle", movementActions, demo.NextIdle, buttons);
        foreach (var label in preview.GetComponentsInChildren<Text>())
            if (label.name == "InventoryTitle") label.text = "Animated panel";
            else if (label.name == "InventoryContent") label.text = "Shared show and hide presets";
        var panelSection = preview.parent;
        Text panelLabel;
        if (HasChild(panelSection, "PanelStyleLabel")) panelLabel = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Panel/PanelStyleLabel").GetComponent<Text>();
        else
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelLabel = CreateStyleLabel(panelSection);
        }
        panelLabel.text = $"Style {(int)panel.AnimationType + 1}/15: {panel.AnimationType}";
        var idleLabel = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Movement/MovementStage/IdleLabel").GetComponent<Text>();
        idleLabel.text = $"Idle {(int)icon.SelectedAnimation + 1}/8: {icon.SelectedAnimation}";
        var movement = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Movement/MovementStage/MovingObject").GetComponent<TweenPlayer>();
        var sequence = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Sequence/SequenceStage/SequenceObject").GetComponent<TweenSequence>();
        var health = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Values/HealthTrack").GetComponent<TweenFill>();
        var slider = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Values/ValueSlider").GetComponent<TweenSlider>();
        var counter = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Values/Score").GetComponent<TweenCounter>();
        var healthLabel = FindInScene(scene, "DemoCanvas/Paper/Examples/Viewport/Content/Values/HealthLabel").GetComponent<Text>();
        var status = FindInScene(scene, "DemoCanvas/Paper/Status").GetComponent<Text>();
        SetReferences(demo, panel, movement, idle, sequence, health, slider, counter, healthLabel, status, buttons, icon, panelLabel, idleLabel);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException($"Не удалось сохранить обновленную Demo: {ScenePath}");
        AssetDatabase.SaveAssets();
    }

    private static void UpgradeButtonPrefab()
    {
        var contents = PrefabUtility.LoadPrefabContents(ButtonPath);
        try
        {
            UpgradeButton(contents.GetComponent<TweenButton>());
            PrefabUtility.SaveAsPrefabAsset(contents, ButtonPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static AnimatedButton UpgradeButton(TweenButton previous)
    {
        if (previous is AnimatedButton existing) return existing;
        if (previous == null) throw new InvalidOperationException("Upgrade Demo: у кнопки отсутствует TweenButton");
        var owner = previous.gameObject;
        var hover = previous.HoverPreset;
        var press = previous.PressPreset;
        var click = previous.ClickPreset;
        bool reduced = previous.ReducedMotion;
        Object.DestroyImmediate(previous);
        var button = owner.AddComponent<AnimatedButton>();
        button.HoverPreset = hover;
        button.PressPreset = press;
        button.ClickPreset = click;
        button.ReducedMotion = reduced;
        button.PunchPreset = LoadPreset("PunchScale");
        button.ShakePreset = LoadPreset("IdleShake");

        return button;
    }

    private static void ConfigureIcon(AnimatedIcon icon)
    {
        icon.Target = icon.transform;
        icon.PlayOnEnable = true;
        icon.SelectedAnimation = AnimatedIcon.IdleAnimation.Pulse;
        icon.Player.PlayOnEnable = false;
        icon.Bindings = new[]
        {
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Bounce, Preset = LoadPreset("IdleBounce")},
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Pulse, Preset = LoadPreset("IdlePulse")},
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Wobble, Preset = LoadPreset("IdleWobble")},
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Float, Preset = LoadPreset("IdleFloat")},
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Shake, Preset = LoadPreset("IdleShake")},
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Spin, Preset = LoadPreset("IdleSpin")},
            new AnimatedIcon.AnimationBinding {Animation = AnimatedIcon.IdleAnimation.Heartbeat, Preset = LoadPreset("IdleHeartbeat")}
        };
    }

    private static Transform FindInScene(Scene scene, string path)
    {
        string[] names = path.Split('/');
        Transform current = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == names[0]) {current = root.transform; break;}
        for (int i = 1; i < names.Length && current != null; i++)
        {
            Transform next = null;
            foreach (Transform child in current)
                if (child.name == names[i]) {next = child; break;}
            current = next;
        }
        if (current == null) throw new InvalidOperationException($"Upgrade Demo: не найден {path} в {scene.path}");

        return current;
    }

    private static bool HasChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return true;

        return false;
    }

    private static TweenPreset Preset(string name, TweenEffect effect, float duration, Vector3 vector, Ease ease = Ease.OutCubic, int loops = 1, LoopType loopType = LoopType.Restart, float amount = 1, Vector3[] points = null, Color? color = null)
    {
        string path = Root + "/Presets/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<TweenPreset>(path);
        if (existing != null) return existing;
        var preset = ScriptableObject.CreateInstance<TweenPreset>();
        preset.name = name;
        preset.Effect = effect;
        preset.Duration = duration;
        preset.Vector = vector;
        preset.Amount = amount;
        preset.Color = color ?? Color.white;
        if (points != null) preset.Points = points;
        preset.Relative = true;
        preset.LocalSpace = true;
        preset.UnscaledTime = true;
        preset.Ease = ease;
        preset.Loops = loops;
        preset.LoopType = loopType;
        AssetDatabase.CreateAsset(preset, path);

        return preset;
    }

    private static TweenPreset LoadPreset(string name)
    {
        string path = Root + "/Presets/" + name + ".asset";
        var preset = AssetDatabase.LoadAssetAtPath<TweenPreset>(path);
        if (preset == null) throw new InvalidOperationException($"Не найден пресет {path}");

        return preset;
    }

    private static void BuildHeader(Transform parent, GoofyTweenDemo controller)
    {
        var eyebrow = Text("Eyebrow", parent, "PRESETS + COMPONENTS", GoofyTweenDemoTokens.Caption, GoofyTweenDemoTokens.Muted, FontStyle.Bold);
        Top(eyebrow.rectTransform, 24, 24, -24, 20);
        var title = Text("Title", parent, "Goofy Tween", GoofyTweenDemoTokens.Title, GoofyTweenDemoTokens.Ink, FontStyle.Bold);
        Top(title.rectTransform, 24, 47, -24, 48);
        var subtitle = Text("Subtitle", parent, "Reusable motion for Unity", GoofyTweenDemoTokens.Body, GoofyTweenDemoTokens.Muted);
        Top(subtitle.rectTransform, 24, 99, -24, 24);
        var toggle = UI("ReducedMotion", parent, typeof(Toggle));
        Top(toggle.GetComponent<RectTransform>(), 24, 133, 224, 32, false);
        var frame = Image("Frame", toggle.transform, GoofyTweenDemoTokens.Ink);
        Top(frame.rectTransform, 0, 4, 24, 24, false);
        var inset = Fill("Inset", frame.transform, GoofyTweenDemoTokens.Paper);
        inset.rectTransform.offsetMin = Vector2.one * 2;
        inset.rectTransform.offsetMax = Vector2.one * -2;
        var mark = Fill("Check", inset.transform, GoofyTweenDemoTokens.Accent);
        mark.rectTransform.offsetMin = Vector2.one * 4;
        mark.rectTransform.offsetMax = Vector2.one * -4;
        var label = Text("Label", toggle.transform, "Reduce motion", GoofyTweenDemoTokens.Body, GoofyTweenDemoTokens.Ink);
        Top(label.rectTransform, 36, 3, 220, 26, false);
        var component = toggle.GetComponent<Toggle>();
        component.targetGraphic = frame;
        component.graphic = mark;
        frame.raycastTarget = true;
        component.isOn = false;
        UnityEventTools.AddPersistentListener(component.onValueChanged, controller.SetReducedMotion);
    }

    private static RectTransform BuildScroll(Transform parent)
    {
        var scroll = UI("Examples", parent, typeof(ScrollRect));
        var rect = scroll.GetComponent<RectTransform>();
        Stretch(rect);
        rect.offsetMin = new Vector2(GoofyTweenDemoTokens.PageInset, GoofyTweenDemoTokens.FooterHeight + 4);
        rect.offsetMax = new Vector2(-GoofyTweenDemoTokens.PageInset, -GoofyTweenDemoTokens.HeaderHeight);
        var viewport = UI("Viewport", scroll.transform, typeof(UnityEngine.UI.Image), typeof(RectMask2D));
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        var content = UI("Content", viewport.transform, typeof(GridLayoutGroup), typeof(ContentSizeFitter), typeof(GoofyTweenDemoLayout));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        var grid = content.GetComponent<GridLayoutGroup>();
        grid.spacing = new Vector2(GoofyTweenDemoTokens.SectionGap, GoofyTweenDemoTokens.SectionGap);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.cellSize = new Vector2(660, GoofyTweenDemoTokens.SectionHeight);
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var component = scroll.GetComponent<ScrollRect>();
        component.viewport = viewport.GetComponent<RectTransform>();
        component.content = contentRect;
        component.horizontal = false;
        component.vertical = true;
        component.movementType = ScrollRect.MovementType.Clamped;
        component.scrollSensitivity = 35;

        return contentRect;
    }

    private static TweenPanel BuildPanel(Transform parent, GoofyTweenDemo controller, List<TweenButton> buttons)
    {
        var section = Section("Panel", parent, "01 / INTERFACE", "Panel transitions", "Show, hide, or change your mind mid-transition.");
        var preview = Image("InventoryPanel", section, GoofyTweenDemoTokens.Surface);
        Top(preview.rectTransform, 0, 114, 0, 126);
        var stripe = Image("Accent", preview.transform, GoofyTweenDemoTokens.Accent);
        Top(stripe.rectTransform, 18, 22, 4, 68, false);
        var title = Text("InventoryTitle", preview.transform, "Animated panel", GoofyTweenDemoTokens.Heading, GoofyTweenDemoTokens.Ink, FontStyle.Bold);
        Top(title.rectTransform, 38, 20, -16, 34);
        var subtitle = Text("InventoryContent", preview.transform, "Shared show and hide presets", GoofyTweenDemoTokens.Label, GoofyTweenDemoTokens.Muted);
        Top(subtitle.rectTransform, 38, 62, -16, 42);
        preview.gameObject.AddComponent<CanvasGroup>();
        var panel = preview.gameObject.AddComponent<AnimatedPanel>();
        panel.ShowPreset = LoadPreset("PanelShow");
        panel.HidePreset = LoadPreset("PanelHide");
        CreateStyleLabel(section).text = $"Style {(int)panel.AnimationType + 1}/15: {panel.AnimationType}";
        var row = ButtonRow(section);
        Button("Show", row, controller.ShowPanel, buttons, true);
        Button("Hide", row, controller.HidePanel, buttons);
        Button("Next style", row, controller.NextPanelStyle, buttons);

        return panel;
    }

    private static Text CreateStyleLabel(Transform parent)
    {
        var label = Text("PanelStyleLabel", parent, "", GoofyTweenDemoTokens.Caption, GoofyTweenDemoTokens.Muted);
        Top(label.rectTransform, 0, 246, 0, 20);

        return label;
    }

    private static TweenPlayer BuildMovement(Transform parent, GoofyTweenDemo controller, List<TweenButton> buttons, out TweenPlayer idle, out AnimatedIcon animatedIcon)
    {
        var section = Section("Movement", parent, "02 / TRANSFORMS", "Move + idle", "One preset can drive any Transform.");
        var stage = Image("MovementStage", section, GoofyTweenDemoTokens.Surface);
        Top(stage.rectTransform, 0, 114, 0, 126);
        var track = Image("Track", stage.transform, GoofyTweenDemoTokens.Track);
        Top(track.rectTransform, 28, 46, -28, 2);
        var target = Image("MovingObject", stage.transform, GoofyTweenDemoTokens.Accent);
        Top(target.rectTransform, 28, 22, 48, 48, false);
        var movement = target.gameObject.AddComponent<TweenPlayer>();
        movement.Target = target.transform;
        movement.Preset = LoadPreset("MoveRight");
        movement.PlayOnEnable = false;
        var idleTarget = Image("IdleObject", stage.transform, GoofyTweenDemoTokens.Ink);
        Top(idleTarget.rectTransform, 28, 88, 20, 20, false);
        idle = idleTarget.gameObject.AddComponent<TweenPlayer>();
        idle.Target = idleTarget.transform;
        idle.Preset = LoadPreset("IdlePulse");
        idle.PlayOnEnable = false;
        animatedIcon = idleTarget.gameObject.AddComponent<AnimatedIcon>();
        ConfigureIcon(animatedIcon);
        var label = Text("IdleLabel", stage.transform, "Pulse / shared idle preset", GoofyTweenDemoTokens.Label, GoofyTweenDemoTokens.Muted);
        Top(label.rectTransform, 62, 86, -12, 28);
        var row = ButtonRow(section);
        Button("Move object", row, controller.MoveObject, buttons, true);
        Button("Reset", row, controller.ResetMovement, buttons);
        Button("Next idle", row, controller.NextIdle, buttons);

        return movement;
    }

    private static void BuildValues(Transform parent, GoofyTweenDemo controller, List<TweenButton> buttons, out TweenFill health, out TweenSlider slider, out TweenCounter counter, out Text healthLabel)
    {
        var section = Section("Values", parent, "03 / VALUES", "Values that catch up", "Change the target while the value is moving.");
        healthLabel = Text("HealthLabel", section, "HEALTH  100 / 100", GoofyTweenDemoTokens.Caption, GoofyTweenDemoTokens.Muted, FontStyle.Bold);
        Top(healthLabel.rectTransform, 0, 112, 0, 20);
        var healthTrack = Image("HealthTrack", section, GoofyTweenDemoTokens.Track);
        Top(healthTrack.rectTransform, 0, 140, 0, 10);
        var secondary = Fill("DamageTrail", healthTrack.transform, GoofyTweenDemoTokens.AccentSoft);
        secondary.sprite = GetFillSprite();
        secondary.type = UnityEngine.UI.Image.Type.Filled;
        secondary.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        var primary = Fill("Health", healthTrack.transform, GoofyTweenDemoTokens.Accent);
        primary.sprite = GetFillSprite();
        primary.type = UnityEngine.UI.Image.Type.Filled;
        primary.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        health = healthTrack.gameObject.AddComponent<TweenFill>();
        health.Target = primary;
        health.SecondaryTarget = secondary;
        health.SecondaryDelay = 0.25f;
        health.Preset = LoadPreset("ValuesSmooth");

        var sliderLabel = Text("SliderLabel", section, "SLIDER", GoofyTweenDemoTokens.Caption, GoofyTweenDemoTokens.Muted, FontStyle.Bold);
        Top(sliderLabel.rectTransform, 0, 164, 0, 20);
        var sliderRoot = UI("ValueSlider", section, typeof(Slider));
        Top(sliderRoot.GetComponent<RectTransform>(), 0, 184, 0, 28);
        var sliderTrack = Image("Track", sliderRoot.transform, GoofyTweenDemoTokens.Track);
        Top(sliderTrack.rectTransform, 0, 10, 0, 6);
        var sliderFill = Fill("Fill", sliderTrack.transform, GoofyTweenDemoTokens.Ink);
        var handleArea = UI("HandleArea", sliderRoot.transform);
        Stretch(handleArea.GetComponent<RectTransform>());
        var handle = Image("Handle", handleArea.transform, GoofyTweenDemoTokens.Ink);
        handle.rectTransform.sizeDelta = new Vector2(16, 22);
        var sliderComponent = sliderRoot.GetComponent<Slider>();
        sliderComponent.fillRect = sliderFill.rectTransform;
        sliderComponent.handleRect = handle.rectTransform;
        sliderComponent.targetGraphic = handle;
        sliderComponent.minValue = 0;
        sliderComponent.maxValue = 100;
        sliderComponent.value = 100;
        sliderComponent.interactable = false;
        slider = sliderRoot.AddComponent<TweenSlider>();
        slider.Target = sliderComponent;
        slider.Preset = LoadPreset("ValuesSmooth");

        var scoreLabel = Text("ScoreLabel", section, "SCORE", GoofyTweenDemoTokens.Caption, GoofyTweenDemoTokens.Muted, FontStyle.Bold);
        Top(scoreLabel.rectTransform, 0, 224, 96, 24, false);
        var score = Text("Score", section, "1,250", GoofyTweenDemoTokens.Value, GoofyTweenDemoTokens.Ink, FontStyle.Bold);
        Top(score.rectTransform, 108, 214, -8, 44);
        counter = score.gameObject.AddComponent<TweenCounter>();
        counter.Target = score;
        counter.Preset = LoadPreset("ValuesSmooth");
        counter.NumberFormat = "N0";
        var row = ButtonRow(section);
        Button("Take damage", row, controller.TakeDamage, buttons, true);
        Button("Restore", row, controller.RestoreValues, buttons);
    }

    private static TweenSequence BuildSequence(Transform parent, GoofyTweenDemo controller, List<TweenButton> buttons)
    {
        var section = Section("Sequence", parent, "04 / COMPOSITION", "Compose a sequence", "Move, then rotate and scale together.");
        var stage = Image("SequenceStage", section, GoofyTweenDemoTokens.Surface);
        Top(stage.rectTransform, 0, 114, 0, 126);
        var target = Image("SequenceObject", stage.transform, GoofyTweenDemoTokens.Accent);
        Top(target.rectTransform, 28, 28, 48, 48, false);
        target.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        target.rectTransform.anchoredPosition = new Vector2(52, -52);
        var sequence = target.gameObject.AddComponent<TweenSequence>();
        sequence.PlayOnEnable = false;
        sequence.Entries = new[]
        {
            new TweenSequenceEntry {Target = target.transform, Preset = LoadPreset("MoveRight")},
            new TweenSequenceEntry {Target = target.transform, Preset = LoadPreset("RotateQuarter")},
            new TweenSequenceEntry {Target = target.transform, Preset = LoadPreset("SequenceScale"), JoinPrevious = true}
        };
        var label = Text("Steps", stage.transform, "1  Move     2  Rotate + scale", GoofyTweenDemoTokens.Label, GoofyTweenDemoTokens.Muted);
        Top(label.rectTransform, 24, 90, -12, 26);
        var row = ButtonRow(section);
        Button("Play sequence", row, controller.PlaySequence, buttons, true);
        Button("Reset", row, controller.ResetSequence, buttons);

        return sequence;
    }

    private static RectTransform Section(string name, Transform parent, string index, string title, string description)
    {
        var section = UI(name, parent).GetComponent<RectTransform>();
        var line = Image("Divider", section, GoofyTweenDemoTokens.Line);
        Top(line.rectTransform, 0, 0, 0, 1);
        var eyebrow = Text("Eyebrow", section, index, GoofyTweenDemoTokens.Caption, GoofyTweenDemoTokens.Muted, FontStyle.Bold);
        Top(eyebrow.rectTransform, 0, 18, 0, 20);
        var heading = Text("Heading", section, title, GoofyTweenDemoTokens.Heading, GoofyTweenDemoTokens.Ink, FontStyle.Bold);
        Top(heading.rectTransform, 0, 43, 0, 34);
        var body = Text("Description", section, description, GoofyTweenDemoTokens.Label, GoofyTweenDemoTokens.Muted);
        Top(body.rectTransform, 0, 82, 0, 28);

        return section;
    }

    private static Transform ButtonRow(Transform parent)
    {
        var row = UI("Actions", parent, typeof(HorizontalLayoutGroup));
        Top(row.GetComponent<RectTransform>(), 0, 272, 0, GoofyTweenDemoTokens.ControlHeight);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = GoofyTweenDemoTokens.DetailGap;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        return row.transform;
    }

    private static void CreateButtonPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath) != null) {UpgradeButtonPrefab(); return;}
        var root = UI("ActionButton", null, typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(LayoutElement), typeof(AnimatedButton), typeof(GoofyTweenDemoFocus));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(180, GoofyTweenDemoTokens.ControlHeight);
        var image = root.GetComponent<UnityEngine.UI.Image>();
        image.color = GoofyTweenDemoTokens.Ink;
        var button = root.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.87f, 0.87f, 0.87f);
        colors.selectedColor = new Color(0.87f, 0.87f, 0.87f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;
        var label = Text("Label", root.transform, "Action", GoofyTweenDemoTokens.Label, GoofyTweenDemoTokens.Surface, FontStyle.Bold);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(GoofyTweenDemoTokens.DetailGap, 0);
        label.rectTransform.offsetMax = new Vector2(-GoofyTweenDemoTokens.DetailGap, 0);
        label.alignment = TextAnchor.MiddleCenter;
        root.GetComponent<LayoutElement>().flexibleWidth = 1;
        root.GetComponent<LayoutElement>().minWidth = 0;
        root.GetComponent<LayoutElement>().preferredHeight = GoofyTweenDemoTokens.ControlHeight;
        var tween = root.GetComponent<TweenButton>();
        tween.HoverPreset = LoadPreset("ButtonHover");
        tween.PressPreset = LoadPreset("ButtonPress");
        tween.ClickPreset = LoadPreset("ButtonClick");
        root.GetComponent<AnimatedButton>().PunchPreset = LoadPreset("PunchScale");
        root.GetComponent<AnimatedButton>().ShakePreset = LoadPreset("IdleShake");
        PrefabUtility.SaveAsPrefabAsset(root, ButtonPath);
        Object.DestroyImmediate(root);
    }

    private static void Button(string label, Transform parent, UnityAction action, List<TweenButton> buttons, bool primary = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = label.Replace(" ", "") + "Button";
        instance.GetComponentInChildren<Text>().text = label;
        if (primary) instance.GetComponent<UnityEngine.UI.Image>().color = GoofyTweenDemoTokens.Accent;
        UnityEventTools.AddPersistentListener(instance.GetComponent<UnityEngine.UI.Button>().onClick, action);
        buttons.Add(instance.GetComponent<TweenButton>());
    }

    private static void SetReferences(GoofyTweenDemo demo, TweenPanel panel, TweenPlayer movement, TweenPlayer idle, TweenSequence sequence, TweenFill health, TweenSlider slider, TweenCounter counter, Text healthLabel, Text status, List<TweenButton> buttons, AnimatedIcon animatedIcon, Text panelStyleLabel, Text idleLabel)
    {
        var serialized = new SerializedObject(demo);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("movement").objectReferenceValue = movement;
        serialized.FindProperty("idle").objectReferenceValue = idle;
        serialized.FindProperty("sequence").objectReferenceValue = sequence;
        serialized.FindProperty("health").objectReferenceValue = health;
        serialized.FindProperty("slider").objectReferenceValue = slider;
        serialized.FindProperty("counter").objectReferenceValue = counter;
        serialized.FindProperty("healthLabel").objectReferenceValue = healthLabel;
        serialized.FindProperty("status").objectReferenceValue = status;
        serialized.FindProperty("animatedIcon").objectReferenceValue = animatedIcon;
        serialized.FindProperty("panelStyleLabel").objectReferenceValue = panelStyleLabel;
        serialized.FindProperty("idleLabel").objectReferenceValue = idleLabel;
        serialized.FindProperty("standardPanelShow").objectReferenceValue = LoadPreset("PanelStyleStandard");
        serialized.FindProperty("elasticPanelShow").objectReferenceValue = LoadPreset("PanelStyleElastic");
        serialized.FindProperty("popPanelShow").objectReferenceValue = LoadPreset("PanelStylePop");
        serialized.FindProperty("scalePanelShow").objectReferenceValue = LoadPreset("PanelStyleScale");
        serialized.FindProperty("panelStyleHide").objectReferenceValue = LoadPreset("PanelStyleHide");
        var array = serialized.FindProperty("buttons");
        array.arraySize = buttons.Count;
        for (int i = 0; i < buttons.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        demo.ApplyPanelProfile();
    }

    private static GameObject UI(string name, Transform parent, params Type[] components)
    {
        var instance = new GameObject(name, typeof(RectTransform));
        if (parent != null) instance.transform.SetParent(parent, false);
        foreach (var type in components) instance.AddComponent(type);

        return instance;
    }

    private static UnityEngine.UI.Image Image(string name, Transform parent, Color color)
    {
        var image = UI(name, parent, typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private static UnityEngine.UI.Image Fill(string name, Transform parent, Color color)
    {
        var image = Image(name, parent, color);
        Stretch(image.rectTransform);

        return image;
    }

    public static Sprite GetFillSprite()
    {
        string path = Root + "/Demo/Art/FillSprite.asset";
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite existing) return existing;
        EnsureFolder(Root + "/Demo/Art");
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException($"GetFillSprite: {path} содержит другой тип ассета");
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = "FillTexture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(new[] {Color.white, Color.white, Color.white, Color.white});
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
        }
        var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
        sprite.name = "FillSprite";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();

        return sprite;
    }

    private static Text Text(string name, Transform parent, string text, int size, Color color, FontStyle style = FontStyle.Normal)
    {
        var label = UI(name, parent, typeof(Text)).GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.alignment = TextAnchor.UpperLeft;

        return label;
    }

    private static void Top(RectTransform rect, float left, float top, float rightOrWidth, float height, bool stretch = true)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(stretch ? 1 : 0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(stretch ? rightOrWidth - left : rightOrWidth, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
    }
}
