// Step catalog loader — ported from agentic-fm (https://github.com/petrowsky/agentic-fm)
// Copyright 2026 Matt Petrowsky, Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SharpFM.Core.ScriptConverter;

public static class StepCatalogLoader
{
    private static readonly Lazy<IReadOnlyList<StepDefinition>> _all = new(LoadCatalog);
    private static readonly Lazy<IReadOnlyDictionary<int, StepDefinition>> _byId = new(BuildByIdIndex);
    private static readonly Lazy<IReadOnlyDictionary<string, StepDefinition>> _byName = new(BuildByNameIndex);

    public static IReadOnlyList<StepDefinition> All => _all.Value;
    public static IReadOnlyDictionary<int, StepDefinition> ById => _byId.Value;
    public static IReadOnlyDictionary<string, StepDefinition> ByName => _byName.Value;

    private static IReadOnlyList<StepDefinition> LoadCatalog()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("step-catalog-en.json"));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Step catalog resource not found");

        var steps = JsonSerializer.Deserialize<List<StepDefinition>>(stream)
            ?? throw new InvalidOperationException("Failed to deserialize step catalog");

        return steps.AsReadOnly();
    }

    private static IReadOnlyDictionary<int, StepDefinition> BuildByIdIndex()
    {
        var dict = new Dictionary<int, StepDefinition>();
        foreach (var step in All)
        {
            if (step.Id.HasValue)
                dict.TryAdd(step.Id.Value, step);
        }
        return dict;
    }

    private static IReadOnlyDictionary<string, StepDefinition> BuildByNameIndex()
    {
        var dict = new Dictionary<string, StepDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in All)
        {
            dict.TryAdd(step.Name, step);
        }
        return dict;
    }
}
