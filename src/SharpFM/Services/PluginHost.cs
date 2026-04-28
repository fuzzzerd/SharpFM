using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SharpFM.Model;
using SharpFM.Model.ClipTypes;
using SharpFM.Model.Schema;
using SharpFM.Model.Scripting;
using SharpFM.Plugin;
using SharpFM.ViewModels;

namespace SharpFM.Services;

/// <summary>
/// Bridges the host application's MainWindowViewModel to the <see cref="IPluginHost"/> interface.
/// Clip-type-agnostic — all change detection is handled by <see cref="Editors.IClipEditor"/>
/// implementations via <see cref="ClipViewModel.EditorContentChanged"/>.
/// </summary>
public class PluginHost : IPluginHost
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ILoggerFactory _loggerFactory;
    private ClipViewModel? _trackedClip;
    private readonly List<IClipRepository> _repositories = [];
    private readonly List<IClipTransform> _transforms = [];

    /// <summary>Repositories registered by plugins.</summary>
    public IReadOnlyList<IClipRepository> Repositories => _repositories;

    /// <summary>Transforms registered by plugins.</summary>
    public IReadOnlyList<IClipTransform> Transforms => _transforms;

    public PluginHost(MainWindowViewModel viewModel, ILoggerFactory loggerFactory)
    {
        _viewModel = viewModel;
        _loggerFactory = loggerFactory;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(MainWindowViewModel.SelectedClip)) return;

            Unsubscribe(_trackedClip);
            _trackedClip = _viewModel.SelectedClip;
            Subscribe(_trackedClip);

            SelectedClipChanged?.Invoke(this, SelectedClip);
        };

        _trackedClip = _viewModel.SelectedClip;
        Subscribe(_trackedClip);

        _viewModel.FileMakerClips.CollectionChanged += (_, _) =>
            ClipCollectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public ClipData? SelectedClip
    {
        get
        {
            var clip = _viewModel.SelectedClip;
            if (clip is null) return null;
            return new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.Xml);
        }
    }

    public event EventHandler<ClipData?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public event EventHandler? ClipCollectionChanged;

    public IReadOnlyList<ClipData> AllClips =>
        _viewModel.FileMakerClips
            .Select(c => new ClipData(c.Clip.Name, c.ClipType, c.Clip.Xml))
            .ToList();

    public ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);

    public void ShowStatus(string message) =>
        EnsureUiThread(() => _viewModel.StatusMessage = message);

    public void UpdateSelectedClipXml(string xml, string originPluginId) =>
        EnsureUiThread(() =>
        {
            var clip = _viewModel.SelectedClip;
            if (clip is null) return;

            clip.Replace(xml);

            var info = new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.Xml);
            ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, originPluginId, false));
        });

    public ClipData? GetClip(string clipName)
    {
        var clip = FindClipByName(clipName);
        if (clip is null) return null;
        return new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.Xml);
    }

    public void UpdateClipXml(string clipName, string xml, string originPluginId) =>
        EnsureUiThread(() =>
        {
            var clip = FindClipByName(clipName);
            if (clip is null) return;

            clip.Replace(xml);

            var info = new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.Xml);
            ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, originPluginId, false));
        });

    public void CreateClip(string name, string clipType, string? xml = null)
    {
        if (!ClipTypeRegistry.IsRegistered(clipType))
        {
            throw new ArgumentException(
                $"Unknown clip type '{clipType}'. Valid types: " +
                $"{string.Join(", ", ClipTypeRegistry.All.Select(s => s.FormatId))}.",
                nameof(clipType));
        }

        EnsureUiThread(() =>
        {
            var seed = xml ?? ClipTypeRegistry.For(clipType).DefaultXml(name);
            var vm = new ClipViewModel(Clip.FromXml(name, clipType, seed));
            _viewModel.FileMakerClips.Add(vm);
        });
    }

    public bool RemoveClip(string clipName) =>
        EnsureUiThread(() =>
        {
            var clip = FindClipByName(clipName);
            if (clip is null) return false;
            _viewModel.FileMakerClips.Remove(clip);
            return true;
        });

    public void RegisterRepository(IClipRepository repository) => _repositories.Add(repository);
    public void RegisterTransform(IClipTransform transform) => _transforms.Add(transform);

    public Task<string?> ShowDialogAsync(string title, string message, string[] buttons)
    {
        // TODO: implement with Avalonia dialog
        ShowStatus(message);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> ShowInputDialogAsync(string title, string prompt, string? defaultValue = null)
    {
        // TODO: implement with Avalonia dialog
        ShowStatus(prompt);
        return Task.FromResult(defaultValue);
    }

    private ClipViewModel? FindClipByName(string clipName) =>
        _viewModel.FileMakerClips.FirstOrDefault(c =>
            c.Clip.Name.Equals(clipName, StringComparison.OrdinalIgnoreCase));

    private void Subscribe(ClipViewModel? clip)
    {
        if (clip is not null)
            clip.EditorContentChanged += OnEditorContentChanged;
    }

    private void Unsubscribe(ClipViewModel? clip)
    {
        if (clip is not null)
            clip.EditorContentChanged -= OnEditorContentChanged;
    }

    private static void EnsureUiThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    private static T EnsureUiThread<T>(Func<T> func)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return func();
        return Dispatcher.UIThread.InvokeAsync(func).GetAwaiter().GetResult();
    }

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        var clip = _viewModel.SelectedClip;
        if (clip is null) return;

        var info = new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.Xml);
        var isPartial = clip.Editor.IsPartial;
        ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, "editor", isPartial));
    }
}
