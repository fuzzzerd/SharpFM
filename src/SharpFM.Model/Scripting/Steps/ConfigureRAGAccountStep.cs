using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Registry;
using SharpFM.Model.Scripting.Values;

namespace SharpFM.Model.Scripting.Steps;

public sealed class ConfigureRAGAccountStep : ScriptStep, IStepFactory
{
    public const int XmlId = 227;
    public const string XmlName = "Configure RAG Account ";

    public Calculation RAGAccountName { get; set; }
    public Calculation Endpoint { get; set; }
    public Calculation APIKey { get; set; }
    public bool VerifySSLCertificates { get; set; }

    public ConfigureRAGAccountStep(
        Calculation? rAGAccountName = null,
        Calculation? endpoint = null,
        Calculation? aPIKey = null,
        bool verifySSLCertificates = false,
        bool enabled = true)
        : base(null, enabled)
    {
        RAGAccountName = rAGAccountName ?? new Calculation("");
        Endpoint = endpoint ?? new Calculation("");
        APIKey = aPIKey ?? new Calculation("");
        VerifySSLCertificates = verifySSLCertificates;
    }

    public override XElement ToXml() =>
        new("Step",
            new XAttribute("enable", Enabled ? "True" : "False"),
            new XAttribute("id", XmlId),
            new XAttribute("name", XmlName),
            new XElement("RAGAccountName", RAGAccountName.ToXml("Calculation")),
            new XElement("Endpoint", Endpoint.ToXml("Calculation")),
            new XElement("AccessAPIKey", APIKey.ToXml("Calculation")),
            new XElement("VerifySSLCertificates", new XAttribute("state", VerifySSLCertificates ? "True" : "False")));

    public override string ToDisplayLine() =>
        "Configure RAG Account  [ " + "RAG Account Name: " + RAGAccountName.Text + " ; " + "Endpoint: " + Endpoint.Text + " ; " + "API key: " + APIKey.Text + " ; " + "Verify SSL Certificates: " + (VerifySSLCertificates ? "On" : "Off") + " ]";

    public static new ScriptStep FromXml(XElement step)
    {
        var enabled = step.Attribute("enable")?.Value != "False";
        var rAGAccountName_vWrapEl = step.Element("RAGAccountName");
        var rAGAccountName_vCalcEl = rAGAccountName_vWrapEl?.Element("Calculation");
        var rAGAccountName_v = rAGAccountName_vCalcEl is not null ? Calculation.FromXml(rAGAccountName_vCalcEl) : new Calculation("");
        var endpoint_vWrapEl = step.Element("Endpoint");
        var endpoint_vCalcEl = endpoint_vWrapEl?.Element("Calculation");
        var endpoint_v = endpoint_vCalcEl is not null ? Calculation.FromXml(endpoint_vCalcEl) : new Calculation("");
        var aPIKey_vWrapEl = step.Element("AccessAPIKey");
        var aPIKey_vCalcEl = aPIKey_vWrapEl?.Element("Calculation");
        var aPIKey_v = aPIKey_vCalcEl is not null ? Calculation.FromXml(aPIKey_vCalcEl) : new Calculation("");
        var verifySSLCertificates_v = step.Element("VerifySSLCertificates")?.Attribute("state")?.Value == "True";
        return new ConfigureRAGAccountStep(rAGAccountName_v, endpoint_v, aPIKey_v, verifySSLCertificates_v, enabled);
    }

    public static ScriptStep FromDisplayParams(bool enabled, string[] hrParams)
    {
        var tokens = hrParams.Select(h => h.Trim()).ToArray();
        Calculation? rAGAccountName_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("RAG Account Name:", StringComparison.OrdinalIgnoreCase)) { rAGAccountName_v = new Calculation(tok.Substring(17).Trim()); break; } }
        Calculation? endpoint_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("Endpoint:", StringComparison.OrdinalIgnoreCase)) { endpoint_v = new Calculation(tok.Substring(9).Trim()); break; } }
        Calculation? aPIKey_v = null;
        foreach (var tok in tokens) { if (tok.StartsWith("API key:", StringComparison.OrdinalIgnoreCase)) { aPIKey_v = new Calculation(tok.Substring(8).Trim()); break; } }
        bool verifySSLCertificates_v = false;
        foreach (var tok in tokens) { if (tok.StartsWith("Verify SSL Certificates:", StringComparison.OrdinalIgnoreCase)) { var v = tok.Substring(24).Trim(); verifySSLCertificates_v = v.Equals("On", StringComparison.OrdinalIgnoreCase); break; } }
        return new ConfigureRAGAccountStep(rAGAccountName_v, endpoint_v, aPIKey_v, verifySSLCertificates_v, enabled);
    }

    public static StepMetadata Metadata { get; } = new()
    {
        Name = XmlName,
        Id = XmlId,
        Category = "artificial intelligence",
        Params =
        [
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "RAG Account Name",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "Endpoint",
            },
            new ParamMetadata
            {
                Name = "Calculation",
                XmlElement = "Calculation",
                Type = "namedCalc",
                HrLabel = "API key",
            },
            new ParamMetadata
            {
                Name = "VerifySSLCertificates",
                XmlElement = "VerifySSLCertificates",
                Type = "boolean",
                XmlAttr = "state",
                HrLabel = "Verify SSL Certificates",
                ValidValues = ["On", "Off"],
                DefaultValue = "False",
            },
        ],
        FromXml = FromXml,
        FromDisplay = FromDisplayParams,
    };
}
