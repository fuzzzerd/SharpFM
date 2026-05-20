using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SharpFM.ViewModels;

namespace SharpFM.Dialogs;

/// <summary>
/// Help → About window. Surfaces host version and the list of loaded plugins.
/// Each entry that opted in via <c>IUpdateCheckable</c> gets a check-for-updates
/// affordance; checks run against whatever channel the implementer chose.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class AboutDialog : Window
{
    private AboutViewModel? _vm;

    public AboutDialog()
    {
        AvaloniaXamlLoader.Load(this);

        this.FindControl<Button>("closeButton")!.Click += (_, _) => Close();
        this.FindControl<Button>("hostCheckButton")!.Click += OnHostCheckClick;
        this.FindControl<Button>("hostOpenUpdateButton")!.Click += OnHostOpenUpdateClick;
        this.FindControl<Button>("hostHomepageButton")!.Click += OnHostHomepageClick;
    }

    public void Configure(AboutViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        vm.Host.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AboutEntryViewModel.UpdateUrl))
                this.FindControl<Button>("hostOpenUpdateButton")!.IsVisible = vm.Host.UpdateUrl is not null;
        };
    }

    private async void OnHostCheckClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        await _vm.Host.CheckAsync(CancellationToken.None);
    }

    private void OnHostOpenUpdateClick(object? sender, RoutedEventArgs e)
    {
        if (_vm?.Host.UpdateUrl is { } url) OpenInBrowser(url.ToString());
    }

    private void OnHostHomepageClick(object? sender, RoutedEventArgs e)
    {
        if (_vm is null) return;
        OpenInBrowser(_vm.HostHomepageUrl.ToString());
    }

    private async void OnPluginCheckClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: AboutEntryViewModel entry })
        {
            await entry.CheckAsync(CancellationToken.None);
        }
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Browser unavailable / sandboxed — silently no-op.
        }
    }
}
