using System;
using System.Collections.Generic;

namespace SharpFM.Scripting.Handlers;

/// <summary>
/// Central registry for step handlers. Lookup by step name, fallback to null (generic handling).
/// </summary>
internal static class StepHandlerRegistry
{
    private static readonly Dictionary<string, IStepHandler> _handlers =
        new(StringComparer.OrdinalIgnoreCase);

    static StepHandlerRegistry()
    {
        Register(new CommentHandler());
        Register(new SetVariableHandler());
        Register(new SetFieldHandler());
        Register(new PerformScriptHandler());
        Register(new GoToLayoutHandler());
        Register(new GoToRecordHandler());
        Register(new ShowCustomDialogHandler());
        Register(new ControlFlowHandler());
    }

    private static void Register(IStepHandler handler)
    {
        foreach (var name in handler.StepNames)
            _handlers[name] = handler;
    }

    internal static IStepHandler? Get(string stepName)
    {
        return _handlers.TryGetValue(stepName, out var handler) ? handler : null;
    }
}
