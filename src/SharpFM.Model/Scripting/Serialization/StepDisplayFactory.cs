using System;
using System.Collections.Generic;

namespace SharpFM.Model.Scripting.Serialization;

/// <summary>
/// Dispatches display-text parsing to typed <see cref="ScriptStep"/>
/// POCOs. Each POCO owns its display-to-domain parsing. Registrations
/// are bridged in from <c>StepRegistry</c>'s reflection scan of
/// <c>IStepFactory</c> implementers.
///
/// <para>
/// Unregistered step names return <c>null</c> from <see cref="TryCreate"/>
/// so the caller can fall through to the catalog-driven build path for
/// forward-compat support of unknown step names.
/// </para>
/// </summary>
public static class StepDisplayFactory
{
    private static readonly Dictionary<string, Func<bool, string[], ScriptStep>> _typed =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a typed POCO's display-text factory delegate.
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
