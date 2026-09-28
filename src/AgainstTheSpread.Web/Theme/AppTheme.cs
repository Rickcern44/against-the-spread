using MudBlazor;

namespace AgainstTheSpread.Web.Theme;

/// <summary>Broadcast-scoreboard dark theme: near-black surfaces, gold ticker accent, condensed display type.</summary>
public static class AppTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#05070b",
            Background = "#0a0e14",
            BackgroundGray = "#0d1219",
            Surface = "#121821",
            AppbarBackground = "#0a0e14",
            DrawerBackground = "#0a0e14",
            DrawerText = "#93a4bc",
            Primary = "#f2b705",
            PrimaryContrastText = "#0a0e14",
            Secondary = "#2dd4bf",
            Tertiary = "#ef4444",
            Success = "#22c55e",
            Warning = "#f2b705",
            Error = "#ef4444",
            Info = "#38bdf8",
            TextPrimary = "#f4f6fb",
            TextSecondary = "#8794a8",
            ActionDefault = "#8794a8",
            ActionDisabled = "#3a4351",
            Divider = "#1e2733",
            LinesDefault = "#1e2733",
            TableLines = "#1e2733",
            TableStriped = "#0d1219",
        },
        Typography = new Typography
        {
            Default = new Default
            {
                FontFamily = ["IBM Plex Sans", "sans-serif"],
                FontSize = "0.9rem",
            },
            H1 = new H1 { FontFamily = ["Teko", "sans-serif"], FontSize = "3.5rem", FontWeight = 600, LetterSpacing = ".01em" },
            H2 = new H2 { FontFamily = ["Teko", "sans-serif"], FontSize = "2.5rem", FontWeight = 600, LetterSpacing = ".01em" },
            H3 = new H3 { FontFamily = ["Teko", "sans-serif"], FontSize = "2rem", FontWeight = 500 },
            H4 = new H4 { FontFamily = ["Teko", "sans-serif"], FontSize = "1.6rem", FontWeight = 500, LetterSpacing = ".02em" },
            H6 = new H6 { FontFamily = ["IBM Plex Sans", "sans-serif"], FontSize = "1rem", FontWeight = 600 },
            Button = new Button { FontFamily = ["IBM Plex Sans", "sans-serif"], FontWeight = 600, TextTransform = "none" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "260px",
        },
    };
}
