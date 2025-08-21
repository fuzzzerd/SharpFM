using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SharpFM.Services;

namespace SharpFM.Views;

public partial class ApiKeyDialog : Window
{
    public string? ApiKey { get; private set; }
    public string? SelectedModel { get; private set; }

    public ApiKeyDialog()
    {
        InitializeComponent();
        InitializeModelComboBox();
    }
    
    private void InitializeModelComboBox()
    {
        if (ModelComboBox != null)
        {
            var modelItems = ClaudeApiService.AvailableModels.Select(kvp => new ComboBoxItem
            {
                Content = kvp.Value,
                Tag = kvp.Key
            }).ToList();
            
            ModelComboBox.ItemsSource = modelItems;
            ModelComboBox.SelectedIndex = 0; // Default to first item
        }
    }

    public void SetCurrentSettings(string? apiKey, string? selectedModel)
    {
        if (ApiKeyTextBox != null && !string.IsNullOrEmpty(apiKey))
        {
            ApiKeyTextBox.Text = apiKey;
        }
        
        if (ModelComboBox != null && !string.IsNullOrEmpty(selectedModel))
        {
            var modelItem = ModelComboBox.ItemsSource?.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Tag?.ToString() == selectedModel);
            if (modelItem != null)
            {
                ModelComboBox.SelectedItem = modelItem;
            }
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        ApiKey = ApiKeyTextBox?.Text;
        
        if (ModelComboBox?.SelectedItem is ComboBoxItem selectedItem)
        {
            SelectedModel = selectedItem.Tag?.ToString();
        }
        
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}