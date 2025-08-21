using Avalonia.Controls;
using Avalonia.Input;
using SharpFM.ViewModels;

namespace SharpFM.Views;

public partial class ChatPanel : UserControl
{
    public ChatPanel()
    {
        InitializeComponent();
    }

    private async void OnMessageTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ChatPanelViewModel viewModel)
        {
            await viewModel.SendMessage();
            e.Handled = true;
        }
    }
}