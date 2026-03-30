// Step catalog loader — ported from agentic-fm (https://github.com/petrowsky/agentic-fm)
// Copyright 2026 Matt Petrowsky, Apache License 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SharpFM.Scripting.Catalog;

public class StepCatalogLoader : IStepCatalog
{
    private static readonly Lazy<StepCatalogLoader> _instance = new(() => new StepCatalogLoader());
    private readonly IReadOnlyList<StepDefinition> _all;
    private readonly IReadOnlyDictionary<int, StepDefinition> _byId;
    private readonly IReadOnlyDictionary<string, StepDefinition> _byName;

    /// <summary>Default singleton instance. Use directly or inject IStepCatalog for testing.</summary>
    public static StepCatalogLoader Default => _instance.Value;

    // Static accessors for backward compatibility
    public static IReadOnlyList<StepDefinition> All => Default._all;
    public static IReadOnlyDictionary<int, StepDefinition> ById => Default._byId;
    public static IReadOnlyDictionary<string, StepDefinition> ByName => Default._byName;

    IReadOnlyList<StepDefinition> IStepCatalog.All => _all;

    public bool TryGetByName(string name, out StepDefinition definition)
    {
        return _byName.TryGetValue(name, out definition!);
    }

    public bool TryGetById(int id, out StepDefinition definition)
    {
        return _byId.TryGetValue(id, out definition!);
    }

    private StepCatalogLoader()
    {
        _all = LoadCatalog();
        _byId = BuildByIdIndex();
        _byName = BuildByNameIndex();
    }

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

    private IReadOnlyDictionary<int, StepDefinition> BuildByIdIndex()
    {
        var dict = new Dictionary<int, StepDefinition>();
        foreach (var step in _all)
        {
            if (step.Id.HasValue)
                dict.TryAdd(step.Id.Value, step);
        }
        return dict;
    }

    private IReadOnlyDictionary<string, StepDefinition> BuildByNameIndex()
    {
        var dict = new Dictionary<string, StepDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in _all)
        {
            dict.TryAdd(step.Name, step);
        }
        return dict;
    }
}
