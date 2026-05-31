using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpGUI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string statusMessage = string.Empty;
}
