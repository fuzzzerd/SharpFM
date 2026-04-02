using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

/// <summary>
/// Handles specialized display, serialization, and parsing for specific step types.
/// Implementations are registered by step name and dispatched by ScriptStep.
/// </summary>
public interface IStepHandler
{
    /// <summary>Step names this handler covers (e.g., ["Set Variable"] or ["If", "Else If"]).</summary>
    string[] StepNames { get; }

    /// <summary>Render the step as a single display line. Return null to fall through to generic.</summary>
    string? ToDisplayLine(ScriptStep step);

    /// <summary>Serialize the step model to XML. Return null to fall through to generic.</summary>
    XElement? ToXml(ScriptStep step);

    /// <summary>Build XML directly from parsed display params. Return null to fall through to generic.</summary>
    XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams);
}
