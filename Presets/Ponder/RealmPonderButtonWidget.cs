using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Flat controls shared by the tutorial transport and chapter strip.</summary>
    public sealed class RealmPonderButtonWidget : ClickableWidget
    {
        public Vector2 Size { get; set; } = new(80f, 48f);
        public string Text { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVisited { get; set; }

        public override void MeasureOverride(Vector2 parentAvailableSize)
        {
            IsDrawRequired = true;
            DesiredSize = Vector2.Min(Size, parentAvailableSize);
        }

        public override void Draw(DrawContext dc)
        {
            bool hover = Input.MousePosition.HasValue && HitTestGlobal(Input.MousePosition.Value) == this;
            Color accent = new(235, 193, 94);
            Color background = IsActive ? new Color(80, 68, 40, 240)
                : IsPressed ? new Color(77, 80, 85, 240)
                : hover && IsEnabledGlobal ? new Color(57, 61, 68, 240)
                : new Color(29, 32, 38, 225);
            Color foreground = !IsEnabledGlobal ? new Color(109, 112, 118)
                : IsActive ? accent : new Color(230, 230, 225);
            FlatBatch2D batch = dc.PrimitivesRenderer2D.FlatBatch();
            int triangles = batch.TriangleVertices.Count;
            batch.QueueQuad(Vector2.Zero, ActualSize, 0f, background * GlobalColorTransform);
            batch.QueueQuad(new Vector2(0, ActualSize.Y - 2), ActualSize, 0f,
                (IsActive || IsVisited ? accent : new Color(83, 87, 92)) * GlobalColorTransform);
            batch.TransformTriangles(GlobalTransform, triangles);
            FontBatch2D font = dc.PrimitivesRenderer2D.FontBatch(LabelWidget.BitmapFont, 1);
            int textStart = font.TriangleVertices.Count;
            font.QueueText(Text, ActualSize / 2, 0f, foreground * GlobalColorTransform,
                TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, new Vector2(0.7f));
            font.TransformTriangles(GlobalTransform, textStart);
        }
    }
}
