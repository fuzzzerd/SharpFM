using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Plugin;
using SharpFM.Plugin.UI;
using SharpFM.Services;
using Xunit;

namespace SharpFM.Plugin.Tests;

public class PluginConfigServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly PluginConfigService _svc;

    public PluginConfigServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"sharpfm-cfg-{Guid.NewGuid()}");
        _svc = new PluginConfigService(NullLogger.Instance, _dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    // ---------------- Happy-path ----------------

    [Fact]
    public void Load_WithNoFile_ReturnsDefaults()
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, "hello"),
            new PluginConfigField("n", "N", PluginConfigFieldType.Int, 7),
        });

        var values = _svc.Load("p1", schema);

        Assert.Equal("hello", values["s"]);
        Assert.Equal(7, values["n"]);
        Assert.False(File.Exists(_svc.GetConfigPath("p1")), "Load must not create a file.");
    }

    [Theory]
    [InlineData(PluginConfigFieldType.String, "hello world")]
    [InlineData(PluginConfigFieldType.MultilineString, "line1\nline2")]
    [InlineData(PluginConfigFieldType.Bool, true)]
    [InlineData(PluginConfigFieldType.Bool, false)]
    [InlineData(PluginConfigFieldType.Int, 42)]
    [InlineData(PluginConfigFieldType.Double, 3.14)]
    public void Save_ThenLoad_ReturnsSameValue(PluginConfigFieldType type, object value)
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("k", "K", type, DefaultValue: GetDefault(type)),
        });

        _svc.Save("p1", schema, new Dictionary<string, object?> { ["k"] = value });
        var loaded = _svc.Load("p1", schema);

        Assert.Equal(value, loaded["k"]);
    }

    [Fact]
    public void Save_ThenLoad_Enum_ReturnsSameValue()
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("color", "Color", PluginConfigFieldType.Enum,
                DefaultValue: "red", EnumValues: new[] { "red", "green", "blue" }),
        });

        _svc.Save("p1", schema, new Dictionary<string, object?> { ["color"] = "green" });
        var loaded = _svc.Load("p1", schema);

        Assert.Equal("green", loaded["color"]);
    }

    [Fact]
    public void Save_WritesJsonAtExpectedPath()
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, ""),
        });

        _svc.Save("my-plugin", schema, new Dictionary<string, object?> { ["s"] = "v" });

        var path = _svc.GetConfigPath("my-plugin");
        Assert.True(File.Exists(path));
        Assert.Contains("\"s\"", File.ReadAllText(path));
    }

    [Fact]
    public void TwoPlugins_WriteIndependentFiles()
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, "def"),
        });

        _svc.Save("a", schema, new Dictionary<string, object?> { ["s"] = "va" });
        _svc.Save("b", schema, new Dictionary<string, object?> { ["s"] = "vb" });

        Assert.Equal("va", _svc.Load("a", schema)["s"]);
        Assert.Equal("vb", _svc.Load("b", schema)["s"]);
    }

    // ---------------- Regression guards ----------------

    [Fact]
    public void Load_WithMalformedJson_ReturnsDefaults_DoesNotThrow()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "p1.json"), "{ not valid json");

        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, "fallback"),
        });

        var values = _svc.Load("p1", schema);
        Assert.Equal("fallback", values["s"]);
    }

    [Fact]
    public void Load_WithMissingField_FillsFromDefault()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "p1.json"), "{\"a\": \"persisted\"}");

        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("a", "A", PluginConfigFieldType.String, "defA"),
            new PluginConfigField("b", "B", PluginConfigFieldType.String, "defB"),
        });

        var values = _svc.Load("p1", schema);
        Assert.Equal("persisted", values["a"]);
        Assert.Equal("defB", values["b"]);
    }

    [Fact]
    public void Load_WithWrongType_FallsBackToDefault_OtherFieldsIntact()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "p1.json"),
            "{\"num\": \"not a number\", \"name\": \"ok\"}");

        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("num", "Num", PluginConfigFieldType.Int, 99),
            new PluginConfigField("name", "Name", PluginConfigFieldType.String, "def"),
        });

        var values = _svc.Load("p1", schema);
        Assert.Equal(99, values["num"]);
        Assert.Equal("ok", values["name"]);
    }

    [Fact]
    public void Load_WithUnknownKey_DropsIt_AndSaveDoesNotReWriteIt()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "p1.json");
        File.WriteAllText(path, "{\"known\": \"v\", \"old-renamed\": \"leftover\"}");

        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("known", "Known", PluginConfigFieldType.String, "def"),
        });

        var values = _svc.Load("p1", schema);
        Assert.False(values.ContainsKey("old-renamed"));
        Assert.Equal("v", values["known"]);

        _svc.Save("p1", schema, values);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("old-renamed", text);
    }

    [Fact]
    public void Load_EnumValueNotInAllowedList_FallsBackToDefault()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "p1.json"), "{\"color\": \"puce\"}");

        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("color", "C", PluginConfigFieldType.Enum,
                DefaultValue: "red", EnumValues: new[] { "red", "green", "blue" }),
        });

        var values = _svc.Load("p1", schema);
        Assert.Equal("red", values["color"]);
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("foo/bar")]
    [InlineData("foo\\bar")]
    [InlineData(":weird")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("....")]
    public void Save_SanitizesPluginId_CannotEscapeConfigDirectory(string pluginId)
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, "def"),
        });
        _svc.Save(pluginId, schema, new Dictionary<string, object?> { ["s"] = "v" });

        var path = _svc.GetConfigPath(pluginId);
        var fullConfigDir = Path.GetFullPath(_dir);
        var fullFile = Path.GetFullPath(path);

        Assert.StartsWith(fullConfigDir + Path.DirectorySeparatorChar, fullFile);

        // Save/Load still works for the sanitized name.
        var loaded = _svc.Load(pluginId, schema);
        Assert.Equal("v", loaded["s"]);
    }

    [Fact]
    public void ConfigDirectory_IsCreatedOnFirstSave()
    {
        Assert.False(Directory.Exists(_dir));

        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, ""),
        });
        _svc.Save("p1", schema, new Dictionary<string, object?> { ["s"] = "v" });

        Assert.True(Directory.Exists(_dir));
    }

    [Fact]
    public void Save_OnlyPersistsSchemaKeys()
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("keep", "K", PluginConfigFieldType.String, ""),
        });

        _svc.Save("p1", schema, new Dictionary<string, object?>
        {
            ["keep"] = "yes",
            ["drop-me"] = "leaked",
        });

        var text = File.ReadAllText(_svc.GetConfigPath("p1"));
        Assert.Contains("keep", text);
        Assert.DoesNotContain("drop-me", text);
        Assert.DoesNotContain("leaked", text);
    }

    // ---------------- Lifecycle (Apply) ----------------

    [Fact]
    public void Apply_PluginWithSchema_ReceivesInitialValues()
    {
        var plugin = new FakePlugin
        {
            ConfigSchemaOverride = new PluginConfigSchema(new[]
            {
                new PluginConfigField("s", "S", PluginConfigFieldType.String, "defaultS"),
                new PluginConfigField("n", "N", PluginConfigFieldType.Int, 10),
            }),
        };
        plugin.Initialize(null!);

        _svc.Apply(plugin);

        Assert.Equal(new[] { "Initialize", "OnConfigChanged" }, plugin.CallOrder);
        Assert.NotNull(plugin.LastConfigValues);
        Assert.Equal("defaultS", plugin.LastConfigValues!["s"]);
        Assert.Equal(10, plugin.LastConfigValues!["n"]);
    }

    [Fact]
    public void Apply_PluginWithPersistedConfig_ReceivesStoredValues()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "fake.json"), "{\"n\": 42}");

        var plugin = new FakePlugin
        {
            Id = "fake",
            ConfigSchemaOverride = new PluginConfigSchema(new[]
            {
                new PluginConfigField("n", "N", PluginConfigFieldType.Int, 1),
            }),
        };

        _svc.Apply(plugin);

        Assert.Equal(42, plugin.LastConfigValues!["n"]);
    }

    [Fact]
    public void Apply_PluginWithEmptySchema_DoesNotInvokeOnConfigChanged_NorWriteFile()
    {
        var plugin = new FakePlugin { ConfigSchemaOverride = PluginConfigSchema.Empty };

        _svc.Apply(plugin);

        Assert.Null(plugin.LastConfigValues);
        Assert.False(Directory.Exists(_dir));
    }

    [Fact]
    public void Apply_PluginThrowsFromOnConfigChanged_DoesNotPropagate()
    {
        var plugin = new FakePlugin
        {
            ConfigSchemaOverride = new PluginConfigSchema(new[]
            {
                new PluginConfigField("s", "S", PluginConfigFieldType.String, "x"),
            }),
            ThrowOnConfigChanged = true,
        };

        // Must not throw — host must keep loading other plugins.
        _svc.Apply(plugin);
    }

    [Fact]
    public void SaveFromUi_Flow_PersistsAndIsReadableByFreshService()
    {
        var schema = new PluginConfigSchema(new[]
        {
            new PluginConfigField("s", "S", PluginConfigFieldType.String, "def"),
        });

        var plugin = new FakePlugin { Id = "ui-flow", ConfigSchemaOverride = schema };
        var edited = new Dictionary<string, object?> { ["s"] = "new-value" };

        _svc.Save(plugin.Id, schema, edited);
        _svc.Apply(plugin);

        Assert.Equal("new-value", plugin.LastConfigValues!["s"]);

        var fresh = new PluginConfigService(NullLogger.Instance, _dir);
        var reloaded = fresh.Load(plugin.Id, schema);
        Assert.Equal("new-value", reloaded["s"]);
    }

    [Fact]
    public void Apply_DoesNotRepeatedlyQueryConfigSchema()
    {
        // Guard against accidental per-call schema allocation in plugin authors'
        // implementations becoming a repeated-read perf problem on the host.
        var plugin = new FakePlugin
        {
            ConfigSchemaOverride = new PluginConfigSchema(new[]
            {
                new PluginConfigField("s", "S", PluginConfigFieldType.String, "x"),
            }),
        };

        _svc.Apply(plugin);

        Assert.InRange(plugin.ConfigSchemaReads, 1, 3);
    }

    // ---------------- Helpers ----------------

    private static object GetDefault(PluginConfigFieldType t) => t switch
    {
        PluginConfigFieldType.String or PluginConfigFieldType.MultilineString => "",
        PluginConfigFieldType.Bool => false,
        PluginConfigFieldType.Int => 0,
        PluginConfigFieldType.Double => 0.0,
        _ => "",
    };

    private sealed class FakePlugin : IPlugin
    {
        public string Id { get; set; } = "fake";
        public string DisplayName => "Fake";
        public string Description => "";
        public string Version => "0.0.0";
        public IReadOnlyList<PluginKeyBinding> KeyBindings => [];
        public IReadOnlyList<PluginMenuAction> MenuActions => [];

        public PluginConfigSchema ConfigSchemaOverride { get; set; } = PluginConfigSchema.Empty;
        public int ConfigSchemaReads;
        public PluginConfigSchema ConfigSchema
        {
            get { ConfigSchemaReads++; return ConfigSchemaOverride; }
        }

        public bool ThrowOnConfigChanged { get; set; }
        public IReadOnlyDictionary<string, object?>? LastConfigValues { get; private set; }
        public List<string> CallOrder { get; } = new();

        public void Initialize(IPluginHost host) => CallOrder.Add("Initialize");

        public void OnConfigChanged(IReadOnlyDictionary<string, object?> values)
        {
            CallOrder.Add("OnConfigChanged");
            LastConfigValues = values;
            if (ThrowOnConfigChanged) throw new InvalidOperationException("boom");
        }

        public void Dispose() { }
    }
}
