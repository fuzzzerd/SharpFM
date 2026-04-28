using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class MainWindowViewModelParseFidelityTests
{
    private static MainWindowViewModel CreateVm() =>
        new(NullLoggerFactory.Instance.CreateLogger("test"),
            new MockClipboardService(),
            new MockFolderService());

    [Fact]
    public void NoSelection_ParseFidelityNotVisible()
    {
        var vm = CreateVm();
        Assert.False(vm.ParseFidelityVisible);
    }

    [Fact]
    public void SelectedLosslessClip_SummaryReadsLossless()
    {
        var vm = CreateVm();
        vm.NewScriptCommand();

        Assert.True(vm.ParseFidelityVisible);
        Assert.True(vm.ParseFidelityIsLossless);
        Assert.Equal("Parsed losslessly", vm.ParseFidelitySummary);
    }

    [Fact]
    public void SelectedLossyClip_SummaryEnumeratesIssues()
    {
        var vm = CreateVm();

        // RawStep ⇒ UnknownStep diagnostic; survives lossless XML round-trip
        // but the report calls it out.
        const string lossyXml =
            "<fmxmlsnippet type=\"FMObjectList\">" +
            "<Step enable=\"True\" id=\"99999\" name=\"FutureFmStep\"/>" +
            "</fmxmlsnippet>";

        vm.FileMakerClips.Add(new ClipViewModel(
            SharpFM.Model.Clip.FromXml("Lossy", "Mac-XMSS", lossyXml)));
        vm.SelectedClip = vm.FileMakerClips[^1];

        Assert.False(vm.ParseFidelityIsLossless);
        Assert.Contains("issue", vm.ParseFidelitySummary);
        Assert.Contains("unknown step", vm.ParseFidelitySummary);
    }
}
