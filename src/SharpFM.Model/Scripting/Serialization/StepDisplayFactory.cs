using System;
using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Dispatches display-text parsing to typed <see cref="ScriptStep"/>
/// POCOs when a step name is registered, allowing each migrated step to
/// own its own display-to-domain parsing without reaching through the
/// generic catalog-driven path.
///
/// <para>
/// Unregistered step names return <c>null</c> from
/// <see cref="TryCreate"/>, and the caller (<c>ScriptTextParser</c>)
/// falls through to <see cref="CatalogXmlBuilder.BuildStep"/> + a
/// <see cref="Steps.RawStep"/> wrapper.
/// </para>
/// </summary>
public static class StepDisplayFactory
{
    private static readonly Dictionary<string, Func<bool, string[], ScriptStep>> _typed =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a typed POCO's display-text factory delegate. Called
    /// once per migrated step at module initialization.
    /// </summary>
    public static void Register(string stepName, Func<bool, string[], ScriptStep> creator)
    {
        _typed[stepName] = creator;
    }

    /// <summary>
    /// Attempt to build a typed step from parsed display tokens. Returns
    /// <c>null</c> when no typed POCO is registered for the given step
    /// name, leaving the caller to fall through to the generic
    /// catalog-driven build path.
    /// </summary>
    public static ScriptStep? TryCreate(string stepName, bool enabled, string[] hrParams)
    {
        return _typed.TryGetValue(stepName, out var creator) ? creator(enabled, hrParams) : null;
    }
}
