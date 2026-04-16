using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Steps;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Dispatches a parsed <c>&lt;Step&gt;</c> element to either a typed
/// <see cref="ScriptStep"/> POCO (when registered) or a
/// <see cref="RawStep"/> fallback. Every migrated step registers its
/// factory delegate here; unmigrated steps ride the RawStep path and
/// retain full lossless round-trip behavior through the preserved
/// source element.
///
/// <para>
/// Registration happens at module initialization time inside the
/// <c>SharpFM.Model.Scripting.Steps</c> namespace so the registry is
/// populated before any <see cref="ScriptStep.FromXml"/> call.
/// </para>
/// </summary>
public static class StepXmlFactory
{
    private static readonly Dictionary<string, Func<XElement, ScriptStep>> _typed =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a typed POCO's XML factory delegate. Called once per
    /// migrated step at module initialization.
    /// </summary>
    public static void Register(string stepName, Func<XElement, ScriptStep> creator)
    {
        _typed[stepName] = creator;
    }

    /// <summary>
    /// Names of step kinds that have typed POCOs registered. Used by the
    /// allow-list contract test to enforce that a POCO-backed step is
    /// never also listed as an allow-list exception — the two mechanisms
    /// are mutually exclusive sources of the "fully editable" property.
    /// </summary>
    public static IReadOnlyCollection<string> RegisteredNames => _typed.Keys;

    /// <summary>
    /// Build a <see cref="ScriptStep"/> from a raw <c>&lt;Step&gt;</c>
    /// element. Returns a typed POCO when the step name is registered,
    /// otherwise wraps the element in a <see cref="RawStep"/> (with the
    /// catalog definition attached when available).
    /// </summary>
    public static ScriptStep Create(XElement stepElement)
    {
        var name = stepElement.Attribute("name")?.Value ?? "";

        if (_typed.TryGetValue(name, out var creator))
            return creator(stepElement);

        var definition = LookupDefinition(name, stepElement.Attribute("id")?.Value);
        return new RawStep(stepElement, definition);
    }

    private static StepDefinition? LookupDefinition(string name, string? idStr)
    {
        if (StepCatalogLoader.ByName.TryGetValue(name, out var byName))
            return byName;
        if (idStr != null && int.TryParse(idStr, out var id) &&
            StepCatalogLoader.ById.TryGetValue(id, out var byId))
            return byId;
        return null;
    }
}
