using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SharpFM.Editors;

/// <summary>
/// Debounces calls to an action: Trigger() schedules the action to run after
/// <paramref name="delayMs"/> milliseconds, and subsequent Trigger() calls cancel
/// and reschedule. Uses CancellationTokenSource so pending invocations are
/// truly cancelled rather than reset — avoiding a class of timer races where
/// an about-to-fire callback slips through.
/// The action runs on the Avalonia UI thread via Dispatcher.UIThread.Post.
/// </summary>
public sealed class DebouncedEventRaiser
{
    private readonly int _delayMs;
    private readonly Action _onFired;
    private CancellationTokenSource? _cts;

    public DebouncedEventRaiser(int delayMs, Action onFired)
    {
        _delayMs = delayMs;
        _onFired = onFired;
    }

    public void Trigger()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delayMs, token);
                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(_onFired);
            }
            catch (OperationCanceledException) { }
        }, token);
    }
}
