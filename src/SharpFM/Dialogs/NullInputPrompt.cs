using System.Threading.Tasks;

namespace SharpFM.Dialogs;

/// <summary>
/// Default <see cref="IInputPrompt"/> for environments without a UI thread
/// (headless tests, the legacy view-model ctor path). Returns the supplied
/// default verbatim, simulating a "user accepted the prefill" outcome
/// without surfacing anything visible.
/// </summary>
public sealed class NullInputPrompt : IInputPrompt
{
    public Task<string?> PromptAsync(string title, string prompt, string defaultValue) =>
        Task.FromResult<string?>(defaultValue);
}
