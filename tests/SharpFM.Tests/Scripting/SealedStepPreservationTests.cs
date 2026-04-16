using AvaloniaEdit.Document;
using SharpFM.Editors;
using SharpFM.Model.Scripting;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace SharpFM.Tests.Scripting;

/// <summary>
/// Verifies the anchor-cache mechanism that preserves sealed (non-POCO,
/// non-allow-list) steps' source XML across display-text edits. The
/// editor maintains TextAnchors pointing at sealed step lines; when the
/// user edits non-sealed parts of the document, sealed steps are
/// recovered from the cache rather than re-parsed (which would lose
/// any XML state absent from the display form).
/// </summary>
public class SealedStepPreservationTests
{
    // Beep is catalog-known, has no typed POCO, and (with the allow-list
    // empty at launch) is sealed. Perfect canary.
    private const string ScriptWithSealedBeepXml = @"<fmxmlsnippet type=""FMObjectList"">
        <Step enable=""True"" id=""68"" name=""If""><Calculation><![CDATA[$x > 0]]></Calculation></Step>
        <Step enable=""True"" id=""93"" name=""Beep""></Step>
        <Step enable=""True"" id=""70"" name=""End If""></Step>
    </fmxmlsnippet>";

    [Fact]
    public void NoEdits_ToXml_ProducesOriginalScriptWithSealedStepIntact()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        var xml = editor.ToXml();
        var doc = XDocument.Parse(xml);
        var steps = doc.Root!.Elements("Step").ToArray();

