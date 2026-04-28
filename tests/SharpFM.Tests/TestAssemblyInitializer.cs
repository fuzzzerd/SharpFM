using System.Runtime.CompilerServices;
using SharpFM.Model.ClipTypes;

namespace SharpFM.Tests;

internal static class TestAssemblyInitializer
{
    /// <summary>
    /// Force-load the SharpFM assembly so its module initializer runs and
    /// installs <see cref="SharpFM.Model.Scripting.ScriptStep.SpecializedDisplayRenderer"/>.
    /// Without this, tests that touch only SharpFM.Model types in isolation
    /// would render steps via the generic path and miss the canonical formatting.
    /// Also registers the built-in clip-type strategies so any test that
    /// constructs a <see cref="SharpFM.Model.Clip"/> sees them.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        _ = typeof(SharpFM.Scripting.ScriptTextParser).FullName;
        ClipTypeRegistry.RegisterBuiltIns();
    }
}
