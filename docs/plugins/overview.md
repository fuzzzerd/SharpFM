# SharpFM Plugin System

SharpFM supports four types of plugins, all sharing a common base interface (`IPlugin`) for metadata and lifecycle.

## Plugin Types

| Type | Interface | Purpose |
|------|-----------|---------|
| **Panel** | `IPanelPlugin` | Sidebar UI panels that display clip data |
| **Event** | `IEventPlugin` | Headless handlers that react to host events |
| **Persistence** | `IPersistencePlugin` | Alternative storage backends (cloud, database) |
| **Transform** | `IClipTransformPlugin` | Modify clip XML during import/export |

## Getting Started

### 1. Create a Class Library

Create a new .NET 10 class library and reference `SharpFM.Plugin`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="path/to/SharpFM.Plugin.csproj" />
  </ItemGroup>
</Project>
```

### 2. Implement a Plugin Interface

Choose the interface that matches your use case and implement it. Every plugin must provide:

- `Id` — unique identifier (e.g., `"my-plugin"`)
- `DisplayName` — shown in the Plugins menu
- `Version` — shown in the Plugin Manager
- `Initialize(IPluginHost host)` — called once at startup
- `Dispose()` — cleanup when unloaded

### 3. Build and Install

Build your plugin as a DLL and install it via the Plugin Manager ("Install from File...") or copy it to the `plugins/` directory next to the SharpFM executable.

## Discovery

SharpFM scans the `plugins/` directory at startup for `.dll` files. Each assembly is loaded in its own `AssemblyLoadContext` and reflected for types implementing `IPlugin` subtypes. Each class is categorized into exactly one plugin type based on the interface it implements.

## Licensing

SharpFM and its plugin interfaces are licensed under the GNU General Public License v3.
