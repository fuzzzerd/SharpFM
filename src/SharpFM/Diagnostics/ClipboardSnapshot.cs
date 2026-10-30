using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Avalonia;

namespace SharpFM.Diagnostics;

internal sealed record ClipboardCaptureEnvironment(
    string SharpFMVersion, string OSVersion, string ProcessArchitecture, string AvaloniaVersion)
{
    public static ClipboardCaptureEnvironment Current() => new(
        VersionOf(typeof(ClipboardSnapshot).Assembly),
        RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        VersionOf(typeof(Application).Assembly));

    private static string VersionOf(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString() ?? "unknown";
}

internal sealed record ClipboardFormatInfo(string Identifier, string Kind, string ExposedRepresentation);

internal enum ClipboardCaptureOutcome { Captured, Null, Unsupported, Failed, ResourceLimit }

internal sealed record ClipboardSnapshotEntry(
    int ItemIndex,
    ClipboardFormatInfo Format,
    ClipboardCaptureOutcome Outcome,
    string? RuntimeType = null,
    long? ByteLength = null,
    string? PayloadFile = null,
    string? PayloadRepresentation = null,
    string? Sha256 = null,
    string? Error = null,
    ClipboardPayloadInterpretation? Interpretation = null,
    [property: JsonIgnore] byte[]? Data = null);

internal sealed record ClipboardCaptureLimits(long MaxRetainedPayloadBytes, int MaxXmlProbeBytes);

internal sealed record ClipboardSnapshot(
    int SchemaVersion,
    DateTimeOffset CapturedAtUtc,
    ClipboardCaptureEnvironment Environment,
    string? FileMakerVersion,
    string? Description,
    string CaptureBoundary,
    ClipboardCaptureLimits Limits,
    IReadOnlyList<ClipboardFormatInfo> Formats,
    IReadOnlyList<ClipboardSnapshotEntry> Entries,
    IReadOnlyList<string> Issues)
{
    public bool IsPartial => Issues.Count > 0 || Entries.Any(e => e.Outcome != ClipboardCaptureOutcome.Captured || e.Error is not null);

    public const string Boundary =
        "Best-effort capture through Avalonia's clipboard API, not a native pasteboard dump. " +
        "Only representations exposed by Avalonia are available; bytes are preserved as returned by Avalonia. " +
        "One transfer is used, but atomicity and clipboard-change detection are not guaranteed. " +
        "Text payloads are exported as strict UTF-8 without a BOM, not as original wire bytes. " +
        "Unsupported object values are described by type only. Errors omit messages to avoid leaking paths or contents. " +
        "The retained-payload budget cannot bound allocations performed inside the platform API during retrieval.";
}

internal static class ClipboardDiagnosticError
{
    public static string Describe(Exception exception) =>
        $"{exception.GetType().FullName} (0x{exception.HResult:X8}); message omitted for privacy.";
}
