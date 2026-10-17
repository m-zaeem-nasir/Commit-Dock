using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace SourceGit.Converters
{
    public static class PopupConverters
    {
        public static readonly FuncValueConverter<ViewModels.Popup, string> ToConfirmText =
            new FuncValueConverter<ViewModels.Popup, string>(v =>
            {
                if (v == null)
                    return App.Text("Sure");

                // Most popups have a title resource named after their view model, e.g. `Text.Push`.
                var title = App.Current?.FindResource($"Text.{v.GetType().Name}") as string;
                if (string.IsNullOrWhiteSpace(title))
                    return App.Text("Sure");

                title = title.Trim().TrimEnd('.', '…', ':').Trim();
                return title.Length is > 0 and <= 28 ? title : App.Text("Sure");
            });

        public static readonly FuncValueConverter<ViewModels.Popup, bool> IsDestructive =
            new FuncValueConverter<ViewModels.Popup, bool>(v => v != null && DESTRUCTIVE.Contains(v.GetType().Name));

        private static readonly HashSet<string> DESTRUCTIVE =
        [
            "ClearStashes",
            "DeinitSubmodule",
            "DeleteBranch",
            "DeleteMultipleBranches",
            "DeleteMultipleTags",
            "DeleteRemote",
            "DeleteRepositoryNode",
            "DeleteSubmodule",
            "DeleteTag",
            "Discard",
            "DropStash",
            "LFSPrune",
            "PruneRemote",
            "PruneWorktrees",
            "RemoveWorktree",
            "Reset",
            "ResetWithoutCheckout",
        ];
    }
}
