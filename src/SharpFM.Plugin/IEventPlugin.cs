namespace SharpFM.Plugin;

/// <summary>
/// A headless plugin that reacts to host events with no UI panel.
/// Subscribe to <see cref="IPluginHost"/> events in <see cref="IPlugin.Initialize"/>
/// and unsubscribe in <see cref="System.IDisposable.Dispose"/>.
/// </summary>
public interface IEventPlugin : IPlugin { }
