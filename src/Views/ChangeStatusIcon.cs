using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace CommitDock.Views
{
    public class ChangeStatusIcon : Control
    {
        private static readonly string[] INDICATOR = ["?", "M", "T", "+", "−", "➜", "❏", "?", "!"];
        private static readonly string[] COLOR_KEY =
        [
            null,
            "Color.Modified",  // modified
            "Color.Modified",  // type changed
            "Color.Added",     // added
            "Color.Deleted",   // deleted
            "Color.Renamed",   // renamed
            "Color.Modified",  // copied
            "Color.Untracked", // untracked
            "Color.Warning",   // conflicted
        ];

        public static readonly DirectProperty<ChangeStatusIcon, bool> IsUnstagedChangeProperty =
            AvaloniaProperty.RegisterDirect<ChangeStatusIcon, bool>(
                nameof(IsUnstagedChange),
                static o => o.IsUnstagedChange,
                static (o, v) => o.IsUnstagedChange = v);

        public bool IsUnstagedChange
        {
            get => _isUnstagedChange;
            set => SetAndRaise(IsUnstagedChangeProperty, ref _isUnstagedChange, value);
        }

        public static readonly DirectProperty<ChangeStatusIcon, Models.Change> ChangeProperty =
            AvaloniaProperty.RegisterDirect<ChangeStatusIcon, Models.Change>(
                nameof(Change),
                static o => o.Change,
                static (o, v) => o.Change = v);

        public Models.Change Change
        {
            get => _change;
            set => SetAndRaise(ChangeProperty, ref _change, value);
        }

        public override void Render(DrawingContext context)
        {
            if (_change == null || Bounds.Width <= 0)
                return;

            var typeface = new Typeface("fonts:CommitDock#JetBrains Mono NL");
            var idx = (int)(_isUnstagedChange ? _change.WorkTree : _change.Index);
            var indicator = INDICATOR[idx];
            var key = COLOR_KEY[idx];
            if (key == null || !this.TryFindResource(key, ActualThemeVariant, out var res) || res is not Color color)
                return;

            var txt = new FormattedText(
                indicator,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(typeface.FontFamily, FontStyle.Normal, FontWeight.Bold),
                Bounds.Width * 0.72,
                new SolidColorBrush(color));

            var fill = new SolidColorBrush(Color.FromArgb(ActualThemeVariant.IsDark() ? (byte)0x33 : (byte)0x26, color.R, color.G, color.B));
            var corner = (float)Math.Max(3, Bounds.Width / 4);
            var textOrigin = new Point((Bounds.Width - txt.Width) * 0.5, (Bounds.Height - txt.Height) * 0.5);
            context.DrawRectangle(fill, null, new Rect(0, 0, Bounds.Width, Bounds.Height), corner, corner);
            context.DrawText(txt, textOrigin);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsUnstagedChangeProperty || change.Property == ChangeProperty)
                InvalidateVisual();
            else if (change.Property.Name == nameof(ActualThemeVariant) && change.NewValue != null)
                InvalidateVisual();
        }

        private bool _isUnstagedChange = false;
        private Models.Change _change = null;
    }
}
