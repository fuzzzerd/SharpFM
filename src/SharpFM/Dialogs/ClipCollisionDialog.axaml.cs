using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SharpFM.Dialogs;

/// <summary>
/// Modal that asks the user how to resolve a paste collision (same name,
/// different XML). Production <see cref="IClipCollisionPrompt"/> opens this
/// over the main window; tests use a fake prompt and never construct this.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class ClipCollisionDialog : Window
{
    private readonly TextBlock _messageLabel;
    private readonly TextBlock _locationLabel;
    private readonly CheckBox _applyToAllBox;

    public ClipCollisionDecision Result { get; private set; } =
        new(ClipCollisionChoice.Cancel, ApplyToAll: false);

    public ClipCollisionDialog()
    {
        AvaloniaXamlLoader.Load(this);
        _messageLabel = this.FindControl<TextBlock>("messageLabel")!;
        _locationLabel = this.FindControl<TextBlock>("locationLabel")!;
        _applyToAllBox = this.FindControl<CheckBox>("applyToAllBox")!;

        this.FindControl<Button>("replaceButton")!.Click += (_, _) => Finish(ClipCollisionChoice.Replace);
        this.FindControl<Button>("keepBothButton")!.Click += (_, _) => Finish(ClipCollisionChoice.KeepBoth);
        this.FindControl<Button>("cancelButton")!.Click += (_, _) => Finish(ClipCollisionChoice.Cancel);
    }

    private void Finish(ClipCollisionChoice choice)
    {
        Result = new ClipCollisionDecision(choice, _applyToAllBox.IsChecked == true);
        Close();
    }

    public void Configure(string clipName, IReadOnlyList<string> folderPath)
    {
        _messageLabel.Text = $"A clip named \"{clipName}\" already exists with different content.";
        _locationLabel.Text = folderPath.Count == 0
            ? "Location: (root)"
            : $"Location: {string.Join(" / ", folderPath)}";
    }

    public static async Task<ClipCollisionDecision> PromptAsync(
        Window owner,
        string clipName,
        IReadOnlyList<string> folderPath)
    {
        var window = new ClipCollisionDialog();
        window.Configure(clipName, folderPath);
        await window.ShowDialog(owner);
        return window.Result;
    }
}
