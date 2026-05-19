using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SharpFM.Model;

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

    /// <summary>
    /// Folder segments leading from the repository root to this node. For
    /// folder nodes this includes the folder's own name; for clip leaves it
    /// matches the clip's <see cref="ClipViewModel.FolderPath"/>.
    /// </summary>
    public IReadOnlyList<string> Path { get; private set; } = [];

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

    public static ClipTreeNodeViewModel Folder(string name, IReadOnlyList<string> path)
    {
        return new ClipTreeNodeViewModel(name, null) { Path = path };
    }

    public static ClipTreeNodeViewModel ClipLeaf(ClipViewModel clip)
    {
        return new ClipTreeNodeViewModel(clip.Clip.Name, clip) { Path = clip.FolderPath };
    }

    /// <summary>
    /// Build a set of root-level nodes from a flat clip collection. Clips are
    /// grouped by <see cref="ClipViewModel.FolderPath"/>. Folders are sorted
    /// alphabetically and listed before clip leaves at each level; clip leaves
    /// are sorted by name for stable display.
    /// </summary>
    /// <param name="clips">Flat clip collection.</param>
    /// <param name="folders">Materialized folders (including empty ones) the
    /// tree should render even when they contain no clips.</param>
    /// <param name="searchText">Optional filter. When non-empty, only clips
    /// whose names contain the text survive (case-insensitive); folders survive
    /// when they have any surviving descendant, and matching folders are
    /// auto-expanded. Empty folders are filtered out while a filter is active.</param>
    public static IReadOnlyList<ClipTreeNodeViewModel> Build(
        IEnumerable<ClipViewModel> clips,
        string searchText = "",
        IEnumerable<FolderData>? folders = null)
    {
        var rootFolders = new Dictionary<string, ClipTreeNodeViewModel>(StringComparer.OrdinalIgnoreCase);
        var rootLeaves = new List<ClipTreeNodeViewModel>();
        var filter = string.IsNullOrEmpty(searchText) ? null : searchText;

        if (filter is null && folders is not null)
        {
            foreach (var folder in folders)
            {
                EnsureFolderPath(rootFolders, folder);
            }
        }

        foreach (var clip in clips)
        {
            var matches = filter is null ||
                clip.Clip.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);

            if (clip.FolderPath is { Count: > 0 })
            {
                var head = clip.FolderPath[0];
                if (!rootFolders.TryGetValue(head, out var folder))
                {
                    folder = Folder(head, new[] { head });
                    rootFolders.Add(head, folder);
                }

                InsertClipIntoFolder(folder, clip, clip.FolderPath, 1, matches, filter is not null);
            }
            else if (matches)
            {
                rootLeaves.Add(ClipLeaf(clip));
            }
        }

        // Filtering hides empty folders; with no filter, materialized folders
        // (created or pasted) stay visible even before they contain anything.
        var folderList = rootFolders.Values
            .Where(f => filter is null || HasAnyDescendant(f))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var f in folderList) SortRecursive(f);

        rootLeaves.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

        var result = new List<ClipTreeNodeViewModel>(folderList.Count + rootLeaves.Count);
        result.AddRange(folderList);
        result.AddRange(rootLeaves);
        return result;
    }

    private static void EnsureFolderPath(
        Dictionary<string, ClipTreeNodeViewModel> rootFolders,
        FolderData folder)
    {
        if (folder.Path.Count == 0) return;

        var head = folder.Path[0];
        if (!rootFolders.TryGetValue(head, out var node))
        {
            node = Folder(head, new[] { head });
            rootFolders.Add(head, node);
        }
        EnsureFolderChildren(node, folder.Path, depth: 1);
    }

    private static void EnsureFolderChildren(
        ClipTreeNodeViewModel parent,
        IReadOnlyList<string> path,
        int depth)
    {
        if (depth >= path.Count) return;
        var segment = path[depth];
        var child = parent.Children.FirstOrDefault(c => c.IsFolder &&
            string.Equals(c.Name, segment, StringComparison.OrdinalIgnoreCase));
        if (child is null)
        {
            child = Folder(segment, path.Take(depth + 1).ToArray());
            parent.Children.Add(child);
        }
        EnsureFolderChildren(child, path, depth + 1);
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
            child = Folder(segment, path.Take(depth + 1).ToArray());
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
