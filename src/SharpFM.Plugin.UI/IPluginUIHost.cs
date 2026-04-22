using System.Threading.Tasks;
using Avalonia.Controls;

namespace SharpFM.Plugin.UI;

/// <summary>
/// Extended host interface for plugins that need UI capabilities beyond
/// the simple dialogs on <see cref="IPluginHost"/>. Provides content
/// dialog hosting backed by Avalonia.
/// </summary>
public interface IPluginUIHost : IPluginHost
{
    /// <summary>
    /// Show a modal dialog containing plugin-provided content.
    /// Returns true if the user accepted (OK), false if cancelled.
    /// </summary>
    Task<bool> ShowContentDialogAsync(string title, Control content);
}
