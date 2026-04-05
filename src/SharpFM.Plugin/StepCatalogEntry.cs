// This file is part of SharpFM and is licensed under the GNU General Public License v3.
//
// Plugin Exception: You may create plugins that implement these interfaces without those
// plugins being subject to the GPL. Such plugins may use any license, including proprietary.

using System.Collections.Generic;

namespace SharpFM.Plugin;

/// <summary>
/// A script step definition from the FileMaker step catalog.
/// </summary>
public record StepCatalogEntry(
    string Name,
    string Category,
    string? Signature,
    IReadOnlyList<StepCatalogParam> Params);

/// <summary>
/// A parameter definition for a catalog step.
/// </summary>
public record StepCatalogParam(
    string Name,
    string Type,
    bool Required);
