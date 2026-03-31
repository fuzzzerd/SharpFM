using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Schema.Model;
using SharpFM.Scripting;
using TextMateSharp.Grammars;

namespace SharpFM.Schema.Editor;

public partial class CalculationEditorWindow : Window
{
    private readonly FmField _field;
    private readonly TextMate.Installation _textMateInstallation;

    public bool Saved { get; private set; }

    // Required by XAML loader
    public CalculationEditorWindow() : this(new FmField()) { }

    public CalculationEditorWindow(FmField field)
    {
        InitializeComponent();

        _field = field;

        // Set up FM script syntax highlighting for calculations
        var registryOptions = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);
        var fmRegistry = new FmScriptRegistryOptions(registryOptions);
        var editor = this.FindControl<TextEditor>("calcEditor")!;
        _textMateInstallation = editor.InstallTextMate(fmRegistry);
        _textMateInstallation.SetGrammar(FmScriptRegistryOptions.ScopeName);

        // Populate fields
        editor.Text = field.Calculation ?? "";
        var contextBox = this.FindControl<TextBox>("contextTableBox")!;
        contextBox.Text = field.CalculationContext ?? "";
        var alwaysCheck = this.FindControl<CheckBox>("alwaysEvaluateCheck")!;
        alwaysCheck.IsChecked = field.AlwaysEvaluate;

        // Wire buttons
        this.FindControl<Button>("okButton")!.Click += OnOk;
        this.FindControl<Button>("cancelButton")!.Click += OnCancel;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        var editor = this.FindControl<TextEditor>("calcEditor")!;
        var contextBox = this.FindControl<TextBox>("contextTableBox")!;
        var alwaysCheck = this.FindControl<CheckBox>("alwaysEvaluateCheck")!;

        _field.Calculation = editor.Text;
        _field.CalculationContext = string.IsNullOrWhiteSpace(contextBox.Text) ? null : contextBox.Text;
        _field.AlwaysEvaluate = alwaysCheck.IsChecked == true;

        if (_field.Kind == FieldKind.Normal && !string.IsNullOrWhiteSpace(editor.Text))
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
