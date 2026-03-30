namespace SharpFM.Core.ScriptConverter;

public record ParsedLine(
    string StepName,
    string[] Params,
    bool Disabled,
    bool IsComment,
    string RawLine
);
