using System;
using System.Xml.Linq;

namespace SharpFM.Core.ScriptConverter.StepHandlers;

/// <summary>
/// Shared helpers for step handler implementations.
/// </summary>
internal abstract class StepHandlerBase
{
    protected static XElement MakeStep(int id, string name, bool enabled)
    {
        return new XElement("Step",
            new XAttribute("enable", enabled ? "True" : "False"),
            new XAttribute("id", id),
            new XAttribute("name", name));
    }

    protected static string? ExtractLabeled(string[] hrParams, string label)
    {
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (trimmed.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(label.Length + 1).TrimStart();
        }
        return null;
    }

    protected static string? ExtractPositional(string[] hrParams, Func<string, bool> predicate)
    {
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (predicate(trimmed)) return trimmed;
        }
        return null;
    }

    protected static string? ExtractFirstUnlabeled(string[] hrParams)
    {
        foreach (var p in hrParams)
        {
            var trimmed = p.Trim();
            if (!trimmed.Contains(':')) return trimmed;
        }
        return null;
    }
}
