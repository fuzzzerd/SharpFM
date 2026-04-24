using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace SharpFM.ViewModels;

/// <summary>
/// A single open clip tab. Wraps a <see cref="ClipViewModel"/> and carries
/// tab-specific state (preview vs permanent). Dirty state is read through to
/// <see cref="ClipViewModel.IsDirty"/>.
/// </summary>
public class OpenTabViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private ClipViewModel _clip;
    public ClipViewModel Clip
    {
        get => _clip;
        set
        {
            if (ReferenceEquals(_clip, value)) return;

            if (_clip is not null)
                _clip.PropertyChanged -= OnClipPropertyChanged;

            _clip = value;
            _clip.PropertyChanged += OnClipPropertyChanged;

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Title));
            NotifyPropertyChanged(nameof(IsDirty));
        }
    }

    private bool _isPreview;
    /// <summary>
    /// True when this tab was opened via single-click (VS Code "preview"). A
    /// preview tab graduates to permanent on double-click of its header or on
    /// the first edit to its clip.
    /// </summary>
    public bool IsPreview
    {
        get => _isPreview;
        set
        {
            if (_isPreview == value) return;
            _isPreview = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(TitleFontStyle));
        }
    }

    public string Title => _clip.Clip.Name;

    /// <summary>
    /// Italic while previewing, normal once graduated. Avalonia doesn't
    /// evaluate data triggers, so the VM computes the style directly.
    /// </summary>
    public FontStyle TitleFontStyle => _isPreview ? FontStyle.Italic : FontStyle.Normal;

    public bool IsDirty => _clip.IsDirty;

    private bool _isActive;
    /// <summary>
    /// Whether this tab is the currently active (visible) one. Driven by
    /// <see cref="OpenTabsViewModel.ActiveTab"/>; bound to the tab content's
    /// <c>IsVisible</c> so every realised editor stays in the visual tree
    /// and switching is a visibility toggle rather than a reparent.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        internal set { if (_isActive == value) return; _isActive = value; NotifyPropertyChanged(); }
    }

    public OpenTabViewModel(ClipViewModel clip, bool isPreview)
    {
        _clip = clip;
        _isPreview = isPreview;
        _clip.PropertyChanged += OnClipPropertyChanged;
    }

    private void OnClipPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClipViewModel.IsDirty))
            NotifyPropertyChanged(nameof(IsDirty));
    }
}
