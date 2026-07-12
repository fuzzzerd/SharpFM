using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Shapes;
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
public sealed class SendMailStep : ScriptStep<SendMailStep>, IStepFactory
{
    public const int XmlId = 63;
    public const string XmlName = "Send Mail";

    public bool WithDialog { get; set; }
    public StepChildBag Children { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteract { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// Display edits are anchor-preserved when the child bag is populated:
    /// the display line carries only the dialog flag (plus To/Subject
    /// annotations), never the bag's SMTP/OAuth/recipient elements.
    /// </summary>
    public override bool IsFullyEditable => Children.Children.Count == 0;

    /// <summary>Shape-facing view of <see cref="Children"/> for the trailing passthrough slot.</summary>
    public List<XElement> ExtraChildren
    {
        get => Children.Children.ToList();
        set => Children = new StepChildBag(value);
    }

    private SendMailStep() : base(false)
    {
        Children = new StepChildBag();
    }

    public SendMailStep(bool withDialog = true, StepChildBag? children = null, bool enabled = true)
        : base(enabled)
    {
        WithDialog = withDialog;
        Children = children ?? new StepChildBag();
    }

    // Hand-written: the To/Subject annotations are conditional tokens read
    // from the passthrough child bag, which the shape renderer cannot surface.
    public override string ToDisplayLine()
    {
        var parts = new List<string>();
        parts.Add($"With dialog: {(WithDialog ? "On" : "Off")}");
        if (To is { } to) parts.Add($"To: {to.Text}");
        if (Subject is { } subject) parts.Add($"Subject: {subject.Text}");
        return $"Send Mail [ {string.Join(" ; ", parts)} ]";
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
        // NoInteract (inverse of WithDialog) followed by every other child
        // preserved verbatim — the hybrid StepChildBag round-trip.
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteract", HrLabel = "With dialog", DisplayInverted = true },
            new Passthrough { PocoProperty = "ExtraChildren" },
            new HrOnly("To") { HrLabel = "To" },
            new HrOnly("Cc") { HrLabel = "Cc" },
            new HrOnly("Bcc") { HrLabel = "Bcc" },
            new HrOnly("Subject") { HrLabel = "Subject" },
            new HrOnly("Message") { HrLabel = "Message" },
        ],
    };
}
