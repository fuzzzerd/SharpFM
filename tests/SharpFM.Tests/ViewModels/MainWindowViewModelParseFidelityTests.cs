using Avalonia.Media;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Model;
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

        vm.FileMakerClips.Add(new ClipViewModel(
            Clip.FromXml("Lossy", "Mac-XMSS", ParseFidelityTestXml.InfoOnlyStepXml)));
        vm.SelectedClip = vm.FileMakerClips[^1];

        Assert.False(vm.ParseFidelityIsLossless);
        Assert.Contains("issue", vm.ParseFidelitySummary);
        Assert.Contains("unknown step", vm.ParseFidelitySummary);
    }

    [Fact]
    public void NoSelection_HasNoFidelityGlyph()
    {
        var vm = CreateVm();

        Assert.Equal("", vm.ParseFidelityGlyph);
        Assert.Null(vm.ParseFidelityBrush);
    }

    [Fact]
    public void SelectedLosslessClip_HasNoFidelityGlyph()
    {
        var vm = CreateVm();
        vm.NewScriptCommand();

        Assert.Equal("", vm.ParseFidelityGlyph);
        Assert.Null(vm.ParseFidelityBrush);
    }

    [Fact]
    public void SelectedInfoOnlyClip_HasGrayGlyph()
    {
        var vm = CreateVm();

        vm.FileMakerClips.Add(new ClipViewModel(
            Clip.FromXml("Lossy", "Mac-XMSS", ParseFidelityTestXml.InfoOnlyStepXml)));
        vm.SelectedClip = vm.FileMakerClips[^1];

        Assert.Equal("i", vm.ParseFidelityGlyph);
        Assert.Equal(Brushes.Gray, vm.ParseFidelityBrush);
    }

    [Fact]
    public void SelectedWarningClip_HasOrangeGlyph()
    {
        var vm = CreateVm();

        vm.FileMakerClips.Add(new ClipViewModel(
            Clip.FromXml("Warning", "Mac-XMSS", ParseFidelityTestXml.WarningStepXml)));
        vm.SelectedClip = vm.FileMakerClips[^1];

        Assert.Equal("!", vm.ParseFidelityGlyph);
        Assert.Equal(Brushes.DarkOrange, vm.ParseFidelityBrush);
    }
}
