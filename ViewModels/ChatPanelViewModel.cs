using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SharpFM.Services;

namespace SharpFM.ViewModels;

public class ChatPanelViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly ClaudeApiService _claudeApiService;
    private ClipViewModel? _selectedClip;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ChatPanelViewModel()
    {
        Messages = new ObservableCollection<ChatMessage>();
        _claudeApiService = new ClaudeApiService();
    }

    public void SetSelectedClip(ClipViewModel? clip)
    {
        _selectedClip = clip;
        NotifyPropertyChanged(nameof(HasSelectedClip));
    }

    public bool HasSelectedClip => _selectedClip != null;

    public string CurrentModelInfo => $"Model: {_claudeApiService.GetCurrentModel().Split('-').LastOrDefault()?.ToUpper() ?? "Unknown"}";

    public ObservableCollection<ChatMessage> Messages { get; }

    private string _currentMessage = string.Empty;
    public string CurrentMessage
    {
        get => _currentMessage;
        set
        {
            _currentMessage = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isVisible = false;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isWaitingForResponse = false;
    public bool IsWaitingForResponse
    {
        get => _isWaitingForResponse;
        set
        {
            _isWaitingForResponse = value;
            NotifyPropertyChanged();
        }
    }

    public void SetApiKey(string apiKey)
    {
        _claudeApiService.SetApiKey(apiKey);
    }

    public void SetModel(string model)
    {
        _claudeApiService.SetModel(model);
        NotifyPropertyChanged(nameof(CurrentModelInfo));
    }

    public async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage) || IsWaitingForResponse)
            return;

        var userMessage = CurrentMessage;
        Messages.Add(new ChatMessage
        {
            Content = userMessage,
            IsUser = true
        });

        CurrentMessage = string.Empty;
        IsWaitingForResponse = true;

        try
        {
            var conversationHistory = Messages.Take(Messages.Count - 1).ToList();

            // Build context message with selected clip if available
            var contextualMessage = userMessage;
            if (_selectedClip != null)
            {
                var clipContext = $"Current FileMaker clip context:\n" +
                                $"Name: {_selectedClip.Name}\n" +
                                $"Type: {_selectedClip.ClipType}\n" +
                                $"XML Content:\n{_selectedClip.ClipXml}\n\n" +
                                $"User message: {userMessage}\n\n" +
                                $"Please help with this FileMaker clip, or snippet, or object. " +
                                $"If you make changes to the XML, wrap the updated XML in <CLIP_UPDATE> tags so I can extract and apply the changes.";
                contextualMessage = clipContext;
            }

            var response = await _claudeApiService.SendMessageAsync(contextualMessage, conversationHistory);

            // Check if response contains clip updates
            var (displayText, updatedXml) = ExtractClipUpdate(response);

            // Apply clip updates if found
            if (!string.IsNullOrEmpty(updatedXml) && _selectedClip != null)
            {
                _selectedClip.ClipXml = updatedXml;
            }

            Messages.Add(new ChatMessage
            {
                Content = displayText,
                IsUser = false
            });
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Content = $"Error: {ex.Message}",
                IsUser = false
            });
        }
        finally
        {
            IsWaitingForResponse = false;
        }
    }

    private static (string displayText, string? updatedXml) ExtractClipUpdate(string response)
    {
        var clipUpdatePattern = @"<CLIP_UPDATE>(.*?)</CLIP_UPDATE>";
        var match = System.Text.RegularExpressions.Regex.Match(response, clipUpdatePattern, System.Text.RegularExpressions.RegexOptions.Singleline);

        if (match.Success)
        {
            var updatedXml = match.Groups[1].Value.Trim();
            var displayText = response.Replace(match.Value, "[Clip updated with changes]").Trim();
            return (displayText, updatedXml);
        }

        return (response, null);
    }

    public void ToggleVisibility()
    {
        IsVisible = !IsVisible;
    }
}

public class ChatMessage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string _content = string.Empty;
    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isUser;
    public bool IsUser
    {
        get => _isUser;
        set
        {
            _isUser = value;
            NotifyPropertyChanged();
        }
    }
}