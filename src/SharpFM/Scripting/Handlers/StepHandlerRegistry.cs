using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SharpFM.Model.Scripting;

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

        // Wire the specialized display renderer hook so ScriptStep.ToDisplayLine
        // can defer to step-specific handlers for canonical FileMaker formatting.
        ScriptStep.SpecializedDisplayRenderer = DispatchDisplayRender;
    }

    /// <summary>
    /// Module initializer: ensures the static constructor above runs as soon as
    /// the SharpFM assembly is loaded, so the SpecializedDisplayRenderer hook is
    /// installed before any code calls ScriptStep.ToDisplayLine.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        RuntimeHelpers.RunClassConstructor(typeof(StepHandlerRegistry).TypeHandle);
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

    internal static string? DispatchDisplayRender(ScriptStep step)
    {
        if (step.Definition == null) return null;
        return Get(step.Definition.Name)?.ToDisplayLine(step);
    }
}
