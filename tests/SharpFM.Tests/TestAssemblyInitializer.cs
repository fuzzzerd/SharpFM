using System.Runtime.CompilerServices;

namespace SharpFM.Tests;

internal static class TestAssemblyInitializer
{
    /// <summary>
    /// Force-load the SharpFM assembly so its module initializer runs and
    /// installs <see cref="SharpFM.Model.Scripting.ScriptStep.SpecializedDisplayRenderer"/>.
    /// Without this, tests that touch only SharpFM.Model types in isolation
    /// would render steps via the generic path and miss the canonical formatting.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        _ = typeof(SharpFM.Scripting.ScriptTextParser).FullName;
    }
}
