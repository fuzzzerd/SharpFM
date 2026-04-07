using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SharpFM.Model;
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
            return new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.XmlData);
        }
    }

    public event EventHandler<ClipData?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public event EventHandler? ClipCollectionChanged;

    public IReadOnlyList<ClipData> AllClips =>
        _viewModel.FileMakerClips
            .Select(c => new ClipData(c.Clip.Name, c.ClipType, c.Clip.XmlData))
            .ToList();

    public ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);

    public void ShowStatus(string message) =>
        EnsureUiThread(() => _viewModel.StatusMessage = message);

    public void UpdateSelectedClipXml(string xml, string originPluginId) =>
        EnsureUiThread(() =>
        {
            var clip = _viewModel.SelectedClip;
            if (clip is null) return;

            clip.ReplaceEditor(xml);

            var info = new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.XmlData);
            ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, originPluginId, false));
        });

    public ClipData? GetClip(string clipName)
    {
        var clip = FindClipByName(clipName);
        if (clip is null) return null;
        // Auto-sync keeps ClipXml current — just return it
        return new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.XmlData);
    }

    public void UpdateClipXml(string clipName, string xml, string originPluginId) =>
        EnsureUiThread(() =>
        {
            var clip = FindClipByName(clipName);
            if (clip is null) return;

            // Wholesale replacement — re-ingest the XML
            clip.ReplaceEditor(xml);

            var info = new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.XmlData);
            ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, originPluginId, false));
        });

    private static readonly HashSet<string> KnownClipTypes = new(StringComparer.Ordinal)
    {
        "Mac-XMSS", // script steps
        "Mac-XMSC", // script
        "Mac-XMTB", // table
        "Mac-XMFD", // field
        "Mac-XML2", // layout
    };

    public void CreateClip(string name, string clipType, string? xml = null)
    {
        if (!KnownClipTypes.Contains(clipType))
            throw new ArgumentException(
                $"Unknown clip type '{clipType}'. Valid types: {string.Join(", ", KnownClipTypes)}.",
                nameof(clipType));

        EnsureUiThread(() =>
        {
            xml ??= clipType switch
            {
                "Mac-XMSS" or "Mac-XMSC" => "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>",
                "Mac-XMTB" => $"<fmxmlsnippet type=\"FMObjectList\"><BaseTable name=\"{name}\"></BaseTable></fmxmlsnippet>",
                _ => "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>",
            };

            var clip = new FileMakerClip(name, clipType, xml);
            var vm = new ClipViewModel(clip);
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

        var info = new ClipData(clip.Clip.Name, clip.ClipType, clip.Clip.XmlData);
        var isPartial = clip.Editor.IsPartial;
        ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, "editor", isPartial));
    }
}
