using Engine;
using Engine.Media;
using Game;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

// Exercise the actual host layout engine and shipped widget tree without a graphics device.
public sealed class PonderLayoutTests
{
    static PonderLayoutTests()
    {
        LabelWidget.BitmapFont = new BitmapFont(null,
            [new BitmapFont.Glyph('?', Vector2.Zero, Vector2.One, Vector2.Zero, 24f)],
            '?', 28f, Vector2.Zero, 1f);
        ContentManager.AddContentReader(new Game.IContentReader.XmlReader());
        ContentInfo content = new("Dialogs/RealmPonderDialog.xml");
        using Stream source = typeof(PonderLayoutTests).Assembly.GetManifestResourceStream("PonderDialog.xml")!;
        MemoryStream stream = new();
        source.CopyTo(stream);
        stream.Position = 0;
        content.SetContentStream(stream);
        ContentManager.Add(content);
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1024, 768)]
    [InlineData(800, 600)]
    [InlineData(640, 480)]
    [InlineData(480, 360)]
    [InlineData(390, 844)]
    public void SceneAndTransportRemainVisibleAtDifferentWindowSizes(int width, int height)
    {
        RealmPonderDialog dialog = new(RealmPonderSamples.CreateAndGateTutorial());
        dialog.Children.Find<LabelWidget>("Ponder.StepTitle").Text = "同时开启：1 与 1";
        dialog.Children.Find<LabelWidget>("Ponder.Caption").Text = "现在两个输入都为 1，输出终于接通，指示灯以绿色表示开启。";
        dialog.Measure(new Vector2(width, height));
        dialog.Arrange(Vector2.Zero, new Vector2(width, height));

        Widget viewport = dialog.Children.Find<RealmPonderWidget>("Ponder.Viewport");
        Assert.True(viewport.ActualSize.X >= 200f, $"Viewport width: {viewport.ActualSize.X}");
        Assert.True(viewport.ActualSize.Y >= 100f, $"Viewport height: {viewport.ActualSize.Y}");
        foreach (string name in new[] { "Ponder.Header", "Ponder.Note", "Ponder.Viewport", "Ponder.Footer" })
        {
            Widget widget = dialog.Children.Find<Widget>(name);
            Assert.True(widget.GlobalBounds.Min.X >= -0.1f && widget.GlobalBounds.Min.Y >= -0.1f, $"{name} begins outside window: {widget.GlobalBounds}");
            Assert.True(widget.GlobalBounds.Max.X <= width + 0.1f && widget.GlobalBounds.Max.Y <= height + 0.1f, $"{name} ends outside window: {widget.GlobalBounds}");
        }
        Widget controls = dialog.Children.Find<StackPanelWidget>("Ponder.Controls");
        Assert.True(controls.GlobalBounds.Min.Y >= viewport.GlobalBounds.Max.Y - 0.1f);
        Assert.All(dialog.Children.Find<StackPanelWidget>("Ponder.Chapters").Children,
            chapter => Assert.True(chapter.ActualSize.Y >= 48f));
        Assert.True(dialog.Children.Find<RealmPonderButtonWidget>("Ponder.Play").ActualSize.Y >= 48f);
        Widget play = dialog.Children.Find<RealmPonderButtonWidget>("Ponder.Play");
        Assert.Same(play, dialog.HitTestGlobal((play.GlobalBounds.Min + play.GlobalBounds.Max) / 2));
        Assert.Same(viewport, dialog.HitTestGlobal((viewport.GlobalBounds.Min + viewport.GlobalBounds.Max) / 2));
    }

    [Fact]
    public void LongInstructionsScrollWithoutTakingSpaceFromScene()
    {
        RealmPonderDialog dialog = new(RealmPonderSamples.CreateAndGateTutorial());
        dialog.Children.Find<LabelWidget>("Ponder.Caption").Text = string.Concat(Enumerable.Repeat("观察两个输入的状态，并比较输出如何变化。", 30));
        dialog.Measure(new Vector2(800, 600));
        dialog.Arrange(Vector2.Zero, new Vector2(800, 600));
        Widget viewport = dialog.Children.Find<RealmPonderWidget>("Ponder.Viewport");
        Widget note = dialog.Children.Find<CanvasWidget>("Ponder.Note");
        Widget text = dialog.Children.Find<StackPanelWidget>("Ponder.NoteContent");
        Assert.True(viewport.ActualSize.Y >= 250f);
        Assert.True(text.ActualSize.Y > note.ActualSize.Y);
        Assert.True(note.GlobalBounds.Max.Y <= dialog.Children.Find<StackPanelWidget>("Ponder.Footer").GlobalBounds.Min.Y);
    }
}
