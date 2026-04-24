using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SharpFM.Diagnostics;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;
using SharpFM.PluginManager;
using SharpFM.Services;
using SharpFM.ViewModels;

namespace SharpFM;

[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    private PluginService? _pluginService;
    private PluginUIHost? _pluginHost;
    private PluginConfigService? _pluginConfigService;

    public MainWindow()
    {
        InitializeComponent();

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

    public void SetPluginServices(PluginService pluginService, PluginUIHost pluginHost, PluginConfigService pluginConfigService)
    {
        _pluginService = pluginService;
        _pluginHost = pluginHost;
        _pluginConfigService = pluginConfigService;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        BuildPluginMenuItems(vm);

        if (vm.PluginUI is { } pluginUI)
            pluginUI.PropertyChanged += OnPluginUIPropertyChanged;
    }

    private void OnPluginUIPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PluginUIHost.IsVisible))
            UpdatePluginPanelVisibility();
        else if (e.PropertyName == nameof(PluginUIHost.PanelControl))
            UpdatePluginPanelContent();
    }

    private void BuildPluginMenuItems(MainWindowViewModel vm)
    {
        var pluginsMenu = this.FindControl<MenuItem>("pluginsMenu");
        var manageItem = this.FindControl<MenuItem>("managePluginsMenuItem");
        var pluginUI = vm.PluginUI;
        if (pluginsMenu is null || manageItem is null || vm.AllPlugins.Count == 0)
        {
            RegisterPluginKeyBindings(vm);
            return;
        }

        var insertIndex = pluginsMenu.Items.IndexOf(manageItem);

        foreach (var plugin in vm.AllPlugins)
        {
            var isPanel = pluginUI?.HasPanel(plugin) ?? false;
            var hasActions = plugin.MenuActions.Count > 0;

            if (!isPanel && !hasActions) continue;

            MenuItem pluginItem;

            if (isPanel && !hasActions)
            {
                // Panel with no custom actions — flat item that toggles the panel
                pluginItem = new MenuItem { Header = plugin.DisplayName, Tag = plugin };
                if (plugin.KeyBindings.Count > 0)
                    pluginItem.InputGesture = KeyGesture.Parse(plugin.KeyBindings[0].Gesture);
                var p = plugin;
                pluginItem.Click += (_, _) => pluginUI?.TogglePanel(p);
            }
            else
            {
                // Submenu with actions (and toggle item for panels)
                pluginItem = new MenuItem { Header = plugin.DisplayName };

                if (isPanel)
                {
                    var toggleItem = new MenuItem { Header = "Toggle Panel" };
                    if (plugin.KeyBindings.Count > 0)
                        toggleItem.InputGesture = KeyGesture.Parse(plugin.KeyBindings[0].Gesture);
                    var p = plugin;
                    toggleItem.Click += (_, _) => pluginUI?.TogglePanel(p);
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
        var pluginUI = vm.PluginUI;

        foreach (var plugin in vm.AllPlugins)
        {
            foreach (var binding in plugin.KeyBindings)
            {
                var gesture = KeyGesture.Parse(binding.Gesture);
                var p = plugin;
                var cb = binding.Callback;

                KeyBindings.Add(new KeyBinding
                {
                    Gesture = gesture,
                    Command = new PluginKeyCommand(() =>
                    {
                        if (pluginUI?.HasPanel(p) == true)
                            pluginUI.TogglePanel(p);
                        cb();
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

        var visible = vm.PluginUI?.IsVisible ?? false;
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
            host.Content = vm.PluginUI?.PanelControl;
    }

    private void ShowPluginManager()
    {
        if (_pluginService is null || _pluginHost is null || _pluginConfigService is null) return;
        if (DataContext is not MainWindowViewModel vm) return;

        var window = new PluginManagerWindow();
        window.Configure(_pluginService, _pluginHost, vm, _pluginConfigService);
        window.ShowDialog(this);
    }

    // --- Tree / tab interaction ---

    // Walk the visual tree up from the tapped item to find the clip node that
    // was hit. TreeView raises Tapped with e.Source pointing at the innermost
    // text/border; we need the DataContext of the enclosing TreeViewItem.
    private static ClipTreeNodeViewModel? FindClipNode(object? source)
    {
        var current = source as Control;
        while (current is not null)
        {
            if (current is TreeViewItem tvi && tvi.DataContext is ClipTreeNodeViewModel node)
                return node;
            if (current.DataContext is ClipTreeNodeViewModel d)
                return d;
            current = current.Parent as Control;
        }
        return null;
    }

    private void ClipsTree_Tapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var node = FindClipNode(e.Source);
        if (node?.Clip is null) return;
        vm.OpenClipAsPreview(node.Clip);
    }

    private void ClipsTree_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        var node = FindClipNode(e.Source);
        if (node?.Clip is null) return;
        vm.OpenClipAsPermanent(node.Clip);
    }

    private void TabHeader_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if ((sender as Control)?.DataContext is OpenTabViewModel tab)
            vm.OpenTabs.Graduate(tab);
    }

    private void CloseTab_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if ((sender as Button)?.Tag is OpenTabViewModel tab)
            vm.OpenTabs.Close(tab);
    }
}
