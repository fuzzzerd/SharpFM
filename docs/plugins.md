# Plugins

SharpFM has a plugin system, currently in development.

The plugin contracts live in [`src/SharpFM.Plugin/`](../src/SharpFM.Plugin/) and [`src/SharpFM.Plugin.UI/`](../src/SharpFM.Plugin.UI/). [`src/SharpFM.Plugin.Sample/`](../src/SharpFM.Plugin.Sample/) is a working reference plugin.

Plugins are loaded from a `plugins/` directory next to the SharpFM executable, or installed through **Plugins > Manage Plugins...** in the app.
