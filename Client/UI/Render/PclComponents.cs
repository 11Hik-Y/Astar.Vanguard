using System;
using System.Collections.Generic;
using Astar.UI.Controls;
using Astar.UI.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Astar.Vanguard.Client.UI.Render
{
    internal enum PclTone
    {
        Neutral,
        Accent,
        Success,
        Warning,
        Danger,
        Muted,
    }

    internal sealed class PclSurfaceView
    {
        public RectTransform Root { get; init; }
        public PclRoundedRectGraphic Shadow { get; init; }
        public PclRoundedRectGraphic Border { get; init; }
        public PclRoundedRectGraphic Fill { get; init; }
    }

    internal sealed class PclCardView
    {
        public RectTransform Root { get; init; }
        public RectTransform ContentRoot { get; init; }
        public Text Title { get; init; }
        public PclSurfaceView Surface { get; init; }
        public PclCardBehaviour Behaviour { get; init; }

        public void SetTitle(string value)
        {
            if (Title is not null)
            {
                Title.text = value ?? string.Empty;
            }
        }
    }

    internal sealed class PclButtonView
    {
        public RectTransform Root { get; init; }
        public Button Button { get; init; }
        public Text Label { get; init; }
        public PclButtonBehaviour Behaviour { get; init; }

        public void SetText(string value) => Label.text = value ?? string.Empty;
        public void SetEnabled(bool value) => Behaviour.SetEnabled(value);
        public void SetSelected(bool value) => Behaviour.SetSelected(value);
    }

    internal sealed class PclListItemView
    {
        public RectTransform Root { get; init; }
        public Text Title { get; init; }
        public Text Info { get; init; }
        public Text Status { get; init; }
        public PclListItemBehaviour Behaviour { get; init; }

        public void SetContent(string title, string info, string status, PclTone tone)
        {
            Title.text = title ?? string.Empty;
            Info.text = info ?? string.Empty;
            Status.text = status ?? string.Empty;
            Behaviour.SetStatusColor(PclComponentFactory.GetToneColor(tone));
        }

        public void SetSelected(bool value) => Behaviour.SetSelected(value);
        public void SetEnabled(bool value) => Behaviour.SetEnabled(value);
    }

    internal sealed class PclProgressBarView
    {
        private readonly PclMotionHost _motion;
        private readonly RectTransform _fill;
        private readonly Text _value;
        private readonly string _motionKey;

        public RectTransform Root { get; init; }
        public Text Label { get; init; }

        public PclProgressBarView(PclMotionHost motion, RectTransform root, RectTransform fill, Text value)
        {
            _motion = motion;
            Root = root;
            _fill = fill;
            _value = value;
            _motionKey = "progress." + root.GetInstanceID();
        }

        public void SetValue(float normalized, string value, bool animate)
        {
            _value.text = value ?? string.Empty;
            var target = Mathf.Clamp01(normalized);
            var start = _fill.anchorMax.x;
            if (!animate || Mathf.Abs(start - target) < 0.001f)
            {
                SetFill(target);
                return;
            }

            _motion.Tween(
                _motionKey,
                PclMotionTokens.DurationNormal,
                progress => SetFill(Mathf.Lerp(start, target, progress)),
                PclEase.OutFluentStrong
            );
        }

        private void SetFill(float value)
        {
            var anchorMax = _fill.anchorMax;
            anchorMax.x = Mathf.Clamp01(value);
            _fill.anchorMax = anchorMax;
        }
    }

    internal enum PclLoadingState
    {
        Loading,
        Stopped,
        Error,
    }

    internal sealed class PclLoadingView
    {
        public RectTransform Root { get; init; }
        public PclLoadingBehaviour Behaviour { get; init; }
        public void SetState(PclLoadingState state, string text) => Behaviour.SetState(state, text);
    }

    internal sealed class PclInputView
    {
        public RectTransform Root { get; init; }
        public InputField Input { get; init; }
    }

    internal sealed class PclComponentFactory
    {
        private readonly AstarScreenContext _context;
        private readonly PclMotionHost _motion;

        public PclComponentFactory(AstarScreenContext context, PclMotionHost motion)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
        }

        public PclCardView CreateCard(
            Transform parent,
            string name,
            string title,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var root = _context.Factory.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var surface = CreateSurface(root, PclDesignTokens.RadiusCard, true);
            var titleText = _context.Factory.CreateText(
                root,
                "Title",
                title ?? string.Empty,
                PclDesignTokens.TypographySubtitle,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(PclDesignTokens.SpacingLg, -40f),
                new Vector2(-PclDesignTokens.SpacingLg, 0f),
                PclDesignTokens.ForegroundPrimary,
                FontStyle.Bold
            );

            var separatorRect = _context.Factory.CreateRect(
                root,
                "HeaderSeparator",
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(PclDesignTokens.SpacingMd, -43f),
                new Vector2(-PclDesignTokens.SpacingMd, -42f)
            );
            var separator = separatorRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            separator.Radius = 0.5f;
            separator.color = PclDesignTokens.WithAlpha(PclDesignTokens.BorderNeutral, 0.34f);
            separator.raycastTarget = false;

            var content = _context.Factory.CreateRect(
                root,
                "Content",
                Vector2.zero,
                Vector2.one,
                new Vector2(PclDesignTokens.SpacingMd, PclDesignTokens.SpacingMd),
                new Vector2(-PclDesignTokens.SpacingMd, -42f)
            );

            var behaviour = root.gameObject.AddComponent<PclCardBehaviour>();
            behaviour.Configure(_motion, surface, titleText);
            return new PclCardView
            {
                Root = root,
                ContentRoot = content,
                Title = titleText,
                Surface = surface,
                Behaviour = behaviour,
            };
        }

        public PclButtonView CreateButton(
            Transform parent,
            string name,
            string text,
            Action onClick,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            bool accent = false,
            bool iconMode = false
        )
        {
            var root = _context.Factory.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var surface = CreateSurface(root, PclDesignTokens.RadiusButton, true);
            surface.Shadow.color = PclDesignTokens.WithAlpha(PclDesignTokens.Shadow, 0f);
            surface.Border.color = accent ? PclDesignTokens.AccentDark : PclDesignTokens.BorderNeutral;
            surface.Fill.color = accent ? PclDesignTokens.AccentSofter : PclDesignTokens.ButtonIdle;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = surface.Fill;
            if (onClick is not null)
            {
                button.onClick.AddListener(() => onClick());
            }

            var label = _context.Factory.CreateText(
                root,
                "Label",
                text ?? string.Empty,
                iconMode ? PclDesignTokens.TypographySubtitle : PclDesignTokens.TypographyBody,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                new Vector2(iconMode ? 2f : 10f, 0f),
                new Vector2(iconMode ? -2f : -10f, 0f),
                accent ? PclDesignTokens.AccentDark : PclDesignTokens.ForegroundPrimary,
                FontStyle.Normal
            );
            label.raycastTarget = false;

            var behaviour = root.gameObject.AddComponent<PclButtonBehaviour>();
            behaviour.Configure(_motion, surface, label, button, accent, iconMode);
            return new PclButtonView { Root = root, Button = button, Label = label, Behaviour = behaviour };
        }

        public PclButtonView CreateIconButton(
            Transform parent,
            string name,
            string iconText,
            Action onClick,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            return CreateButton(
                parent,
                name,
                iconText,
                onClick,
                anchorMin,
                anchorMax,
                offsetMin,
                offsetMax,
                false,
                true
            );
        }

        public PclListItemView CreateListItem(
            Transform parent,
            string name,
            Action onClick,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var root = _context.Factory.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var surface = CreateSurface(root, PclDesignTokens.RadiusListItem, true);
            surface.Shadow.color = PclDesignTokens.WithAlpha(PclDesignTokens.Shadow, 0f);
            surface.Border.color = PclDesignTokens.WithAlpha(PclDesignTokens.Border, 0f);
            surface.Fill.color = PclDesignTokens.WithAlpha(PclDesignTokens.AccentBackground, 0f);
            surface.Fill.rectTransform.localScale = new Vector3(
                PclMotionTokens.ListHoverStartScale,
                PclMotionTokens.ListHoverStartScale,
                1f
            );

            var selectionRect = _context.Factory.CreateRect(
                root,
                "SelectionBar",
                new Vector2(0f, 0.18f),
                new Vector2(0f, 0.82f),
                new Vector2(2f, 0f),
                new Vector2(6f, 0f)
            );
            var selection = selectionRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            selection.Radius = 2f;
            selection.color = PclDesignTokens.WithAlpha(PclDesignTokens.Accent, 0f);
            selection.raycastTarget = false;

            var title = _context.Factory.CreateText(
                root,
                "Title",
                string.Empty,
                PclDesignTokens.TypographyBodyLarge,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0.50f),
                new Vector2(0.72f, 0.96f),
                new Vector2(14f, 1f),
                new Vector2(-4f, -1f),
                PclDesignTokens.ForegroundPrimary,
                FontStyle.Normal
            );
            title.raycastTarget = false;

            var info = _context.Factory.CreateText(
                root,
                "Info",
                string.Empty,
                PclDesignTokens.TypographyCaption,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0.08f),
                new Vector2(0.72f, 0.42f),
                new Vector2(14f, 0f),
                new Vector2(-4f, 0f),
                PclDesignTokens.ForegroundMuted
            );
            info.raycastTarget = false;

            var status = _context.Factory.CreateText(
                root,
                "Status",
                string.Empty,
                PclDesignTokens.TypographyCaption,
                TextAnchor.MiddleRight,
                new Vector2(0.70f, 0f),
                Vector2.one,
                new Vector2(0f, 0f),
                new Vector2(-14f, 0f),
                PclDesignTokens.ForegroundMuted,
                FontStyle.Bold
            );
            status.raycastTarget = false;

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = surface.Border;
            if (onClick is not null)
            {
                button.onClick.AddListener(() => onClick());
            }

            var behaviour = root.gameObject.AddComponent<PclListItemBehaviour>();
            behaviour.Configure(_motion, surface, selection, title, info, status, button);
            return new PclListItemView
            {
                Root = root,
                Title = title,
                Info = info,
                Status = status,
                Behaviour = behaviour,
            };
        }

        public PclProgressBarView CreateProgressBar(
            Transform parent,
            string name,
            string label,
            float normalized,
            string value,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var root = _context.Factory.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var labelText = _context.Factory.CreateText(
                root,
                "Label",
                label ?? string.Empty,
                PclDesignTokens.TypographyBody,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0.38f),
                new Vector2(0.75f, 1f),
                Vector2.zero,
                Vector2.zero,
                PclDesignTokens.ForegroundPrimary
            );
            labelText.raycastTarget = false;
            var valueText = _context.Factory.CreateText(
                root,
                "Value",
                value ?? string.Empty,
                PclDesignTokens.TypographyCaption,
                TextAnchor.MiddleRight,
                new Vector2(0.72f, 0.38f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                PclDesignTokens.ForegroundMuted
            );
            valueText.raycastTarget = false;

            var trackRect = _context.Factory.CreateRect(
                root,
                "Track",
                new Vector2(0f, 0.08f),
                new Vector2(1f, 0.27f),
                Vector2.zero,
                Vector2.zero
            );
            var track = trackRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            track.Radius = PclDesignTokens.RadiusProgress;
            track.color = PclDesignTokens.AccentSofter;
            track.raycastTarget = false;

            var fillRect = _context.Factory.CreateRect(
                trackRect,
                "Fill",
                Vector2.zero,
                new Vector2(Mathf.Clamp01(normalized), 1f),
                Vector2.zero,
                Vector2.zero
            );
            var fill = fillRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            fill.Radius = PclDesignTokens.RadiusProgress;
            fill.color = PclDesignTokens.Accent;
            fill.raycastTarget = false;

            return new PclProgressBarView(_motion, root, fillRect, valueText) { Label = labelText };
        }

        public PclLoadingView CreateLoading(
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var root = _context.Factory.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var tool = _context.Factory.CreateRect(
                root,
                "Tool",
                new Vector2(0.5f, 0.55f),
                new Vector2(0.5f, 0.55f),
                new Vector2(-20f, -18f),
                new Vector2(20f, 22f)
            );
            tool.localEulerAngles = new Vector3(0f, 0f, 35f);

            var shaft = CreateFixedRounded(tool, "Shaft", new Vector2(3f, 25f), new Vector2(-2f, -3f), PclDesignTokens.Accent, 1.5f);
            shaft.rectTransform.localEulerAngles = new Vector3(0f, 0f, -24f);
            var head = CreateFixedRounded(tool, "Head", new Vector2(23f, 3f), new Vector2(2f, 9f), PclDesignTokens.Accent, 1.5f);
            head.rectTransform.localEulerAngles = new Vector3(0f, 0f, -24f);

            var chipLeft = CreateFixedRounded(root, "ChipLeft", new Vector2(3f, 5f), new Vector2(-9f, -2f), PclDesignTokens.Accent, 1f);
            var chipRight = CreateFixedRounded(root, "ChipRight", new Vector2(3f, 5f), new Vector2(8f, -2f), PclDesignTokens.Accent, 1f);
            chipLeft.color = PclDesignTokens.WithAlpha(chipLeft.color, 0f);
            chipRight.color = PclDesignTokens.WithAlpha(chipRight.color, 0f);

            var errorA = CreateFixedRounded(root, "ErrorA", new Vector2(20f, 3f), Vector2.zero, PclDesignTokens.Danger, 1.5f);
            var errorB = CreateFixedRounded(root, "ErrorB", new Vector2(20f, 3f), Vector2.zero, PclDesignTokens.Danger, 1.5f);
            errorA.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            errorB.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);
            errorA.gameObject.SetActive(false);
            errorB.gameObject.SetActive(false);

            var label = _context.Factory.CreateText(
                root,
                "Label",
                text ?? string.Empty,
                PclDesignTokens.TypographySubtitle,
                TextAnchor.UpperCenter,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.42f),
                Vector2.zero,
                Vector2.zero,
                PclDesignTokens.Accent
            );
            label.raycastTarget = false;

            var behaviour = root.gameObject.AddComponent<PclLoadingBehaviour>();
            behaviour.Configure(
                _motion,
                tool,
                chipLeft,
                chipRight,
                errorA,
                errorB,
                label,
                new[] { shaft, head }
            );
            behaviour.SetState(PclLoadingState.Loading, text);
            return new PclLoadingView { Root = root, Behaviour = behaviour };
        }

        public PclInputView CreateInput(
            Transform parent,
            string name,
            string placeholderText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var root = _context.Factory.CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var surface = CreateSurface(root, PclDesignTokens.RadiusInput, true);
            surface.Shadow.color = PclDesignTokens.WithAlpha(PclDesignTokens.Shadow, 0f);
            surface.Border.color = PclDesignTokens.Border;
            surface.Fill.color = PclDesignTokens.ButtonIdle;

            var text = _context.Factory.CreateText(
                root,
                "Text",
                string.Empty,
                PclDesignTokens.TypographyBody,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f),
                PclDesignTokens.ForegroundPrimary
            );
            text.supportRichText = false;
            text.raycastTarget = false;
            var placeholder = _context.Factory.CreateText(
                root,
                "Placeholder",
                placeholderText ?? string.Empty,
                PclDesignTokens.TypographyBody,
                TextAnchor.MiddleLeft,
                Vector2.zero,
                Vector2.one,
                new Vector2(10f, 0f),
                new Vector2(-10f, 0f),
                PclDesignTokens.ForegroundMuted
            );
            placeholder.raycastTarget = false;

            var input = root.gameObject.AddComponent<InputField>();
            input.transition = Selectable.Transition.None;
            input.targetGraphic = surface.Fill;
            input.textComponent = text;
            input.placeholder = placeholder;

            var behaviour = root.gameObject.AddComponent<PclInputBehaviour>();
            behaviour.Configure(_motion, surface, input);
            return new PclInputView { Root = root, Input = input };
        }

        public void StyleScrollView(AstarScrollView scrollView)
        {
            if (scrollView?.Root is null)
            {
                return;
            }

            // Astar UI's stock scroll view uses the host theme background. Explicitly
            // pull it into the same graphite hierarchy as the surrounding PCL card so
            // the roster does not appear as a black rectangle inside a light surface.
            if (scrollView.Root.GetComponent<Image>() is { } rootBackground)
            {
                rootBackground.color = PclDesignTokens.SurfaceInset;
            }

            if (scrollView.Viewport?.GetComponent<Image>() is { } viewportBackground)
            {
                viewportBackground.color = PclDesignTokens.SurfaceInset;
            }

            foreach (var scrollbar in scrollView.Root.GetComponentsInChildren<Scrollbar>(true))
            {
                scrollbar.transition = Selectable.Transition.None;
                if (scrollbar.GetComponent<Image>() is { } background)
                {
                    background.color = PclDesignTokens.WithAlpha(PclDesignTokens.BorderNeutral, 0.24f);
                }

                if (scrollbar.handleRect?.GetComponent<Image>() is { } handle)
                {
                    handle.color = PclDesignTokens.WithAlpha(PclDesignTokens.AccentHover, 0.62f);
                }
            }
        }

        public static Color GetToneColor(PclTone tone)
        {
            return tone switch
            {
                PclTone.Accent => PclDesignTokens.AccentDark,
                PclTone.Success => PclDesignTokens.Success,
                PclTone.Warning => PclDesignTokens.Warning,
                PclTone.Danger => PclDesignTokens.Danger,
                PclTone.Muted => PclDesignTokens.ForegroundMuted,
                _ => PclDesignTokens.ForegroundPrimary,
            };
        }

        private PclSurfaceView CreateSurface(RectTransform root, float radius, bool interactive)
        {
            var shadowRect = _context.Factory.CreateRect(
                root,
                "Shadow",
                Vector2.zero,
                Vector2.one,
                new Vector2(-3f, -4f),
                new Vector2(3f, 1f)
            );
            var shadow = shadowRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            shadow.Radius = radius + 2f;
            shadow.color = PclDesignTokens.WithAlpha(PclDesignTokens.Shadow, PclDesignTokens.ShadowIdleOpacity);
            shadow.raycastTarget = false;

            var borderRect = _context.Factory.CreateRect(root, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var border = borderRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            border.Radius = radius;
            border.color = PclDesignTokens.Border;
            border.raycastTarget = interactive;

            var fillRect = _context.Factory.CreateRect(
                root,
                "Fill",
                Vector2.zero,
                Vector2.one,
                new Vector2(1f, 1f),
                new Vector2(-1f, -1f)
            );
            var fill = fillRect.gameObject.AddComponent<PclRoundedRectGraphic>();
            fill.Radius = Mathf.Max(0f, radius - 1f);
            fill.color = PclDesignTokens.Surface;
            fill.raycastTarget = false;

            return new PclSurfaceView { Root = root, Shadow = shadow, Border = border, Fill = fill };
        }

        private PclRoundedRectGraphic CreateFixedRounded(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 position,
            Color color,
            float radius
        )
        {
            var half = size * 0.5f;
            var rect = _context.Factory.CreateRect(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position - half,
                position + half
            );
            var graphic = rect.gameObject.AddComponent<PclRoundedRectGraphic>();
            graphic.Radius = radius;
            graphic.color = color;
            graphic.raycastTarget = false;
            return graphic;
        }
    }

    internal sealed class PclButtonBehaviour : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private PclMotionHost _motion;
        private PclSurfaceView _surface;
        private Text _label;
        private Button _button;
        private bool _accent;
        private bool _iconMode;
        private bool _hovered;
        private bool _pressed;
        private bool _selected;
        private bool _focused;
        private bool _enabled = true;
        private string _keyPrefix;

        public void Configure(PclMotionHost motion, PclSurfaceView surface, Text label, Button button, bool accent, bool iconMode)
        {
            _motion = motion;
            _surface = surface;
            _label = label;
            _button = button;
            _accent = accent;
            _iconMode = iconMode;
            _keyPrefix = "button." + GetInstanceID();
            RefreshPalette(false);
        }

        public void SetEnabled(bool value)
        {
            _enabled = value;
            if (_button is not null)
            {
                _button.interactable = value;
            }
            if (!value)
            {
                _pressed = false;
                AnimateScale(1f, PclMotionTokens.DurationNormal, PclEase.OutFluent);
            }
            RefreshPalette(true);
        }

        public void SetSelected(bool value)
        {
            _selected = value;
            RefreshPalette(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RefreshPalette(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            AnimateScale(1f, PclMotionTokens.DurationSlow, PclEase.OutFluentStrong);
            RefreshPalette(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enabled || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pressed = true;
            AnimateScale(
                _iconMode ? PclMotionTokens.IconPressScale : PclMotionTokens.ButtonPressScale,
                PclMotionTokens.PressIn,
                PclEase.OutFluentStrong
            );
            RefreshPalette(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed)
            {
                return;
            }

            _pressed = false;
            if (_iconMode)
            {
                AnimateIconRelease();
            }
            else
            {
                AnimateScale(1f, PclMotionTokens.PressRelease, PclEase.OutFluent);
            }
            RefreshPalette(true);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            RefreshPalette(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            RefreshPalette(true);
        }

        private void AnimateIconRelease()
        {
            var start = transform.localScale.x;
            _motion.Tween(
                _keyPrefix + ".scale",
                0.25f,
                progress =>
                {
                    var target = progress < 0.55f
                        ? Mathf.Lerp(start, 1.05f, progress / 0.55f)
                        : Mathf.Lerp(1.05f, 1f, (progress - 0.55f) / 0.45f);
                    transform.localScale = new Vector3(target, target, 1f);
                },
                PclEase.OutBack
            );
        }

        private void AnimateScale(float target, float duration, PclEase ease)
        {
            if (_motion is null)
            {
                transform.localScale = new Vector3(target, target, 1f);
                return;
            }

            var start = transform.localScale.x;
            _motion.Tween(
                _keyPrefix + ".scale",
                duration,
                progress =>
                {
                    var value = Mathf.Lerp(start, target, progress);
                    transform.localScale = new Vector3(value, value, 1f);
                },
                ease
            );
        }

        private void RefreshPalette(bool animate)
        {
            if (_surface is null || _label is null)
            {
                return;
            }

            Color border;
            Color fill;
            Color text;
            if (!_enabled)
            {
                border = PclDesignTokens.ForegroundDisabled;
                fill = PclDesignTokens.SurfaceDisabled;
                text = PclDesignTokens.ForegroundDisabled;
            }
            else if (_pressed)
            {
                border = PclDesignTokens.Accent;
                fill = PclDesignTokens.AccentSoft;
                text = PclDesignTokens.AccentDark;
            }
            else if (_hovered || _focused)
            {
                border = PclDesignTokens.Accent;
                fill = PclDesignTokens.AccentSofter;
                text = PclDesignTokens.AccentDark;
            }
            else if (_selected || _accent)
            {
                border = PclDesignTokens.AccentDark;
                fill = PclDesignTokens.WithAlpha(PclDesignTokens.AccentSoft, 0.74f);
                text = PclDesignTokens.AccentDark;
            }
            else
            {
                border = PclDesignTokens.BorderNeutral;
                fill = PclDesignTokens.ButtonIdle;
                text = PclDesignTokens.ForegroundPrimary;
            }

            if (!animate || _motion is null)
            {
                _surface.Border.color = border;
                _surface.Fill.color = fill;
                _label.color = text;
                return;
            }

            var startBorder = _surface.Border.color;
            var startFill = _surface.Fill.color;
            var startText = _label.color;
            _motion.Tween(
                _keyPrefix + ".palette",
                _hovered ? PclMotionTokens.HoverIn : PclMotionTokens.HoverOut,
                progress =>
                {
                    _surface.Border.color = Color.LerpUnclamped(startBorder, border, progress);
                    _surface.Fill.color = Color.LerpUnclamped(startFill, fill, progress);
                    _label.color = Color.LerpUnclamped(startText, text, progress);
                },
                PclEase.OutFluent
            );
        }
    }

    internal sealed class PclCardBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private PclMotionHost _motion;
        private PclSurfaceView _surface;
        private Text _title;
        private string _keyPrefix;

        public void Configure(PclMotionHost motion, PclSurfaceView surface, Text title)
        {
            _motion = motion;
            _surface = surface;
            _title = title;
            _keyPrefix = "card." + GetInstanceID();
        }

        public void OnPointerEnter(PointerEventData eventData) => AnimateHover(true);
        public void OnPointerExit(PointerEventData eventData) => AnimateHover(false);

        public void SetExpanded(RectTransform content, bool expanded, float collapsedHeight, float expandedHeight)
        {
            if (content is null)
            {
                return;
            }

            var root = (RectTransform)transform;
            var startHeight = root.rect.height;
            var targetHeight = expanded ? expandedHeight : collapsedHeight;
            var group = content.GetComponent<PclOpacityGroup>() ?? content.gameObject.AddComponent<PclOpacityGroup>();
            group.Refresh();
            var startAlpha = group.Alpha;
            var targetAlpha = expanded ? 1f : 0f;
            content.gameObject.SetActive(true);
            _motion.Tween(
                _keyPrefix + ".expand",
                PclMotionTokens.CardExpand,
                progress =>
                {
                    root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(startHeight, targetHeight, progress));
                    group.Alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
                },
                PclEase.OutFluentStrong,
                completed: () => content.gameObject.SetActive(expanded)
            );
        }

        private void AnimateHover(bool hovered)
        {
            if (_motion is null || _surface is null)
            {
                return;
            }

            var targetShadow = hovered ? PclDesignTokens.ShadowHoverOpacity : PclDesignTokens.ShadowIdleOpacity;
            var targetTitle = hovered ? PclDesignTokens.AccentDark : PclDesignTokens.ForegroundPrimary;
            var startShadow = _surface.Shadow.color;
            var endShadow = PclDesignTokens.WithAlpha(PclDesignTokens.Shadow, targetShadow);
            var startTitle = _title.color;
            _motion.Tween(
                _keyPrefix + ".hover",
                PclMotionTokens.CardHover,
                progress =>
                {
                    _surface.Shadow.color = Color.LerpUnclamped(startShadow, endShadow, progress);
                    _title.color = Color.LerpUnclamped(startTitle, targetTitle, progress);
                },
                PclEase.OutFluent
            );
        }
    }

    internal sealed class PclListItemBehaviour : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private PclMotionHost _motion;
        private PclSurfaceView _surface;
        private PclRoundedRectGraphic _selection;
        private Text _title;
        private Text _info;
        private Text _status;
        private Button _button;
        private bool _hovered;
        private bool _pressed;
        private bool _selected;
        private bool _focused;
        private bool _enabled = true;
        private Color _statusColor = PclDesignTokens.ForegroundMuted;
        private string _keyPrefix;

        public void Configure(
            PclMotionHost motion,
            PclSurfaceView surface,
            PclRoundedRectGraphic selection,
            Text title,
            Text info,
            Text status,
            Button button
        )
        {
            _motion = motion;
            _surface = surface;
            _selection = selection;
            _title = title;
            _info = info;
            _status = status;
            _button = button;
            _keyPrefix = "listitem." + GetInstanceID();
            RefreshVisual(false);
        }

        public void SetSelected(bool value)
        {
            _selected = value;
            RefreshVisual(true);
        }

        public void SetEnabled(bool value)
        {
            _enabled = value;
            _button.interactable = value;
            if (!value)
            {
                _pressed = false;
            }
            RefreshVisual(true);
        }

        public void SetStatusColor(Color color)
        {
            _statusColor = color;
            RefreshVisual(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RefreshVisual(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            AnimateRootScale(1f, PclMotionTokens.ListHoverOut);
            RefreshVisual(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enabled || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            _pressed = true;
            AnimateRootScale(PclMotionTokens.ListPressScale, PclMotionTokens.PressIn);
            RefreshVisual(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed)
            {
                return;
            }
            _pressed = false;
            AnimateRootScale(1f, PclMotionTokens.DurationNormal);
            RefreshVisual(true);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            RefreshVisual(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            RefreshVisual(true);
        }

        private void AnimateRootScale(float target, float duration)
        {
            var start = transform.localScale.x;
            _motion.Tween(
                _keyPrefix + ".scale",
                duration,
                progress =>
                {
                    var value = Mathf.Lerp(start, target, progress);
                    transform.localScale = new Vector3(value, value, 1f);
                },
                PclEase.OutFluentStrong
            );
        }

        private void RefreshVisual(bool animate)
        {
            if (_surface is null)
            {
                return;
            }

            var showHover = _enabled && (_hovered || _focused || _selected);
            var fillTarget = showHover
                ? PclDesignTokens.WithAlpha(PclDesignTokens.AccentBackground, _selected ? 0.82f : (_pressed ? 0.92f : 0.66f))
                : PclDesignTokens.WithAlpha(PclDesignTokens.AccentBackground, 0f);
            var borderTarget = _selected || _focused
                ? PclDesignTokens.WithAlpha(PclDesignTokens.Border, 0.95f)
                : PclDesignTokens.WithAlpha(PclDesignTokens.Border, _hovered ? 0.65f : 0f);
            var titleTarget = !_enabled
                ? PclDesignTokens.ForegroundDisabled
                : _selected
                    ? PclDesignTokens.AccentDark
                    : PclDesignTokens.ForegroundPrimary;
            var infoTarget = !_enabled ? PclDesignTokens.ForegroundDisabled : PclDesignTokens.ForegroundMuted;
            var statusTarget = !_enabled ? PclDesignTokens.ForegroundDisabled : _statusColor;
            var selectionTarget = PclDesignTokens.WithAlpha(PclDesignTokens.Accent, _selected ? 1f : 0f);
            var fillScaleTarget = showHover ? 1f : PclMotionTokens.ListHoverStartScale;

            if (!animate)
            {
                _surface.Fill.color = fillTarget;
                _surface.Border.color = borderTarget;
                _surface.Fill.rectTransform.localScale = new Vector3(fillScaleTarget, fillScaleTarget, 1f);
                _title.color = titleTarget;
                _info.color = infoTarget;
                _status.color = statusTarget;
                _selection.color = selectionTarget;
                return;
            }

            var startFill = _surface.Fill.color;
            var startBorder = _surface.Border.color;
            var startScale = _surface.Fill.rectTransform.localScale.x;
            var startTitle = _title.color;
            var startInfo = _info.color;
            var startStatus = _status.color;
            var startSelection = _selection.color;
            _motion.Tween(
                _keyPrefix + ".visual",
                showHover ? PclMotionTokens.ListHoverIn : PclMotionTokens.ListHoverOut,
                progress =>
                {
                    _surface.Fill.color = Color.LerpUnclamped(startFill, fillTarget, progress);
                    _surface.Border.color = Color.LerpUnclamped(startBorder, borderTarget, progress);
                    var scale = Mathf.Lerp(startScale, fillScaleTarget, progress);
                    _surface.Fill.rectTransform.localScale = new Vector3(scale, scale, 1f);
                    _title.color = Color.LerpUnclamped(startTitle, titleTarget, progress);
                    _info.color = Color.LerpUnclamped(startInfo, infoTarget, progress);
                    _status.color = Color.LerpUnclamped(startStatus, statusTarget, progress);
                    _selection.color = Color.LerpUnclamped(startSelection, selectionTarget, progress);
                },
                PclEase.OutFluent
            );
        }
    }

    internal sealed class PclInputBehaviour : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private PclMotionHost _motion;
        private PclSurfaceView _surface;
        private InputField _input;
        private bool _hovered;
        private bool _focused;
        private string _key;

        public void Configure(PclMotionHost motion, PclSurfaceView surface, InputField input)
        {
            _motion = motion;
            _surface = surface;
            _input = input;
            _key = "input." + GetInstanceID();
            Refresh(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Refresh(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Refresh(true);
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            Refresh(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            Refresh(true);
        }

        private void Refresh(bool animate)
        {
            var enabled = _input?.interactable != false;
            var border = !enabled
                ? PclDesignTokens.ForegroundDisabled
                : _focused
                    ? PclDesignTokens.Accent
                    : _hovered
                        ? PclDesignTokens.AccentHover
                        : PclDesignTokens.Border;
            var fill = !enabled
                ? PclDesignTokens.SurfaceDisabled
                : (_focused || _hovered)
                    ? PclDesignTokens.AccentSofter
                    : PclDesignTokens.ButtonIdle;

            if (!animate)
            {
                _surface.Border.color = border;
                _surface.Fill.color = fill;
                return;
            }

            var startBorder = _surface.Border.color;
            var startFill = _surface.Fill.color;
            _motion.Tween(
                _key,
                _focused ? PclMotionTokens.DurationFast : PclMotionTokens.DurationNormal,
                progress =>
                {
                    _surface.Border.color = Color.LerpUnclamped(startBorder, border, progress);
                    _surface.Fill.color = Color.LerpUnclamped(startFill, fill, progress);
                },
                PclEase.OutFluent
            );
        }
    }

    internal sealed class PclLoadingBehaviour : MonoBehaviour
    {
        private PclMotionHost _motion;
        private RectTransform _tool;
        private PclRoundedRectGraphic _chipLeft;
        private PclRoundedRectGraphic _chipRight;
        private PclRoundedRectGraphic _errorA;
        private PclRoundedRectGraphic _errorB;
        private Text _label;
        private IReadOnlyList<PclRoundedRectGraphic> _toolGraphics;
        private string _loopKey;
        private PclLoadingState _state;
        private Vector2 _chipLeftStart;
        private Vector2 _chipRightStart;

        public void Configure(
            PclMotionHost motion,
            RectTransform tool,
            PclRoundedRectGraphic chipLeft,
            PclRoundedRectGraphic chipRight,
            PclRoundedRectGraphic errorA,
            PclRoundedRectGraphic errorB,
            Text label,
            IReadOnlyList<PclRoundedRectGraphic> toolGraphics
        )
        {
            _motion = motion;
            _tool = tool;
            _chipLeft = chipLeft;
            _chipRight = chipRight;
            _errorA = errorA;
            _errorB = errorB;
            _label = label;
            _toolGraphics = toolGraphics;
            _loopKey = "loading." + GetInstanceID();
            _chipLeftStart = chipLeft.rectTransform.anchoredPosition;
            _chipRightStart = chipRight.rectTransform.anchoredPosition;
        }

        public void SetState(PclLoadingState state, string text)
        {
            _state = state;
            _label.text = text ?? string.Empty;
            _motion.StopMotion(_loopKey);

            var isError = state == PclLoadingState.Error;
            _errorA.gameObject.SetActive(isError);
            _errorB.gameObject.SetActive(isError);
            _tool.gameObject.SetActive(!isError);
            _label.color = isError ? PclDesignTokens.Danger : PclDesignTokens.Accent;

            foreach (var graphic in _toolGraphics)
            {
                graphic.color = isError ? PclDesignTokens.Danger : PclDesignTokens.Accent;
            }

            if (state == PclLoadingState.Loading)
            {
                StartLoop();
            }
            else
            {
                ResetChips();
                _tool.localEulerAngles = new Vector3(0f, 0f, 35f);
                if (isError)
                {
                    _errorA.rectTransform.localScale = Vector3.one * 0.6f;
                    _errorB.rectTransform.localScale = Vector3.one * 0.6f;
                    _motion.Tween(
                        _loopKey + ".error",
                        PclMotionTokens.DurationSlow,
                        progress =>
                        {
                            var scale = Mathf.Lerp(0.6f, 1f, progress);
                            _errorA.rectTransform.localScale = new Vector3(scale, scale, 1f);
                            _errorB.rectTransform.localScale = new Vector3(scale, scale, 1f);
                        },
                        PclEase.OutBack
                    );
                }
            }
        }

        private void StartLoop()
        {
            if (_state != PclLoadingState.Loading || !isActiveAndEnabled)
            {
                return;
            }

            ResetChips();
            _motion.Tween(
                _loopKey,
                PclMotionTokens.LoadingCycle,
                progress =>
                {
                    var swing = PclEasing.Evaluate(PclEase.OutBack, Mathf.Clamp01(progress / 0.72f));
                    var angle = Mathf.Lerp(42f, -28f, swing);
                    _tool.localEulerAngles = new Vector3(0f, 0f, angle);

                    var chipPhase = Mathf.Clamp01((progress - 0.55f) / 0.38f);
                    var alpha = chipPhase <= 0f ? 0f : 1f - chipPhase;
                    _chipLeft.color = PclDesignTokens.WithAlpha(PclDesignTokens.Accent, alpha);
                    _chipRight.color = PclDesignTokens.WithAlpha(PclDesignTokens.Accent, alpha);
                    _chipLeft.rectTransform.anchoredPosition = _chipLeftStart + new Vector2(-6f, 7f) * chipPhase;
                    _chipRight.rectTransform.anchoredPosition = _chipRightStart + new Vector2(6f, 7f) * chipPhase;
                },
                PclEase.Linear,
                completed: StartLoop
            );
        }

        private void ResetChips()
        {
            _chipLeft.rectTransform.anchoredPosition = _chipLeftStart;
            _chipRight.rectTransform.anchoredPosition = _chipRightStart;
            _chipLeft.color = PclDesignTokens.WithAlpha(PclDesignTokens.Accent, 0f);
            _chipRight.color = PclDesignTokens.WithAlpha(PclDesignTokens.Accent, 0f);
        }
    }

    internal static class PclPageTransitions
    {
        public static void Enter(PclMotionHost motion, IReadOnlyList<RectTransform> elements)
        {
            if (motion is null || elements is null)
            {
                return;
            }

            for (var index = 0; index < elements.Count; index++)
            {
                var element = elements[index];
                if (element is null)
                {
                    continue;
                }

                var group = element.GetComponent<PclOpacityGroup>() ?? element.gameObject.AddComponent<PclOpacityGroup>();
                group.Refresh();
                var targetPosition = element.anchoredPosition;
                var startPosition = targetPosition + new Vector2(0f, PclMotionTokens.PageEnterOffset);
                group.Alpha = 0f;
                element.anchoredPosition = startPosition;
                var key = "page.enter." + element.GetInstanceID();
                motion.Tween(
                    key,
                    PclMotionTokens.PageEnter,
                    progress =>
                    {
                        group.Alpha = Mathf.Clamp01(progress * 1.35f);
                        element.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, progress);
                    },
                    PclEase.OutBack,
                    index * PclMotionTokens.PageStagger,
                    deferUntilActive: true
                );
            }
        }

        public static void Exit(PclMotionHost motion, IReadOnlyList<RectTransform> elements, Action completed = null)
        {
            if (motion is null || elements is null || elements.Count == 0)
            {
                completed?.Invoke();
                return;
            }

            var remaining = elements.Count;
            for (var index = 0; index < elements.Count; index++)
            {
                var element = elements[index];
                if (element is null)
                {
                    if (--remaining == 0)
                    {
                        completed?.Invoke();
                    }
                    continue;
                }

                var group = element.GetComponent<PclOpacityGroup>() ?? element.gameObject.AddComponent<PclOpacityGroup>();
                group.Refresh();
                var startPosition = element.anchoredPosition;
                var targetPosition = startPosition + new Vector2(0f, PclMotionTokens.PageExitOffset);
                var startAlpha = group.Alpha;
                motion.Tween(
                    "page.exit." + element.GetInstanceID(),
                    PclMotionTokens.PageExit,
                    progress =>
                    {
                        group.Alpha = Mathf.Lerp(startAlpha, 0f, progress);
                        element.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
                    },
                    PclEase.InFluent,
                    index * 0.015f,
                    () =>
                    {
                        if (--remaining == 0)
                        {
                            completed?.Invoke();
                        }
                    }
                );
            }
        }
    }
}
