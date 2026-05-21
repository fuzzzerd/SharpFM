using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using SharpFM.Models;
using SharpFM.Services;
using Xunit;

namespace SharpFM.Tests.Services;

public class SessionStateServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly SessionStateService _svc;

    public SessionStateServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"sharpfm-session-{Guid.NewGuid()}");
        _svc = new SessionStateService(NullLogger.Instance, Path.Combine(_dir, "session.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_NoFile_ReturnsEmptyState()
    {
        var state = _svc.Load();

        Assert.Empty(state.OpenTabs);
        Assert.Null(state.ActiveTab);
    }

    [Fact]
    public void RoundTrip_PreservesOpenTabsAndActive()
    {
        var state = new SessionState(
            OpenTabs:
            [
                new TabRef(["Marketing"], "Welcome"),
                new TabRef([], "Notes"),
                new TabRef(["Marketing", "Drafts"], "Launch"),
            ],
            ActiveTab: new TabRef([], "Notes"));

        _svc.Save(state);
        var loaded = _svc.Load();

        Assert.Equal(3, loaded.OpenTabs.Count);
        Assert.Equal(["Marketing"], loaded.OpenTabs[0].FolderPath);
        Assert.Equal("Welcome", loaded.OpenTabs[0].Name);
        Assert.Equal("Notes", loaded.ActiveTab?.Name);
    }

    [Fact]
    public void RoundTrip_NullActiveTab_Preserved()
    {
        var state = new SessionState([new TabRef([], "Only")], ActiveTab: null);

        _svc.Save(state);
        var loaded = _svc.Load();

        Assert.Single(loaded.OpenTabs);
        Assert.Null(loaded.ActiveTab);
    }

    [Fact]
    public void Save_CreatesParentDirectory_WhenAbsent()
    {
        // _dir doesn't exist yet because we haven't saved anything
        Assert.False(Directory.Exists(_dir));

        _svc.Save(new SessionState([new TabRef([], "X")], null));

        Assert.True(Directory.Exists(_dir));
    }

    [Fact]
    public void Load_MalformedJson_ReturnsEmptyState()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "session.json"), "not json");

        var state = _svc.Load();

        Assert.Empty(state.OpenTabs);
        Assert.Null(state.ActiveTab);
    }

    [Fact]
    public void Load_PartialJsonMissingOpenTabs_YieldsEmptyOpenTabs()
    {
        // System.Text.Json fills missing record fields with default (null for
        // reference types), so a hand-edited file like `{}` would otherwise
        // produce a SessionState with a null OpenTabs and break the restore
        // path's iteration. Load coerces.
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "session.json"), "{}");

        var state = _svc.Load();

        Assert.NotNull(state.OpenTabs);
        Assert.Empty(state.OpenTabs);
        Assert.Null(state.ActiveTab);
    }

    [Fact]
    public void Save_IOFailure_DoesNotThrow()
    {
        // Point the service at a path whose parent cannot be created (a file
        // standing where a directory needs to exist). Save must swallow the
        // resulting IOException — failure to persist must not crash app exit.
        var blocker = Path.Combine(Path.GetTempPath(), $"sharpfm-blocker-{Guid.NewGuid()}");
        File.WriteAllText(blocker, "");
        try
        {
            var bad = new SessionStateService(NullLogger.Instance, Path.Combine(blocker, "session.json"));
            var ex = Record.Exception(() => bad.Save(new SessionState([], null)));
            Assert.Null(ex);
        }
        finally
        {
            if (File.Exists(blocker)) File.Delete(blocker);
        }
    }
}
