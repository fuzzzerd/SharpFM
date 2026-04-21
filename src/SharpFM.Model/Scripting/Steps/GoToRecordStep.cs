using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Go to Record/Request/Page. Navigates to First / Last / Previous /
/// Next / ByCalculation.
///
/// <para>
/// Display is driven by <see cref="Location"/> plus element-presence:
/// First / Last render bare; Next / Previous always render
/// <c>Exit after last: On|Off</c> (FM Pro always emits the element for
/// those locations); ByCalculation renders <c>With dialog: On|Off</c>
/// (inverted from <c>NoInteract</c> state) and the target calculation.
/// </para>
/// </summary>
public sealed class GoToRecordStep : ScriptStep, IStepFactory
{
    public const int XmlId = 16;
    public const string XmlName = "Go to Record/Request/Page";

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
        : base(null, enabled)
    {
        Location = location;
        LocationCalc = locationCalc;
        ExitAfterLast = exitAfterLast;
        NoInteract = noInteract;
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
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName));

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

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "navigation",
        HelpUrl = "https://help.claris.com/en/pro-help/content/go-to-record-request-page.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "RowPageLocation", XmlElement = "RowPageLocation", XmlAttr = "value", Type = "enum", ValidValues = ["First", "Last", "Previous", "Next", "ByCalculation"], DefaultValue = "Next" },
            new ParamMetadata { Name = "Exit", XmlElement = "Exit", XmlAttr = "state", Type = "boolean", HrLabel = "Exit after last" },
            new ParamMetadata { Name = "Calculation", XmlElement = "Calculation", Type = "calculation" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };

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
