using System;
using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Presets.Ponder
{
    public enum RealmPonderIcon { None, Close, Index, Play, Pause, PreviousScene, NextScene, PreviousKeyframe, NextKeyframe, Replay, ResetView, Inspect, Reading }
    /// <summary>Flat translucent controls with Remix Icon artwork and normal host text size.</summary>
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
            Color foreground = new Color(255, 255, 255, !IsEnabledGlobal ? 64 : IsActive || IsHovered ? 255 : 200) * GlobalColorTransform;
            Color background = IsEnabledGlobal && IsPressed ? new(48, 48, 48, 48)
                : IsEnabledGlobal && (IsActive || IsHovered) ? new(24, 24, 24, 24) : Color.Transparent;
            FlatBatch2D batch = dc.PrimitivesRenderer2D.FlatBatch();
            int triangles = batch.TriangleVertices.Count;
            batch.QueueQuad(Vector2.Zero, ActualSize, 0, background * GlobalColorTransform);
            batch.TransformTriangles(GlobalTransform, triangles);
            Vector2 center = ActualSize / 2;
            if (Icon != RealmPonderIcon.None)
            {
                string name = Icon switch
                {
                    RealmPonderIcon.Close => "close-line", RealmPonderIcon.Index => "list-check",
                    RealmPonderIcon.Play => "play-fill", RealmPonderIcon.Pause => "pause-fill",
                    RealmPonderIcon.PreviousScene => "skip-back-line", RealmPonderIcon.NextScene => "skip-forward-line",
                    RealmPonderIcon.PreviousKeyframe => "arrow-left-s-line", RealmPonderIcon.NextKeyframe => "arrow-right-s-line",
                    RealmPonderIcon.Replay => "restart-line", RealmPonderIcon.ResetView => "focus-3-line",
                    RealmPonderIcon.Inspect => "search-line", RealmPonderIcon.Reading => "book-open-line",
                    _ => throw new ArgumentOutOfRangeException(nameof(Icon))
                };
                var texture = ContentManager.Get<Texture2D>("RealmEX/Icons/" + name);
                var icons = dc.PrimitivesRenderer2D.TexturedBatch(texture, false, 1, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.NonPremultiplied, SamplerState.LinearClamp);
                int start = icons.TriangleVertices.Count;
                icons.QueueQuad(center - new Vector2(12), center + new Vector2(12), 0, Vector2.Zero, Vector2.One, foreground);
                icons.TransformTriangles(GlobalTransform, start);
            }
            else
            {
                var font = dc.PrimitivesRenderer2D.FontBatch(LabelWidget.BitmapFont, 1); int start = font.TriangleVertices.Count;
                font.QueueText(Text, center, 0, foreground, TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, Vector2.One);
                font.TransformTriangles(GlobalTransform, start);
            }
        }
    }
}
