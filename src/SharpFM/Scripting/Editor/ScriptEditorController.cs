using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using SharpFM.Diagnostics;
using SharpFM.Editors;
using SharpFM.Editors.SealedSteps;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Manages script editor behavior: validation, completion, tooltips, and document tracking.
/// </summary>
[ExcludeFromCodeCoverage]
public class ScriptEditorController : IDisposable
{
    private readonly TextEditor _editor;
    private readonly DispatcherTimer _validationTimer;
    private ErrorMarkerRenderer? _errorRenderer;
    private CompletionWindow? _completionWindow;
    private ScriptClipEditor? _clipEditor;

    // Sealed-step renderers are installed per clip and must be removed
    // before the next AttachClipEditor call. Stale instances hold anchor
    // references against their original document, and AvaloniaEdit will
    // keep invoking them against whatever document the shared TextView
    // is now displaying — offsets go out of range and throw.
    private SealedStepSquiggleRenderer? _sealedSquiggleRenderer;
    private SealedStepItalicColorizer? _sealedItalicColorizer;
    private SealedStepCogGenerator? _cogGenerator;

    /// <summary>
    /// Raised when the controller wants to surface a transient message
    /// (e.g. "copy blocked across sealed line"). Handled by MainWindow,
    /// which routes to the view model's ShowStatusMessage.
    /// </summary>
    public event EventHandler<StatusMessageEventArgs>? StatusMessageRaised;

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

        // Continuation rail for multi-line calc steps
        var continuationRenderer = new ContinuationLineRenderer(_editor.TextArea);
        _editor.TextArea.TextView.BackgroundRenderers.Add(continuationRenderer);

        // Replace AvaloniaEdit's built-in line-number margin with a step-index
        // margin (FileMaker-style: one number per script step, regardless of
        // physical line count).
        InstallStepIndexMargin();

        // Auto-indent on Enter inside multi-line calc regions.
        _editor.TextArea.IndentationStrategy = new ContinuationIndentStrategy();

        // Copy/Cut interception — block selections that span a sealed line.
        _editor.KeyDown += OnKeyDownGuardSealed;

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

    private async void RunValidation()
    {
        if (_errorRenderer == null) return;

        var text = _editor.Document.Text;

        try
        {
            var diagnostics = await System.Threading.Tasks.Task.Run(
                () => ScriptValidator.Validate(text));

            // Only invalidate when the diagnostics actually changed —
            // typing inside a line that's still good (or still bad in
            // the same way) returns identical lists every cycle, and an
            // invalidation here forces a full TextView render pass.
            if (_errorRenderer.UpdateDiagnostics(diagnostics))
                _editor.TextArea.TextView.InvalidateLayer(_errorRenderer.Layer);
        }
        catch
        {
            // Validation failure during typing is non-fatal
        }
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

    /// <summary>
    /// Minimum identifier-prefix length before the script editor pops the
    /// completion window automatically. Two characters is enough for the
    /// list to narrow meaningfully (Set, Sho, Lay…) and keeps the popup
    /// out of the way on the first keystroke when the user often isn't
    /// looking for a step name suggestion.
    /// </summary>
    private const int AutoCompleteMinPrefix = 2;

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (_completionWindow != null) return;
        if (string.IsNullOrEmpty(e.Text)) return;

        var ch = e.Text[0];
        var isIdentifierTrigger = IsTriggerChar(ch);
        // Argument-boundary characters: ( and ; should pop the menu *only*
        // when we end up in a CalcParamValue context (e.g. Get( →
        // selectors, JSONSetElement(j;k;v; → JSON types). Without this,
        // every ( and ; would spam the window with the full identifier
        // catalog. Mirrors the calculation editor's gating logic.
        var isArgBoundary = ch == '(' || ch == ';';
        if (!isIdentifierTrigger && !isArgBoundary) return;

        TryShowCompletions(isArgBoundary);
    }

