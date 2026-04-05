using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using SharpFM.Scripting;

namespace SharpFM.Editors;

/// <summary>
/// Editor for script clips (Mac-XMSS, Mac-XMSC). Wraps a TextDocument containing the
/// plain-text script representation and handles FmScript model round-tripping.
/// </summary>
public class ScriptClipEditor : IClipEditor
{
    private CancellationTokenSource? _debounceCts;
    private FmScript _script;

    public event EventHandler? ContentChanged;

    /// <summary>The TextDocument bound to the AvaloniaEdit script editor.</summary>
    public TextDocument Document { get; }

    public bool IsPartial { get; private set; }

    public ScriptClipEditor(string? xml)
    {
        _script = FmScript.FromXml(xml ?? "");
        Document = new TextDocument(_script.ToDisplayText());

        Document.TextChanged += (_, _) => ScheduleContentChanged();
    }

    private void ScheduleContentChanged()
    {
        // Cancel any pending debounce — truly resets the timer
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(() => ContentChanged?.Invoke(this, EventArgs.Empty));
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    public string ToXml()
    {
        try
        {
            _script = FmScript.FromDisplayText(Document.Text);
            IsPartial = false;
            return _script.ToXml();
        }
        catch
        {
            IsPartial = true;
            return _script.ToXml();
        }
    }

    public void FromXml(string xml)
    {
        _script = FmScript.FromXml(xml);
        Document.Text = _script.ToDisplayText();
    }
}
