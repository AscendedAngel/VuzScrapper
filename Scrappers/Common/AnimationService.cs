namespace VuzScrapper.Scrappers.Common;

internal sealed class AnimationService : IAsyncDisposable
{
    private readonly Lock _lock = new();
    private bool _isDisposed;

    private Task? _task;
    private string? _string;
    private CancellationTokenSource? _cts;

    private const int DotsCount = 4;
    private const int DelayMs = 250;

    public async Task Animate(string str)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        await DisposeInternal(DoneMessage);

        lock (_lock)
        {
            if (_isDisposed) return;
            _string = str;
            _cts = new CancellationTokenSource();
            _task = Animate(_cts.Token);
        }
    }

    public async Task Stop()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        await DisposeInternal(DoneMessage);
    }

    private async Task Animate(CancellationToken token)
    {
        try
        {
            var i = 0;
            while (!token.IsCancellationRequested)
            {
                lock (_lock) Console.Write('\r' + _string + new string('.', i + 1) + new string(' ', DotsCount - (i + 1)));
                i = (i + 1) % DotsCount;
                await Task.Delay(DelayMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void DoneMessage()
    {
        Console.WriteLine("Готово!");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        await DisposeInternal();

        _isDisposed = true;
    }

    private async Task DisposeInternal(Action? executeOnDone = null)
    {
        Task? oldTask = null;
        CancellationTokenSource? oldCts = null;

        lock (_lock)
        {
            if (_task is not null)
            {
                oldTask = _task;
                oldCts = _cts;

                _task = null;
                _cts = null;
            }
        }

        if (oldTask is null) return;

        await oldCts!.CancelAsync().ConfigureAwait(false);
        await oldTask.ConfigureAwait(false);

        oldTask.Dispose();
        oldCts.Dispose();

        executeOnDone?.Invoke();
    }
}