using Engine;
using Engine.Media;
using Game;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

public sealed class PonderLayoutTests
{
    static PonderLayoutTests()
    {
        ContentManager.AddContentReader(new Game.IContentReader.XmlReader());
        Add("PonderDialog.xml", "Dialogs/RealmPonderDialog.xml");
        Add("AndGate.xml", "RealmEX/Ponder/AndGate.xml");
        Add("Pumpkin.xml", "RealmEX/Ponder/Pumpkin.xml");
        static void Add(string resource, string target)
        {
            ContentInfo content = new(target);
            using Stream source = typeof(PonderLayoutTests).Assembly.GetManifestResourceStream(resource)!;
            MemoryStream stream = new(); source.CopyTo(stream); stream.Position = 0;
            content.SetContentStream(stream); ContentManager.Add(content);
        }
    }
    [Theory]
    [InlineData(1280, 720, "zh-CN")]
    [InlineData(1024, 768, "en-US")]
    [InlineData(800, 600, "zh-CN")]
    [InlineData(640, 480, "en-US")]
    [InlineData(480, 360, "zh-CN")]
    [InlineData(390, 844, "en-US")]
    public void SceneAndControlsFitThroughEveryKeyframe(int width, int height, string language)
    {
        LanguageControl.CurrentLanguageName = language;
        var dialog = new RealmPonderDialog(RealmPonderSamples.CreateAndGateTutorial());
        foreach (var keyframe in dialog.Player.Tutorial.Keyframes)
        {
            dialog.Player.Seek(Math.Min(keyframe.Tick + 20, dialog.Player.Tutorial.Duration));
            dialog.RefreshPresentation();
            dialog.Measure(new(width, height)); dialog.Arrange(Vector2.Zero, new(width, height));
            Widget viewport = dialog.Children.Find<RealmPonderWidget>("Ponder.Viewport");
            Assert.True(viewport.ActualSize.X >= 200 && viewport.ActualSize.Y >= 80, $"Viewport: {viewport.ActualSize}");
            foreach (string name in new[] { "Ponder.Header", "Ponder.Viewport", "Ponder.Footer" })
            {
                Widget widget = dialog.Children.Find<Widget>(name);
                Assert.True(widget.GlobalBounds.Min.X >= -0.1f && widget.GlobalBounds.Min.Y >= -0.1f, $"{name} starts outside: {widget.GlobalBounds}");
                Assert.True(widget.GlobalBounds.Max.X <= width + 0.1f && widget.GlobalBounds.Max.Y <= height + 0.1f, $"{name} ends outside: {widget.GlobalBounds}");
            }
            var play = dialog.Children.Find<RealmPonderButtonWidget>("Ponder.Play");
            Assert.True(play.ActualSize.Y >= 48);
            Assert.Same(play, dialog.HitTestGlobal((play.GlobalBounds.Min + play.GlobalBounds.Max) / 2));
            var timeline = dialog.Children.Find<RealmPonderTimelineWidget>("Ponder.Timeline");
            Assert.Equal(0, timeline.TickAt(0));
            Assert.Equal(dialog.Player.Tutorial.Duration, timeline.TickAt(timeline.ActualSize.X));
            Assert.InRange(timeline.TickAt(timeline.ActualSize.X / 2), dialog.Player.Tutorial.Duration / 2, dialog.Player.Tutorial.Duration / 2 + 1);
        }
    }
    [Fact]
    public void LongTextScrollsInsideTheCalloutWithoutResizingTheScene()
    {
        RealmPonderSceneBuilder builder = new("test:long", "Long instructions", new([]));
        builder.Text("caption", string.Concat(Enumerable.Repeat("Long instructions for a tutorial. ", 60)), Vector3.Zero, 200);
        RealmPonderDialog dialog = new(builder.Build());
        dialog.Player.Seek(10); dialog.RefreshPresentation(); dialog.Measure(new(800, 600)); dialog.Arrange(Vector2.Zero, new(800, 600));
        var viewport = dialog.Children.Find<RealmPonderWidget>("Ponder.Viewport");
        var callout = Assert.Single(viewport.Children);
        var scroll = Assert.Single(((ContainerWidget)callout).Children.OfType<ScrollPanelWidget>());
        Assert.True(scroll.Children[0].ActualSize.Y > scroll.ActualSize.Y);
        Assert.True(viewport.ActualSize.Y >= 200);
    }
}
