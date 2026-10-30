using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;

namespace SharpFM.Diagnostics;

/// <summary>Reads one transfer, preserving each item/format without FileMaker decoding.</summary>
internal sealed class ClipboardSnapshotCapture(Func<Task<IAsyncDataTransfer?>> openTransfer,
    long maxCaptureBytes = 128L * 1024 * 1024)
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public async Task<ClipboardSnapshot> CaptureAsync(
        ClipboardCaptureEnvironment environment, string? fileMakerVersion = null, string? description = null)
    {
        var capturedAt = DateTimeOffset.UtcNow;
        var formats = new List<ClipboardFormatInfo>();
        var entries = new List<ClipboardSnapshotEntry>();
        var issues = new List<string>();
        long retainedBytes = 0;

        try
        {
            using var transfer = await openTransfer();
            if (transfer is null)
            {
                issues.Add("No clipboard transfer was available.");
            }
            else
            {
                try { formats.AddRange(transfer.Formats.Select(Describe)); }
                catch (Exception ex)
                {
                    issues.Add($"Transfer format discovery: {ClipboardDiagnosticError.Describe(ex)}");
                }
                var items = transfer.Items;
                for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
                {
                    IAsyncDataTransferItem item;
                    DataFormat[] itemFormats;
                    try
                    {
                        item = items[itemIndex];
                        itemFormats = item.Formats.ToArray();
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"Item {itemIndex} format discovery: {ClipboardDiagnosticError.Describe(ex)}");
                        continue;
                    }

                    foreach (var format in itemFormats)
                    {
                        // Retain identifiers present on items even if absent from the transfer's union.
                        var info = Describe(format);
                        if (!formats.Contains(info)) formats.Add(info);
                        var entry = new ClipboardSnapshotEntry(itemIndex, info, ClipboardCaptureOutcome.Failed);
                        try
                        {
                            // Do not reconstruct the format as a byte request: that can hide
                            // representation differences and platform-specific identifiers.
                            var value = await item.TryGetRawAsync(format);
                            entry = entry with { RuntimeType = value?.GetType().FullName };
                            if (value is null)
                            {
                                entry = entry with { Outcome = ClipboardCaptureOutcome.Null };
                            }
                            else if (value is byte[] || value is string)
                            {
                                var size = value is byte[] raw ? raw.LongLength
                                    : await Task.Run(() => (long)StrictUtf8.GetByteCount((string)value));
                                entry = entry with { ByteLength = size };
                                if (size > maxCaptureBytes - retainedBytes)
                                {
                                    entry = entry with
                                    {
                                        Outcome = ClipboardCaptureOutcome.ResourceLimit,
                                        Error = $"Capture budget of {maxCaptureBytes} bytes exceeded; this payload was not retained or truncated."
                                    };
                                }
                                else
                                {
                                    var isText = value is string;
                                    var payloadFile = $"payloads/{entries.Count:D4}.{(isText ? "txt" : "bin")}";
                                    // Copy before disposing the transfer. CPU-heavy work is off the UI thread.
                                    entry = await Task.Run(() =>
                                    {
                                        var data = value is byte[] bytes ? (byte[])bytes.Clone() : StrictUtf8.GetBytes((string)value);
                                        var captured = entry with
                                        {
                                            Outcome = ClipboardCaptureOutcome.Captured,
                                            Data = data,
                                            PayloadFile = payloadFile,
                                            PayloadRepresentation = isText ? "Text encoded as UTF-8 (not wire bytes)" : "Bytes returned by Avalonia",
                                            Sha256 = Convert.ToHexString(SHA256.HashData(data))
                                        };
                                        try
                                        {
                                            return captured with { Interpretation = ClipboardPayloadInterpreter.Interpret(data) };
                                        }
                                        catch (Exception ex)
                                        {
                                            // Interpretation must never prevent preserving the original payload.
                                            return captured with { Error = $"Interpretation failed: {ClipboardDiagnosticError.Describe(ex)}" };
                                        }
                                    });
                                    retainedBytes += size;
                                }
                            }
                            else
                            {
                                entry = entry with
                                {
                                    Outcome = ClipboardCaptureOutcome.Unsupported,
                                    Error = "Only byte arrays and text are persisted; no arbitrary object serialization or file access was attempted."
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            entry = entry with { Outcome = ClipboardCaptureOutcome.Failed, Error = ClipboardDiagnosticError.Describe(ex) };
                        }
                        entries.Add(entry);
                        await Task.Yield();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            issues.Add($"Clipboard access/disposal: {ClipboardDiagnosticError.Describe(ex)}");
        }

        if (formats.Any(f => !entries.Any(e => e.Format == f)))
            issues.Add("One or more advertised formats had no accessible item; see the discovered format list.");

        return new ClipboardSnapshot(1, capturedAt, environment, EmptyToNull(fileMakerVersion), EmptyToNull(description),
            ClipboardSnapshot.Boundary, new ClipboardCaptureLimits(maxCaptureBytes, ClipboardPayloadInterpreter.MaxProbeBytes),
            formats.ToArray(), entries.ToArray(), issues.ToArray());
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClipboardFormatInfo Describe(DataFormat format) => new(
        format.Identifier, format.Kind.ToString(),
        format.GetType().GenericTypeArguments.FirstOrDefault()?.FullName ?? "Unknown");
}
