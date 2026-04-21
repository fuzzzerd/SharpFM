using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SharpFM.Model.Scripting.Steps;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Dispatches a parsed <c>&lt;Step&gt;</c> element to a typed
/// <see cref="ScriptStep"/> POCO, or to a <see cref="RawStep"/> fallback
/// for unknown step names. The typed-POCO registrations are bridged in
/// from <c>StepRegistry</c>'s reflection scan of <c>IStepFactory</c>
/// implementers.
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
    /// Names of step kinds that have typed POCOs registered.
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
