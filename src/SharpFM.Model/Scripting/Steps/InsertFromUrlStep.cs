using System;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Serialization;
using SharpFM.Model.Scripting.Shapes;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class InsertFromUrlStep : ScriptStep, IStepFactory
{
    public const int XmlId = 160;
    public const string XmlName = "Insert from URL";

    public bool SelectAll { get; set; }
    public bool WithDialog { get; set; }
    public bool VerifySslCertificates { get; set; }
    public bool DontEncodeUrl { get; set; }
    public FieldRef? Target { get; set; }
    public Calculation? Url { get; set; }
    public Calculation? CurlOptions { get; set; }

    /// <summary><c>&lt;NoInteract&gt;</c> XML state — the inverse of <see cref="WithDialog"/>. Bound by the shape.</summary>
    public bool NoInteractState { get => !WithDialog; set => WithDialog = !value; }

    /// <summary>
    /// Emit-only projection of FileMaker's bare <c>&lt;Text/&gt;</c> marker,
    /// written before <c>&lt;Field&gt;</c> when the target is a variable.
    /// Declared as an explicit shape node (rather than
    /// <c>FieldChild.VariableTextMarker</c>) so the canonical-shape validator
    /// recognizes the marker element. Get-only, so the shape parser skips it.
    /// </summary>
    public bool TargetIsVariable => Target?.IsVariable == true;

    private InsertFromUrlStep() : this(enabled: true) { }

    public InsertFromUrlStep(
        bool selectAll = true,
        bool withDialog = true,
        bool verifySslCertificates = false,
        bool dontEncodeUrl = false,
        FieldRef? target = null,
        Calculation? url = null,
        Calculation? curlOptions = null,
        bool enabled = true)
        : base(enabled)
    {
        SelectAll = selectAll;
        WithDialog = withDialog;
        VerifySslCertificates = verifySslCertificates;
        DontEncodeUrl = dontEncodeUrl;
        Target = target;
        Url = url;
        CurlOptions = curlOptions;
    }

    public override XElement ToXml() => StepXmlRenderer.Render(this, Metadata);

    // Hand-written: bare "Select" presence token and conditional per-flag
    // grammar the shape renderer cannot produce.
    public override string ToDisplayLine()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (SelectAll) parts.Add("Select");
        parts.Add($"With dialog: {(WithDialog ? "On" : "Off")}");
        if (Target is not null) parts.Add($"Target: {Target.ToDisplayString()}");
        if (Url is not null) parts.Add(Url.Text);
        if (VerifySslCertificates) parts.Add("Verify SSL Certificates");
        if (CurlOptions is not null) parts.Add($"cURL options: {CurlOptions.Text}");
        if (DontEncodeUrl) parts.Add("Don't encode URL");
        return $"Insert from URL [ {string.Join(" ; ", parts)} ]";
    }

    public static new ScriptStep FromXml(XElement step) =>
        StepXmlParser.Parse<InsertFromUrlStep>(step, Metadata);

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        bool selectAll = false;
        bool withDialog = true;
        bool verify = false;
        bool dontEncode = false;
        FieldRef? target = null;
        Calculation? url = null;
        Calculation? curl = null;
        bool urlSeen = false;
        foreach (var tok in hrParams)
        {
            var t = tok.Trim();
            if (t.Equals("Select", StringComparison.OrdinalIgnoreCase))
                selectAll = true;
            else if (t.Equals("Verify SSL Certificates", StringComparison.OrdinalIgnoreCase))
                verify = true;
            else if (t.Equals("Don't encode URL", StringComparison.OrdinalIgnoreCase))
                dontEncode = true;
            else if (t.StartsWith("With dialog:", StringComparison.OrdinalIgnoreCase))
                withDialog = t.Substring(12).Trim().Equals("On", StringComparison.OrdinalIgnoreCase);
            else if (t.StartsWith("Target:", StringComparison.OrdinalIgnoreCase))
                target = FieldRef.FromDisplayToken(t.Substring(7).Trim());
            else if (t.StartsWith("cURL options:", StringComparison.OrdinalIgnoreCase))
                curl = new Calculation(t.Substring(13).Trim());
            else if (!urlSeen && !string.IsNullOrWhiteSpace(t))
            {
                url = new Calculation(t);
                urlSeen = true;
            }
        }
        return new InsertFromUrlStep(selectAll, withDialog, verify, dontEncode, target, url, curl, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "fields",
        HelpUrl = "https://help.claris.com/en/pro-help/content/insert-from-url.html",
        Shape =
        [
            new BoolStateChild("NoInteract") { PocoProperty = "NoInteractState", HrLabel = "With dialog" },
            new BoolStateChild("DontEncodeURL") { PocoProperty = "DontEncodeUrl", Display = DisplayMode.Augmented },
            new BoolStateChild("SelectAll") { HrLabel = "Select" },
            new BoolStateChild("VerifySSLCertificates") { PocoProperty = "VerifySslCertificates", HrLabel = "Verify SSL Certificates" },
            new NamedCalcChild("CURLOptions") { PocoProperty = "CurlOptions", Optional = true, HrLabel = "cURL options" },
            new BareCalcChild { PocoProperty = "Url", Optional = true },
            new FlagChild("Text") { PocoProperty = "TargetIsVariable", Display = DisplayMode.Hidden },
            new FieldChild { PocoProperty = "Target", Optional = true, HrLabel = "Target" },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
