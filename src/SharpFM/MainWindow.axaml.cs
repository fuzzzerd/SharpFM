using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Plugin;
using SharpFM.PluginManager;
using SharpFM.Scripting;
using SharpFM.Services;
using SharpFM.ViewModels;
using TextMateSharp.Grammars;

namespace SharpFM;

public partial class MainWindow : Window
{
    private readonly RegistryOptions _registryOptions;
    private ScriptEditorController? _scriptController;
    private TextMate.Installation? _scriptTextMateInstallation;
    private PluginService? _pluginService;
    private IPluginHost? _pluginHost;

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

        // "Manage Plugins..." menu item
        var managePlugins = this.FindControl<MenuItem>("managePluginsMenuItem");
        if (managePlugins != null)
            managePlugins.Click += (_, _) => ShowPluginManager();

        // Wire up plugin UI when DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    public void SetPluginServices(PluginService pluginService, IPluginHost pluginHost)
    {
        _pluginService = pluginService;
        _pluginHost = pluginHost;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        BuildPluginMenuItems(vm);
        vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsPluginPanelVisible))
            UpdatePluginPanelVisibility();
        else if (e.PropertyName == nameof(MainWindowViewModel.PluginPanelControl))
            UpdatePluginPanelContent();
    }

    private void BuildPluginMenuItems(MainWindowViewModel vm)
    {
        var separator = this.FindControl<Separator>("pluginMenuSeparator");
        if (separator is null) return;

        var viewMenu = separator.Parent as MenuItem;
        if (viewMenu is null || vm.PanelPlugins.Count == 0) return;

        separator.IsVisible = true;

        foreach (var plugin in vm.PanelPlugins)
        {
            var item = new MenuItem { Header = plugin.DisplayName, Tag = plugin };
            item.Click += (_, _) =>
            {
                if (item.Tag is IPanelPlugin p)
                    vm.TogglePluginPanel(p);
            };

            // Show the first keybinding gesture as an InputGesture hint
            if (plugin.KeyBindings.Count > 0)
                item.InputGesture = KeyGesture.Parse(plugin.KeyBindings[0].Gesture);

            viewMenu.Items.Add(item);
        }

        RegisterPluginKeyBindings(vm);
    }

    private void RegisterPluginKeyBindings(MainWindowViewModel vm)
    {
        foreach (var plugin in vm.PanelPlugins)
        {
            foreach (var binding in plugin.KeyBindings)
            {
                var gesture = KeyGesture.Parse(binding.Gesture);
                var pluginRef = plugin;
                KeyBindings.Add(new KeyBinding
                {
                    Gesture = gesture,
                    Command = new PluginKeyCommand(() =>
                    {
                        vm.TogglePluginPanel(pluginRef);
                        binding.Callback();
                    })
                });
            }
        }
    }

    /// <summary>
    /// Simple ICommand wrapper for plugin key binding callbacks.
    /// </summary>
    private class PluginKeyCommand(Action callback) : System.Windows.Input.ICommand
    {
#pragma warning disable CS0067 // Required by ICommand interface
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => callback();
    }

    private void UpdatePluginPanelVisibility()
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var visible = vm.IsPluginPanelVisible;
        pluginSplitter.IsVisible = visible;
        pluginPanelBorder.IsVisible = visible;
        editorPluginGrid.ColumnDefinitions[1].Width = visible ? new GridLength(16) : new GridLength(0);
        editorPluginGrid.ColumnDefinitions[2].Width = visible ? new GridLength(350) : new GridLength(0);
    }

    private void UpdatePluginPanelContent()
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var host = this.FindControl<ContentControl>("pluginPanelHost");
        if (host is not null)
            host.Content = vm.PluginPanelControl;
    }

    private void ShowPluginManager()
    {
        if (_pluginService is null || _pluginHost is null) return;
        if (DataContext is not MainWindowViewModel vm) return;

        var window = new PluginManagerWindow();
        window.Configure(_pluginService, _pluginHost, vm);
        window.ShowDialog(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _scriptController?.Dispose();
        _scriptTextMateInstallation?.Dispose();
    }
}