    private static bool IsTriggerChar(char c) =>
        char.IsLetter(c) || c == '_';

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';

    private void TryShowCompletions(bool isArgBoundaryTrigger = false)
    {
        var caret = _editor.TextArea.Caret;
        var line = _editor.Document.GetLineByNumber(caret.Line);
        var lineText = _editor.Document.GetText(line.Offset, line.Length);
        var col = caret.Column - 1;

        // Walk back over the identifier prefix immediately before the
        // caret. We need this in two places: the min-prefix gate below
        // and the StartOffset anchor for calc-context completions.
        var prefixStart = col;
        while (prefixStart > 0 && IsIdentifierChar(lineText[prefixStart - 1]))
            prefixStart--;

        // Require AutoCompleteMinPrefix consecutive identifier characters
        // before showing — skips the empty-prefix popup on the first
        // keystroke. Skipped for ( / ; triggers because those are
        // argument-boundary characters where the prefix is intentionally
        // empty (e.g. `Get(` should pop the selector list immediately).
        if (!isArgBoundaryTrigger && col - prefixStart < AutoCompleteMinPrefix) return;

        var (context, items) = FmScriptCompletionProvider.GetCompletions(lineText, col);
        if (context == CompletionContext.None || items.Count == 0) return;

        // Only commit to opening the window on ( or ; triggers when the
        // result is a function-param keyword list — anything else and the
        // user just typed a separator and doesn't want a popup.
        if (isArgBoundaryTrigger && context != CompletionContext.CalcParamValue) return;

        _completionWindow = new CompletionWindow(_editor.TextArea);

        if (context == CompletionContext.StepName)
        {
            var wordStart = lineText.Length - lineText.TrimStart().Length;
            _completionWindow.StartOffset = line.Offset + wordStart;
        }
        else if (context == CompletionContext.ParamLabel)
        {
            _completionWindow.StartOffset = _editor.CaretOffset;
        }
        else if (context == CompletionContext.CalcExpression
                 || context == CompletionContext.CalcParamValue)
        {
            // Anchor at the start of the identifier prefix so accepting
            // an item replaces the partial prefix rather than appending
            // after it.
            _completionWindow.StartOffset = line.Offset + prefixStart;
        }

        foreach (var item in items)
            _completionWindow.CompletionList.CompletionData.Add(item);

        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }

    private void InstallStepIndexMargin()
    {
        // Strip the default line-number margin (and its companion separator
        // line) so we can replace them. AvaloniaEdit installs both when
        // ShowLineNumbers is true. We leave any other left margins in place.
        _editor.ShowLineNumbers = false;

        var margins = _editor.TextArea.LeftMargins;
        for (int i = margins.Count - 1; i >= 0; i--)
        {
            var m = margins[i];
            if (m is AvaloniaEdit.Editing.LineNumberMargin
                || m.GetType().Name == "Line") // separator line installed by AvaloniaEdit
            {
                margins.RemoveAt(i);
            }
        }

        margins.Insert(0, new StepIndexMargin());
    }

    /// <summary>
    /// Attach the controller to a <see cref="ScriptClipEditor"/> so the
    /// sealed-step visuals (squiggle, italic, cog, read-only provider)
    /// can find the anchor cache. Call whenever the underlying clip
    /// editor is swapped (e.g. user selects a different script clip).
    /// Previous-clip renderers are removed before new ones are installed
    /// so stale anchors can't leak into the new document's render loop.
    /// </summary>
    public void AttachClipEditor(ScriptClipEditor clipEditor)
    {
        DetachSealedStepRenderers();

        _clipEditor = clipEditor;

        // Squiggle + italic renderers read from clipEditor.SealedAnchors.
        _sealedSquiggleRenderer = new SealedStepSquiggleRenderer(_editor.TextArea, clipEditor);
        _editor.TextArea.TextView.BackgroundRenderers.Add(_sealedSquiggleRenderer);

        _sealedItalicColorizer = new SealedStepItalicColorizer(clipEditor);
        _editor.TextArea.TextView.LineTransformers.Add(_sealedItalicColorizer);

        // Cog-button inline element + click handler.
        _cogGenerator = new SealedStepCogGenerator(clipEditor);
        _cogGenerator.CogClicked += OnCogClicked;
        _editor.TextArea.TextView.ElementGenerators.Add(_cogGenerator);

        // Read-only protection: user can't type inside a sealed line.
        _editor.TextArea.ReadOnlySectionProvider =
            new SealedStepReadOnlyProvider(clipEditor.Document, clipEditor);
    }

