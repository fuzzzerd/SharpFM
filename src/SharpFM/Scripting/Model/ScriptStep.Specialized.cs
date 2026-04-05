using System.Xml.Linq;
using SharpFM.Scripting.Handlers;

namespace SharpFM.Scripting.Model;

public partial class ScriptStep
{
    /// <summary>
    /// Dispatches to a specialized handler's BuildXmlFromDisplay if one exists.
    /// Used only by FromDisplayLine (the UI text editing path).
    /// </summary>
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
