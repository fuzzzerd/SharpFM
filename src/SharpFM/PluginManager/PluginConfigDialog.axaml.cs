using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using SharpFM.Plugin;

namespace SharpFM.PluginManager;

[ExcludeFromCodeCoverage]
public partial class PluginConfigDialog : Window
{
    private readonly Dictionary<string, Func<object?>> _readers = new(StringComparer.Ordinal);
    private Dictionary<string, object?>? _result;

    public PluginConfigDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public static async System.Threading.Tasks.Task<IReadOnlyDictionary<string, object?>?> ShowAsync(
        Window owner,
        string pluginDisplayName,
        PluginConfigSchema schema,
        IReadOnlyDictionary<string, object?> currentValues)
    {
        var dialog = new PluginConfigDialog();
        dialog.Build(pluginDisplayName, schema, currentValues);
        await dialog.ShowDialog(owner);
        return dialog._result;
    }

    private void Build(string pluginDisplayName, PluginConfigSchema schema, IReadOnlyDictionary<string, object?> currentValues)
    {
        Title = $"Configure {pluginDisplayName}";
        var header = this.FindControl<TextBlock>("headerText");
        if (header is not null) header.Text = pluginDisplayName;

        var panel = this.FindControl<StackPanel>("fieldsPanel");
        if (panel is not null)
        {
            foreach (var field in schema.Fields)
            {
                currentValues.TryGetValue(field.Key, out var current);
                panel.Children.Add(BuildFieldControl(field, current));
            }
        }

        var ok = this.FindControl<Button>("okButton");
        var cancel = this.FindControl<Button>("cancelButton");
        if (ok is not null) ok.Click += (_, _) => { _result = Collect(schema); Close(); };
        if (cancel is not null) cancel.Click += (_, _) => { _result = null; Close(); };
    }

    private Control BuildFieldControl(PluginConfigField field, object? current)
    {
        var container = new StackPanel { Spacing = 4, Orientation = Orientation.Vertical };
        container.Children.Add(new TextBlock { Text = field.Label, FontWeight = Avalonia.Media.FontWeight.SemiBold });

        Control editor;
        switch (field.Type)
        {
            case PluginConfigFieldType.String:
            {
                var tb = new TextBox { Text = current?.ToString() ?? string.Empty };
                _readers[field.Key] = () => tb.Text ?? string.Empty;
                editor = tb;
                break;
            }
            case PluginConfigFieldType.MultilineString:
            {
                var tb = new TextBox
                {
                    Text = current?.ToString() ?? string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    MinHeight = 100,
                };
                _readers[field.Key] = () => tb.Text ?? string.Empty;
                editor = tb;
                break;
            }
            case PluginConfigFieldType.Bool:
            {
                var cb = new CheckBox { IsChecked = current is bool b && b };
                _readers[field.Key] = () => cb.IsChecked ?? false;
                editor = cb;
                break;
            }
            case PluginConfigFieldType.Int:
            {
                var nud = new NumericUpDown
                {
                    Value = current is int i ? (decimal?)i : null,
                    Increment = 1m,
                    FormatString = "0",
                };
                _readers[field.Key] = () => nud.Value is { } v ? (object)(int)v : null;
                editor = nud;
                break;
            }
            case PluginConfigFieldType.Double:
            {
                var nud = new NumericUpDown
                {
                    Value = current is double d ? (decimal?)(decimal)d : null,
                    Increment = 0.1m,
                };
                _readers[field.Key] = () => nud.Value is { } v ? (object)(double)v : null;
                editor = nud;
                break;
            }
            case PluginConfigFieldType.Enum:
            {
                var combo = new ComboBox();
                if (field.EnumValues is not null)
                {
                    foreach (var v in field.EnumValues) combo.Items.Add(v);
                    combo.SelectedItem = current as string ?? field.DefaultValue as string;
                }
                _readers[field.Key] = () => combo.SelectedItem as string;
                editor = combo;
                break;
            }
            default:
                editor = new TextBlock { Text = $"(unsupported field type: {field.Type})" };
                break;
        }

        container.Children.Add(editor);

        if (!string.IsNullOrWhiteSpace(field.Description))
        {
            container.Children.Add(new TextBlock
            {
                Text = field.Description,
                Classes = { "Fluent2Caption" },
                Opacity = 0.7,
            });
        }

        return container;
    }

    private Dictionary<string, object?> Collect(PluginConfigSchema schema)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in schema.Fields)
        {
            if (_readers.TryGetValue(field.Key, out var reader))
                values[field.Key] = reader();
        }
        return values;
    }
}
