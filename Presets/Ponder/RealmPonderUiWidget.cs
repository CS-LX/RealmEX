using System;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Embeds the original machine widget tree, scrolls at native scale and replays isolated UI actions.</summary>
    public sealed class RealmPonderUiWidget : CanvasWidget, IDisposable
    {
        private readonly CanvasWidget m_surface = new();
        private readonly CanvasWidget m_scrollFrame = new();
        private readonly CanvasWidget m_horizontalFrame = new();
        private readonly CanvasWidget m_titleFrame = new();
        private readonly LabelWidget m_title = new() { FontScale = 1, WordWrap = true, IsHitTestVisible = false };
        private readonly ScrollPanelWidget m_vertical = new() { Direction = LayoutDirection.Vertical, ScrollPosition = 0, ScrollSpeed = 0, IsUpdateEnabled = false };
        private readonly ScrollPanelWidget m_horizontal = new() { Direction = LayoutDirection.Horizontal, ScrollPosition = 0, ScrollSpeed = 0, IsUpdateEnabled = false };
        private readonly CueWidget m_cue = new() { IsHitTestVisible = false };
        private RealmPonderUiState m_state;
        private int m_applied;
        private RealmPonderUiCue m_revealedCue;
        private Vector2 m_revealedSize;
        private Vector2? m_pan;
        private ClickableWidget m_pressed;
        public CanvasWidget Content { get; private set; }
        public RealmPonderUiWidget()
        {
            ClampToBounds = true;
            Children.Add(new RectangleWidget { FillColor = new(0, 0, 0, 160), OutlineColor = Color.Transparent, IsHitTestVisible = false });
            Children.Add(m_titleFrame); m_titleFrame.Children.Add(m_title); Children.Add(m_scrollFrame); m_scrollFrame.Children.Add(m_vertical);
            m_vertical.Children.Add(m_horizontalFrame); m_horizontalFrame.Children.Add(m_horizontal); m_horizontal.Children.Add(m_surface);
        }
        public void Synchronize(RealmPonderUiState state, float tick, string language)
        {
            if (!ReferenceEquals(state, m_state))
            {
                Release(); m_state = state; m_applied = 0; m_revealedCue = null;
                if (state != null)
                {
                    CanvasWidget content = state.Definition.Create();
                    if (content.ParentWidget != null) throw new InvalidOperationException("Ponder UI factories must return a new, unattached widget.");
                    Content = content;
                    Content.Size = state.Definition.Size;
                    Content.WidgetsHierarchyInput = new(WidgetInputDevice.None);
                    Content.IsUpdateEnabled = false;
                    foreach (var slot in Content.AllChildren.OfType<InventorySlotWidget>().Where(s => s.m_inventory == null))
                        slot.AssignInventorySlot(new RealmPonderUiInventory(1), 0);
                    m_surface.Children.Add(Content);
                    m_surface.Children.Add(m_cue);
                    m_cue.Size = state.Definition.Size;
                    m_horizontal.ScrollPosition = m_vertical.ScrollPosition = 0;
                }
            }
            IsVisible = state != null;
            if (state == null) return;
            m_cue.Content = Content; m_cue.Cue = state.Cue; m_cue.Tick = tick;
            m_cue.IsVisible = state.Cue != null && tick < state.Cue.StartTick + state.Cue.Duration;
            m_title.Text = RealmPonderLocalization.BuiltIn.Text("ui.demonstration_title", state.Definition.Title.Resolve(language)).Resolve(language);
            // Only pending edits need an extra layout for scripted clicks. The host lays out the visible tree each frame.
            if (m_applied < state.Edits.Count) Widget.LayoutWidgetsHierarchy(Content, state.Definition.Size);
            while (m_applied < state.Edits.Count) state.Edits[m_applied++](Content, language);
            if (state.Cue != null)
            {
                Find(Content, state.Cue.Target);
                if (!string.IsNullOrEmpty(state.Cue.From)) Find(Content, state.Cue.From);
            }
            if (m_pressed != null) { m_pressed.IsPressed = false; m_pressed = null; }
            if (state.Cue is { Action: RealmPonderUiAction.Click } cue && tick < cue.StartTick + cue.Duration)
            {
                Widget target = Find(Content, cue.Target);
                var click = target as ClickableWidget ?? (target as ContainerWidget)?.AllChildren.OfType<ClickableWidget>().FirstOrDefault();
                if (click != null) { m_pressed = click; click.IsPressed = cue.Progress(tick) is > 0.35f and < 0.7f; }
            }
        }
        internal static Widget Find(CanvasWidget root, string name)
        {
            Widget current = root;
            foreach (string segment in name.Split('/'))
                current = current.Name == segment ? current : (current as ContainerWidget)?.Children.Find<Widget>(segment, true)
                    ?? throw new InvalidOperationException($"UI target '{name}' was not found.");
            return current;
        }
        public override void MeasureOverride(Vector2 available)
        {
            if (m_state == null) { base.MeasureOverride(available); return; }
            Vector2 size = Vector2.Min(Size, available);
            m_titleFrame.Size = new(size.X - 24, -1); SetWidgetPosition(m_titleFrame, new(12, 4));
            m_titleFrame.Measure(new(size.X - 24, float.PositiveInfinity));
            float headingHeight = Math.Max(40, m_titleFrame.ParentDesiredSize.Y + 8);
            m_scrollFrame.Size = new(size.X, Math.Max(1, size.Y - headingHeight)); SetWidgetPosition(m_scrollFrame, new(0, headingHeight));
            m_horizontalFrame.Size = new(size.X, m_state.Definition.Size.Y);
            m_surface.Size = m_state.Definition.Size;
            base.MeasureOverride(available); IsDrawRequired = true;
        }
        public override void ArrangeOverride()
        {
            base.ArrangeOverride();
            if (m_state == null) return;
            ClampScroll();
            if (m_state?.Cue is not { } cue || ReferenceEquals(cue, m_revealedCue) && m_revealedSize == ActualSize) return;
            var target = Find(Content, cue.Target);
            Vector2 min = Content.ScreenToWidget(target.GlobalBounds.Min), max = Content.ScreenToWidget(target.GlobalBounds.Max);
            float Reveal(float current, float near, float far, float size) => near < current + 12 ? Math.Max(0, near - 12) : far > current + size - 12 ? Math.Max(0, far - size + 12) : current;
            m_horizontal.ScrollPosition = Reveal(m_horizontal.ScrollPosition, min.X, max.X, m_scrollFrame.ActualSize.X);
            m_vertical.ScrollPosition = Reveal(m_vertical.ScrollPosition, min.Y, max.Y, m_scrollFrame.ActualSize.Y);
            m_revealedCue = cue; m_revealedSize = ActualSize;
        }
        public override void Update()
        {
            if (m_state == null) return;
            if (Input.Drag.HasValue && ContainsPointer(Input.Drag.Value)) m_pan ??= Input.Drag.Value;
            if (m_pan.HasValue && Input.Press.HasValue)
            {
                Vector2 delta = (m_pan.Value - Input.Press.Value) / GlobalScale;
                m_horizontal.ScrollPosition += delta.X; m_vertical.ScrollPosition += delta.Y;
                m_pan = Input.Press.Value; Input.Clear();
            }
            else if (!Input.Press.HasValue) m_pan = null;
            if (Input.Scroll.HasValue && ContainsPointer(Input.Scroll.Value.XY))
            {
                if (m_state.Definition.Size.Y > m_scrollFrame.ActualSize.Y) m_vertical.ScrollPosition -= 40 * Input.Scroll.Value.Z;
                else m_horizontal.ScrollPosition -= 40 * Input.Scroll.Value.Z;
                Input.Clear();
            }
            ClampScroll();
            m_horizontal.m_scrollAreaLength = m_state.Definition.Size.X; m_vertical.m_scrollAreaLength = m_state.Definition.Size.Y;
            m_horizontal.m_scrollBarAlpha = m_state.Definition.Size.X > m_scrollFrame.ActualSize.X ? 1 : 0;
            m_vertical.m_scrollBarAlpha = m_state.Definition.Size.Y > m_scrollFrame.ActualSize.Y ? 1 : 0;
        }
        private void ClampScroll()
        {
            m_horizontal.ScrollPosition = Math.Clamp(m_horizontal.ScrollPosition, 0, Math.Max(0, m_state.Definition.Size.X - m_scrollFrame.ActualSize.X));
            m_vertical.ScrollPosition = Math.Clamp(m_vertical.ScrollPosition, 0, Math.Max(0, m_state.Definition.Size.Y - m_scrollFrame.ActualSize.Y));
        }
        private bool ContainsPointer(Vector2 screen) => GlobalBounds.Contains(screen);
        public override void UpdateCeases() { m_pan = null; base.UpdateCeases(); }
        private sealed class CueWidget : CanvasWidget
        {
            public CanvasWidget Content;
            public RealmPonderUiCue Cue;
            public float Tick;
            public override void MeasureOverride(Vector2 available) { base.MeasureOverride(available); IsDrawRequired = true; }
            public override void Draw(DrawContext dc)
            {
                Widget target = Find(Content, Cue.Target);
                Vector2 min = ScreenToWidget(target.GlobalBounds.Min), max = ScreenToWidget(target.GlobalBounds.Max);
                Vector2 point = (min + max) / 2;
                if (!string.IsNullOrEmpty(Cue.From))
                {
                    var from = Find(Content, Cue.From);
                    point = Vector2.Lerp(ScreenToWidget((from.GlobalBounds.Min + from.GlobalBounds.Max) / 2), point, Cue.Progress(Tick));
                }
                var batch = dc.PrimitivesRenderer2D.FlatBatch(100); int lines = batch.LineVertices.Count, triangles = batch.TriangleVertices.Count;
                Color color = new Color(240, 213, 125) * GlobalColorTransform;
                batch.QueueLine(min, new(max.X, min.Y), 0, color); batch.QueueLine(new(max.X, min.Y), max, 0, color);
                batch.QueueLine(max, new(min.X, max.Y), 0, color); batch.QueueLine(new(min.X, max.Y), min, 0, color);
                batch.QueueTriangle(point, point + new Vector2(4, 19), point + new Vector2(14, 13), 0, Color.White * GlobalColorTransform);
                if (Cue.Action == RealmPonderUiAction.Click && Cue.Progress(Tick) is > 0.35f and < 0.7f)
                    batch.QueueDisc(point, new Vector2(6), 0, color);
                batch.TransformLines(GlobalTransform, lines); batch.TransformTriangles(GlobalTransform, triangles);
            }
        }
        private void Release()
        {
            if (m_pressed != null) { m_pressed.IsPressed = false; m_pressed = null; }
            if (Content is IDisposable disposable) disposable.Dispose();
            m_surface.Children.Clear(); Content = null; m_cue.Content = null; m_cue.Cue = null;
        }
        public override void Dispose() { Release(); m_state = null; base.Dispose(); }
    }
}
