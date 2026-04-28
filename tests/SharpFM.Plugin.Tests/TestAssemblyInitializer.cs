using System.Runtime.CompilerServices;
using SharpFM.Model.ClipTypes;

namespace SharpFM.Plugin.Tests;

internal static class TestAssemblyInitializer
{
    /// <summary>
    /// Register built-in clip-type strategies once at assembly load time so any
    /// test that constructs a <see cref="SharpFM.Model.Clip"/> sees them.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        ClipTypeRegistry.RegisterBuiltIns();
    }
}
