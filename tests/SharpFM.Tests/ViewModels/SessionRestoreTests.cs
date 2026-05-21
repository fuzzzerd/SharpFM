using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Model;
using SharpFM.Models;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

/// <summary>
/// Capture/Restore round-trip behaviour on <see cref="MainWindowViewModel"/>
/// for the session-restore feature. The clip repository for these tests is
/// empty (the default <c>%LocalAppData%/SharpFM/Clips</c> path returns no
/// clips in CI); each test seeds <see cref="MainWindowViewModel.FileMakerClips"/>
/// directly with the (folder, name) tuples it needs.
/// </summary>
public class SessionRestoreTests
{
    private static MainWindowViewModel CreateVm() =>
        new(
            NullLoggerFactory.Instance.CreateLogger<MainWindowViewModel>(),
            new MockClipboardService(),
            new MockFolderService());

    private static ClipViewModel SeedClip(MainWindowViewModel vm, string name, params string[] folder)
    {
        var clip = new ClipViewModel(Clip.FromXml(name, "Mac-XMSS", "<fmxmlsnippet/>"))
        {
            FolderPath = folder,
        };
        vm.FileMakerClips.Add(clip);
        return clip;
    }

    [Fact]
    public void CaptureSessionState_NoTabsOpen_ReturnsEmpty()
    {
        var vm = CreateVm();

        var state = vm.CaptureSessionState();

        Assert.Empty(state.OpenTabs);
        Assert.Null(state.ActiveTab);
    }

    [Fact]
    public void CaptureSessionState_RecordsTabsInOrder_AndActiveTab()
    {
        var vm = CreateVm();
        var a = SeedClip(vm, "Alpha");
        var b = SeedClip(vm, "Beta", "Drafts");
        var c = SeedClip(vm, "Gamma", "Drafts", "Old");

        vm.OpenTabs.OpenAsPermanent(a);
        vm.OpenTabs.OpenAsPermanent(b);
        vm.OpenTabs.OpenAsPermanent(c);
        vm.OpenTabs.ActiveTab = vm.OpenTabs.Tabs[1];

        var state = vm.CaptureSessionState();

        Assert.Equal(3, state.OpenTabs.Count);
        Assert.Equal("Alpha", state.OpenTabs[0].Name);
        Assert.Empty(state.OpenTabs[0].FolderPath);
        Assert.Equal("Beta", state.OpenTabs[1].Name);
        Assert.Equal(["Drafts"], state.OpenTabs[1].FolderPath);
        Assert.Equal(["Drafts", "Old"], state.OpenTabs[2].FolderPath);
        Assert.Equal("Beta", state.ActiveTab?.Name);
    }

    [Fact]
    public void RestoreSessionState_ResolvesByFolderAndName_OpensAndSetsActive()
    {
        var vm = CreateVm();
        SeedClip(vm, "Alpha");
        SeedClip(vm, "Beta", "Drafts");

        var state = new SessionState(
            OpenTabs:
            [
                new TabRef([], "Alpha"),
                new TabRef(["Drafts"], "Beta"),
            ],
            ActiveTab: new TabRef(["Drafts"], "Beta"));

        vm.RestoreSessionState(state);

        Assert.Equal(2, vm.OpenTabs.Tabs.Count);
        Assert.Equal("Alpha", vm.OpenTabs.Tabs[0].Clip.Clip.Name);
        Assert.Equal("Beta", vm.OpenTabs.Tabs[1].Clip.Clip.Name);
        Assert.Equal("Beta", vm.OpenTabs.ActiveTab?.Clip.Clip.Name);
    }

    [Fact]
    public void RestoreSessionState_RestoredTabsAreNotPreview()
    {
        var vm = CreateVm();
        SeedClip(vm, "Alpha");

        vm.RestoreSessionState(new SessionState([new TabRef([], "Alpha")], null));

        Assert.False(vm.OpenTabs.Tabs[0].IsPreview);
    }

    [Fact]
    public void RestoreSessionState_SkipsMissingClipsSilently()
    {
        var vm = CreateVm();
        SeedClip(vm, "Alpha");

        var state = new SessionState(
            OpenTabs:
            [
                new TabRef([], "Alpha"),
                new TabRef([], "Gone"),
                new TabRef(["Wrong"], "Alpha"),
            ],
            ActiveTab: null);

        vm.RestoreSessionState(state);

        Assert.Single(vm.OpenTabs.Tabs);
        Assert.Equal("Alpha", vm.OpenTabs.Tabs[0].Clip.Clip.Name);
    }

    [Fact]
    public void RestoreSessionState_AllClipsMissing_LeavesEditorEmpty()
    {
        var vm = CreateVm();

        var state = new SessionState(
            OpenTabs: [new TabRef([], "Gone"), new TabRef([], "AlsoGone")],
            ActiveTab: new TabRef([], "Gone"));

        vm.RestoreSessionState(state);

        Assert.Empty(vm.OpenTabs.Tabs);
        Assert.Null(vm.OpenTabs.ActiveTab);
    }

    [Fact]
    public void RestoreSessionState_MissingActive_LeavesLastRestoredActive()
    {
        var vm = CreateVm();
        SeedClip(vm, "Alpha");
        SeedClip(vm, "Beta");

        var state = new SessionState(
            OpenTabs: [new TabRef([], "Alpha"), new TabRef([], "Beta")],
            ActiveTab: new TabRef([], "Gone"));

        vm.RestoreSessionState(state);

        Assert.Equal(2, vm.OpenTabs.Tabs.Count);
        // OpenAsPermanent sets ActiveTab to whatever was just opened; with the
        // saved active unresolved, we leave that natural last-opened state alone.
        Assert.Equal("Beta", vm.OpenTabs.ActiveTab?.Clip.Clip.Name);
    }

    [Fact]
    public void Capture_then_Restore_RoundTripsThroughEmpty()
    {
        var vm1 = CreateVm();
        var a = SeedClip(vm1, "Alpha");
        var b = SeedClip(vm1, "Beta", "Folder");
        vm1.OpenTabs.OpenAsPermanent(a);
        vm1.OpenTabs.OpenAsPermanent(b);
        vm1.OpenTabs.ActiveTab = vm1.OpenTabs.Tabs[0];
        var captured = vm1.CaptureSessionState();

        var vm2 = CreateVm();
        SeedClip(vm2, "Alpha");
        SeedClip(vm2, "Beta", "Folder");
        vm2.RestoreSessionState(captured);

        Assert.Equal(2, vm2.OpenTabs.Tabs.Count);
        Assert.Equal("Alpha", vm2.OpenTabs.ActiveTab?.Clip.Clip.Name);
    }
}
