using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.TextMate;
using SharpFM.Model.Schema;
using SharpFM.Scripting;
using SharpFM.Scripting.Editor;
using TextMateSharp.Grammars;

namespace SharpFM.Schema.Editor;

[ExcludeFromCodeCoverage]
public partial class CalculationEditorWindow : Window
{
    private readonly FmField _field;
    private readonly TextMate.Installation _textMateInstallation;
    private readonly TextEditor _editor;
    private readonly CalcCompletionContextProvider _completionContext;
    private CompletionWindow? _completionWindow;

    public bool Saved { get; private set; }

    // Required by XAML loader
    public CalculationEditorWindow() : this(new FmField(), null) { }

    public CalculationEditorWindow(FmField field) : this(field, null) { }

    public CalculationEditorWindow(FmField field, FmTable? currentTable)
    {
        InitializeComponent();

        _field = field;

        // Set up FM calculation syntax highlighting
        var registryOptions = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);
        var fmRegistry = new FmLanguageRegistryOptions(registryOptions);
        _editor = this.FindControl<TextEditor>("calcEditor")!;
        _textMateInstallation = _editor.InstallTextMate(fmRegistry);
        _textMateInstallation.SetGrammar(FmLanguageRegistryOptions.CalcScopeName);

        // Populate fields
        _editor.Text = field.Calculation ?? "";
        var contextBox = this.FindControl<TextBox>("contextTableBox")!;
        contextBox.Text = field.CalculationContext ?? "";
        var alwaysCheck = this.FindControl<CheckBox>("alwaysEvaluateCheck")!;
        alwaysCheck.IsChecked = field.AlwaysEvaluate;

        _completionContext = new CalcCompletionContextProvider(
            getDocumentText: () => _editor.Document.Text,
            currentTable: currentTable);

        // Completion: pop the menu on every text input. Same trigger model
        // the script editor uses, kept inline rather than introducing a
        // controller — calc editor has no other adornments to coordinate.
        _editor.TextArea.TextEntered += OnTextEntered;
        _editor.TextArea.TextEntering += OnTextEntering;

        // Wire buttons
        this.FindControl<Button>("okButton")!.Click += OnOk;
        this.FindControl<Button>("cancelButton")!.Click += OnCancel;
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (_completionWindow != null) return;

        // Only auto-trigger on characters that begin or extend an identifier
        // (incl. the $ that introduces a variable). Otherwise typing ( ; ,
        // space and similar would each pop a fresh window — the perceived
        // lag the user reports comes from those spurious triggers.
        if (string.IsNullOrEmpty(e.Text) || !IsTriggerChar(e.Text[0])) return;

        var caret = _editor.TextArea.Caret;
        var line = _editor.Document.GetLineByNumber(caret.Line);
        var lineText = _editor.Document.GetText(line.Offset, line.Length);
        var col = caret.Column - 1;

        var (context, items) = FmCalcCompletionProvider.GetCompletions(lineText, col, _completionContext);
        if (context == CalcCompletionContext.None || items.Count == 0) return;

        // Anchor the completion window to the start of the identifier the
        // user is typing so accepting an item replaces the partial prefix
        // instead of appending after it.
        var prefixStart = col;
        while (prefixStart > 0 && IsIdentifierChar(lineText[prefixStart - 1]))
            prefixStart--;

        _completionWindow = new CompletionWindow(_editor.TextArea)
        {
            StartOffset = line.Offset + prefixStart,
        };
        foreach (var item in items)
            _completionWindow.CompletionList.CompletionData.Add(item);

        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }

    /// <summary>
    /// Forward non-letter characters typed while the completion window is
    /// open as accept-and-insert events. Mirrors AvaloniaEdit's standard
    /// completion-binding pattern so typing <c>(</c> after <c>Lengt</c>
    /// completes <c>Length</c> first then inserts the paren.
    /// </summary>
    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (_completionWindow == null) return;
        if (string.IsNullOrEmpty(e.Text)) return;
        if (!IsIdentifierChar(e.Text[0]))
            _completionWindow.CompletionList.RequestInsertion(e);
    }

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// Characters that should pop the completion window when typed in a
    /// fresh editor with no window open. Letters and underscore start
    /// identifiers; <c>$</c> starts a variable reference. Digits and other
    /// punctuation never start a completion-worthy token, so we ignore them.
    /// </summary>
    private static bool IsTriggerChar(char c) =>
        char.IsLetter(c) || c == '_' || c == '$';

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        var contextBox = this.FindControl<TextBox>("contextTableBox")!;
        var alwaysCheck = this.FindControl<CheckBox>("alwaysEvaluateCheck")!;

        _field.Calculation = _editor.Text;
        _field.CalculationContext = string.IsNullOrWhiteSpace(contextBox.Text) ? null : contextBox.Text;
        _field.AlwaysEvaluate = alwaysCheck.IsChecked == true;

        if (_field.Kind == FieldKind.Normal && !string.IsNullOrWhiteSpace(_editor.Text))
            _field.Kind = FieldKind.Calculated;

        Saved = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        _textMateInstallation.Dispose();
    }
}
