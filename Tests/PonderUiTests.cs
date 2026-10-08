using Engine;
using Game;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

public sealed class PonderUiTests
{
    [Fact]
    public void CursorMovesBetweenSimulationTicksAndHonorsPauseSeekAndReplay()
    {
        var builder = new RealmPonderSceneBuilder("test:cursor", "Cursor", new([]));
        builder.ShowUi(new("Machine", new(614, 382), () => new Machine())).UiDrag("Run", "Meter", 12).Idle(20);
        var player = new RealmPonderPlayer(builder.Build());
        for (int frame = 1; frame <= 36; frame++)
        {
            player.Advance(1d / 60);
            Assert.Equal(frame / 3, player.State.Tick);
            Assert.Equal(frame / 36f, player.State.Ui.Cue.Progress(player.PresentationTick), 5);
        }
        player.Seek(0); player.Advance(1d / 120);
        float paused = player.PresentationTick;
        Assert.True(paused > 0); Assert.Equal(0, player.State.Tick);
        player.IsPaused = true; player.Advance(10); Assert.Equal(paused, player.PresentationTick);
        player.Seek(5); Assert.Equal(5, player.PresentationTick); Assert.True(player.IsPaused);
        player.Replay(); Assert.Equal(0, player.PresentationTick);
        player.Advance(10); Assert.Equal(player.Tutorial.Duration, player.PresentationTick);
    }
    [Fact]
    public void CursorInterpolationUsesReadingSpeedWithoutChangingScriptTiming()
    {
        var builder = new RealmPonderSceneBuilder("test:reading-cursor", "Cursor", new([]));
        builder.ShowUi(new("Machine", new(614, 382), () => new Machine())).UiDrag("Run", "Meter", 12)
            .Text("caption", "Read", Vector3.Zero, 20).Idle(20);
        var player = new RealmPonderPlayer(builder.Build()) { ComfyReading = true, Speed = 2 };
        player.Advance(.025);
        Assert.Equal(0, player.State.Tick); Assert.Equal(1f / 3, player.PresentationTick, 5);
        Assert.Equal(1f / 36, player.State.Ui.Cue.Progress(player.PresentationTick), 5);
    }
    sealed class Machine : CanvasWidget
    {
        public int Clicks;
        public int Layouts;
        public readonly ClickableWidget Run = new() { Name = "Run", IsAutoCheckingEnabled = true };
        public readonly ValueBarWidget Meter = new() { Name = "Meter" };
        public Machine() { Size = new(614, 382); Children.Add(Run); Children.Add(Meter); }
        public override void Update() { if (Run.IsClicked) Clicks++; }
        public override void MeasureOverride(Vector2 available) { Layouts++; base.MeasureOverride(available); }
    }
    [Fact]
    public void SynchronizingCursorFramesDoesNotRepeatFullUiLayout()
    {
        var builder = new RealmPonderSceneBuilder("test:layout", "UI", new([]));
        builder.ShowUi(new("Machine", new(614, 382), () => new Machine())).UiValue("Meter", .5f)
            .UiDrag("Run", "Meter", 12).Idle(20);
        var player = new RealmPonderPlayer(builder.Build());
        using var view = new RealmPonderUiWidget();
        view.Synchronize(player.State.Ui, player.PresentationTick, "en-US");
        var machine = (Machine)view.Content;
        int initialLayouts = machine.Layouts;
        Assert.True(initialLayouts > 0);
        for (int i = 0; i < 30; i++) { player.Advance(1d / 60); view.Synchronize(player.State.Ui, player.PresentationTick, "en-US"); }
        Assert.Equal(initialLayouts, machine.Layouts); Assert.Equal(.5f, machine.Meter.Value);
    }
    [Fact]
    public void UiClickUsesOriginalHandlerOnceAndBackseekCreatesFreshState()
    {
        var builder = new RealmPonderSceneBuilder("test:ui", "UI", new([]));
        builder.ShowUi(new("Machine", new(614, 382), () => new Machine())).Idle(10).UiClick("Run", 20).Idle(30);
        var player = new RealmPonderPlayer(builder.Build());
        using var view = new RealmPonderUiWidget();
        view.Synchronize(player.State.Ui, 0, "en-US");
        Machine original = (Machine)view.Content;
        player.Seek(19); view.Synchronize(player.State.Ui, player.State.Tick, "en-US");
        Assert.Equal(0, original.Clicks);
        player.Seek(20); view.Synchronize(player.State.Ui, player.State.Tick, "en-US");
        Assert.Equal(1, original.Clicks); Assert.True(original.Run.IsChecked); Assert.False(original.Run.IsClicked);
        view.Synchronize(player.State.Ui, player.State.Tick, "en-US"); Assert.Equal(1, original.Clicks);
        player.Seek(5); view.Synchronize(player.State.Ui, player.State.Tick, "en-US");
        var replay = (Machine)view.Content;
        Assert.NotSame(original, replay); Assert.Equal(0, replay.Clicks); Assert.False(replay.Run.IsChecked);
        player.Seek(20); view.Synchronize(player.State.Ui, player.State.Tick, "en-US"); Assert.Equal(1, replay.Clicks);
    }
    [Fact]
    public void UiValueAnimationPauseAndHideShareTheSceneClock()
    {
        var builder = new RealmPonderSceneBuilder("test:ui", "UI", new([]));
        builder.ShowUi(new("Machine", new(614, 382), () => new Machine())).UiAnimateValue("Meter", 0, 1, 20).Idle(25).HideUi().Idle(5);
        var player = new RealmPonderPlayer(builder.Build());
        using var view = new RealmPonderUiWidget();
        player.Seek(10); view.Synchronize(player.State.Ui, player.State.Tick, "en-US");
        Assert.Equal(.5f, ((Machine)view.Content).Meter.Value);
        player.IsPaused = true; player.Advance(2); view.Synchronize(player.State.Ui, player.State.Tick, "en-US");
        Assert.Equal(.5f, ((Machine)view.Content).Meter.Value);
        player.Seek(20); view.Synchronize(player.State.Ui, player.State.Tick, "en-US"); Assert.Equal(1f, ((Machine)view.Content).Meter.Value);
        player.Seek(25); view.Synchronize(player.State.Ui, player.State.Tick, "en-US"); Assert.Null(view.Content); Assert.False(view.IsVisible);
    }
    [Fact]
    public void ScriptConfiguresOnlyTheNamedNestedWidgetAndReplaysItsInstanceProperties()
    {
        CanvasWidget Create() {
            CanvasWidget root = new();
            foreach (string name in new[] { "Left", "Right" }) {
                CanvasWidget panel = new() { Name = name };
                panel.Children.Add(new ValueBarWidget { Name = "Meter" }); root.Children.Add(panel);
            }
            return root;
        }
        var builder = new RealmPonderSceneBuilder("test:configure", "Configure", new([]));
        RealmPonderScript.Compile(builder, "scene.uiShow('machine'); scene.idle(10); scene.uiConfigure('Right/Meter', {Value:'0.75', Margin:'2, 3'}); scene.idle(10);",
            "configure.pjs", new Dictionary<string, int>(), new Dictionary<string, RealmPonderUiDefinition> { ["machine"] = new("Machine", new(614, 382), Create) });
        var player = new RealmPonderPlayer(builder.Build());
        using var view = new RealmPonderUiWidget();
        player.Seek(15); view.Synchronize(player.State.Ui, player.State.Tick, "en-US");
        ValueBarWidget Meter(string panel) => view.Content.Children.Find<CanvasWidget>(panel).Children.Find<ValueBarWidget>("Meter");
        Assert.Equal(.75f, Meter("Right").Value); Assert.Equal(new Vector2(2, 3), Meter("Right").Margin);
        Assert.Equal(0f, Meter("Left").Value);
        player.Seek(5); view.Synchronize(player.State.Ui, player.State.Tick, "en-US"); Assert.Equal(0f, Meter("Right").Value);
        player.Seek(15); view.Synchronize(player.State.Ui, player.State.Tick, "en-US"); Assert.Equal(.75f, Meter("Right").Value);
    }
}
