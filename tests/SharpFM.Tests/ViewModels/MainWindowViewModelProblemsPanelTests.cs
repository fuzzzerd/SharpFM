using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Model;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class MainWindowViewModelProblemsPanelTests
{
    private static MainWindowViewModel CreateVm() =>
        new(NullLoggerFactory.Instance.CreateLogger("test"),
            new MockClipboardService(),
            new MockFolderService());

    // RawStep ⇒ UnknownStep diagnostic; same fixture as MainWindowViewModelParseFidelityTests.
    private const string LossyXml =
        "<fmxmlsnippet type=\"FMObjectList\">" +
        "<Step enable=\"True\" id=\"99999\" name=\"FutureFmStep\"/>" +
        "</fmxmlsnippet>";

    [Fact]
    public void NoSelection_ProblemsPanelIsEmpty()
    {
        var vm = CreateVm();

        Assert.Empty(vm.ProblemsPanel.Diagnostics);
    }

    [Fact]
    public void SelectingLossyClip_PopulatesProblemsPanel()
    {
        var vm = CreateVm();
        vm.FileMakerClips.Add(new ClipViewModel(Clip.FromXml("Lossy", "Mac-XMSS", LossyXml)));

        vm.SelectedClip = vm.FileMakerClips[^1];

        Assert.Single(vm.ProblemsPanel.Diagnostics);
    }

    [Fact]
    public void SwitchingToLosslessClip_ClearsProblemsPanel()
    {
        var vm = CreateVm();
        vm.FileMakerClips.Add(new ClipViewModel(Clip.FromXml("Lossy", "Mac-XMSS", LossyXml)));
        vm.SelectedClip = vm.FileMakerClips[^1];
        Assert.Single(vm.ProblemsPanel.Diagnostics);

        vm.NewScriptCommand();

        Assert.Empty(vm.ProblemsPanel.Diagnostics);
    }

    [Fact]
    public void ToggleProblemsPanel_FlipsVisibility()
    {
        var vm = CreateVm();
        Assert.False(vm.ProblemsPanel.IsPanelVisible);

        vm.ToggleProblemsPanel();
        Assert.True(vm.ProblemsPanel.IsPanelVisible);

        vm.ToggleProblemsPanel();
        Assert.False(vm.ProblemsPanel.IsPanelVisible);
    }
}
