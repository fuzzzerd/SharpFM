using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;

namespace SharpFM.Diagnostics;

/// <summary>
/// Modal dialog for editing a single script step's raw XML in isolation.
/// Used as the only path to modify a sealed step — the main script
/// editor rejects direct edits to sealed lines, funnelling the user
/// here instead. The dialog validates that the user's edit parses as
/// XML before returning; an unparseable edit keeps the dialog open
/// with a status message.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class RawStepEditorWindow : Window
{
    private readonly TextEditor _editor;
    private readonly TextBlock _statusLabel;

    /// <summary>Result of the dialog — non-null when the user saved a parseable edit.</summary>
    public XElement? Result { get; private set; }

    public RawStepEditorWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _editor = this.FindControl<TextEditor>("xmlEditor")!;
        _statusLabel = this.FindControl<TextBlock>("statusLabel")!;

        this.FindControl<Button>("saveButton")!.Click += (_, _) => OnSave();
        this.FindControl<Button>("cancelButton")!.Click += (_, _) => Close();
    }

    public void LoadXml(XElement initial)
    {
        _editor.Text = initial.ToString();
    }

    private void OnSave()
    {
        var text = _editor.Text ?? string.Empty;
        try
        {
            Result = XElement.Parse(text);
            Close();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"XML parse error: {ex.Message}";
        }
    }

    /// <summary>
    /// Show the dialog modally and return the edited XElement, or null
    /// if the user cancelled.
    /// </summary>
    public static async Task<XElement?> EditAsync(Window owner, XElement initial)
    {
        var window = new RawStepEditorWindow();
        window.LoadXml(initial);
        await window.ShowDialog(owner);
        return window.Result;
    }
}
