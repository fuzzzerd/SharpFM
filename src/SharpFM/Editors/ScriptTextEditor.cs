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
public class ScriptTextEditor : TextEditor
{
    // Avalonia 12 matches styles by exact type. Without this, a subclass of
    // TextEditor gets no control template and renders blank — the editor's
    // TextArea/TextView visual tree is never built.
    protected override Type StyleKeyOverride => typeof(TextEditor);

    private readonly TextMate.Installation _textMate;
    private readonly ScriptEditorController _controller;

    public ScriptTextEditor()
    {
        var registry = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);
        var fmScriptRegistry = new FmScriptRegistryOptions(registry);
        _textMate = this.InstallTextMate(fmScriptRegistry);
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

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

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
