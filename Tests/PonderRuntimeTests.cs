using Engine;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

public sealed class PonderRuntimeTests
{
    private static readonly Point3 m_cell = new(0, 1, 0);
    private static readonly RealmPonderLocalization m_text = new(new Dictionary<string, string> {
        ["en-US"] = """{"scene":"Scene","logic":"Logic"}""", ["zh-CN"] = """{"scene":"场景","logic":"逻辑"}""" });
    private static RealmPonderSceneBuilder Builder() => new("test:scene", m_text.Text("scene"), new(new Dictionary<Point3, int> { [m_cell] = 5 }));
    [Fact]
    public void IdleDoesNotBlockConcurrentWorldCameraAndOverlayAnimations()
    {
        var b = Builder();
        b.IndependentSection("moving", new([m_cell])).ShowSection("moving", new(0, 2, 0), 20)
            .MoveSection("moving", new(10, 0, 0), 20).RotateCamera(90, 20).Text("note", "Hello", Vector3.Zero, 12).Idle(10)
            .SetBlocks(new([m_cell]), 7).Idle(10);
        var player = new RealmPonderPlayer(b.Build());
        player.Advance(0.5);
        Assert.Equal(10, player.State.Tick); Assert.Equal(7, player.State.Blocks[m_cell]);
        Assert.Equal(0.5f, player.State.Sections["moving"].Opacity, 3);
        Assert.Equal(new Vector3(5, 0, 0), player.State.Sections["moving"].Offset);
        Assert.Equal(new Vector3(0, 1, 0), player.State.Sections["moving"].RevealOffset);
        Assert.Equal(80, player.State.CameraYaw, 3); Assert.Single(player.State.Overlays);
        player.Advance(0.5); Assert.Empty(player.State.Overlays); Assert.True(player.IsCompleted);
        Assert.Equal(new Vector3(10, 0, 0), player.State.Sections["moving"].Offset);
    }
    [Fact]
    public void BackwardSeekRestoresBlocksAndMatchesContinuousPlayback()
    {
        var b = Builder();
        b.ShowSection("base", duration: 0).MoveSection("base", new(4, 0, 0), 40).Text("a", "A", Vector3.Zero, 15)
            .Idle(20).Keyframe("B").SetBlocks(new([m_cell]), 9).Text("b", "B", Vector3.One, 30).Idle(20).RestoreBlocks(new([m_cell])).Idle(10);
        var tutorial = b.Build(); var player = new RealmPonderPlayer(tutorial);
        player.Seek(45); Assert.Equal(5, player.State.Blocks[m_cell]);
        player.IsPaused = true; player.Seek(25);
        var continuous = new RealmPonderPlayer(tutorial); continuous.Advance(1.25);
        Assert.True(player.IsPaused); Assert.Equal(continuous.State.Tick, player.State.Tick);
        Assert.Equal(continuous.State.Blocks[m_cell], player.State.Blocks[m_cell]);
        Assert.Equal(continuous.State.Sections["base"].Transform, player.State.Sections["base"].Transform);
        Assert.Equal(continuous.State.Overlays.Keys, player.State.Overlays.Keys);
        player.Seek(0); Assert.Equal(5, player.State.Blocks[m_cell]); Assert.False(player.IsCompleted);
    }
    [Fact]
    public void PausingAndReadingUseTheSameClockForEverything()
    {
        var b = Builder().Text("note", "Read", Vector3.Zero, 20).Idle(40);
        var player = new RealmPonderPlayer(b.Build()) { ComfyReading = true };
        player.Advance(1.5); Assert.Equal(10, player.State.Tick);
        player.IsPaused = true; player.Advance(10); Assert.Equal(10, player.State.Tick);
        player.IsPaused = false; player.Advance(1.5); Assert.Equal(20, player.State.Tick);
        player.Advance(0.5); Assert.Equal(30, player.State.Tick);
        player.Replay(); Assert.Equal(0, player.State.Tick); Assert.False(player.IsPaused);
    }
    [Fact]
    public void FramePartitioningDoesNotChangeTheResult()
    {
        var tutorial = Builder().Text("a", "A", Vector3.Zero, 17).Idle(200).Build();
        var a = new RealmPonderPlayer(tutorial) { ComfyReading = true };
        var b = new RealmPonderPlayer(tutorial) { ComfyReading = true };
        a.Advance(4); for (int i = 0; i < 240; i++) b.Advance(1.0 / 60);
        Assert.Equal(a.State.Tick, b.State.Tick);
    }
    [Fact]
    public void RaycastUsesSectionTransformsAndIgnoresHiddenSections()
    {
        var b = Builder().ShowSection("base", duration: 0).MoveSection("base", new(4, 0, 0), 0).Idle(20).HideSection("base", duration: 0);
        var player = new RealmPonderPlayer(b.Build());
        Assert.Null(player.State.Raycast(new(new(0.5f, 1.5f, 5), -Vector3.UnitZ)));
        Assert.Equal(m_cell, player.State.Raycast(new(new(4.5f, 1.5f, 5), -Vector3.UnitZ))!.Value.Position);
        player.Seek(20); Assert.Null(player.State.Raycast(new(new(4.5f, 1.5f, 5), -Vector3.UnitZ)));
    }
    [Fact]
    public void SelectionSetOperationsAreInclusiveAndImmutable()
    {
        var a = RealmPonderSelection.Box(new(2, 2, 2), new(0, 0, 0));
        Assert.Equal(27, a.Count); Assert.Equal(9, a.Layers(1, 1).Count);
        var removed = a.Subtract(RealmPonderSelection.At(1, 1, 1));
        Assert.Equal(26, removed.Count); Assert.Equal(27, a.Count);
        Assert.Equal(new Vector3(1.5f), a.Center);
    }
    [Fact]
    public void NamedOverlaysReplaceAndAnimationsDoNotFightTheirSuccessors()
    {
        var b = Builder().MoveSection("base", new(10, 0, 0), 100).Text("note", "Old", Vector3.Zero, 100).Idle(10)
            .MoveSection("base", new(0, 5, 0), 10).Text("note", "New", Vector3.Zero, 5).Idle(15);
        var player = new RealmPonderPlayer(b.Build()); player.Seek(15);
        Assert.Empty(player.State.Overlays); player.Seek(20); var position = player.State.Sections["base"].Offset;
        player.Seek(90); Assert.Equal(position, player.State.Sections["base"].Offset);
    }
    [Fact]
    public void RegistrySharesOrderingBetweenIndexAndSubjectsAndRejectsDuplicateIds()
    {
        RealmPonderRegistry registry = new(); var tutorial = Builder().Idle(20).Build();
        registry.RegisterTag(new("test:logic", m_text.Text("logic"), ""));
        registry.Register(tutorial, ["test:logic"], [5]);
        Assert.Single(registry.Search("逻辑", language: "zh-CN")); Assert.Single(registry.Search("Scene"));
        Assert.Empty(registry.Search("missing")); Assert.Same(tutorial, Assert.Single(registry.ForSubject(5)));
        Assert.Throws<ArgumentException>(() => registry.Register(tutorial));
    }
    [Fact]
    public void ItemActorsReplayTheirFullTransformAndRemoval()
    {
        var b = Builder().CreateItem("item", 5, Vector3.Zero).MoveActor("item", new(4, 0, 0), 20)
            .RotateActor("item", new(0, 90, 0), 20).Idle(20).RemoveActor("item").Idle(10);
        var player = new RealmPonderPlayer(b.Build()); player.Seek(25); Assert.Empty(player.State.Actors);
        player.Seek(10); var actor = player.State.Actors["item"];
        Assert.Equal(new Vector3(2, 0, 0), actor.Position); Assert.Equal(new Vector3(0, 45, 0), actor.Rotation);
    }
    [Fact]
    public void ChapterSequenceStaysWithinItsSeriesAndUsesChapterOrder()
    {
        RealmPonderRegistry registry = new();
        registry.RegisterSeries(new("test:reactor", "Reactor")); registry.RegisterSeries(new("test:other", "Other"));
        RealmPonderTutorial Chapter(string id) => new RealmPonderSceneBuilder("test:" + id, id, new([])).Idle(20).Build();
        registry.Register(Chapter("shutdown"), order: 3, series: "test:reactor");
        registry.Register(Chapter("build"), order: 1, series: "test:reactor");
        registry.Register(Chapter("hidden"), order: 2, series: "test:reactor", showInIndex: false);
        registry.Register(Chapter("elsewhere"), series: "test:other"); registry.Register(Chapter("standalone"));
        Assert.Equal(new[] { "test:build", "test:shutdown" }, registry.Sequence("test:build").Select(t => t.Id));
        Assert.Equal(new[] { "test:build", "test:hidden", "test:shutdown" }, registry.Sequence("test:hidden").Select(t => t.Id));
        Assert.Equal("test:standalone", Assert.Single(registry.Sequence("test:standalone")).Id);
        Assert.Equal(2, registry.Search("Reactor").Count);
    }
    [Fact]
    public void ReconstructingRuntimeReceivesResetBeforeTimeZeroInstructions()
    {
        var tutorial = Builder().Idle(5).SetBlocks(new([m_cell]), 8).Idle(5).Build();
        var player = new RealmPonderPlayer(tutorial); List<string> events = [];
        player.Resetting += state => events.Add($"reset:{state.Tick}");
        player.StateChanged += state => events.Add($"state:{state.Tick}:{state.Blocks[m_cell]}");
        player.Seek(10); events.Clear(); player.Seek(5);
        Assert.Equal("reset:0", events[0]); Assert.Equal("state:0:5", events[1]); Assert.Equal("state:5:8", events[^1]);
    }
    [Fact]
    public void SchematicLoadResolvesPaletteAndRestoresAnIndependentSnapshot()
    {
        var xml = System.Xml.Linq.XElement.Parse("<PonderSchematic Version='1'><Fill From='-2,1,0' To='2,1,0' Block='stone'/><Fill From='0,1,0' Block='ore'/></PonderSchematic>");
        var schematic = RealmPonderSchematic.Load(xml, name => name == "stone" ? 3 : 9);
        Assert.Equal(5, schematic.Blocks.Count); Assert.Equal(9, schematic.Blocks[m_cell]);
        var b = new RealmPonderSceneBuilder("test:restore", "Restore", schematic).SetBlocks(schematic.Selection, 0).Idle(10).RestoreBlocks(schematic.Selection);
        var player = new RealmPonderPlayer(b.Build()); Assert.Equal(0, player.State.Blocks[m_cell]);
        player.Seek(10); Assert.Equal(9, player.State.Blocks[m_cell]); Assert.Equal(3, player.State.Blocks[new(-2, 1, 0)]);
    }
    [Fact]
    public void PluginRollbackPreservesExistingRegistrations()
    {
        var registry = new RealmPonderRegistry(); registry.RegisterTag(new("test:existing", "Keep", ""));
        Assert.Throws<InvalidOperationException>(() => registry.RegisterPlugin(new FailingPlugin()));
        Assert.Equal("test:existing", Assert.Single(registry.Tags).Id);
    }
    private sealed class FailingPlugin : IRealmPonderPlugin
    {
        public string Namespace => "test";
        public void Register(RealmPonderRegistry registry) { registry.RegisterTag(new("test:new", "Temporary", "")); throw new InvalidOperationException(); }
    }
    [Fact]
    public void BoundsChaseIsAnimatedAndRemovalCancelsTheTrack()
    {
        var b = Builder().ChaseBounds("bounds", new(Vector3.Zero, Vector3.One), 40, 0, Color.White).Idle(10)
            .ChaseBounds("bounds", new(new Vector3(4), new Vector3(5)), 30, 20, Color.White).Idle(12).RemoveOverlay("bounds").Idle(20);
        var player = new RealmPonderPlayer(b.Build()); player.Seek(20);
        Assert.Equal(new Vector3(2), player.State.Overlays["bounds"].Position);
        player.Seek(25); Assert.Empty(player.State.Overlays);
        player.Seek(35); Assert.Empty(player.State.Overlays);
    }
    [Fact]
    public void IndexExclusionStillAllowsRelatedItemLookup()
    {
        RealmPonderRegistry registry = new(); var tutorial = Builder().Idle(10).Build();
        registry.Register(tutorial, subjects: [5], showInIndex: false);
        Assert.Empty(registry.Search()); Assert.Single(registry.ForSubject(5)); Assert.Single(registry.Search(includeHidden: true));
    }
    [Fact]
    public void VisualInterpolationDoesNotAdvanceSimulationAndSeekReturnsExactTick()
    {
        var player = new RealmPonderPlayer(Builder().MoveSection("base", new(10, 0, 0), 20).Build());
        player.Advance(0.525); Assert.Equal(10, player.State.Tick); Assert.True(player.State.Sections["base"].Offset.X > 5);
        player.Seek(10); Assert.Equal(5, player.State.Sections["base"].Offset.X, 4);
    }
}
