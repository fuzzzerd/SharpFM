using System.Collections.Generic;

namespace SharpFM.Model.Scripting;

/// <summary>
/// Abstracts step catalog access for testability and potential locale/version swapping.
/// </summary>
public interface IStepCatalog
{
    IReadOnlyList<StepDefinition> All { get; }
    bool TryGetByName(string name, out StepDefinition definition);
    bool TryGetById(int id, out StepDefinition definition);
}
