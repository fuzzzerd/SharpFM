using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using SharpFM.Diagnostics;
using SharpFM.Plugin;
using SharpFM.PluginManager;
using SharpFM.Scripting;
using SharpFM.Services;
using SharpFM.ViewModels;
using TextMateSharp.Grammars;

namespace SharpFM;

[ExcludeFromCodeCoverage]
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
            _scriptController.StatusMessageRaised += OnScriptControllerStatusMessage;
        }

        // "Manage Plugins..." menu item
        var managePlugins = this.FindControl<MenuItem>("managePluginsMenuItem");
        if (managePlugins != null)
            managePlugins.Click += (_, _) => ShowPluginManager();

        // "Raw Clipboard Viewer..." menu item
        var rawClipboard = this.FindControl<MenuItem>("rawClipboardMenuItem");
        if (rawClipboard != null)
            rawClipboard.Click += (_, _) => new RawClipboardWindow().Show(this);

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
        else if (e.PropertyName == nameof(MainWindowViewModel.SelectedClip))
            AttachScriptClipEditorIfApplicable();
    }

    private void OnScriptControllerStatusMessage(object? sender, SharpFM.Scripting.Editor.StatusMessageEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            vm.ShowStatusMessage(e.Message, e.IsError);
    }

    private void AttachScriptClipEditorIfApplicable()
    {
        if (_scriptController == null) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.SelectedClip?.Editor is SharpFM.Editors.ScriptClipEditor clipEditor)
        {
            _scriptController.AttachClipEditor(clipEditor);
        }
    }

    private void BuildPluginMenuItems(MainWindowViewModel vm)
    {
        var pluginsMenu = this.FindControl<MenuItem>("pluginsMenu");
        var manageItem = this.FindControl<MenuItem>("managePluginsMenuItem");
        if (pluginsMenu is null || manageItem is null || vm.AllPlugins.Count == 0)
        {
            RegisterPluginKeyBindings(vm);
            return;
        }

        var insertIndex = pluginsMenu.Items.IndexOf(manageItem);

        foreach (var plugin in vm.AllPlugins)
        {
            var isPanel = plugin is IPanelPlugin;
            var hasActions = plugin.MenuActions.Count > 0;

            if (!isPanel && !hasActions) continue;

            MenuItem pluginItem;

            if (isPanel && !hasActions)
            {
                // Panel with no custom actions — flat item that toggles the panel
                pluginItem = new MenuItem { Header = plugin.DisplayName, Tag = plugin };
                if (plugin.KeyBindings.Count > 0)
                    pluginItem.InputGesture = KeyGesture.Parse(plugin.KeyBindings[0].Gesture);
                pluginItem.Click += (_, _) =>
                {
                    if (pluginItem.Tag is IPanelPlugin p) vm.TogglePluginPanel(p);
                };
            }
            else
            {
                // Submenu with actions (and toggle item for panels)
                pluginItem = new MenuItem { Header = plugin.DisplayName };

                if (plugin is IPanelPlugin)
                {
                    var toggleItem = new MenuItem { Header = "Toggle Panel", Tag = plugin };
                    if (plugin.KeyBindings.Count > 0)
                        toggleItem.InputGesture = KeyGesture.Parse(plugin.KeyBindings[0].Gesture);
                    toggleItem.Click += (_, _) =>
                    {
                        if (toggleItem.Tag is IPanelPlugin p) vm.TogglePluginPanel(p);
                    };
                    pluginItem.Items.Add(toggleItem);
                }

                foreach (var action in plugin.MenuActions)
                {
                    var actionItem = new MenuItem { Header = action.Label };
                    if (action.Gesture is not null)
                        actionItem.InputGesture = KeyGesture.Parse(action.Gesture);
                    var cb = action.Callback;
                    actionItem.Click += (_, _) => cb();
                    pluginItem.Items.Add(actionItem);
                }
            }

            pluginsMenu.Items.Insert(insertIndex++, pluginItem);
        }

        pluginsMenu.Items.Insert(insertIndex, new Separator());
        RegisterPluginKeyBindings(vm);
    }

    private void RegisterPluginKeyBindings(MainWindowViewModel vm)
    {
        foreach (var plugin in vm.AllPlugins)
        {
            foreach (var binding in plugin.KeyBindings)
            {
                var gesture = KeyGesture.Parse(binding.Gesture);
                if (plugin is IPanelPlugin)
                {
                    var pluginRef = (IPanelPlugin)plugin;
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
                else
                {
                    var cb = binding.Callback;
                    KeyBindings.Add(new KeyBinding
                    {
                        Gesture = gesture,
                        Command = new PluginKeyCommand(cb)
                    });
                }
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
