namespace YtDlpGUI.Services;

public interface IUiDispatcher
{
    Task EnqueueAsync(Action action);
}
