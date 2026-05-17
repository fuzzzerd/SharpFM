using System.Threading.Tasks;

namespace SharpFM.Dialogs;

/// <summary>
/// Host-supplied input prompt used by view-models (and the plugin host) to
/// ask the user for a single line of text. Abstracted so view-model tests
/// can substitute a fake without standing up an Avalonia window.
/// </summary>
public interface IInputPrompt
{
    /// <summary>
    /// Show a modal prompt and return the user's input, or <c>null</c> if
    /// they cancelled.
    /// </summary>
    Task<string?> PromptAsync(string title, string prompt, string defaultValue);
}
