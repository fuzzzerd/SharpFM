using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpFM.Dialogs;

/// <summary>How the user wants to resolve a same-name + different-content paste collision.</summary>
public enum ClipCollisionChoice
{
    /// <summary>Skip the incoming clip and abort the remainder of the paste batch.</summary>
    Cancel,
    /// <summary>Overwrite the existing clip with the incoming XML.</summary>
    Replace,
    /// <summary>Add the incoming clip alongside the existing one with a unique suffix.</summary>
    KeepBoth,
}

/// <summary>Result of <see cref="IClipCollisionPrompt.PromptAsync"/>.</summary>
/// <param name="Choice">The action selected by the user.</param>
/// <param name="ApplyToAll">When true, reuse this choice for the remainder of the paste batch.</param>
public sealed record ClipCollisionDecision(ClipCollisionChoice Choice, bool ApplyToAll);

/// <summary>
/// Host-supplied prompt for resolving paste collisions where a clip with the
/// same name already exists in the target folder but holds different XML.
/// Mirrors <see cref="IInputPrompt"/>'s test-friendly abstraction.
/// </summary>
public interface IClipCollisionPrompt
{
    Task<ClipCollisionDecision> PromptAsync(string clipName, IReadOnlyList<string> folderPath);
}
