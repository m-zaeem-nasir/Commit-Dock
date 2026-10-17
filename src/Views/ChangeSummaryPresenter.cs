using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace CommitDock.Views
{
    public class ChangeSummaryPresenter : TextBlock
    {
        public static readonly StyledProperty<List<Models.Change>> ChangesProperty =
            AvaloniaProperty.Register<ChangeSummaryPresenter, List<Models.Change>>(nameof(Changes));

        public List<Models.Change> Changes
        {
            get => GetValue(ChangesProperty);
            set => SetValue(ChangesProperty, value);
        }

        protected override System.Type StyleKeyOverride => typeof(TextBlock);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChangesProperty)
                Rebuild();
        }

        private void Rebuild()
        {
            Inlines?.Clear();

            var changes = Changes;
            if (changes == null || changes.Count == 0)
                return;

            int modified = 0, added = 0, deleted = 0, renamed = 0;
            foreach (var c in changes)
            {
                switch (c.Index)
                {
                    case Models.ChangeState.Added:
                    case Models.ChangeState.Copied:
                        added++;
                        break;
                    case Models.ChangeState.Deleted:
                        deleted++;
                        break;
                    case Models.ChangeState.Renamed:
                        renamed++;
                        break;
                    default:
                        modified++;
                        break;
                }
            }

            Inlines ??= new InlineCollection();
            Append("✎", modified, "CommitDetail.Info.Summary.Modified", Brush("Brush.Modified"));
            Append("+", added, "CommitDetail.Info.Summary.Added", Brush("Brush.Added"));
            Append("−", deleted, "CommitDetail.Info.Summary.Deleted", Brush("Brush.Deleted"));
            Append("➜", renamed, "CommitDetail.Info.Summary.Renamed", Brush("Brush.Renamed"));
        }

        private void Append(string glyph, int count, string key, IBrush brush)
        {
            if (count == 0)
                return;

            if (Inlines.Count > 0)
                Inlines.Add(new Run("     "));

            Inlines.Add(new Run(glyph) { Foreground = brush, FontWeight = FontWeight.Bold });
            Inlines.Add(new Run($" {count} ") { FontWeight = FontWeight.SemiBold });
            Inlines.Add(new Run(App.Text(key)) { Foreground = brush });
        }

        private IBrush Brush(string key)
        {
            return this.TryFindResource(key, ActualThemeVariant, out var res) && res is IBrush brush ? brush : Foreground;
        }
    }
}
