using System.Xml.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Model;
using SharpFM.ViewModels;
using Xunit;

namespace SharpFM.Tests.ViewModels;

public class MainWindowClipboardTests
{
    [Theory]
    [InlineData("normal", "Mac-XMSS", false)]
    [InlineData("script", "Mac-XMSC", true)]
    [InlineData("steps", "Mac-XMSS", false)]
    public async Task CopyCommands_UseServiceAndPreserveWrapperChoices(string command, string expectedFormat, bool wrapper)
    {
        const string step = "<Step enable=\"True\" id=\"93\" name=\"Beep\"/>";
        var clipboard = new MockClipboardService();
        var vm = new MainWindowViewModel(NullLogger.Instance, clipboard, new MockFolderService());
        vm.SelectedClip = new ClipViewModel(command == "steps"
            ? Clip.FromXml("Example", "Mac-XMSC", $"<fmxmlsnippet><Script name=\"Example\">{step}</Script></fmxmlsnippet>")
            : Clip.FromXml("Example", "Mac-XMSS", $"<fmxmlsnippet>{step}</fmxmlsnippet>"));

        switch (command)
        {
            case "normal": await vm.CopySelectedToClip(); break;
            case "script": await vm.CopyAsScript(); break;
            default: await vm.CopyAsScriptSteps(); break;
        }

        Assert.Equal(expectedFormat, clipboard.LastClip!.FormatId);
        var xml = XDocument.Parse(clipboard.LastClip.Xml);
        Assert.Equal(wrapper, xml.Root!.Element("Script") is not null);
        Assert.Single(xml.Descendants("Step"));
        if (wrapper) Assert.Equal("Example", (string?)xml.Root.Element("Script")!.Attribute("name"));
        Assert.Contains("Copied", vm.StatusMessage);
    }

    [Fact]
    public async Task NormalCopy_UsesCurrentEditorXml()
    {
        var clipboard = new MockClipboardService();
        var vm = new MainWindowViewModel(NullLogger.Instance, clipboard, new MockFolderService());
        vm.SelectedClip = new ClipViewModel(Clip.FromXml("Values", "Mac-XMVL", "<fmxmlsnippet><Value>old</Value></fmxmlsnippet>"));
        vm.SelectedClip.XmlDocument.Text = "<fmxmlsnippet><Value>édité 日本語</Value></fmxmlsnippet>";
        await vm.CopySelectedToClip();
        Assert.Equal("édité 日本語", XDocument.Parse(clipboard.LastClip!.Xml).Root!.Element("Value")!.Value);
    }
}
