using Engine;
using Game;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

public sealed class PonderUiTests
{
    sealed class Machine : CanvasWidget
    {
        public int Clicks;
        public readonly ClickableWidget Run = new() { Name = "Run", IsAutoCheckingEnabled = true };
        public readonly ValueBarWidget Meter = new() { Name = "Meter" };
        public Machine() { Size = new(614, 382); Children.Add(Run); Children.Add(Meter); }
        public override void Update() { if (Run.IsClicked) Clicks++; }
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
