using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Orbitable scene, timed callouts and transformed block inspection.</summary>
    public sealed class RealmPonderWidget : CanvasWidget
    {
        private sealed class Callout : CanvasWidget
        {
            public readonly LabelWidget Label = new() { WordWrap = true, FontScale = 1, Color = Color.White, IsHitTestVisible = false };
            public readonly ScrollPanelWidget Scroll = new() { Direction = LayoutDirection.Vertical, Margin = new(14, 10), ScrollPosition = 0, ScrollSpeed = 0 };
            public RealmPonderOverlay Overlay;
            public BlockIconWidget Item;
            public Callout()
            {
                IsHitTestVisible = false;
                Children.Add(new RectangleWidget { FillColor = new(0, 0, 0, 180), OutlineColor = Color.Transparent, IsHitTestVisible = false });
                Scroll.Children.Add(Label); Children.Add(Scroll);
            }
        }
        private readonly Dictionary<string, Callout> m_callouts = new(StringComparer.Ordinal);
        private RealmPonderUiWidget m_ui;
        private Vector2? m_dragPoint;
        private float m_orbit;
        private float m_pitch;
        private float m_zoom = 1;
        private Matrix m_viewProjection = Matrix.Identity;
        private (Point3 Position, int Value, float Distance)? m_inspected;
        public SandboxRealm Realm { get; private set; }
        public RealmPonderPlayer Player { get; private set; }
        public bool InspectMode { get; set; }
        public string Language { get; set; } = LanguageControl.CurrentLanguageName;
        public event Action<int> SubjectSelected;
        public string InspectedName => m_inspected.HasValue ? BlocksManager.Blocks[Terrain.ExtractContents(m_inspected.Value.Value)]?.GetDisplayName(Realm?.Project.FindSubsystem<SubsystemTerrain>(false), m_inspected.Value.Value) : null;
        public RealmPonderWidget() { ClampToBounds = true; IsHitTestVisible = true; }
        public void Setup(SandboxRealm realm, RealmPonderPlayer player = null)
        {
            bool samePlayer = ReferenceEquals(Player, player);
            Realm = realm; Player = player;
            if (m_ui != null) { m_ui.Dispose(); Children.Remove(m_ui); m_ui = null; }
            foreach (var callout in m_callouts.Values) Children.Remove(callout);
            m_callouts.Clear(); if (!samePlayer) ResetView(); RefreshPresentation();
        }
        public void ResetView() { m_dragPoint = null; m_orbit = 0; m_pitch = 0; m_zoom = 1; m_inspected = null; }
        public void RefreshPresentation()
        {
            if (Player?.State.Ui != null)
            {
                if (m_ui == null) { m_ui = new(); Children.Insert(0, m_ui); }
                m_ui.Synchronize(Player.State.Ui, Player.State.Tick, Language);
            }
            else if (m_ui != null) { m_ui.Dispose(); Children.Remove(m_ui); m_ui = null; }
            var overlays = Player?.State.Overlays.Values.Where(o => o.Kind is RealmPonderOverlayKind.Text or RealmPonderOverlayKind.Controls).ToDictionary(o => o.Id) ?? [];
            foreach (string id in m_callouts.Keys.Where(id => !overlays.ContainsKey(id)).ToArray()) { Children.Remove(m_callouts[id]); m_callouts.Remove(id); }
            foreach (var overlay in overlays.Values)
            {
                if (!m_callouts.TryGetValue(overlay.Id, out Callout callout)) { callout = new(); m_callouts.Add(overlay.Id, callout); Children.Add(callout); }
                if (!ReferenceEquals(callout.Overlay, overlay)) { callout.Scroll.ScrollPosition = 0; callout.Scroll.ScrollSpeed = 0; }
                callout.Overlay = overlay;
                string text = overlay.Text.Resolve(Language);
                if (overlay.Kind == RealmPonderOverlayKind.Controls)
                {
                    bool zh = Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
                    text = overlay.Input switch { RealmPonderInput.Interact => zh ? "交互" : "Interact", RealmPonderInput.Use => zh ? "使用" : "Use", RealmPonderInput.Scroll => zh ? "滚动" : "Scroll", _ => zh ? "移动" : "Move" };
                    if (overlay.Sneak) text = (zh ? "潜行 + " : "Sneak + ") + text;
                    if (overlay.ItemValue != 0) text += " · " + BlocksManager.Blocks[Terrain.ExtractContents(overlay.ItemValue)].GetDisplayName(Realm?.Project.FindSubsystem<SubsystemTerrain>(false), overlay.ItemValue);
                    if (overlay.ItemValue != 0 && callout.Item == null)
                    {
                        callout.Item = new BlockIconWidget { Size = new(36), HorizontalAlignment = WidgetAlignment.Near, VerticalAlignment = WidgetAlignment.Center, Margin = new(2, 0) };
                        callout.Children.Add(callout.Item);
                    }
                    if (callout.Item != null) { callout.Item.IsVisible = overlay.ItemValue != 0; if (overlay.ItemValue != 0) callout.Item.Value = Terrain.ReplaceLight(overlay.ItemValue, 15); }
                    callout.Scroll.Margin = new(overlay.ItemValue == 0 ? 14 : 40, 10);
                }
                callout.Label.Text = text;
                callout.ColorTransform = Color.White * overlay.Opacity(Player.State.Tick);
                callout.IsVisible = !InspectMode;
            }
        }
        public override void Update()
        {
            if (Player == null) return;
            if (InspectMode)
            {
                Vector2? pointer = Input.MousePosition ?? Input.Tap;
                m_inspected = pointer.HasValue && HitTestGlobal(pointer.Value) == this ? Pick(ScreenToWidget(pointer.Value)) : null;
                if (Input.Click.HasValue && HitTestGlobal(Input.Click.Value.End) == this)
                {
                    var selected = Pick(ScreenToWidget(Input.Click.Value.End));
                    if (selected.HasValue) SubjectSelected?.Invoke(Terrain.ReplaceLight(selected.Value.Value, 0));
                }
            }
            else
            {
                if (Input.Tap.HasValue && HitTestGlobal(Input.Tap.Value) == this) m_dragPoint = ScreenToWidget(Input.Tap.Value);
                if (Input.Press.HasValue && m_dragPoint.HasValue)
                {
                    Vector2 current = ScreenToWidget(Input.Press.Value);
                    m_orbit -= (current.X - m_dragPoint.Value.X) * 0.008f;
                    m_pitch = Math.Clamp(m_pitch + (current.Y - m_dragPoint.Value.Y) * 0.005f, -0.5f, 0.65f);
                    m_dragPoint = current;
                }
                else if (!Input.Press.HasValue) m_dragPoint = null;
            }
            if (Input.Scroll.HasValue)
            {
                Vector3 scroll = Input.Scroll.Value;
                if (HitTestGlobal(new(scroll.X, scroll.Y)) == this) m_zoom = Math.Clamp(m_zoom * MathF.Pow(0.9f, scroll.Z), 0.55f, 1.8f);
            }
        }
        public override void UpdateCeases() { m_dragPoint = null; base.UpdateCeases(); }
        private (Point3 Position, int Value, float Distance)? Pick(Vector2 point)
        {
            Vector3 clip = new(2 * point.X / ActualSize.X - 1, 1 - 2 * point.Y / ActualSize.Y, 0);
            Matrix inverse = Matrix.Invert(m_viewProjection);
            Vector3 near = Vector3.Transform(clip, inverse), far = Vector3.Transform(clip + Vector3.UnitZ, inverse);
            return Player.State.Raycast(new Ray3(near, Vector3.Normalize(far - near)));
        }
        private void ConfigureCamera(Vector2 size)
        {
            if (Player == null || size.X <= 0 || size.Y <= 0) return;
            var s = Player.State; float yaw = MathUtils.DegToRad(s.CameraYaw) + m_orbit;
            float distance = MathF.Sqrt(s.CameraRadius * s.CameraRadius + s.CameraHeight * s.CameraHeight);
            float pitch = Math.Clamp(MathF.Atan2(s.CameraHeight, s.CameraRadius) + m_pitch, 0.15f, 1.4f);
            Vector3 position = s.CameraTarget + new Vector3(MathF.Sin(yaw) * distance * MathF.Cos(pitch), distance * MathF.Sin(pitch), MathF.Cos(yaw) * distance * MathF.Cos(pitch));
            float height = Math.Max(s.ViewHeight, 7f * size.Y / size.X) * m_zoom;
            float offset = size.X >= 850 ? 0.04f : size.X > size.Y ? 0.18f : 0;
            bool besideUi = m_ui != null && size.X >= 1000;
            float captionSpace = 0;
            if (besideUi)
            {
                float sceneWidth = Math.Max(1, size.X - s.Ui.Definition.Size.X - 48);
                captionSpace = m_callouts.Values.Select(c => c.Size.Y + 24).DefaultIfEmpty(0).Max();
                float sceneHeight = Math.Max(80, size.Y - captionSpace - 16);
                height = Math.Max(s.ViewHeight, Math.Max(10 * size.Y / sceneWidth, 7 * size.Y / sceneHeight)) * m_zoom;
                offset = (16 + sceneWidth / 2) / size.X - 0.5f;
            }
            Vector3 shift = Vector3.Normalize(Vector3.Cross(s.CameraTarget - position, Vector3.UnitY)) * (height * size.X / size.Y * offset);
            if (besideUi)
            {
                Vector3 right = Vector3.Normalize(Vector3.Cross(s.CameraTarget - position, Vector3.UnitY));
                Vector3 up = Vector3.Normalize(Vector3.Cross(right, s.CameraTarget - position));
                shift -= up * (height * captionSpace / (2 * size.Y));
            }
            Vector3 target = s.CameraTarget - shift; position -= shift;
            m_viewProjection = Matrix.CreateLookAt(position, target, Vector3.UnitY) * Matrix.CreateOrthographic(height * size.X / size.Y, height, 0.1f, 1000);
            if (Realm != null) { Realm.Viewport.LookTarget = target; Realm.Viewport.LookPosition = position; Realm.Viewport.OrthographicWorldHeight = height; }
        }
        private Vector2 Project(Vector3 point)
        {
            Vector3 clip = Vector3.Transform(point, m_viewProjection);
            return new((clip.X + 1) * ActualSize.X / 2, (1 - clip.Y) * ActualSize.Y / 2);
        }
        private Vector3 Anchor(Vector3 point)
        {
            Point3 cell = new((int)MathF.Floor(point.X), (int)MathF.Floor(point.Y), (int)MathF.Floor(point.Z));
            var section = Player.State.Sections.Values.FirstOrDefault(s => s.Selection.Contains(cell));
            return section == null ? point : Vector3.Transform(point, section.Transform);
        }
        public override void MeasureOverride(Vector2 parentAvailableSize)
        {
            Vector2 size = new(Size.X >= 0 ? Math.Min(Size.X, parentAvailableSize.X) : parentAvailableSize.X, Size.Y >= 0 ? Math.Min(Size.Y, parentAvailableSize.Y) : parentAvailableSize.Y);
            ConfigureCamera(size);
            bool stackedUi = m_ui != null && size.X < 1000;
            float captionHeight = stackedUi && m_callouts.Count > 0 ? Math.Min(124, size.Y * 0.3f) : 0;
            if (m_ui != null)
            {
                Vector2 natural = Player.State.Ui.Definition.Size + new Vector2(0, 40);
                m_ui.Size = new(Math.Min(natural.X, size.X - 32), Math.Min(natural.Y, size.Y - captionHeight - 24));
                SetWidgetPosition(m_ui, new(size.X - m_ui.Size.X - 16, Math.Max(8, captionHeight)));
            }
            float y = 16;
            foreach (var callout in m_callouts.Values)
            {
                float width = Math.Min(350, Math.Max(80, size.X > size.Y && size.X < 850 ? size.X * 0.43f : size.X - 32));
                float margins = callout.Item?.IsVisible == true ? 80 : 28;
                callout.Label.Measure(new(width - margins, float.PositiveInfinity));
                if (callout.Overlay.Kind == RealmPonderOverlayKind.Controls) width = Math.Min(width, Math.Max(100, callout.Label.DesiredSize.X + margins));
                float height = Math.Min(callout.Label.DesiredSize.Y + 20, Math.Max(48, size.Y * 0.42f));
                if (stackedUi) { width = size.X - 32; height = Math.Min(height, Math.Max(48, captionHeight - 8)); }
                callout.Size = new(width, height);
                float x = callout.Overlay.Kind == RealmPonderOverlayKind.Controls && size.X >= 650 ? size.X - width - 16 : 16;
                Vector2 position = new(x, Math.Min(y, Math.Max(0, size.Y - height)));
                switch (callout.Overlay.Placement)
                {
                    case RealmPonderTextPlacement.TopLeft: position = new(16, 16); break;
                    case RealmPonderTextPlacement.TopRight: position = new(size.X - width - 16, 16); break;
                    case RealmPonderTextPlacement.BottomLeft: position = new(16, size.Y - height - 16); break;
                    case RealmPonderTextPlacement.BottomRight: position = size - new Vector2(width + 16, height + 16); break;
                    case RealmPonderTextPlacement.NearTarget:
                        Vector3 clip = Vector3.Transform(Anchor(callout.Overlay.Position), m_viewProjection);
                        position = new((clip.X + 1) * size.X / 2 - width / 2, (1 - clip.Y) * size.Y / 2 - height - 32); break;
                }
                position = Vector2.Max(Vector2.Zero, Vector2.Min(position, size - new Vector2(width, height)));
                SetWidgetPosition(callout, position);
                y += height + 8;
            }
            base.MeasureOverride(size); DesiredSize = size; IsDrawRequired = true;
        }
        public override void Draw(DrawContext dc)
        {
            if (Player == null || ActualSize.X <= 0 || ActualSize.Y <= 0) return;
            ConfigureCamera(ActualSize);
            if (Player.State.ShowShadow)
            {
                var shadow = dc.PrimitivesRenderer2D.FlatBatch(); int start = shadow.TriangleVertices.Count;
                shadow.QueueDisc(Project(Player.State.CameraTarget - new Vector3(0, 1, 0)), new Vector2(ActualSize.Y * 0.31f, ActualSize.Y * 0.09f), 0, new Color(0, 0, 0, 65) * GlobalColorTransform);
                shadow.TransformTriangles(GlobalTransform, start);
            }
            if (Realm != null && !Realm.IsDisposed)
            {
                dc.PrimitivesRenderer2D.Flush();
                float scale = Math.Min(Math.Clamp(GlobalScale, 1, 2), 2048f / Math.Max(ActualSize.X, ActualSize.Y));
                Point2 renderSize = new(Math.Max(1, (int)MathF.Round(ActualSize.X * scale)), Math.Max(1, (int)MathF.Round(ActualSize.Y * scale)));
                Viewport viewport = Display.Viewport; Rectangle scissor = Display.ScissorRectangle;
                try { Realm.Draw(renderSize); }
                finally { Display.Viewport = viewport; Display.ScissorRectangle = scissor; }
                m_viewProjection = Realm.Viewport.Camera.ViewProjectionMatrix;
                var texture = dc.PrimitivesRenderer2D.TexturedBatch(Realm.Viewport.Texture, false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
                int start = texture.TriangleVertices.Count;
                texture.QueueQuad(Vector2.Zero, ActualSize, 0, Vector2.Zero, Vector2.One, GlobalColorTransform); texture.TransformTriangles(GlobalTransform, start);
                dc.PrimitivesRenderer2D.Flush();
            }
            DrawOverlays(dc);
        }
        private void DrawOverlays(DrawContext dc)
        {
            var batch = dc.PrimitivesRenderer2D.FlatBatch(1); int lines = batch.LineVertices.Count, triangles = batch.TriangleVertices.Count;
            void Line(Vector3 a, Vector3 b, Color color) => batch.QueueLine(Project(a), Project(b), 0, color * GlobalColorTransform);
            void Outline(RealmPonderSelection selection, Color color)
            {
                foreach (Point3 cell in selection)
                {
                    Vector3 origin = new(cell.X, cell.Y, cell.Z);
                    var section = Player.State.Sections.Values.FirstOrDefault(s => s.Selection.Contains(cell));
                    if (section == null || section.Opacity <= 0.2f) continue;
                    Vector3[] corners = new Vector3[8];
                    for (int i = 0; i < 8; i++) corners[i] = Vector3.Transform(origin + new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1), section.Transform);
                    for (int i = 0; i < 8; i++) for (int axis = 1; axis <= 4; axis *= 2) if ((i & axis) == 0) Line(corners[i], corners[i | axis], color);
                }
            }
            if (InspectMode && m_inspected.HasValue) Outline(new([m_inspected.Value.Position]), new(250, 216, 132));
            if (!InspectMode) foreach (var overlay in Player.State.Overlays.Values)
            {
                Color color = overlay.Color * overlay.Opacity(Player.State.Tick);
                if (overlay.Kind == RealmPonderOverlayKind.Outline) Outline(overlay.Selection, color);
                else if (overlay.Kind == RealmPonderOverlayKind.Bounds)
                {
                    Vector3[] corners = new Vector3[8];
                    for (int i = 0; i < 8; i++) corners[i] = new((i & 1) == 0 ? overlay.Position.X : overlay.End.X, (i & 2) == 0 ? overlay.Position.Y : overlay.End.Y, (i & 4) == 0 ? overlay.Position.Z : overlay.End.Z);
                    for (int i = 0; i < 8; i++) for (int axis = 1; axis <= 4; axis *= 2) if ((i & axis) == 0) Line(corners[i], corners[i | axis], color);
                }
                else if (overlay.Kind == RealmPonderOverlayKind.Line) Line(Anchor(overlay.Position), Anchor(overlay.End), color);
                else if (overlay.Kind is RealmPonderOverlayKind.Success or RealmPonderOverlayKind.Particle)
                {
                    float time = (Player.State.Tick - overlay.StartTick) / 20f;
                    for (int i = 0; i < 10; i++)
                    {
                        float angle = i * MathF.Tau / 10;
                        Vector3 position = overlay.Position + new Vector3(MathF.Cos(angle), 0.5f, MathF.Sin(angle)) * time * 0.65f + overlay.End * time;
                        batch.QueueDisc(Project(position), new Vector2(3), 0, color * GlobalColorTransform);
                    }
                }
                else if (m_ui == null && m_callouts.TryGetValue(overlay.Id, out var callout))
                {
                    Vector2 anchor = Project(Anchor(overlay.Position));
                    Vector2 position = GetWidgetPosition(callout) ?? Vector2.Zero;
                    Vector2 end = new(Math.Clamp(anchor.X, position.X, position.X + callout.ActualSize.X), position.Y + callout.ActualSize.Y);
                    batch.QueueLine(anchor, end, 0, color * GlobalColorTransform);
                    batch.QueueDisc(anchor, new Vector2(3), 0, color * GlobalColorTransform);
                }
            }
            batch.TransformLines(GlobalTransform, lines); batch.TransformTriangles(GlobalTransform, triangles);
        }
    }
}
