using Xunit;

namespace SharpFM.Tests.ClipTypes;

/// <summary>
/// Test collection for classes that mutate <see cref="SharpFM.Model.ClipTypes.ClipTypeRegistry"/>
/// (e.g. <c>Reset</c>). Marked non-parallel so they don't race with tests in other classes
/// that assume the registry is populated.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RegistryMutatingCollection
{
    public const string Name = "RegistryMutating";
}