    private void DetachSealedStepRenderers()
    {
        if (_sealedSquiggleRenderer != null)
        {
            _editor.TextArea.TextView.BackgroundRenderers.Remove(_sealedSquiggleRenderer);
            _sealedSquiggleRenderer = null;
        }
        if (_sealedItalicColorizer != null)
        {
            _editor.TextArea.TextView.LineTransformers.Remove(_sealedItalicColorizer);
            _sealedItalicColorizer = null;
        }
        if (_cogGenerator != null)
        {
            _cogGenerator.CogClicked -= OnCogClicked;
            _editor.TextArea.TextView.ElementGenerators.Remove(_cogGenerator);
            _cogGenerator = null;
        }
        // ReadOnlySectionProvider is a single-slot property — the caller
        // (AttachClipEditor) assigns a fresh provider immediately after
        // detach, so the old reference is replaced, not cleared here.
    }

    private async void OnCogClicked(object? sender, TextAnchor anchor)
    {
        if (_clipEditor == null) return;

        // Locate the current XML for this sealed step.
        if (!_clipEditor.TryGetSealedXml(anchor, out var xml)) return;

        // Find the parent Window so the modal dialog gets a proper owner.
        var owner = TopLevelWindow(_editor);
        if (owner == null) return;

        var edited = await RawStepEditorWindow.EditAsync(owner, xml);
        if (edited != null)
        {
            _clipEditor.UpdateSealedXml(anchor, edited);
        }
    }

    private static Window? TopLevelWindow(Avalonia.Controls.Control control)
    {
        var tl = Avalonia.Controls.TopLevel.GetTopLevel(control);
        return tl as Window;
    }

    private void OnKeyDownGuardSealed(object? sender, KeyEventArgs e)
    {
        if (_clipEditor == null) return;
        var modifier = e.KeyModifiers;
        if ((modifier & KeyModifiers.Control) == 0) return;
        if (e.Key != Key.C && e.Key != Key.X) return;

        var sel = _editor.TextArea.Selection;
        if (sel.IsEmpty) return;

        var startOffset = _editor.Document.GetOffset(sel.StartPosition.Location);
        var endOffset = _editor.Document.GetOffset(sel.EndPosition.Location);

        // Collect sealed-line offsets for the overlap test.
        var sealedRanges = new List<(int, int)>();
        foreach (var anchor in _clipEditor.SealedAnchors)
        {
            if (anchor.IsDeleted) continue;
            var line = _editor.Document.GetLineByOffset(anchor.Offset);
            sealedRanges.Add((line.Offset, line.EndOffset));
        }

        if (SealedSelectionCheck.SpansSealed(startOffset, endOffset, sealedRanges))
        {
            e.Handled = true;
            var action = e.Key == Key.X ? "Cut" : "Copy";
            StatusMessageRaised?.Invoke(this, new StatusMessageEventArgs(
                $"{action} blocked: selection crosses a sealed step. Edit it via the cog icon.",
                isError: true));
        }
    }

    public void Dispose()
    {
        _validationTimer.Stop();
        _editor.TextArea.TextEntered -= OnTextEntered;
        _editor.PointerMoved -= OnPointerMoved;
        _editor.KeyDown -= OnKeyDownGuardSealed;
    }
}
