using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace SharpFM.Dialogs;

/// <summary>
/// Production <see cref="IInputPrompt"/>. Opens an <see cref="InputDialog"/>
/// modal over the supplied owner window.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WindowInputPrompt(Window owner) : IInputPrompt
{
    public Task<string?> PromptAsync(string title, string prompt, string defaultValue) =>
        InputDialog.PromptAsync(owner, title, prompt, defaultValue);
}
