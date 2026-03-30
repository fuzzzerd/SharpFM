using System;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Scripting;
using TextMateSharp.Grammars;

namespace SharpFM;

public partial class MainWindow : Window
{
    private readonly RegistryOptions _registryOptions;
    private ScriptEditorController? _scriptController;
    private TextMate.Installation? _xmlTextMateInstallation;
    private TextMate.Installation? _scriptTextMateInstallation;
    private Window? _xmlWindow;

    public MainWindow()
    {
        InitializeComponent();

        _registryOptions = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);

        // Script editor: setup on first load (deferred until control is available)
        var scriptEditor = this.FindControl<TextEditor>("scriptEditor");
        if (scriptEditor != null)
        {
            var fmScriptRegistry = new FmScriptRegistryOptions(_registryOptions);
            _scriptTextMateInstallation = scriptEditor.InstallTextMate(fmScriptRegistry);
            _scriptTextMateInstallation.SetGrammar(FmScriptRegistryOptions.ScopeName);
            _scriptController = new ScriptEditorController(scriptEditor);
        }

        // Fallback XML editor for non-script clips (lightweight — no TextMate needed,
        // built-in SyntaxHighlighting="Xml" in the XAML handles it)

        // "View XML" menu item — opens XML in a separate window on demand
        var viewXmlItem = this.FindControl<MenuItem>("viewXmlMenuItem");
        if (viewXmlItem != null)
        {
            viewXmlItem.Click += (_, _) => ShowXmlWindow();
        }
    }

    private void ShowXmlWindow()
    {
        var vm = (DataContext as SharpFM.ViewModels.MainWindowViewModel)?.SelectedClip;
        if (vm == null)
            return;

        // Sync model to XML before showing
        vm.SyncModelFromEditor();

        // Reuse or create the XML window
        if (_xmlWindow == null || !_xmlWindow.IsVisible)
        {
            var xmlEditor = new TextEditor
            {
                FontFamily = new Avalonia.Media.FontFamily("Cascadia Code,Consolas,Menlo,Monospace"),
                ShowLineNumbers = true,
                WordWrap = false,
            };

            // Lazy-load XML TextMate only when first needed
            if (_xmlTextMateInstallation == null)
            {
                _xmlTextMateInstallation = xmlEditor.InstallTextMate(_registryOptions);
                var xmlLang = _registryOptions.GetLanguageByExtension(".xml");
                _xmlTextMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(xmlLang.Id));
            }

            xmlEditor.Document = new AvaloniaEdit.Document.TextDocument(vm.ClipXml ?? "");

            _xmlWindow = new Window
            {
                Title = $"XML — {vm.Name}",
                Width = 600,
                Height = 500,
                Content = xmlEditor,
            };

            // Sync XML edits back to the model when the window closes
            _xmlWindow.Closing += (_, _) =>
            {
                if (_xmlWindow.Content is TextEditor editor)
                {
                    var currentVm = (DataContext as SharpFM.ViewModels.MainWindowViewModel)?.SelectedClip;
                    if (currentVm != null)
                    {
                        currentVm.ClipXml = editor.Document.Text;
                        currentVm.SyncEditorFromXml();
                    }
                }
            };
        }
        else
        {
            // Update existing window content
            if (_xmlWindow.Content is TextEditor existing)
                existing.Document = new AvaloniaEdit.Document.TextDocument(vm.ClipXml ?? "");
            _xmlWindow.Title = $"XML — {vm.Name}";
        }

        _xmlWindow.Show();
        _xmlWindow.Activate();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _xmlWindow?.Close();
        _scriptController?.Dispose();
        _xmlTextMateInstallation?.Dispose();
        _scriptTextMateInstallation?.Dispose();
    }
}
