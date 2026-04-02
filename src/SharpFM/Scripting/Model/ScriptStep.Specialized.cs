using System.Xml.Linq;
using SharpFM.Scripting.Handlers;

namespace SharpFM.Scripting.Model;

public partial class ScriptStep
{
    private string? ToDisplayLine_Specialized()
    {
        if (Definition == null) return null;
        return StepHandlerRegistry.Get(Definition.Name)?.ToDisplayLine(this);
    }

    internal XElement? ToXml_Specialized()
    {
        if (Definition == null) return null;
        return StepHandlerRegistry.Get(Definition.Name)?.ToXml(this);
    }

    internal static XElement? BuildXmlFromDisplay_Specialized(
        StepDefinition definition, bool enabled, string[] hrParams)
    {
        return StepHandlerRegistry.Get(definition.Name)
            ?.BuildXmlFromDisplay(definition, enabled, hrParams);
    }

    // Shared helpers used by ScriptStep and handlers
    internal static (string Name, string Repetition) ParseVarRepetition(string text) =>
        SetVariableHandler.ParseVarRepetition(text);
}
