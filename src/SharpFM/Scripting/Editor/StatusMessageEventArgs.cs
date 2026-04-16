using System;

namespace SharpFM.Scripting.Editor;

/// <summary>
/// Payload for <see cref="ScriptEditorController.StatusMessageRaised"/>.
/// Carries a message the controller wants displayed in the status bar
/// plus a flag indicating error severity (controls the dismiss timer
/// duration in the view model).
/// </summary>
public class StatusMessageEventArgs : EventArgs
{
    public string Message { get; }
    public bool IsError { get; }

    public StatusMessageEventArgs(string message, bool isError = false)
    {
        Message = message;
        IsError = isError;
    }
}