        Assert.Equal(3, steps.Length);
        Assert.Equal("Beep", steps[1].Attribute("name")!.Value);
        Assert.Equal("93", steps[1].Attribute("id")!.Value);
    }

    [Fact]
    public void EditNonSealedLine_SealedStepPreservedByAnchor()
    {
        // User edits the If step's calculation — the Beep step's XML
        // must survive unchanged via the anchor cache.
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);

        // Change the If calc from "$x > 0" to "$x > 10" via document edit.
        var line1 = editor.Document.GetLineByNumber(1);
        var line1Text = editor.Document.GetText(line1.Offset, line1.Length);
        var newLine1 = line1Text.Replace("$x > 0", "$x > 10");
        editor.Document.Replace(line1.Offset, line1.Length, newLine1);

        var xml = editor.ToXml();
        var doc = XDocument.Parse(xml);
        var steps = doc.Root!.Elements("Step").ToArray();

        // If step reflects the edit
        Assert.Contains("$x > 10", steps[0].Element("Calculation")!.Value);
        // Beep step still present with id
        Assert.Equal("Beep", steps[1].Attribute("name")!.Value);
        Assert.Equal("93", steps[1].Attribute("id")!.Value);
    }

    [Fact]
    public void DeleteSealedLine_StepDropsOutOfScript()
    {
        // Deleting a sealed line entirely is allowed — the anchor
        // invalidates and the step is gone from the output. Intentional
        // per the design: delete OK, edit-in-place not OK.
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);

        var line2 = editor.Document.GetLineByNumber(2);
        // Include the trailing newline so we delete the whole line
        var deleteLength = line2.TotalLength;
        editor.Document.Remove(line2.Offset, deleteLength);

        var xml = editor.ToXml();
        var doc = XDocument.Parse(xml);
        var steps = doc.Root!.Elements("Step").ToArray();

        Assert.Equal(2, steps.Length);
        Assert.Equal("If", steps[0].Attribute("name")!.Value);
        Assert.Equal("End If", steps[1].Attribute("name")!.Value);
    }

    [Fact]
    public void InsertNewLineBeforeSealed_SealedStepSurvivesAtNewPosition()
    {
        // Insert a new If step above Beep. Beep must still be preserved
        // via the anchor (which moves with the line).
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);

        var line2 = editor.Document.GetLineByNumber(2); // Beep
        editor.Document.Insert(line2.Offset, "Beep\n");

        var xml = editor.ToXml();
        var doc = XDocument.Parse(xml);
        var steps = doc.Root!.Elements("Step").ToArray();

        // Four steps now; the original Beep (the one at the original
        // anchor position) must still have its id preserved.
        Assert.Equal(4, steps.Length);

        // Find all Beep steps. The one with id=93 is the original sealed one.
        var originalBeep = steps.FirstOrDefault(s => s.Attribute("id")?.Value == "93");
        Assert.NotNull(originalBeep);
        Assert.Equal("Beep", originalBeep!.Attribute("name")!.Value);
    }

    // --- UpdateSealedXml (cog-edit write-back) ---

    [Fact]
    public void UpdateSealedXml_ReplacesCachedXmlAndRerendersLine()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);

        // Find the Beep anchor and edit its XML in place — simulating what
        // RawStepEditorWindow does after the user saves. Replace the Beep
        // step with a hypothetical different-id Beep to prove the cache
        // update propagates to ToXml output.
        var anchor = editor.SealedAnchors.First();
        var replacement = XElement.Parse(
            "<Step enable=\"True\" id=\"93\" name=\"Beep\"><SomeChild value=\"42\"/></Step>");

        var updated = editor.UpdateSealedXml(anchor, replacement);
        Assert.True(updated);

        var xml = editor.ToXml();
        var doc = XDocument.Parse(xml);
        var beep = doc.Root!.Elements("Step").ElementAt(1);

        // The cached XML now reflects the replacement — <SomeChild> is
        // present even though the display line (still "Beep") hasn't
        // exposed it.
        Assert.NotNull(beep.Element("SomeChild"));
        Assert.Equal("42", beep.Element("SomeChild")!.Attribute("value")!.Value);
    }

    [Fact]
    public void UpdateSealedXml_DeadAnchor_ReturnsFalse()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        var anchor = editor.SealedAnchors.First();

        // Delete the sealed line — anchor is now dead.
        var line2 = editor.Document.GetLineByNumber(2);
        editor.Document.Remove(line2.Offset, line2.TotalLength);

        var replacement = XElement.Parse("<Step enable=\"True\" id=\"93\" name=\"Beep\"/>");
        Assert.False(editor.UpdateSealedXml(anchor, replacement));
    }

    [Fact]
    public void UpdateSealedXml_ThenEditNonSealedLine_UpdateIsPreserved()
    {
        // The cog-edit must survive a subsequent non-sealed edit: the
        // rebuild loop must NOT drop the updated XML when it re-runs.
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        var anchor = editor.SealedAnchors.First();
        var replacement = XElement.Parse(
            "<Step enable=\"True\" id=\"93\" name=\"Beep\"><SomeChild value=\"99\"/></Step>");
        editor.UpdateSealedXml(anchor, replacement);

        // Now edit the If step's calc.
        var line1 = editor.Document.GetLineByNumber(1);
        var line1Text = editor.Document.GetText(line1.Offset, line1.Length);
        editor.Document.Replace(line1.Offset, line1.Length, line1Text.Replace("$x > 0", "$x > 5"));

        var xml = editor.ToXml();
        var doc = XDocument.Parse(xml);
        var beep = doc.Root!.Elements("Step").ElementAt(1);

        Assert.NotNull(beep.Element("SomeChild"));
        Assert.Equal("99", beep.Element("SomeChild")!.Attribute("value")!.Value);
    }

    // --- TryGetSealedXml ---

    [Fact]
    public void TryGetSealedXml_LiveAnchor_ReturnsCachedXml()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        var anchor = editor.SealedAnchors.First();

        Assert.True(editor.TryGetSealedXml(anchor, out var xml));
        Assert.Equal("Beep", xml.Attribute("name")!.Value);
        Assert.Equal("93", xml.Attribute("id")!.Value);
    }

    [Fact]
    public void TryGetSealedXml_DeadAnchor_ReturnsFalse()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        var anchor = editor.SealedAnchors.First();

        var line2 = editor.Document.GetLineByNumber(2);
        editor.Document.Remove(line2.Offset, line2.TotalLength);

        Assert.False(editor.TryGetSealedXml(anchor, out _));
    }

    // --- Multiple sealed steps in one script ---

    [Fact]
    public void MultipleSealedSteps_EachPreservedIndependently()
    {
        // Two Beeps + a comment (all non-POCO-except-comment) — comment
        // has a POCO so it stays editable. The two Beeps are sealed.
        var xml = @"<fmxmlsnippet type=""FMObjectList"">
            <Step enable=""True"" id=""68"" name=""If""><Calculation><![CDATA[$x > 0]]></Calculation></Step>
            <Step enable=""True"" id=""93"" name=""Beep""><FirstMarker/></Step>
            <Step enable=""True"" id=""89"" name=""# (comment)""><Text>between</Text></Step>
            <Step enable=""True"" id=""93"" name=""Beep""><SecondMarker/></Step>
            <Step enable=""True"" id=""70"" name=""End If""></Step>
        </fmxmlsnippet>";

        var editor = new ScriptClipEditor(xml);
        // Edit the comment line (non-sealed) to force a rebuild.
        var line3 = editor.Document.GetLineByNumber(3);
        var line3Text = editor.Document.GetText(line3.Offset, line3.Length);
        editor.Document.Replace(line3.Offset, line3.Length, line3Text + " (edited)");

        var outXml = editor.ToXml();
        var outDoc = XDocument.Parse(outXml);
        var steps = outDoc.Root!.Elements("Step").ToArray();

        // Two Beeps each with their distinguishing marker child intact.
        var beepsWithFirst = steps.Count(s => s.Element("FirstMarker") != null);
        var beepsWithSecond = steps.Count(s => s.Element("SecondMarker") != null);

        Assert.Equal(1, beepsWithFirst);
        Assert.Equal(1, beepsWithSecond);
    }

    // --- Sealed steps + script wrapper (Mac-XMSC) metadata ---

    [Fact]
    public void ScriptMetadata_AndSealedStep_BothPreservedOnRoundTrip()
    {
        var xml = @"<fmxmlsnippet type=""FMObjectList"">
            <Script includeInMenu=""True"" runFullAccess=""False"" id=""42"" name=""MyScript"">
                <Step enable=""True"" id=""68"" name=""If""><Calculation><![CDATA[$x > 0]]></Calculation></Step>
                <Step enable=""True"" id=""93"" name=""Beep""><WrapperPreserved/></Step>
                <Step enable=""True"" id=""70"" name=""End If""></Step>
            </Script>
        </fmxmlsnippet>";

        var editor = new ScriptClipEditor(xml);
        // Trigger a non-sealed edit so the rebuild loop runs end-to-end.
        var line1 = editor.Document.GetLineByNumber(1);
        var t = editor.Document.GetText(line1.Offset, line1.Length);
        editor.Document.Replace(line1.Offset, line1.Length, t.Replace("$x > 0", "$x > 1"));

        var outXml = editor.ToXml();
        var outDoc = XDocument.Parse(outXml);

        // Script wrapper metadata preserved
        var scriptEl = outDoc.Root!.Element("Script");
        Assert.NotNull(scriptEl);
        Assert.Equal("42", scriptEl!.Attribute("id")!.Value);
        Assert.Equal("MyScript", scriptEl.Attribute("name")!.Value);

        // Sealed Beep's custom child preserved
        var beep = scriptEl.Elements("Step").FirstOrDefault(s => s.Attribute("id")?.Value == "93");
        Assert.NotNull(beep);
        Assert.NotNull(beep!.Element("WrapperPreserved"));
    }

    // --- Idempotency ---

    [Fact]
    public void ToXml_CalledTwice_ProducesIdenticalOutput()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        var first = editor.ToXml();
        var second = editor.ToXml();
        Assert.Equal(first, second);
    }

    // --- Sealed step as the very last line (no trailing newline) ---

    [Fact]
    public void SealedStep_AtEndOfDocumentNoTrailingNewline_Preserved()
    {
        var xml = @"<fmxmlsnippet type=""FMObjectList"">
            <Step enable=""True"" id=""68"" name=""If""><Calculation><![CDATA[$x > 0]]></Calculation></Step>
            <Step enable=""True"" id=""93"" name=""Beep""><EofCanary/></Step>
        </fmxmlsnippet>";

        var editor = new ScriptClipEditor(xml);
        var outXml = editor.ToXml();
        var outDoc = XDocument.Parse(outXml);
        var beep = outDoc.Root!.Elements("Step").Last();

        Assert.Equal("Beep", beep.Attribute("name")!.Value);
        Assert.NotNull(beep.Element("EofCanary"));
    }

    // --- FromXml reload ---

    [Fact]
    public void SealedAnchors_AfterDocumentShrink_AllOffsetsWithinBounds()
    {
        // Regression guard for the "stale renderer survives clip swap" bug
        // that crashed AvaloniaEdit with ArgumentOutOfRangeException on
        // Document.GetLineByOffset. The architectural fix (detach
        // renderers on clip swap) is at the UI layer and needs an Avalonia
        // context to test. This covers the defensive side: the editor's
        // own SealedAnchors iterator must never yield an anchor whose
        // offset lies past the current document length.
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        Assert.Single(editor.SealedAnchors);

        // Drastically shrink the document — clears every line the
        // original anchor could live on.
        editor.Document.Replace(0, editor.Document.TextLength, string.Empty);

        foreach (var anchor in editor.SealedAnchors)
        {
            Assert.InRange(anchor.Offset, 0, editor.Document.TextLength);
        }
    }

    [Fact]
    public void SealedAnchors_AfterDocumentReplacedWithShorterContent_AllOffsetsWithinBounds()
    {
        // Same invariant but with content that has steps — proves the
        // iterator stays safe when the TextView swaps documents too.
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        Assert.Single(editor.SealedAnchors);

        editor.Document.Replace(0, editor.Document.TextLength, "If [ $x > 0 ]");

        foreach (var anchor in editor.SealedAnchors)
        {
            Assert.InRange(anchor.Offset, 0, editor.Document.TextLength);
        }
    }

    [Fact]
    public void FromXml_Reload_RebuildsAnchorCacheFreshForNewContent()
    {
        var editor = new ScriptClipEditor(ScriptWithSealedBeepXml);
        Assert.Single(editor.SealedAnchors);

        // Reload with different content — previous anchor should be gone,
        // new anchors built for whatever sealed steps the new XML has.
        var differentXml = @"<fmxmlsnippet type=""FMObjectList"">
            <Step enable=""True"" id=""68"" name=""If""><Calculation><![CDATA[$y > 0]]></Calculation></Step>
            <Step enable=""True"" id=""70"" name=""End If""></Step>
        </fmxmlsnippet>";
        editor.FromXml(differentXml);

        // No sealed anchors — both If and End If are POCOs.
        Assert.Empty(editor.SealedAnchors);

        // Reload with a sealed step back — a fresh anchor appears.
        editor.FromXml(ScriptWithSealedBeepXml);
        Assert.Single(editor.SealedAnchors);
    }
}
