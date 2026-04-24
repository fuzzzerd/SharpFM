using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpFM.ViewModels;

/// <summary>
/// Node in the left-panel clip tree. A node is either a folder (with children)
/// or a clip leaf (wrapping a <see cref="ClipViewModel"/>). The tree mirrors
/// the <c>FolderPath</c> hierarchy exposed by the active repository.
/// </summary>
public class ClipTreeNodeViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Display name for this node (folder segment or clip name).</summary>
    public string Name { get; }

    /// <summary>Null when this is a folder node; non-null when a clip leaf.</summary>
    public ClipViewModel? Clip { get; }

    public bool IsFolder => Clip is null;
    public bool IsClip => Clip is not null;

    public ObservableCollection<ClipTreeNodeViewModel> Children { get; } = [];

    private bool _isExpanded = true;
    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded == value) return; _isExpanded = value; NotifyPropertyChanged(); }
    }

    private ClipTreeNodeViewModel(string name, ClipViewModel? clip)
    {
        Name = name;
        Clip = clip;
    }

    public static ClipTreeNodeViewModel Folder(string name) => new(name, null);
    public static ClipTreeNodeViewModel ClipLeaf(ClipViewModel clip) => new(clip.Clip.Name, clip);

    /// <summary>
    /// Build a set of root-level nodes from a flat clip collection. Clips are
    /// grouped by <see cref="ClipViewModel.FolderPath"/>. Folders are sorted
    /// alphabetically and listed before clip leaves at each level; clip leaves
    /// are sorted by name for stable display.
    /// </summary>
    /// <param name="clips">Flat clip collection.</param>
    /// <param name="searchText">Optional filter. When non-empty, only clips
    /// whose names contain the text survive (case-insensitive); folders survive
    /// when they have any surviving descendant, and matching folders are
    /// auto-expanded.</param>
    public static IReadOnlyList<ClipTreeNodeViewModel> Build(
        IEnumerable<ClipViewModel> clips,
        string searchText = "")
    {
        var rootFolders = new Dictionary<string, ClipTreeNodeViewModel>(StringComparer.OrdinalIgnoreCase);
        var rootLeaves = new List<ClipTreeNodeViewModel>();
        var filter = string.IsNullOrEmpty(searchText) ? null : searchText;

        foreach (var clip in clips)
        {
            var matches = filter is null ||
                clip.Clip.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

            if (clip.FolderPath is { Count: > 0 })
            {
                var head = clip.FolderPath[0];
                if (!rootFolders.TryGetValue(head, out var folder))
                {
                    folder = Folder(head);
                    rootFolders.Add(head, folder);
                }

                InsertClipIntoFolder(folder, clip, clip.FolderPath, 1, matches, filter is not null);
            }
            else if (matches)
            {
                rootLeaves.Add(ClipLeaf(clip));
            }
        }

        // Drop folders that end up empty after filtering.
        var folderList = rootFolders.Values
            .Where(f => HasAnyDescendant(f))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var f in folderList) SortRecursive(f);

        rootLeaves.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

        var result = new List<ClipTreeNodeViewModel>(folderList.Count + rootLeaves.Count);
        result.AddRange(folderList);
        result.AddRange(rootLeaves);
        return result;
    }

    private static void InsertClipIntoFolder(
        ClipTreeNodeViewModel folder,
        ClipViewModel clip,
        IReadOnlyList<string> path,
        int depth,
        bool matches,
        bool filtering)
    {
        if (depth >= path.Count)
        {
            if (matches) folder.Children.Add(ClipLeaf(clip));
            if (filtering && matches) folder.IsExpanded = true;
            return;
        }

        var segment = path[depth];
        var child = folder.Children.FirstOrDefault(c => c.IsFolder &&
            string.Equals(c.Name, segment, StringComparison.OrdinalIgnoreCase));
        if (child is null)
        {
            child = Folder(segment);
            folder.Children.Add(child);
        }

        InsertClipIntoFolder(child, clip, path, depth + 1, matches, filtering);

        if (filtering && HasAnyDescendant(child))
            folder.IsExpanded = true;
    }

    private static bool HasAnyDescendant(ClipTreeNodeViewModel node)
    {
        foreach (var c in node.Children)
        {
            if (c.IsClip) return true;
            if (HasAnyDescendant(c)) return true;
        }
        return false;
    }

    private static void SortRecursive(ClipTreeNodeViewModel node)
    {
        if (!node.IsFolder || node.Children.Count == 0) return;

        var folders = node.Children.Where(c => c.IsFolder)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var leaves = node.Children.Where(c => c.IsClip)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        node.Children.Clear();
        foreach (var f in folders) { node.Children.Add(f); SortRecursive(f); }
        foreach (var l in leaves) node.Children.Add(l);
    }
}
