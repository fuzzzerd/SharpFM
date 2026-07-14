using SharpFM.Model;
using SharpFM.Model.Parsing;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class ProblemsPanelViewModelTests
{
    [Fact]
    public void NoClip_HasNoDiagnostics()
    {
        var vm = new ProblemsPanelViewModel();

        vm.RefreshFrom(null);

        Assert.Empty(vm.Diagnostics);
        Assert.Equal(0, vm.Count);
    }

    [Fact]
    public void LossyClip_PopulatesDiagnostics()
    {
        var vm = new ProblemsPanelViewModel();
        var clip = new ClipViewModel(Clip.FromXml("Lossy", "Mac-XMSS", ParseFidelityTestXml.InfoOnlyStepXml));

        vm.RefreshFrom(clip);

        var row = Assert.Single(vm.Diagnostics);
        Assert.Equal(ParseDiagnosticKind.UnknownStep, row.Kind);
        Assert.False(row.IsSemantic);
        Assert.False(string.IsNullOrEmpty(row.Location));
        Assert.False(string.IsNullOrEmpty(row.Message));
        Assert.Equal(1, vm.Count);
    }

    [Fact]
    public void LosslessClip_HasNoDiagnostics()
    {
        var vm = new ProblemsPanelViewModel();
        var clip = new ClipViewModel(Clip.FromXml(
            "Clean", "Mac-XMSS", "<fmxmlsnippet type=\"FMObjectList\"></fmxmlsnippet>"));

        vm.RefreshFrom(clip);

        Assert.Empty(vm.Diagnostics);
    }

    [Fact]
    public void SelectingDiagnostic_ResolvesXmlSnippet()
    {
        var vm = new ProblemsPanelViewModel();
        var clip = new ClipViewModel(Clip.FromXml("Lossy", "Mac-XMSS", ParseFidelityTestXml.InfoOnlyStepXml));
        vm.RefreshFrom(clip);

        vm.SelectedDiagnostic = vm.Diagnostics[0];

        Assert.NotNull(vm.SelectedXmlSnippet);
        Assert.Contains("Step", vm.SelectedXmlSnippet);
    }

    [Fact]
    public void NoSelection_HasNoXmlSnippet()
    {
        var vm = new ProblemsPanelViewModel();

        Assert.Null(vm.SelectedXmlSnippet);
    }

    [Fact]
    public void RefreshFrom_ClearsPreviousDiagnostics()
    {
        var vm = new ProblemsPanelViewModel();
        var lossy = new ClipViewModel(Clip.FromXml("Lossy", "Mac-XMSS", ParseFidelityTestXml.InfoOnlyStepXml));
        vm.RefreshFrom(lossy);
        Assert.NotEmpty(vm.Diagnostics);

        vm.RefreshFrom(null);

        Assert.Empty(vm.Diagnostics);
        Assert.Null(vm.SelectedDiagnostic);
    }

    [Fact]
    public void IsPanelVisible_DefaultsToFalse()
    {
        var vm = new ProblemsPanelViewModel();

        Assert.False(vm.IsPanelVisible);
    }

    [Fact]
    public void RefreshFrom_WithUnchangedDiagnostics_PreservesSelection()
    {
        var vm = new ProblemsPanelViewModel();
        var clip = new ClipViewModel(Clip.FromXml("Lossy", "Mac-XMSS", ParseFidelityTestXml.InfoOnlyStepXml));
        vm.RefreshFrom(clip);
        vm.SelectedDiagnostic = vm.Diagnostics[0];

        // Same clip, same diagnostics — e.g. a debounced edit tick that didn't
        // change the fidelity report. Selection and its resolved snippet must
        // survive since nothing actually changed.
        vm.RefreshFrom(clip);

        Assert.NotNull(vm.SelectedDiagnostic);
        Assert.NotNull(vm.SelectedXmlSnippet);
    }

    // Info diagnostic (RawStep) appears first in source order, Warning
    // (unmodeled child on Beep) second — exercises that RefreshFrom actually
    // reorders rather than happening to already be sorted.
    private const string MixedSeverityXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"99999\" name=\"FutureFmStep\"/>" +
        "<Step enable=\"True\" id=\"93\" name=\"Beep\"><Mystery/></Step>" +
        "</fmxmlsnippet>";

    [Fact]
    public void RefreshFrom_SortsWorstSeverityFirst()
    {
        var vm = new ProblemsPanelViewModel();
        var clip = new ClipViewModel(Clip.FromXml("Mixed", "Mac-XMSS", MixedSeverityXml));

        vm.RefreshFrom(clip);

        Assert.Equal(2, vm.Diagnostics.Count);
        Assert.Equal(ParseDiagnosticSeverity.Warning, vm.Diagnostics[0].Severity);
        Assert.Equal(ParseDiagnosticSeverity.Info, vm.Diagnostics[1].Severity);
    }

    [Fact]
    public void HeaderText_ShowsSeverityBreakdown()
    {
        var vm = new ProblemsPanelViewModel();
        var clip = new ClipViewModel(Clip.FromXml("Mixed", "Mac-XMSS", MixedSeverityXml));

        vm.RefreshFrom(clip);

        Assert.Equal("Problems (1 warning, 1 info)", vm.HeaderText);
    }

    [Fact]
    public void HeaderText_WhenEmpty_ShowsZero()
    {
        var vm = new ProblemsPanelViewModel();

        Assert.Equal("Problems (0)", vm.HeaderText);
    }
}
