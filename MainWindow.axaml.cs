using System;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Core.ScriptConverter;
using TextMateSharp.Grammars;

namespace SharpFM;

public partial class MainWindow : Window
{
    private readonly TextMate.Installation _xmlTextMateInstallation;
    private readonly TextMate.Installation? _scriptTextMateInstallation;
    private ScriptEditorController? _scriptController;

    public MainWindow()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);

        // XML editor: syntax highlighting
        var xmlEditor = this.FindControl<TextEditor>("avaloniaEditor") ?? throw new Exception("no control");
        _xmlTextMateInstallation = xmlEditor.InstallTextMate(registryOptions);
        var xmlLang = registryOptions.GetLanguageByExtension(".xml");
        _xmlTextMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(xmlLang.Id));

        // Script editor: syntax highlighting + controller for validation/completion/tooltips
        var scriptEditor = this.FindControl<TextEditor>("scriptEditor");
        if (scriptEditor != null)
        {
            var fmScriptRegistry = new FmScriptRegistryOptions(registryOptions);
            _scriptTextMateInstallation = scriptEditor.InstallTextMate(fmScriptRegistry);
            _scriptTextMateInstallation.SetGrammar(FmScriptRegistryOptions.ScopeName);

            _scriptController = new ScriptEditorController(scriptEditor);
        }

        // Tab switch: sync model between Script and XML views
        var editorTabs = this.FindControl<TabControl>("editorTabs");
        if (editorTabs != null)
        {
            editorTabs.SelectionChanged += (_, _) =>
            {
                var vm = (DataContext as SharpFM.ViewModels.MainWindowViewModel)?.SelectedClip;
                if (vm == null || !vm.IsScriptClip) return;

                if (editorTabs.SelectedIndex == 0)
                    vm.SyncEditorFromXml();
                else
                    vm.SyncModelFromEditor();
            };
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _scriptController?.Dispose();
        _xmlTextMateInstallation.Dispose();
        _scriptTextMateInstallation?.Dispose();
    }
}
