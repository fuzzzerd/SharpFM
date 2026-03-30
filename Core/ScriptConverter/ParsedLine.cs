namespace SharpFM.Core.ScriptConverter;

public record ParsedStep(
    string StepName,
    string[] Params,
    bool Disabled,
    bool IsComment,
    string RawLine
);
