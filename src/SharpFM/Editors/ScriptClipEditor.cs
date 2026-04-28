using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AvaloniaEdit.Document;
using SharpFM.Model.Scripting;
using SharpFM.Model.Scripting.Steps;
using SharpFM.Scripting;

namespace SharpFM.Editors;

/// <summary>
/// Editor for script clips (Mac-XMSS, Mac-XMSC). Wraps a TextDocument containing the
/// plain-text script representation and handles FmScript model round-tripping.
/// <para>
/// Script wrapper metadata (Mac-XMSC only) is cached here because display
/// text carries only steps, not wrapper attrs. On <see cref="ToXml"/>, the
/// cached metadata is re-applied so the emitted XML keeps its <c>&lt;Script&gt;</c>
/// envelope across display-text edits.
/// </para>
/// <para>
/// Sealed (non-POCO, non-allow-list) steps are preserved across display-text
/// edits via an anchor cache: each sealed step's original <see cref="XElement"/>
/// is stashed against a <see cref="TextAnchor"/> at the step's first line.
/// On <see cref="ToXml"/>, logical step ranges that cover a sealed anchor
/// bypass display-text parsing and reuse the cached XML verbatim — no
/// fidelity loss even when the catalog's display format can't reproduce
/// every XML detail.
/// </para>
/// </summary>
public class ScriptClipEditor : IClipEditor
{
    private readonly DebouncedEventRaiser _debouncer;
    private FmScript _script;
    private ScriptMetadata? _metadata;
    // Anchor + signature: the line text at capture time. If a later
    // lookup finds the anchor still alive at the same offset but the
    // underlying line text no longer matches (e.g. because the whole
    // line was deleted and an unrelated line slid into its offset),
    // the anchor is considered stale and ignored.
    private readonly Dictionary<TextAnchor, (XElement Xml, string Signature)> _sealedAnchors = new();

    // Hot-path cache: sealed line numbers + end-offsets, recomputed
    // lazily on next read after Document.TextChanged. Visual components
    // (italic colorizer, cog generator, squiggle renderer) fire per
    // visible line per layout — without this, each fired SealedAnchors
    // enumerator walks every anchor and allocates a fresh string per
    // anchor through SignatureMatches/GetText. With N visible lines and
    // M anchors that's N*M strings per paint. The cache collapses this
    // to a single pass per text change.
    private HashSet<int>? _sealedLineNumbersCache;
    private Dictionary<int, int>? _sealedEndOffsetsCache; // line# -> end offset
    private bool _sealedCacheDirty = true;

    public event EventHandler? ContentChanged;

    /// <summary>The TextDocument bound to the AvaloniaEdit script editor.</summary>
    public TextDocument Document { get; }

    public bool IsPartial { get; private set; }

    public ScriptClipEditor(FmScript script)
    {
        _script = script;
        _metadata = _script.Metadata;
        Document = new TextDocument(_script.ToDisplayText());
        BuildSealedAnchors();

        _debouncer = new DebouncedEventRaiser(500, () => ContentChanged?.Invoke(this, EventArgs.Empty));
        Document.TextChanged += (_, _) =>
        {
            _sealedCacheDirty = true;
            _debouncer.Trigger();
        };
    }

    /// <summary>
    /// Document line numbers (1-based) currently occupied by a sealed step.
    /// Built lazily on first read after a text change; the underlying set
    /// is reused across reads so render-pipeline consumers (colorizer,
    /// cog generator, sealed-step layer) get O(1) lookup. Skips the
    /// SignatureMatches string allocations that the live SealedAnchors
    /// enumerator does — for visual rendering, anchor liveness + bounds
    /// is sufficient; signature checks belong on the model-rebuild path.
    /// </summary>
    internal IReadOnlySet<int> SealedLineNumbers
    {
        get
        {
            EnsureSealedCache();
            return _sealedLineNumbersCache!;
        }
    }

    /// <summary>
    /// Map from sealed-line number (1-based) to the document offset of
    /// that line's end. Used by the cog generator to decide which visual
    /// line ends carry a cog button.
    /// </summary>
    internal IReadOnlyDictionary<int, int> SealedLineEndOffsets
    {
        get
        {
            EnsureSealedCache();
            return _sealedEndOffsetsCache!;
        }
    }

