namespace SharpFM.Tests.ViewModels;

/// <summary>
/// Shared XML fixtures for tests asserting parse-fidelity/severity behavior
/// across <c>ClipViewModel</c>, <c>MainWindowViewModel</c>, and
/// <c>ProblemsPanelViewModel</c>.
/// </summary>
internal static class ParseFidelityTestXml
{
    /// <summary>RawStep ⇒ Info-severity UnknownStep diagnostic; survives lossless XML round-trip but the report calls it out.</summary>
    public const string InfoOnlyStepXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"99999\" name=\"FutureFmStep\"/>" +
        "</fmxmlsnippet>";

    /// <summary>Beep takes no children; an unmodeled child ⇒ Warning-severity UnknownStepElement.</summary>
    public const string WarningStepXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"93\" name=\"Beep\"><Mystery/></Step>" +
        "</fmxmlsnippet>";
}
