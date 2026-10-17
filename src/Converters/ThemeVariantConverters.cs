using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace CommitDock.Converters
{
    public static class ThemeVariantConverters
    {
        public static readonly FuncValueConverter<ThemeVariant, IBrush> ToAccentBrush =
            new(v => ResolveBrush(v, "SystemAccentColor", Colors.Gray));

        public static readonly FuncValueConverter<ThemeVariant, IBrush> ToContentsBrush =
            new(v => ResolveBrush(v, "Color.Contents", Colors.DimGray));

        private static IBrush ResolveBrush(ThemeVariant variant, string key, Color fallback)
        {
            // "Default" follows the OS, so preview it with whatever is active right now.
            if (variant == null || variant == ThemeVariant.Default)
                variant = Application.Current?.ActualThemeVariant;

            if (Application.Current is { } app && app.TryGetResource(key, variant, out var value) && value is Color color)
                return new SolidColorBrush(color);

            return new SolidColorBrush(fallback);
        }
    }
}