    private void EnsureSealedCache()
    {
        if (!_sealedCacheDirty && _sealedLineNumbersCache != null) return;

        var lineNumbers = new HashSet<int>();
        var endOffsets = new Dictionary<int, int>();
        foreach (var kv in _sealedAnchors)
        {
            var anchor = kv.Key;
            if (anchor.IsDeleted) continue;
            if (anchor.Offset < 0 || anchor.Offset > Document.TextLength) continue;
            var line = Document.GetLineByOffset(anchor.Offset);
            lineNumbers.Add(line.LineNumber);
            endOffsets[line.LineNumber] = line.EndOffset;
        }
        _sealedLineNumbersCache = lineNumbers;
        _sealedEndOffsetsCache = endOffsets;
        _sealedCacheDirty = false;
    }

    /// <summary>
    /// Force-invalidate the sealed-line cache. Called from paths that
    /// mutate <see cref="_sealedAnchors"/> outside a TextChanged event
    /// (BuildSealedAnchors, UpdateSealedXml, RebuildFromDocument).
    /// </summary>
    private void InvalidateSealedCache() => _sealedCacheDirty = true;

    /// <summary>
    /// Live sealed anchors (entries whose line text still matches the
    /// signature captured at creation). Exposed for the editor's
    /// rendering extensions (read-only provider, squiggle, cog) to know
    /// which document lines are sealed.
    /// </summary>
    internal IEnumerable<TextAnchor> SealedAnchors =>
        _sealedAnchors
            .Where(kv => !kv.Key.IsDeleted && SignatureMatches(kv.Key, kv.Value.Signature))
            .Select(kv => kv.Key);

    /// <summary>
    /// Cheap predicate for sealed-step renderers / colorizers to fast-exit
    /// when no sealed anchors exist. The full <see cref="SealedAnchors"/>
    /// enumerator is a Where+Select chain that calls <c>Document.GetText</c>
    /// per anchor — fine when the loop body runs, expensive when it never
    /// does because the script has zero sealed steps. Sealed-step
    /// components fire per visible line per layout, so even an empty
    /// iteration adds up.
    /// </summary>
    internal bool HasSealedAnchors => _sealedAnchors.Count > 0;

    /// <summary>
    /// Retrieve the cached XML for a sealed anchor. Returns false if the
    /// anchor is dead, evicted, or whose underlying line no longer matches
    /// the signature captured at seal time (whole-line deletion case).
    /// </summary>
    internal bool TryGetSealedXml(TextAnchor anchor, out XElement xml)
    {
        if (_sealedAnchors.TryGetValue(anchor, out var entry)
            && !anchor.IsDeleted
            && SignatureMatches(anchor, entry.Signature))
        {
            xml = new XElement(entry.Xml);
            return true;
        }
        xml = new XElement("Step");
        return false;
    }

    /// <summary>
    /// Replace the XML stored for a sealed anchor with a new, edited
    /// version. Called by the cog-triggered raw-XML editor dialog after
    /// the user saves. Re-renders the sealed line's display text to
    /// match the new XML. Returns false if the anchor is no longer in
    /// the cache (e.g. the line was deleted while the dialog was open).
    /// </summary>
    internal bool UpdateSealedXml(TextAnchor anchor, XElement newXml)
    {
        if (!_sealedAnchors.TryGetValue(anchor, out var entry)) return false;
        if (anchor.IsDeleted) return false;
        if (!SignatureMatches(anchor, entry.Signature)) return false;

        var line = Document.GetLineByOffset(anchor.Offset);
        var currentText = Document.GetText(line.Offset, line.Length);
        var indent = currentText[..^currentText.AsSpan().TrimStart().Length];

        var newStep = ScriptStep.FromXml(newXml);
        var newDisplay = indent + newStep.ToDisplayLine();

        Document.Replace(line.Offset, line.Length, newDisplay);

        // Update the cache entry with the fresh XML + new signature.
        _sealedAnchors[anchor] = (new XElement(newXml), newDisplay);
        InvalidateSealedCache();
        return true;
    }

    private bool SignatureMatches(TextAnchor anchor, string signature)
    {
        if (anchor.Offset < 0 || anchor.Offset > Document.TextLength) return false;
        var line = Document.GetLineByOffset(anchor.Offset);
        var current = Document.GetText(line.Offset, line.Length);
        return current == signature;
    }

