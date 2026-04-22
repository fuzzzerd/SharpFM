using System.Threading.Tasks;

namespace SharpFM.Plugin;

/// <summary>
/// A clip transform that runs during import/export operations.
/// Register instances with <see cref="IPluginHost.RegisterTransform"/>.
/// </summary>
public interface IClipTransform
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
    /// Whether this transform is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }
}
