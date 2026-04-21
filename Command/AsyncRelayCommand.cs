using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi.Commands;

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Action<Exception>? _onError;

    // volatile đảm bảo read/write không bị CPU cache → thread-safe cho bool
    private volatile bool _isExecuting;

    /// <param name="executeAsync">Async action cần thực thi</param>
    /// <param name="canExecute">Điều kiện cho phép chạy</param>
    /// <param name="onError">
    ///     Callback xử lý exception. Nếu null → exception sẽ được re-throw
    ///     lên Application.DispatcherUnhandledException
    /// </param>
    public AsyncRelayCommand(
        Func<object?, Task> executeAsync,
        Func<object?, bool>? canExecute = null,
        Action<Exception>? onError = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onError = onError;
    }

    // Shortcut không cần parameter
    public AsyncRelayCommand(
        Func<Task> executeAsync,
        Func<bool>? canExecute = null,
        Action<Exception>? onError = null)
        : this(_ => executeAsync(),
            canExecute is null ? null : _ => canExecute(),
            onError)
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    // async void là bắt buộc vì ICommand.Execute trả về void
    // Exception phải được bắt thủ công, không thể dùng await ở ngoài
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _executeAsync(parameter);
        }
        catch (Exception ex) when (_onError is not null)
        {
            // Có handler → giao cho caller xử lý (hiện toast, log, v.v.)
            _onError(ex);
        }
        // Không catch nếu _onError null → bubble lên DispatcherUnhandledException
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public void RaiseCanExecuteChanged()
        => CommandManager.InvalidateRequerySuggested();
}