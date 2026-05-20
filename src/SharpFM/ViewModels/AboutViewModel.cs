using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharpFM.Plugin;

namespace SharpFM.ViewModels;

/// <summary>
/// Backing model for the About dialog. Holds one entry for the host itself
/// and one entry per loaded plugin. Plugins that implement
/// <see cref="IUpdateCheckable"/> get a check-for-updates affordance; ones
/// that don't simply show <see cref="IPlugin.Version"/>.
/// </summary>
public sealed class AboutViewModel
{
    public AboutEntryViewModel Host { get; }
    public ObservableCollection<AboutEntryViewModel> Plugins { get; }
    public Uri HostHomepageUrl { get; }

    public AboutViewModel(
        string hostName,
        string hostVersion,
        IUpdateCheckable hostChecker,
        Uri hostHomepageUrl,
        IEnumerable<IPlugin> plugins)
    {
        Host = new AboutEntryViewModel(hostName, hostVersion, hostChecker);
        HostHomepageUrl = hostHomepageUrl;
        Plugins = new ObservableCollection<AboutEntryViewModel>(
            plugins.Select(p => new AboutEntryViewModel(
                p.DisplayName,
                p.Version,
                p as IUpdateCheckable)));
    }
}
