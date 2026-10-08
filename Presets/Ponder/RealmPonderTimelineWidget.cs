using System;
using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Thin proportional timeline with a larger pointer/touch target.</summary>
    public sealed class RealmPonderTimelineWidget : Widget
    {
        private bool m_dragging;
        public RealmPonderPlayer Player { get; set; }
        public event Action<int> SeekRequested;
        public override void MeasureOverride(Vector2 parentAvailableSize) { DesiredSize = new(parentAvailableSize.X, 30); IsDrawRequired = true; }
        public override void Update()
        {
            if (Input.Tap.HasValue && HitTestGlobal(Input.Tap.Value) == this) { m_dragging = true; SeekAt(Input.Tap.Value); }
            if (m_dragging && Input.Press.HasValue) SeekAt(Input.Press.Value);
            else if (!Input.Press.HasValue) m_dragging = false;
        }
        public override void UpdateCeases() { m_dragging = false; base.UpdateCeases(); }
        public int TickAt(float x) => Player == null ? 0 : (int)MathF.Round(Math.Clamp((x - 8) / Math.Max(1, ActualSize.X - 16), 0, 1) * Player.Tutorial.Duration);
        private void SeekAt(Vector2 screen) => SeekRequested?.Invoke(TickAt(ScreenToWidget(screen).X));
        public override void Draw(DrawContext dc)
        {
            if (Player == null) return;
            var batch = dc.PrimitivesRenderer2D.FlatBatch(); int start = batch.TriangleVertices.Count;
            float width = Math.Max(1, ActualSize.X - 16), y = ActualSize.Y / 2;
            Color accent = new Color(158, 199, 234) * GlobalColorTransform;
            batch.QueueQuad(new(8, y - 2), new(8 + width, y + 2), 0, new Color(65, 81, 104) * GlobalColorTransform);
            float current = 8 + width * Player.State.Tick / Player.Tutorial.Duration;
            batch.QueueQuad(new(8, y - 2), new(current, y + 2), 0, accent);
            foreach (var keyframe in Player.Tutorial.Keyframes)
            {
                float x = 8 + width * keyframe.Tick / Player.Tutorial.Duration;
                batch.QueueQuad(new(x - 1, y - 5), new(x + 1, y + 5), 0, accent);
            }
            batch.QueueDisc(new(current, y), new Vector2(5), 0, accent);
            batch.TransformTriangles(GlobalTransform, start);
        }
    }
}
