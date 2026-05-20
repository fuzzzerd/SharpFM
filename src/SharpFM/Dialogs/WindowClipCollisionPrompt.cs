using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace SharpFM.Dialogs;

/// <summary>
/// Production <see cref="IClipCollisionPrompt"/>. Opens a
/// <see cref="ClipCollisionDialog"/> modal over the supplied owner window.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WindowClipCollisionPrompt(Window owner) : IClipCollisionPrompt
{
    public Task<ClipCollisionDecision> PromptAsync(string clipName, IReadOnlyList<string> folderPath) =>
        ClipCollisionDialog.PromptAsync(owner, clipName, folderPath);
}