    public string ToXml()
    {
        try
        {
            RebuildFromDocument();
            IsPartial = false;
        }
        catch
        {
            IsPartial = true;
        }

        _script.Metadata = _metadata;
        return _script.ToXml();
    }

    /// <summary>
    /// Reparse the current document into <see cref="_script"/>, preserving
    /// sealed steps via the anchor cache. For each logical step range in
    /// the document, if the range overlaps a sealed anchor, reuse the
    /// cached XML; otherwise re-parse from display text.
    /// </summary>
    private void RebuildFromDocument()
    {
        var ranges = SharpFM.Scripting.Editor.CachedMultiLineRanges.Compute(Document);

        var newSteps = new List<ScriptStep>();
        var consumedAnchors = new HashSet<TextAnchor>();

        foreach (var (startLine, endLine) in ranges)
        {
            var rangeStartOffset = Document.GetLineByNumber(startLine).Offset;
            var rangeEndOffset = Document.GetLineByNumber(endLine).EndOffset;
            var rangeText = Document.GetText(rangeStartOffset, rangeEndOffset - rangeStartOffset);

            // Blank lines correspond to empty CommentSteps (FM Pro
            // convention: blank lines in the script editor are
            // <Step id="89"> with empty Text). Map directly — no parse
            // needed. Sealed-anchor lookups are skipped because empty
            // comments are POCO-backed and fully editable.
            if (string.IsNullOrWhiteSpace(rangeText))
            {
                newSteps.Add(new CommentStep(enabled: true, text: string.Empty));
                continue;
            }

            // Look for a sealed anchor whose line still contains its
            // original signature text, positioned at this range's start.
            TextAnchor? matchedAnchor = null;
            XElement? matchedXml = null;
            foreach (var kv in _sealedAnchors)
            {
                if (consumedAnchors.Contains(kv.Key)) continue;
                if (kv.Key.IsDeleted) continue;
                if (kv.Key.Offset < rangeStartOffset || kv.Key.Offset > rangeEndOffset) continue;
                if (!SignatureMatches(kv.Key, kv.Value.Signature)) continue;

                matchedAnchor = kv.Key;
                matchedXml = kv.Value.Xml;
                break;
            }

            if (matchedAnchor != null && matchedXml != null)
            {
                consumedAnchors.Add(matchedAnchor);
                newSteps.Add(ScriptStep.FromXml(matchedXml));
                continue;
            }

            newSteps.Add(ScriptTextParser.FromDisplayLine(rangeText.TrimEnd('\r', '\n')));
        }

        // Prune anchors that are dead OR whose signatures no longer match
        // (their underlying line was deleted or replaced).
        var dead = _sealedAnchors
            .Where(kv => kv.Key.IsDeleted || !SignatureMatches(kv.Key, kv.Value.Signature))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var d in dead) _sealedAnchors.Remove(d);
        if (dead.Count > 0) InvalidateSealedCache();

        _script = new FmScript(newSteps);
    }

    /// <summary>
    /// Walk the current document and create a TextAnchor + cached XML
    /// entry for each sealed step. Called after initial load and after
    /// <see cref="FromXml"/>.
    /// </summary>
    private void BuildSealedAnchors()
    {
        _sealedAnchors.Clear();

        var ranges = SharpFM.Scripting.Editor.CachedMultiLineRanges.Compute(Document);
        int stepIdx = 0;

        foreach (var (startLine, endLine) in ranges)
        {
            // Every range corresponds to one step in _script.Steps —
            // including blank-line ranges which map to empty CommentSteps.
            // We advance stepIdx uniformly so the step <-> range pairing
            // stays aligned.
            if (stepIdx >= _script.Steps.Count) break;
            var step = _script.Steps[stepIdx];
            stepIdx++;

            if (step.IsFullyEditable) continue;

            var lineOffset = Document.GetLineByNumber(startLine).Offset;
            var lineLen = Document.GetLineByNumber(startLine).Length;
            var lineText = Document.GetText(lineOffset, lineLen);

            var anchor = Document.CreateAnchor(lineOffset);
            anchor.MovementType = AnchorMovementType.AfterInsertion;
            anchor.SurviveDeletion = false;
            _sealedAnchors[anchor] = (step.ToXml(), lineText);
        }
        InvalidateSealedCache();
    }
}
