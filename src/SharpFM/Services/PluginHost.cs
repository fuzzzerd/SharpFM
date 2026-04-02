using System;
using System.Collections.Generic;
using System.Linq;
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
    private ClipViewModel? _trackedClip;

    public PluginHost(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(MainWindowViewModel.SelectedClip)) return;

            // Sync outgoing clip before switching
            _trackedClip?.SyncModelFromEditor();

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

    public ClipInfo? SelectedClip
    {
        get
        {
            var clip = _viewModel.SelectedClip;
            if (clip is null) return null;
            return new ClipInfo(clip.Name, clip.ClipType, clip.ClipXml);
        }
    }

    public event EventHandler<ClipInfo?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public event EventHandler? ClipCollectionChanged;

    public IReadOnlyList<ClipInfo> AllClips =>
        _viewModel.FileMakerClips
            .Select(c => new ClipInfo(c.Name, c.ClipType, c.ClipXml))
            .ToList();

    public void ShowStatus(string message) => _viewModel.StatusMessage = message;

    public void UpdateSelectedClipXml(string xml, string originPluginId)
    {
        var clip = _viewModel.SelectedClip;
        if (clip is null) return;

        clip.ClipXml = xml;
        clip.SyncEditorFromXml(); // bumps generation counter — debounce will discard stale tick

        var info = new ClipInfo(clip.Name, clip.ClipType, clip.ClipXml);
        ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, originPluginId, false));
    }

    public ClipInfo? RefreshSelectedClip()
    {
        var clip = _viewModel.SelectedClip;
        if (clip is null) return null;
        clip.SyncModelFromEditor();
        return new ClipInfo(clip.Name, clip.ClipType, clip.ClipXml);
    }

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

    private void OnEditorContentChanged(object? sender, EventArgs e)
    {
        var clip = _viewModel.SelectedClip;
        if (clip is null) return;

        var info = new ClipInfo(clip.Name, clip.ClipType, clip.ClipXml);
        var isPartial = clip.Editor.IsPartial;
        ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, "editor", isPartial));
    }
}
