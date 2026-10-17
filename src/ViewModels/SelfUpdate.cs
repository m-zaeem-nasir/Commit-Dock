using CommunityToolkit.Mvvm.ComponentModel;

namespace CommitDock.ViewModels
{
    public class SelfUpdate : ObservableObject
    {
        public object Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        private object _data = null;
    }
}
