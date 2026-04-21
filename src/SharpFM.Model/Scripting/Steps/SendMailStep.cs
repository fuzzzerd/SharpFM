using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

/// <summary>
/// Send Mail has 30 parameters spanning SMTP auth, OAuth2, attachment
/// paths, and the usual To/Cc/Bcc/Subject/Message calcs. Rather than
/// lock 30 typed properties — each of which can be absent — this POCO
/// uses a hybrid shape: the full child element sequence is preserved
/// verbatim via <see cref="StepChildBag"/> for lossless round-trip,
/// and typed accessors for the handful of "hot" fields (To, Cc, Bcc,
/// Subject, Message) read/write through the bag.
///
/// <para>
/// The To/Cc/Bcc elements carry a <c>UseFoundSet</c> attribute that
/// controls whether the address calc is evaluated once per record of
/// the found set. Hot-field accessors preserve that attribute when
/// setting a new calc.
/// </para>
/// </summary>
public sealed class SendMailStep : ScriptStep, IStepFactory
{
    public const int XmlId = 63;
    public const string XmlName = "Send Mail";

    public bool WithDialog { get; set; }
    public StepChildBag Children { get; set; }

    public SendMailStep(bool withDialog = true, StepChildBag? children = null, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Children = children ?? new StepChildBag();
    }

    public override XElement ToXml()
    {
        var step = new XElement("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("NoInteract", new XAttribute("state", WithDialog ? "False" : "True")));
        Children.AppendTo(step);
        return step;
    }

    public override string ToDisplayLine()
    {
        var parts = new List<string>();
        parts.Add($"With dialog: {(WithDialog ? "On" : "Off")}");
        if (To is { } to) parts.Add($"To: {to.Text}");
        if (Subject is { } subject) parts.Add($"Subject: {subject.Text}");
        return $"Send Mail [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var withDialog = step.Element("NoInteract")?.Attribute("state")?.Value != "True";
        // Skip NoInteract — it's already captured as WithDialog; preserve
        // everything else verbatim.
        var children = step.Elements()
            .Where(e => e.Name.LocalName != "NoInteract")
            .ToList();
        return new SendMailStep(withDialog, new StepChildBag(children), enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        // Display is inherently lossy — 30 params can't survive a short form.
        bool withDialog = true;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
        }
        return new SendMailStep(withDialog, null, enabled);
    }

    // --- Hot-field accessors (read through the bag) ---

    public Calculation? To => ReadCalcChild("To");
    public Calculation? Cc => ReadCalcChild("Cc");
    public Calculation? Bcc => ReadCalcChild("Bcc");
    public Calculation? Subject => ReadCalcChild("Subject");
    public Calculation? Message => ReadCalcChild("Message");

    private Calculation? ReadCalcChild(string elementName)
    {
        var el = Children.FirstByName(elementName);
        var calcEl = el?.Element("Calculation");
        return calcEl is not null ? Calculation.FromXml(calcEl) : null;
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "miscellaneous",
        HelpUrl = "https://help.claris.com/en/pro-help/content/send-mail.html",
        Params =
        [
            new ParamMetadata { Name = "NoInteract", XmlElement = "NoInteract", XmlAttr = "state", Type = "boolean", HrLabel = "With dialog" },
            new ParamMetadata { Name = "To", XmlElement = "To", Type = "namedCalc", HrLabel = "To" },
            new ParamMetadata { Name = "Cc", XmlElement = "Cc", Type = "namedCalc", HrLabel = "Cc" },
            new ParamMetadata { Name = "Bcc", XmlElement = "Bcc", Type = "namedCalc", HrLabel = "Bcc" },
            new ParamMetadata { Name = "Subject", XmlElement = "Subject", Type = "namedCalc", HrLabel = "Subject" },
            new ParamMetadata { Name = "Message", XmlElement = "Message", Type = "namedCalc", HrLabel = "Message" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
