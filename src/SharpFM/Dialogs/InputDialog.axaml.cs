using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpFM.Dialogs;

/// <summary>
/// Single-line text-input modal. The host application's production
/// <see cref="IInputPrompt"/> opens this dialog over the main window; tests
/// use a fake prompt and never construct this class.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class InputDialog : Window
{
    private readonly TextBlock _promptLabel;
    private readonly TextBox _inputBox;

    public string? Result { get; private set; }

    public InputDialog()
    {
        AvaloniaXamlLoader.Load(this);
        _promptLabel = this.FindControl<TextBlock>("promptLabel")!;
        _inputBox = this.FindControl<TextBox>("inputBox")!;

        this.FindControl<Button>("okButton")!.Click += (_, _) =>
        {
            Result = _inputBox.Text ?? string.Empty;
            Close();
        };
        this.FindControl<Button>("cancelButton")!.Click += (_, _) => Close();
    }

    public void Configure(string title, string prompt, string defaultValue)
    {
        Title = title;
        _promptLabel.Text = prompt;
        _inputBox.Text = defaultValue;
        _inputBox.SelectAll();
        _inputBox.Focus();
    }

    public static async Task<string?> PromptAsync(
        Window owner,
        string title,
        string prompt,
        string defaultValue)
    {
        var window = new InputDialog();
        window.Configure(title, prompt, defaultValue);
        await window.ShowDialog(owner);
        return window.Result;
    }
}
