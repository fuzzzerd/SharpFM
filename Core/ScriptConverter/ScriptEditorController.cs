using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;

namespace SharpFM.Core.ScriptConverter;

/// <summary>
/// Manages script editor behavior: validation, completion, tooltips, and document tracking.
/// Extracted from MainWindow to keep UI code thin and this logic independently testable.
/// </summary>
public class ScriptEditorController : IDisposable
{
    private readonly TextEditor _editor;
    private readonly DispatcherTimer _validationTimer;
    private ErrorMarkerRenderer? _errorRenderer;
    private CompletionWindow? _completionWindow;

    public ScriptEditorController(TextEditor editor)
    {
        _editor = editor;

        _validationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _validationTimer.Tick += (_, _) =>
        {
            _validationTimer.Stop();
            RunValidation();
        };

        // Bracket matching
        var bracketRenderer = new BracketMatchRenderer(_editor.TextArea);
        _editor.TextArea.TextView.BackgroundRenderers.Add(bracketRenderer);

        // Multi-line statement highlighting
        var statementRenderer = new StatementHighlightRenderer(_editor.TextArea);
        _editor.TextArea.TextView.BackgroundRenderers.Add(statementRenderer);

        // Wire events
        _editor.TextArea.TextEntered += OnTextEntered;
        _editor.PointerMoved += OnPointerMoved;

        // Attach to initial document and track document changes
        AttachToDocument(_editor.Document);
        _editor.PropertyChanged += (_, args) =>
        {
            if (args.Property.Name == "Document" && _editor.Document != null)
                AttachToDocument(_editor.Document);
        };
    }

    private void AttachToDocument(TextDocument document)
    {
        if (_errorRenderer != null)
            _editor.TextArea.TextView.BackgroundRenderers.Remove(_errorRenderer);

        _errorRenderer = new ErrorMarkerRenderer(document);
        _editor.TextArea.TextView.BackgroundRenderers.Add(_errorRenderer);

        document.TextChanged += (_, _) =>
        {
            _validationTimer.Stop();
            _validationTimer.Start();
        };

        RunValidation();
    }

    private void RunValidation()
    {
        if (_errorRenderer == null) return;

        var text = _editor.Document.Text;
        var diagnostics = ScriptValidator.Validate(text);
        _errorRenderer.UpdateDiagnostics(diagnostics);
        _editor.TextArea.TextView.InvalidateLayer(_errorRenderer.Layer);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_errorRenderer == null) return;

        var pos = _editor.GetPositionFromPoint(e.GetPosition(_editor));
        if (pos == null)
        {
            ToolTip.SetIsOpen(_editor, false);
            return;
        }

        var offset = _editor.Document.GetOffset(pos.Value.Location);
        var diag = _errorRenderer.GetDiagnosticAtOffset(offset);

        if (diag != null)
        {
            ToolTip.SetTip(_editor, diag.Message);
            ToolTip.SetIsOpen(_editor, true);
        }
        else
        {
            ToolTip.SetIsOpen(_editor, false);
        }
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (_completionWindow != null) return;

        var caret = _editor.TextArea.Caret;
        var line = _editor.Document.GetLineByNumber(caret.Line);
        var lineText = _editor.Document.GetText(line.Offset, line.Length);
        var col = caret.Column - 1;

        var (context, items) = FmScriptCompletionProvider.GetCompletions(lineText, col);
        if (context == CompletionContext.None || items.Count == 0) return;

        _completionWindow = new CompletionWindow(_editor.TextArea);

        if (context == CompletionContext.StepName)
        {
            var wordStart = lineText.Length - lineText.TrimStart().Length;
            _completionWindow.StartOffset = line.Offset + wordStart;
        }

        foreach (var item in items)
            _completionWindow.CompletionList.CompletionData.Add(item);

        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }

    public void Dispose()
    {
        _validationTimer.Stop();
        _editor.TextArea.TextEntered -= OnTextEntered;
        _editor.PointerMoved -= OnPointerMoved;
    }
}
