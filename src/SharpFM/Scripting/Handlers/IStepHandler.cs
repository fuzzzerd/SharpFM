using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

/// <summary>
/// Handles specialized parsing of display text input for specific step types.
/// Used only by the FromDisplayLine path (UI text editing).
/// Serialization (ToXml) and display (ToDisplayLine) always use the generic
/// ParamValues-based path — handlers are NOT involved in output.
/// </summary>
public interface IStepHandler
{
    /// <summary>Step names this handler covers (e.g., ["Set Variable"] or ["If", "Else If"]).</summary>
    string[] StepNames { get; }

    /// <summary>Build XML from parsed display text params. Return null to fall through to generic.</summary>
    XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams);

    /// <summary>
    /// Render a specialized display line for this step. Return null to fall through
    /// to the generic ParamValues-based renderer on <see cref="ScriptStep.ToDisplayLine"/>.
    /// </summary>
    string? ToDisplayLine(ScriptStep step) => null;
}
