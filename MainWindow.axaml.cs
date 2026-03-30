using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.TextMate;
using SharpFM.Core.ScriptConverter;
using TextMateSharp.Grammars;

namespace SharpFM;

public partial class MainWindow : Window
{
    private readonly RegistryOptions _registryOptions;
    private readonly int _currentTheme = (int)ThemeName.DarkPlus;
    private readonly TextMate.Installation _xmlTextMateInstallation;
    private readonly TextMate.Installation? _scriptTextMateInstallation;
    private readonly TextEditor _xmlEditor;
    private readonly TextEditor? _scriptEditor;
    private ErrorMarkerRenderer? _errorRenderer;
    private DispatcherTimer? _validationTimer;
    private CompletionWindow? _completionWindow;

    public MainWindow()
    {
        InitializeComponent();

        _xmlEditor = this.FindControl<TextEditor>("avaloniaEditor") ?? throw new Exception("no control");
        _scriptEditor = this.FindControl<TextEditor>("scriptEditor");

        _registryOptions = new RegistryOptions((ThemeName)_currentTheme);

        // XML editor: XML syntax highlighting
        _xmlTextMateInstallation = _xmlEditor.InstallTextMate(_registryOptions);
        Language xmlLang = _registryOptions.GetLanguageByExtension(".xml");
        _xmlTextMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(xmlLang.Id));

        // Script editor: FM Script syntax highlighting + error markers
        if (_scriptEditor != null)
        {
            var fmScriptRegistry = new FmScriptRegistryOptions(_registryOptions);
            _scriptTextMateInstallation = _scriptEditor.InstallTextMate(fmScriptRegistry);
            _scriptTextMateInstallation.SetGrammar(FmScriptRegistryOptions.ScopeName);

            // Error highlighting
            _errorRenderer = new ErrorMarkerRenderer(_scriptEditor.Document);
            _scriptEditor.TextArea.TextView.BackgroundRenderers.Add(_errorRenderer);

            // Debounced validation on text changes
            _validationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _validationTimer.Tick += (_, _) =>
            {
                _validationTimer.Stop();
                RunValidation();
            };
            _scriptEditor.Document.TextChanged += (_, _) =>
            {
                _validationTimer.Stop();
                _validationTimer.Start();
            };

            // Bracket matching
            var bracketRenderer = new BracketMatchRenderer(_scriptEditor.TextArea);
            _scriptEditor.TextArea.TextView.BackgroundRenderers.Add(bracketRenderer);

            // Multi-line statement highlighting
            var statementRenderer = new StatementHighlightRenderer(_scriptEditor.TextArea);
            _scriptEditor.TextArea.TextView.BackgroundRenderers.Add(statementRenderer);

            // Intellisense
            _scriptEditor.TextArea.TextEntered += OnScriptTextEntered;

            // Error tooltips on hover
            _scriptEditor.PointerMoved += OnScriptPointerMoved;
        }
    }

    private void OnScriptPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_scriptEditor == null || _errorRenderer == null) return;

        var pos = _scriptEditor.GetPositionFromPoint(e.GetPosition(_scriptEditor));
        if (pos == null)
        {
            ToolTip.SetIsOpen(_scriptEditor, false);
            return;
        }

        var offset = _scriptEditor.Document.GetOffset(pos.Value.Location);
        var diag = _errorRenderer.GetDiagnosticAtOffset(offset);

        if (diag != null)
        {
            ToolTip.SetTip(_scriptEditor, diag.Message);
            ToolTip.SetIsOpen(_scriptEditor, true);
        }
        else
        {
            ToolTip.SetIsOpen(_scriptEditor, false);
        }
    }

    private void OnScriptTextEntered(object? sender, TextInputEventArgs e)
    {
        if (_scriptEditor == null || _completionWindow != null) return;

        var caret = _scriptEditor.TextArea.Caret;
        var line = _scriptEditor.Document.GetLineByNumber(caret.Line);
        var lineText = _scriptEditor.Document.GetText(line.Offset, line.Length);
        var col = caret.Column - 1;

        var (context, items) = FmScriptCompletionProvider.GetCompletions(lineText, col);
        if (context == CompletionContext.None || items.Count == 0) return;

        _completionWindow = new CompletionWindow(_scriptEditor.TextArea);

        // Set the start offset for filtering
        if (context == CompletionContext.StepName)
        {
            // Filter from start of current word on the line
            var wordStart = lineText.Length - lineText.TrimStart().Length;
            _completionWindow.StartOffset = line.Offset + wordStart;
        }

        foreach (var item in items)
            _completionWindow.CompletionList.CompletionData.Add(item);

        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }

    private void RunValidation()
    {
        if (_scriptEditor == null || _errorRenderer == null) return;

        var text = _scriptEditor.Document.Text;
        var diagnostics = ScriptValidator.Validate(text);
        _errorRenderer.UpdateDiagnostics(diagnostics);
        _scriptEditor.TextArea.TextView.InvalidateLayer(_errorRenderer.Layer);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _validationTimer?.Stop();
        _xmlTextMateInstallation.Dispose();
        _scriptTextMateInstallation?.Dispose();
    }
}