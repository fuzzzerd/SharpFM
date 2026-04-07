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
            return new ClipData(clip.Name, clip.ClipType, clip.ClipXml);
        }
    }

    public event EventHandler<ClipData?>? SelectedClipChanged;
    public event EventHandler<ClipContentChangedArgs>? ClipContentChanged;
    public event EventHandler? ClipCollectionChanged;

    public IReadOnlyList<ClipData> AllClips =>
        _viewModel.FileMakerClips
            .Select(c => new ClipData(c.Name, c.ClipType, c.ClipXml))
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

            var info = new ClipData(clip.Name, clip.ClipType, clip.ClipXml);
            ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, originPluginId, false));
        });

    public ClipData? RefreshSelectedClip()
    {
        var clip = _viewModel.SelectedClip;
        if (clip is null) return null;
        // Auto-sync keeps ClipXml current — just return it
        return new ClipData(clip.Name, clip.ClipType, clip.ClipXml);
    }

    public ClipData? GetClip(string clipName)
    {
        var clip = FindClipByName(clipName);
        if (clip is null) return null;
        // Auto-sync keeps ClipXml current — just return it
        return new ClipData(clip.Name, clip.ClipType, clip.ClipXml);
    }

    public void UpdateClipXml(string clipName, string xml, string originPluginId) =>
        EnsureUiThread(() =>
        {
            var clip = FindClipByName(clipName);
            if (clip is null) return;

            // Wholesale replacement — re-ingest the XML
            clip.ReplaceEditor(xml);

            var info = new ClipData(clip.Name, clip.ClipType, clip.ClipXml);
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

    // --- Step catalog ---

    public IReadOnlyList<StepDefinition> GetAvailableSteps(string? category = null)
    {
        var steps = StepCatalogLoader.All;
        if (category is not null)
            steps = steps.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        return steps.ToList();
    }

    public StepDefinition? GetStepDefinition(string stepName)
    {
        if (!StepCatalogLoader.ByName.TryGetValue(stepName, out var def))
            return null;
        return def;
    }

    // --- Script domain operations ---

    private static bool IsScriptClip(string clipType) => clipType is "Mac-XMSS" or "Mac-XMSC";
    private static bool IsTableClip(string clipType) => clipType is "Mac-XMTB" or "Mac-XMFD";

    public IReadOnlyList<ScriptStep>? GetScriptSteps(string clipName)
    {
        var clip = GetClip(clipName);
        if (clip is null || !IsScriptClip(clip.ClipType)) return null;

        var script = FmScript.FromXml(clip.Xml);
        return script.Steps.ToList();
    }

    public IReadOnlyList<string> UpdateScriptSteps(string clipName, IReadOnlyList<ScriptStepOperation> operations, string originPluginId)
    {
        var clip = GetClip(clipName);
        if (clip is null) return [$"Clip '{clipName}' not found."];
        if (!IsScriptClip(clip.ClipType)) return [$"Clip '{clipName}' is not a script (type: {clip.ClipType})."];

        var script = FmScript.FromXml(clip.Xml);
        var errors = new List<string>();

        foreach (var op in operations)
        {
            var result = op.Action.ToLowerInvariant() switch
            {
                "add" => ApplyAddStep(script, op),
                "update" => ApplyUpdateStep(script, op),
                "remove" => ApplyRemoveStep(script, op),
                "move" => ApplyMoveStep(script, op),
                _ => [$"Unknown action '{op.Action}'."],
            };
            errors.AddRange(result);
        }

        if (errors.Count == 0)
            UpdateClipXml(clipName, script.ToXml(), originPluginId);

        return errors;
    }

    private static List<string> ApplyAddStep(FmScript script, ScriptStepOperation op)
    {
        if (op.StepName is null) return ["StepName is required for add operations."];
        if (!StepCatalogLoader.ByName.TryGetValue(op.StepName, out var definition))
            return [$"Unknown step name '{op.StepName}'."];

        var paramValues = definition.Params.Select(p =>
        {
            var paramName = p.HrLabel ?? p.WrapperElement ?? p.XmlElement;
            string? value = null;
            op.Params?.TryGetValue(paramName, out value);
            return new StepParamValue(p, value);
        }).ToList();

        var step = new ScriptStep(definition, op.Enabled ?? true, paramValues);
        var index = op.Index < 0 || op.Index >= script.Steps.Count ? script.Steps.Count : op.Index;
        script.Steps.Insert(index, step);
        return [];
    }

    private static List<string> ApplyUpdateStep(FmScript script, ScriptStepOperation op)
    {
        if (op.Index < 0 || op.Index >= script.Steps.Count)
            return [$"Step index {op.Index} out of range (0-{script.Steps.Count - 1})."];

        var step = script.Steps[op.Index];
        if (op.Enabled is not null) step.Enabled = op.Enabled.Value;

        if (op.Params is not null)
        {
            foreach (var (name, value) in op.Params)
            {
                var param = step.ParamValues.FirstOrDefault(p =>
                {
                    var paramName = p.Definition.HrLabel ?? p.Definition.WrapperElement ?? p.Definition.XmlElement;
                    return paramName.Equals(name, StringComparison.OrdinalIgnoreCase);
                });
                if (param is not null) param.Value = value;
                else return [$"Parameter '{name}' not found on step '{step.Definition?.Name ?? "unknown"}'."];
            }

        }
        return [];
    }

    private static List<string> ApplyRemoveStep(FmScript script, ScriptStepOperation op)
    {
        if (op.Index < 0 || op.Index >= script.Steps.Count)
            return [$"Step index {op.Index} out of range (0-{script.Steps.Count - 1})."];
        script.Steps.RemoveAt(op.Index);
        return [];
    }

    private static List<string> ApplyMoveStep(FmScript script, ScriptStepOperation op)
    {
        if (op.Index < 0 || op.Index >= script.Steps.Count)
            return [$"Step index {op.Index} out of range (0-{script.Steps.Count - 1})."];
        if (op.MoveToIndex is null) return ["MoveToIndex is required for move operations."];
        var dest = op.MoveToIndex.Value;
        if (dest < 0 || dest >= script.Steps.Count)
            return [$"MoveToIndex {dest} out of range (0-{script.Steps.Count - 1})."];

        var step = script.Steps[op.Index];
        script.Steps.RemoveAt(op.Index);
        script.Steps.Insert(dest, step);
        return [];
    }

    // --- Table domain operations ---

    public IReadOnlyList<FmField>? GetTableFields(string clipName)
    {
        var clip = GetClip(clipName);
        if (clip is null || !IsTableClip(clip.ClipType)) return null;

        var table = FmTable.FromXml(clip.Xml);
        return table.Fields;
    }

    public IReadOnlyList<string> UpdateTableFields(string clipName, IReadOnlyList<FieldOperation> operations, string originPluginId)
    {
        var clip = GetClip(clipName);
        if (clip is null) return [$"Clip '{clipName}' not found."];
        if (!IsTableClip(clip.ClipType)) return [$"Clip '{clipName}' is not a table (type: {clip.ClipType})."];

        var table = FmTable.FromXml(clip.Xml);
        var errors = new List<string>();

        foreach (var op in operations)
        {
            var result = op.Action.ToLowerInvariant() switch
            {
                "add" => ApplyAddField(table, op),
                "modify" => ApplyModifyField(table, op),
                "remove" => ApplyRemoveField(table, op),
                _ => [$"Unknown action '{op.Action}' for field '{op.FieldName}'."],
            };
            errors.AddRange(result);
        }

        if (errors.Count == 0)
            UpdateClipXml(clipName, table.ToXml(), originPluginId);

        return errors;
    }

    private static List<string> ApplyAddField(FmTable table, FieldOperation op)
    {
        if (table.Fields.Any(f => f.Name.Equals(op.FieldName, StringComparison.OrdinalIgnoreCase)))
            return [$"Field '{op.FieldName}' already exists."];

        var field = new FmField { Name = op.FieldName };
        if (op.DataType is not null && Enum.TryParse<FieldDataType>(op.DataType, ignoreCase: true, out var dt)) field.DataType = dt;
        if (op.Kind is not null && Enum.TryParse<FieldKind>(op.Kind, ignoreCase: true, out var kind)) field.Kind = kind;
        if (op.Comment is not null) field.Comment = op.Comment;
        if (op.Calculation is not null) field.Calculation = op.Calculation;
        if (op.IsGlobal is not null) field.IsGlobal = op.IsGlobal.Value;
        if (op.Repetitions is not null) field.Repetitions = op.Repetitions.Value;

        table.AddField(field);
        return [];
    }

    private static List<string> ApplyModifyField(FmTable table, FieldOperation op)
    {
        var field = table.Fields.FirstOrDefault(f => f.Name.Equals(op.FieldName, StringComparison.OrdinalIgnoreCase));
        if (field is null) return [$"Field '{op.FieldName}' not found."];

        if (op.NewName is not null) field.Name = op.NewName;
        if (op.DataType is not null && Enum.TryParse<FieldDataType>(op.DataType, ignoreCase: true, out var dt)) field.DataType = dt;
        if (op.Kind is not null && Enum.TryParse<FieldKind>(op.Kind, ignoreCase: true, out var kind)) field.Kind = kind;
        if (op.Comment is not null) field.Comment = op.Comment;
        if (op.Calculation is not null) field.Calculation = op.Calculation;
        if (op.IsGlobal is not null) field.IsGlobal = op.IsGlobal.Value;
        if (op.Repetitions is not null) field.Repetitions = op.Repetitions.Value;

        return [];
    }

    private static List<string> ApplyRemoveField(FmTable table, FieldOperation op)
    {
        var field = table.Fields.FirstOrDefault(f => f.Name.Equals(op.FieldName, StringComparison.OrdinalIgnoreCase));
        if (field is null) return [$"Field '{op.FieldName}' not found."];
        table.RemoveField(field);
        return [];
    }

    private ClipViewModel? FindClipByName(string clipName) =>
        _viewModel.FileMakerClips.FirstOrDefault(c =>
            c.Name.Equals(clipName, StringComparison.OrdinalIgnoreCase));

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

        var info = new ClipData(clip.Name, clip.ClipType, clip.ClipXml);
        var isPartial = clip.Editor.IsPartial;
        ClipContentChanged?.Invoke(this, new ClipContentChangedArgs(info, "editor", isPartial));
    }
}
