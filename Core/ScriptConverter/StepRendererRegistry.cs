using System;
using System.Collections.Generic;

namespace SharpFM.Core.ScriptConverter;

public static class StepRendererRegistry
{
    private static readonly Dictionary<string, IStepRenderer> _byName =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly IStepRenderer _fallback = new GenericStepRenderer();

    static StepRendererRegistry()
    {
        Register(new Renderers.CommentStepRenderer());
        Register(new Renderers.SetVariableRenderer());
        Register(new Renderers.SetFieldRenderer());
        Register(new Renderers.ControlFlowRenderer());
        Register(new Renderers.PerformScriptRenderer());
        Register(new Renderers.GoToLayoutRenderer());
        Register(new Renderers.GoToRecordRenderer());
        Register(new Renderers.ShowCustomDialogRenderer());
    }

    public static void Register(IStepRenderer renderer, params string[] stepNames)
    {
        foreach (var name in stepNames)
        {
            _byName[name] = renderer;
        }
    }

    public static void Register(Renderers.IMultiStepRenderer renderer)
    {
        foreach (var name in renderer.StepNames)
        {
            _byName[name] = renderer;
        }
    }

    public static IStepRenderer GetRenderer(string stepName)
    {
        return _byName.TryGetValue(stepName, out var renderer) ? renderer : _fallback;
    }
}
