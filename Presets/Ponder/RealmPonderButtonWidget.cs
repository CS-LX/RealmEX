using System;
using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Presets.Ponder
{
    public enum RealmPonderIcon { None, Close, Index, Play, Pause, PreviousScene, NextScene, PreviousKeyframe, NextKeyframe, Replay, ResetView, Inspect, Reading }
    /// <summary>Vector controls avoid font-dependent icon glyphs; text uses the host's normal scale.</summary>
    public sealed class RealmPonderButtonWidget : ClickableWidget
    {
        public Vector2 Size { get; set; } = new(48, 48);
        public string Text { get; set; } = string.Empty;
        public RealmPonderIcon Icon { get; set; }
        public bool IsActive { get; set; }
        public bool IsHovered => Input.MousePosition.HasValue && HitTestGlobal(Input.MousePosition.Value) == this;
        public override void MeasureOverride(Vector2 parentAvailableSize) { IsDrawRequired = true; DesiredSize = Vector2.Min(Size, parentAvailableSize); }
        public override void Draw(DrawContext dc)
        {
            Color accent = new(148, 192, 230);
            Color background = IsActive ? new(43, 65, 86, 235) : IsPressed ? new(72, 85, 102, 235) : IsHovered && IsEnabledGlobal ? new(53, 66, 83, 235) : new(24, 32, 45, 220);
            Color foreground = (!IsEnabledGlobal ? new Color(91, 102, 120) : IsActive ? accent : new Color(221, 231, 242)) * GlobalColorTransform;
            FlatBatch2D batch = dc.PrimitivesRenderer2D.FlatBatch();
            int triangles = batch.TriangleVertices.Count, lines = batch.LineVertices.Count;
            batch.QueueQuad(Vector2.Zero, ActualSize, 0, background * GlobalColorTransform);
            batch.QueueQuad(new(0, ActualSize.Y - 2), ActualSize, 0, (IsActive ? accent : new Color(63, 81, 104)) * GlobalColorTransform);
            Vector2 center = ActualSize / 2;
            void Line(float x, float y, float x2, float y2) => batch.QueueLine(center + new Vector2(x, y), center + new Vector2(x2, y2), 0, foreground);
            void Rect(float x, float y, float w, float h) => batch.QueueQuad(center + new Vector2(x, y), center + new Vector2(x + w, y + h), 0, foreground);
            void Arrow(float direction, float offset = 0) { Line(offset - direction * 4, -8, offset + direction * 4, 0); Line(offset + direction * 4, 0, offset - direction * 4, 8); }
            switch (Icon)
            {
                case RealmPonderIcon.Close: Line(-7, -7, 7, 7); Line(-7, 7, 7, -7); break;
                case RealmPonderIcon.Play: batch.QueueTriangle(center + new Vector2(-5, -9), center + new Vector2(9, 0), center + new Vector2(-5, 9), 0, foreground); break;
                case RealmPonderIcon.Pause: Rect(-7, -8, 4, 16); Rect(3, -8, 4, 16); break;
                case RealmPonderIcon.PreviousScene: Arrow(-1); Line(-9, -8, -9, 8); break;
                case RealmPonderIcon.NextScene: Arrow(1); Line(9, -8, 9, 8); break;
                case RealmPonderIcon.PreviousKeyframe: Arrow(-1); break;
                case RealmPonderIcon.NextKeyframe: Arrow(1); break;
                case RealmPonderIcon.Index: for (int i = -1; i <= 1; i++) { Rect(-9, i * 6 - 1, 3, 3); Rect(-2, i * 6 - 1, 12, 3); } break;
                case RealmPonderIcon.Reading: Line(0, -7, 0, 10); Line(-10, -9, 0, -6); Line(10, -9, 0, -6); Line(-10, -9, -10, 7); Line(10, -9, 10, 7); Line(-10, 7, 0, 10); Line(10, 7, 0, 10); break;
                case RealmPonderIcon.Inspect: for (int i = 0; i < 24; i++) { float a = i * MathF.Tau / 24, b = (i + 1) * MathF.Tau / 24; Line(-2 + MathF.Cos(a) * 7, -2 + MathF.Sin(a) * 7, -2 + MathF.Cos(b) * 7, -2 + MathF.Sin(b) * 7); } Line(3, 3, 11, 11); break;
                case RealmPonderIcon.Replay: for (int i = 0; i < 20; i++) { float a = i * MathF.PI / 12, b = (i + 1) * MathF.PI / 12; Line(MathF.Cos(a) * 9, MathF.Sin(a) * 9, MathF.Cos(b) * 9, MathF.Sin(b) * 9); } Line(4, -6, 4, -12); Line(4, -6, 10, -6); break;
                case RealmPonderIcon.ResetView: Line(-10, 0, 0, -9); Line(0, -9, 10, 0); Line(-7, -2, -7, 9); Line(7, -2, 7, 9); Line(-7, 9, 7, 9); break;
            }
            batch.TransformTriangles(GlobalTransform, triangles); batch.TransformLines(GlobalTransform, lines);
            if (Icon == RealmPonderIcon.None)
            {
                var font = dc.PrimitivesRenderer2D.FontBatch(LabelWidget.BitmapFont, 1); int start = font.TriangleVertices.Count;
                font.QueueText(Text, center, 0, foreground, TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, Vector2.One);
                font.TransformTriangles(GlobalTransform, start);
            }
        }
    }
}
