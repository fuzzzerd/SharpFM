using System.Linq;
using System.Text;

namespace SharpFM.Diagnostics;

internal static class ClipboardSnapshotSummary
{
    public static string Describe(ClipboardSnapshot snapshot)
    {
        var captured = snapshot.Entries.Count(e => e.Outcome == ClipboardCaptureOutcome.Captured);
        var summary = new StringBuilder()
            .AppendLine($"Captured {snapshot.CapturedAtUtc:u} — {(snapshot.IsPartial ? "partial capture" : "capture complete through Avalonia")}")
            .AppendLine($"{snapshot.Formats.Count} formats; {captured}/{snapshot.Entries.Count} item/format payloads retained.")
            .AppendLine("All formats exposed by Avalonia (including accompanying text) are included; not a native pasteboard dump.");
        foreach (var issue in snapshot.Issues) summary.AppendLine(issue);
        foreach (var format in snapshot.Formats)
            summary.AppendLine($"• {format.Identifier} [{format.Kind}; {format.ExposedRepresentation}]");
        return summary.ToString();
    }

    public static string Label(ClipboardSnapshotEntry entry) =>
        $"Item {entry.ItemIndex} · {entry.Format.Identifier} · {entry.Outcome} · {entry.ByteLength?.ToString() ?? "?"} bytes";

    public static string Describe(ClipboardSnapshotEntry entry)
    {
        var summary = new StringBuilder()
            .AppendLine(Label(entry))
            .AppendLine($"Exposed representation: {entry.Format.ExposedRepresentation}")
            .AppendLine($"Returned type: {entry.RuntimeType ?? "(none)"}");
        if (entry.PayloadRepresentation is not null) summary.AppendLine(entry.PayloadRepresentation);
        if (entry.Error is not null) summary.AppendLine(entry.Error);
        if (entry.Interpretation is { } interpretation)
        {
            summary.AppendLine($"Leading bytes (hex): {interpretation.LeadingHex}")
                .AppendLine($"BOM at offset 0: {interpretation.Bom}")
                .AppendLine($"First four bytes as uint32 LE: {interpretation.LittleEndianLength?.ToString() ?? "unavailable"}")
                .AppendLine($"Matches remaining byte count: {interpretation.LengthMatchesRemainingBytes?.ToString() ?? "unavailable"}")
                .AppendLine("Independent hypotheses, not detected platform rules:");
            foreach (var probe in interpretation.XmlHypotheses)
            {
                summary.AppendLine($"  Offset {probe.Offset}: UTF-8 {probe.Utf8Status}; XML {probe.XmlStatus}; BOM {probe.Bom ?? "unavailable"}.");
                if (probe.RootName is not null) summary.AppendLine($"    Root: {probe.RootName}; snippet type: {probe.SnippetType ?? "(absent)"}");
                if (probe.Detail is not null) summary.AppendLine($"    {probe.Detail}");
            }
        }
        return summary.ToString();
    }
}
