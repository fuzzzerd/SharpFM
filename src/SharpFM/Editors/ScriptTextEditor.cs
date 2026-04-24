using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Scripting;
using SharpFM.Scripting.Editor;
using SharpFM.ViewModels;
using TextMateSharp.Grammars;

namespace SharpFM.Editors;

/// <summary>
/// A specialized <see cref="TextEditor"/> for script clips. Owns its
/// TextMate installation and <see cref="ScriptEditorController"/> so it
/// can be dropped into a <c>DataTemplate</c> without any parent wiring —
/// each instance self-installs on construction and tears down on detach.
/// DataContext is expected to be a <see cref="ScriptClipEditor"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public class ScriptTextEditor : TextEditor, IDisposable
{
    // Avalonia 12 matches styles by exact type. Without this, a subclass of
    // TextEditor gets no control template and renders blank — the editor's
    // TextArea/TextView visual tree is never built.
    protected override Type StyleKeyOverride => typeof(TextEditor);

    // Shared across every script editor in the process. Building RegistryOptions
    // pulls bundled themes into memory; doing that once (and reusing the result)
    // measurably improves the hitch on first tab realisation for new clips.
    private static readonly RegistryOptions SharedRegistry =
        new((ThemeName)(int)ThemeName.DarkPlus);

    private static readonly FmScriptRegistryOptions SharedFmRegistry =
        new(SharedRegistry);

    private readonly TextMate.Installation _textMate;
    private readonly ScriptEditorController _controller;

    public ScriptTextEditor()
    {
        _textMate = this.InstallTextMate(SharedFmRegistry);
        _textMate.SetGrammar(FmScriptRegistryOptions.ScopeName);

        _controller = new ScriptEditorController(this);
        _controller.StatusMessageRaised += OnStatusMessageRaised;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not ScriptClipEditor clipEditor) return;

        Document = clipEditor.Document;
        _controller.AttachClipEditor(clipEditor);
    }

    // Intentionally no dispose-on-detach. Tab switches reparent this control
    // many times; tearing down TextMate on each detach would force a full
    // grammar/theme reinstall on every reattach. Lifetime is owned by the
    // ClipViewModel, which calls <see cref="Dispose"/> when the clip is
    // removed or its editor is replaced.

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _controller.StatusMessageRaised -= OnStatusMessageRaised;
        _controller.Dispose();
        _textMate.Dispose();
    }

    private void OnStatusMessageRaised(object? sender, StatusMessageEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window?.DataContext is MainWindowViewModel vm)
            vm.ShowStatusMessage(e.Message, e.IsError);
    }
}
