using UnityEngine;

namespace Astar.Vanguard.Client.UI.Render
{
    /// <summary>
    /// Centralized visual tokens derived from PCL's default blue visual language.
    /// Values are re-expressed for Unity uGUI and are not a runtime dependency on PCL.
    /// </summary>
    internal static class PclDesignTokens
    {
        // The command center sits inside EFT's already-dark shell. A cool graphite
        // palette keeps the PCL hierarchy without introducing a white "web page"
        // island inside the game UI.
        public static readonly Color ForegroundPrimary = Rgb(0xE7EDF4);
        public static readonly Color ForegroundMuted = Rgb(0x9AA8B8);
        public static readonly Color ForegroundDisabled = Rgb(0x687583);

        // AccentDark is retained for API compatibility; in the dark palette it is
        // intentionally the bright/readable accent used for text and selected edges.
        public static readonly Color AccentDark = Rgb(0x86B9F6);
        public static readonly Color Accent = Rgb(0x438DE6);
        public static readonly Color AccentHover = Rgb(0x69A8F0);
        public static readonly Color AccentSoft = Rgb(0x315E8E, 0.92f);
        public static readonly Color AccentSofter = Rgb(0x263F59, 0.94f);
        public static readonly Color AccentBackground = Rgb(0x294963, 0.92f);

        public static readonly Color Canvas = Rgb(0x151C24, 0.98f);
        public static readonly Color Surface = Rgb(0x202933, 0.985f);
        public static readonly Color SurfaceAlt = Rgb(0x26313D, 0.98f);
        public static readonly Color SurfaceInset = Rgb(0x18212A, 0.985f);
        public static readonly Color SurfaceDisabled = Rgb(0x272E36, 0.96f);
        public static readonly Color SurfaceTransparent = new(0.12f, 0.16f, 0.20f, 0.015f);
        public static readonly Color ButtonIdle = Rgb(0x2A3642, 0.96f);

        public static readonly Color Border = Rgb(0x3C5874, 0.92f);
        public static readonly Color BorderNeutral = Rgb(0x465667, 0.86f);
        public static readonly Color Shadow = Rgb(0x05080C);

        public static readonly Color Success = Rgb(0x69C994);
        public static readonly Color Warning = Rgb(0xE2B35F);
        public static readonly Color Danger = Rgb(0xF17676);
        public static readonly Color DangerDark = Rgb(0xE35D5D);
        public static readonly Color DangerBackground = Rgb(0x633A3F, 0.66f);

        public const float RadiusButton = 3f;
        public const float RadiusCard = 5f;
        public const float RadiusListItem = 6f;
        public const float RadiusInput = 3f;
        public const float RadiusProgress = 3f;

        public const float SpacingXs = 4f;
        public const float SpacingSm = 8f;
        public const float SpacingMd = 12f;
        public const float SpacingLg = 16f;
        public const float SpacingXl = 24f;

        public const int TypographyCaption = 12;
        public const int TypographyBody = 13;
        public const int TypographyBodyLarge = 14;
        public const int TypographySubtitle = 16;
        public const int TypographyTitle = 20;
        public const int TypographyHero = 27;

        public const float ShadowIdleOpacity = 0.16f;
        public const float ShadowHoverOpacity = 0.34f;
        public const float DisabledOpacity = 0.55f;

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color Rgb(uint rgb, float alpha = 1f)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha
            );
        }
    }

    internal static class PclMotionTokens
    {
        public const float DurationFast = 0.10f;
        public const float DurationNormal = 0.18f;
        public const float DurationSlow = 0.30f;

        public const float HoverIn = 0.10f;
        public const float HoverOut = 0.18f;
        public const float PressIn = 0.08f;
        public const float PressRelease = 0.30f;
        public const float CardHover = 0.09f;
        public const float CardExpand = 0.22f;
        public const float ListHoverIn = 0.12f;
        public const float ListHoverOut = 0.18f;
        public const float PageEnter = 0.36f;
        public const float PageExit = 0.09f;
        public const float PageStagger = 0.025f;
        public const float LoadingCycle = 1.05f;

        public const float ButtonPressScale = 0.955f;
        public const float IconPressScale = 0.82f;
        public const float ListPressScale = 0.98f;
        public const float ListHoverStartScale = 0.82f;
        public const float PageEnterOffset = 16f;
        public const float PageExitOffset = 6f;
    }

    internal enum PclEase
    {
        Linear,
        InFluent,
        OutFluent,
        OutFluentStrong,
        OutBack,
    }

    internal static class PclEasing
    {
        public static float Evaluate(PclEase ease, float value)
        {
            var t = Mathf.Clamp01(value);
            return ease switch
            {
                PclEase.InFluent => t * t * t,
                PclEase.OutFluent => 1f - Mathf.Pow(1f - t, 3f),
                PclEase.OutFluentStrong => 1f - Mathf.Pow(1f - t, 5f),
                PclEase.OutBack => OutBack(t),
                _ => t,
            };
        }

        private static float OutBack(float t)
        {
            // Re-expressed from PCL's fluent/back motion shape with a restrained overshoot.
            const float c1 = 1.35f;
            const float c3 = c1 + 1f;
            var x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
