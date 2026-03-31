using System;
using System.ComponentModel;
using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Schema.Editor;
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
    private TableEditorControl? _tableEditor;

    public MainWindow()
    {
        InitializeComponent();

        _registryOptions = new RegistryOptions((ThemeName)(int)ThemeName.DarkPlus);

        // Script editor
        var scriptEditor = this.FindControl<TextEditor>("scriptEditor");
        if (scriptEditor != null)
        {
            var fmScriptRegistry = new FmScriptRegistryOptions(_registryOptions);
            _scriptTextMateInstallation = scriptEditor.InstallTextMate(fmScriptRegistry);
            _scriptTextMateInstallation.SetGrammar(FmScriptRegistryOptions.ScopeName);
            _scriptController = new ScriptEditorController(scriptEditor);
        }

        // Table editor — wire DataContext when selection changes
        _tableEditor = this.FindControl<TableEditorControl>("tableEditorControl");

        // Listen for SelectedClip changes to update table editor DataContext
        DataContextChanged += (_, _) =>
        {
            if (DataContext is SharpFM.ViewModels.MainWindowViewModel mainVm)
            {
                mainVm.PropertyChanged += OnMainVmPropertyChanged;
            }
        };

        // "View XML" menu item
        var viewXmlItem = this.FindControl<MenuItem>("viewXmlMenuItem");
        if (viewXmlItem != null)
        {
            viewXmlItem.Click += (_, _) => ShowXmlWindow();
        }
    }

    private void OnMainVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "SelectedClip" || _tableEditor == null) return;

        var mainVm = sender as SharpFM.ViewModels.MainWindowViewModel;
        var clip = mainVm?.SelectedClip;

        _tableEditor.DataContext = clip?.IsTableClip == true ? clip.TableEditor : null;
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
