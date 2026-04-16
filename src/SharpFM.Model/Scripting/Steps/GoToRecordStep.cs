using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Typed domain representation of FileMaker's "Go to Record/Request/Page"
/// script step.
/// <para>
/// Display form is driven by <c>RowPageLocation</c> plus element-presence:
/// First / Last render bare; Next / Previous always render <c>Exit after
/// last: On|Off</c> (FM Pro always emits the element for those locations);
/// ByCalculation renders <c>With dialog: On|Off</c> (inverted from
/// <c>NoInteract</c> state) plus the target calculation.
/// </para>
/// <para>
/// Zero-loss on NoInteract for non-calc locations is proven by construction:
/// FM Pro always emits <c>state="False"</c> there and provides no UI to
/// toggle it, so display-text round-trip defaulting to False matches FM Pro.
/// </para>
/// </summary>
public sealed class GoToRecordStep : ScriptStep
{
    public RowPageLocationKind Location { get; set; }
    public Calculation? LocationCalc { get; set; }
    public bool ExitAfterLast { get; set; }
    public bool NoInteract { get; set; }

    public GoToRecordStep(
        bool enabled,
        RowPageLocationKind location,
        Calculation? locationCalc = null,
        bool exitAfterLast = false,
        bool noInteract = false)
        : base(StepCatalogLoader.ByName["Go to Record/Request/Page"], enabled)
    {
        Location = location;
        LocationCalc = locationCalc;
        ExitAfterLast = exitAfterLast;
        NoInteract = noInteract;
    }

    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Register typed step factories on assembly load.")]
    [ModuleInitializer]
    internal static void Register()
    {
        StepXmlFactory.Register("Go to Record/Request/Page", FromXml);
        StepDisplayFactory.Register("Go to Record/Request/Page", FromDisplayParams);
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";

        var locationWire = step.Element("RowPageLocation")?.Attribute("value")?.Value ?? "First";
        var location = ParseLocation(locationWire);

        Calculation? locationCalc = null;
        if (location == RowPageLocationKind.ByCalculation)
        {
            var calcEl = step.Element("Calculation");
            if (calcEl is not null) locationCalc = Calculation.FromXml(calcEl);
        }

        var exitAfterLast = step.Element("Exit")?.Attribute("state")?.Value == "True";
        var noInteract = step.Element("NoInteract")?.Attribute("state")?.Value == "True";

        return new GoToRecordStep(enabled, location, locationCalc, exitAfterLast, noInteract);
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", 16),
            new XAttribute("name", "Go to Record/Request/Page"));

        // FM Pro always emits <NoInteract> regardless of location.
        step.Add(new XElement("NoInteract",
            new XAttribute("state", NoInteract ? "True" : "False")));

        // <Exit> is emitted only for Next / Previous.
        if (Location is RowPageLocationKind.Next or RowPageLocationKind.Previous)
        {
            step.Add(new XElement("Exit",
                new XAttribute("state", ExitAfterLast ? "True" : "False")));
        }

        step.Add(new XElement("RowPageLocation",
            new XAttribute("value", LocationWireValue(Location))));

        if (Location == RowPageLocationKind.ByCalculation && LocationCalc is not null)
            step.Add(LocationCalc.ToXml());

        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new List<string>();

        switch (Location)
        {
            case RowPageLocationKind.First:
                parts.Add("First");
                break;
            case RowPageLocationKind.Last:
                parts.Add("Last");
                break;
            case RowPageLocationKind.Next:
                parts.Add("Next");
                parts.Add($"Exit after last: {OnOff(ExitAfterLast)}");
                break;
            case RowPageLocationKind.Previous:
                parts.Add("Previous");
                parts.Add($"Exit after last: {OnOff(ExitAfterLast)}");
                break;
            case RowPageLocationKind.ByCalculation:
                parts.Add($"By calculation: {LocationCalc?.Text ?? ""}");
                parts.Add($"With dialog: {OnOff(!NoInteract)}");
                break;
        }

        return $"Go to Record/Request/Page [ {string.Join(" ; ", parts)} ]";
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var location = RowPageLocationKind.First;
        Calculation? locationCalc = null;
        var exitAfterLast = false;
        var noInteract = false;

        foreach (var raw in hrParams)
        {
            var token = raw.Trim();

            if (token.StartsWith("Exit after last:", StringComparison.OrdinalIgnoreCase))
            {
                exitAfterLast = IsOn(token.Substring("Exit after last:".Length));
            }
            else if (token.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
            {
                // With dialog:On  ⇒ NoInteract=False
                // With dialog:Off ⇒ NoInteract=True
                noInteract = !IsOn(token.Substring("With dialog:".Length));
            }
            else if (token.StartsWith("By calculation:", StringComparison.OrdinalIgnoreCase))
            {
                location = RowPageLocationKind.ByCalculation;
                locationCalc = new Calculation(token.Substring("By calculation:".Length).Trim());
            }
            else if (token.Equals("First", StringComparison.OrdinalIgnoreCase))
                location = RowPageLocationKind.First;
            else if (token.Equals("Last", StringComparison.OrdinalIgnoreCase))
                location = RowPageLocationKind.Last;
            else if (token.Equals("Next", StringComparison.OrdinalIgnoreCase))
                location = RowPageLocationKind.Next;
            else if (token.Equals("Previous", StringComparison.OrdinalIgnoreCase))
                location = RowPageLocationKind.Previous;
        }

        return new GoToRecordStep(enabled, location, locationCalc, exitAfterLast, noInteract);
    }

    private static RowPageLocationKind ParseLocation(string wire) => wire switch
    {
        "First" => RowPageLocationKind.First,
        "Last" => RowPageLocationKind.Last,
        "Previous" => RowPageLocationKind.Previous,
        "Next" => RowPageLocationKind.Next,
        "ByCalculation" => RowPageLocationKind.ByCalculation,
        _ => RowPageLocationKind.First,
    };

    private static string LocationWireValue(RowPageLocationKind kind) => kind switch
    {
        RowPageLocationKind.First => "First",
        RowPageLocationKind.Last => "Last",
        RowPageLocationKind.Previous => "Previous",
        RowPageLocationKind.Next => "Next",
        RowPageLocationKind.ByCalculation => "ByCalculation",
        _ => "First",
    };

    private static string OnOff(bool on) => on ? "On" : "Off";

    private static bool IsOn(string suffix) =>
        suffix.Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
}

public enum RowPageLocationKind
{
    First,
    Last,
    Previous,
    Next,
    ByCalculation,
}
