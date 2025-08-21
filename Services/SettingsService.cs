using System;
using System.IO;
using System.Text.Json;
using NLog;

namespace SharpFM.Services;

public class SettingsService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly string _settingsPath;
    private Settings _settings = new();

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "SharpFM");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
        
        LoadSettings();
    }

    public string? ApiKey
    {
        get => _settings.ApiKey;
        set
        {
            _settings.ApiKey = value;
            SaveSettings();
        }
    }
    
    public string SelectedModel
    {
        get => _settings.SelectedModel ?? "claude-3-5-sonnet-20241022";
        set
        {
            _settings.SelectedModel = value;
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
            else
            {
                _settings = new Settings();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error loading settings");
            _settings = new Settings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error saving settings");
        }
    }

    private class Settings
    {
        public string? ApiKey { get; set; }
        public string? SelectedModel { get; set; }
    }
}