// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System.Threading.Tasks;

namespace SharpFM.Plugin;

/// <summary>
/// A plugin that transforms clip XML during import or export operations.
/// Transforms run in plugin load order. Use <see cref="IsEnabled"/> to allow
/// users to toggle transforms without uninstalling.
/// </summary>
public interface IClipTransformPlugin : IPlugin
{
    /// <summary>
    /// Transform clip XML when a clip is imported (pasted from FileMaker or loaded from storage).
    /// Return the input unchanged to skip transformation.
    /// </summary>
    Task<string> OnImportAsync(string clipType, string xml);

    /// <summary>
    /// Transform clip XML when a clip is exported (copied to FileMaker clipboard).
    /// Return the input unchanged to skip transformation.
    /// </summary>
    Task<string> OnExportAsync(string clipType, string xml);

    /// <summary>
    /// Whether this transformer is currently enabled.
    /// The user can toggle transformers on/off without uninstalling them.
    /// </summary>
    bool IsEnabled { get; set; }
}
