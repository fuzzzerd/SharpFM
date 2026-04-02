// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

namespace SharpFM.Plugin;

/// <summary>
/// A headless plugin that reacts to host events with no UI panel.
/// Subscribe to <see cref="IPluginHost"/> events in <see cref="IPlugin.Initialize"/>
/// and unsubscribe in <see cref="System.IDisposable.Dispose"/>.
/// </summary>
public interface IEventPlugin : IPlugin { }
