using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpFM.ViewModels;

/// <summary>
/// Collection of VS Code-style document tabs shown in the main editor area.
/// Implements preview/permanent semantics:
/// <list type="bullet">
/// <item>Single-click in the tree calls <see cref="OpenAsPreview"/>: reuses a
/// single "preview" tab slot if one exists, otherwise creates a new italic
/// tab.</item>
/// <item>Double-click in the tree calls <see cref="OpenAsPermanent"/>: opens
/// a non-italic, sticky tab.</item>
/// <item>Double-tapping a preview tab header, or editing its clip, graduates
/// the preview to a permanent tab (<see cref="GraduateActive"/>).</item>
/// </list>
/// </summary>
public class OpenTabsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public ObservableCollection<OpenTabViewModel> Tabs { get; } = [];

    private OpenTabViewModel? _activeTab;
    public OpenTabViewModel? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (ReferenceEquals(_activeTab, value)) return;
            if (_activeTab is not null) _activeTab.IsActive = false;
            _activeTab = value;
            if (_activeTab is not null) _activeTab.IsActive = true;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The lone preview tab, if any. A tab leaves this slot when it graduates
    /// to permanent or is closed.
    /// </summary>
    public OpenTabViewModel? PreviewTab { get; private set; }

    /// <summary>
    /// Open a clip in the preview slot. If the clip is already open (preview
    /// or permanent) its tab is activated; otherwise the preview slot is
    /// reused — or created if absent.
    /// </summary>
    public OpenTabViewModel OpenAsPreview(ClipViewModel clip)
    {
        var existing = Tabs.FirstOrDefault(t => ReferenceEquals(t.Clip, clip));
        if (existing is not null)
        {
            ActiveTab = existing;
            return existing;
        }

        if (PreviewTab is not null)
        {
            DetachGraduationWatch(PreviewTab);
            PreviewTab.Clip = clip;
            AttachGraduationWatch(PreviewTab);
            ActiveTab = PreviewTab;
            return PreviewTab;
        }

        var tab = new OpenTabViewModel(clip, isPreview: true);
        Tabs.Add(tab);
        PreviewTab = tab;
        AttachGraduationWatch(tab);
        ActiveTab = tab;
        return tab;
    }

    /// <summary>
    /// Open a clip as a permanent tab. If the clip is already in the preview
    /// slot the preview simply graduates; if it's already open as permanent,
    /// its tab is activated.
    /// </summary>
    public OpenTabViewModel OpenAsPermanent(ClipViewModel clip)
    {
        var existing = Tabs.FirstOrDefault(t => ReferenceEquals(t.Clip, clip));
        if (existing is not null)
        {
            if (existing.IsPreview) Graduate(existing);
            ActiveTab = existing;
            return existing;
        }

        var tab = new OpenTabViewModel(clip, isPreview: false);
        Tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    /// <summary>
    /// Graduate a preview tab to permanent. No-op if the tab is not a preview.
    /// </summary>
    public void Graduate(OpenTabViewModel tab)
    {
        if (!tab.IsPreview) return;
        DetachGraduationWatch(tab);
        tab.IsPreview = false;
        if (ReferenceEquals(PreviewTab, tab)) PreviewTab = null;
    }

    /// <summary>
    /// Convenience: graduate the currently active tab if it's a preview.
    /// Called when the user double-taps the active tab header.
    /// </summary>
    public void GraduateActive()
    {
        if (_activeTab is not null) Graduate(_activeTab);
    }

    /// <summary>
    /// Close a tab. If it was the active tab, picks the neighbour (next, else
    /// previous) as the new active; falls back to null when the last tab is
    /// closed. Clears the preview slot if the closed tab held it.
    /// </summary>
    public void Close(OpenTabViewModel tab)
    {
        var idx = Tabs.IndexOf(tab);
        if (idx < 0) return;

        DetachGraduationWatch(tab);
        Tabs.RemoveAt(idx);
        if (ReferenceEquals(PreviewTab, tab)) PreviewTab = null;

        if (ReferenceEquals(_activeTab, tab))
        {
            if (Tabs.Count == 0) ActiveTab = null;
            else if (idx < Tabs.Count) ActiveTab = Tabs[idx];
            else ActiveTab = Tabs[Tabs.Count - 1];
        }
    }

    /// <summary>
    /// Remove every open tab that points at <paramref name="clip"/>. Used when
    /// a clip is deleted from the repository so it doesn't linger in the
    /// editor area.
    /// </summary>
    public void CloseClip(ClipViewModel clip)
    {
        foreach (var tab in Tabs.Where(t => ReferenceEquals(t.Clip, clip)).ToList())
            Close(tab);
    }

    private void AttachGraduationWatch(OpenTabViewModel tab)
    {
        tab.Clip.PropertyChanged += OnWatchedClipChanged;
    }

    private void DetachGraduationWatch(OpenTabViewModel tab)
    {
        tab.Clip.PropertyChanged -= OnWatchedClipChanged;
    }

    private void OnWatchedClipChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The first user edit graduates a preview tab — VS Code parity.
        if (e.PropertyName != nameof(ClipViewModel.IsDirty)) return;
        if (sender is not ClipViewModel clip) return;
        if (!clip.IsDirty) return;

        var tab = Tabs.FirstOrDefault(t => ReferenceEquals(t.Clip, clip));
        if (tab is not null && tab.IsPreview) Graduate(tab);
    }
}
