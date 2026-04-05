using System.Xml.Linq;

namespace SharpFM.Scripting.Handlers;

internal class CommentHandler : StepHandlerBase, IStepHandler
{
    public string[] StepNames => ["# (comment)"];

    public XElement? BuildXmlFromDisplay(StepDefinition definition, bool enabled, string[] hrParams)
    {
        return null; // Comments are handled by ScriptStep.FromDisplayLine directly
    }
}
