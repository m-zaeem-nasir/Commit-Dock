using CommunityToolkit.Mvvm.ComponentModel;

namespace CommitDock.ViewModels
{
    public class ICommandPalette : ObservableObject
    {
        public void Open()
        {
            var host = App.GetLauncher();
            if (host != null)
                host.CommandPalette = this;
        }

        public void Close()
        {
            var host = App.GetLauncher();
            if (host != null)
                host.CommandPalette = null;
        }
    }
}
