using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter;

public interface IStepRenderer
{
    string ToHr(XElement step, StepDefinition definition);
    string ToXml(ParsedLine line, StepDefinition definition);
}
