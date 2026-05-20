using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpFM.Dialogs;

/// <summary>
/// Default <see cref="IClipCollisionPrompt"/> for environments without a UI
/// thread (headless tests, legacy view-model ctor path). Returns
/// <see cref="ClipCollisionChoice.Cancel"/> so a collision in a non-interactive
/// context aborts the rest of the paste instead of silently mutating existing
/// clips.
/// </summary>
public sealed class NullClipCollisionPrompt : IClipCollisionPrompt
{
    public Task<ClipCollisionDecision> PromptAsync(string clipName, IReadOnlyList<string> folderPath) =>
        Task.FromResult(new ClipCollisionDecision(ClipCollisionChoice.Cancel, ApplyToAll: false));
}
