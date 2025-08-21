using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using SharpFM.ViewModels;

namespace SharpFM.Services;

public class ClaudeApiService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";
    private string _selectedModel = "claude-3-5-sonnet-20241022";

    public static readonly Dictionary<string, string> AvailableModels = new()
    {
        { "claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet (Latest)" },
        { "claude-3-5-haiku-20241022", "Claude 3.5 Haiku (Fast & Affordable)" },
        { "claude-3-opus-20240229", "Claude 3 Opus (Most Capable)" },
        { "claude-3-sonnet-20240229", "Claude 3 Sonnet (Balanced)" },
        { "claude-3-haiku-20240307", "Claude 3 Haiku (Fast)" }
    };

    public ClaudeApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);

        // Get version dynamically from assembly
        var assembly = Assembly.GetExecutingAssembly();
        var version = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion ?? "1.0";
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"SharpFM/{version}");
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
    }

    public void SetModel(string model)
    {
        if (AvailableModels.ContainsKey(model))
        {
            _selectedModel = model;
            Logger.Debug($"Model changed to: {model}");
        }
    }

    public string GetCurrentModel() => _selectedModel;

    public async Task<string> SendMessageAsync(string message, List<ChatMessage>? conversationHistory = null, int? maxTokens = null)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("API key not set. Please configure your Claude API key.");
        }

        try
        {
            // Add a small delay to prevent rapid successive requests
            var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
            if (timeSinceLastRequest < TimeSpan.FromSeconds(1))
            {
                await Task.Delay(1000 - (int)timeSinceLastRequest.TotalMilliseconds);
            }
            _lastRequestTime = DateTime.Now;

            var messages = new List<object>();

            if (conversationHistory != null)
            {
                // Limit conversation history to last 10 messages to avoid hitting token limits
                var recentHistory = conversationHistory.TakeLast(10);
                foreach (var msg in recentHistory)
                {
                    messages.Add(new
                    {
                        role = msg.IsUser ? "user" : "assistant",
                        content = msg.Content
                    });
                }
            }

            messages.Add(new
            {
                role = "user",
                content = message
            });

            var requestBody = new
            {
                model = _selectedModel,
                max_tokens = maxTokens ?? 8192,
                messages = messages
            };

            Logger.Debug($"Sending {messages.Count} messages to Claude API");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Logger.Debug($"Request payload size: {json.Length} characters");
            var response = await _httpClient.PostAsync(ApiBaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.Error($"Claude API error: {response.StatusCode} - {errorContent}");

                // Handle specific error types
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new HttpRequestException("Rate limit exceeded. Please wait a moment before sending another message.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new HttpRequestException("Invalid API key. Please check your Claude API key configuration.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException($"Bad request: {errorContent}");
                }

                throw new HttpRequestException($"Claude API error ({response.StatusCode}): {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("content", out var contentArray) &&
                contentArray.GetArrayLength() > 0)
            {
                var firstContent = contentArray[0];
                if (firstContent.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString() ?? "No response content";
                }
            }

            return "Unexpected response format";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error sending message to Claude API");
            throw;
        }
    }
}

