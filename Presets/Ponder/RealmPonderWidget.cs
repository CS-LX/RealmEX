using System;
using Engine;
using Engine.Graphics;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Realm viewport with local orbit/zoom and world-anchored tutorial callouts.</summary>
    public sealed class RealmPonderWidget : Widget
    {
        private Vector2? m_dragPoint;
        private float m_orbit;
        private float m_zoom = 1f;

        public Vector2 Size { get; set; } = new(float.PositiveInfinity);
        public SandboxRealm Realm { get; private set; }
        public RealmPonderAnnotation[] Annotations { get; set; } = Array.Empty<RealmPonderAnnotation>();

        public RealmPonderWidget()
        {
            ClampToBounds = true;
            IsHitTestVisible = true;
        }

        public void Setup(SandboxRealm realm)
        {
            Realm = realm;
            Annotations = Array.Empty<RealmPonderAnnotation>();
            ResetView();
            if (Realm != null) Realm.Viewport.IsEnabled = true;
        }

        public void ResetView()
        {
            m_dragPoint = null;
            m_orbit = 0f;
            m_zoom = 1f;
        }

        public override void Update()
        {
            if (Input.Tap.HasValue && HitTestGlobal(Input.Tap.Value) == this)
                m_dragPoint = ScreenToWidget(Input.Tap.Value);
            if (Input.Press.HasValue && m_dragPoint.HasValue)
            {
                Vector2 current = ScreenToWidget(Input.Press.Value);
                m_orbit -= (current.X - m_dragPoint.Value.X) * 0.008f;
                m_dragPoint = current;
            }
            else if (!Input.Press.HasValue) m_dragPoint = null;
            if (Input.Scroll.HasValue)
            {
                Vector3 scroll = Input.Scroll.Value;
                if (HitTestGlobal(new Vector2(scroll.X, scroll.Y)) == this)
                    m_zoom = Math.Clamp(m_zoom * MathF.Pow(0.9f, scroll.Z), 0.65f, 1.6f);
            }
        }

        public override void UpdateCeases()
        {
            m_dragPoint = null;
            base.UpdateCeases();
        }

        public override void Draw(DrawContext dc)
        {
            if (Realm == null || Realm.IsDisposed || ActualSize.X <= 0f || ActualSize.Y <= 0f) return;
            // Flush the parent UI before changing render targets. Size in physical pixels for crisp edges.
            dc.PrimitivesRenderer2D.Flush();
            float scale = Math.Min(Math.Min(Math.Max(GlobalScale, 1f), 2f), 2048f / Math.Max(ActualSize.X, ActualSize.Y));
            Point2 renderSize = new(Math.Clamp((int)MathF.Round(ActualSize.X * scale), 1, 2048),
                Math.Clamp((int)MathF.Round(ActualSize.Y * scale), 1, 2048));
            Vector3 position = Realm.Viewport.LookPosition;
            float worldHeight = Realm.Viewport.OrthographicWorldHeight;
            Viewport previousViewport = Display.Viewport;
            Rectangle previousScissor = Display.ScissorRectangle;
            try
            {
                Vector3 relative = position - Realm.Viewport.LookTarget;
                relative = Vector3.Transform(relative, Matrix.CreateRotationY(m_orbit));
                Realm.Viewport.LookPosition = Realm.Viewport.LookTarget + relative;
                float aspect = ActualSize.X / ActualSize.Y;
                Realm.Viewport.OrthographicWorldHeight = Math.Max(worldHeight, 9f / aspect) * m_zoom;
                Realm.Draw(renderSize);
            }
            finally
            {
                Realm.Viewport.LookPosition = position;
                Realm.Viewport.OrthographicWorldHeight = worldHeight;
                Display.Viewport = previousViewport;
                Display.ScissorRectangle = previousScissor;
            }
            Texture2D texture = Realm.Viewport.Texture;
            if (texture == null) return;
            TexturedBatch2D batch = dc.PrimitivesRenderer2D.TexturedBatch(texture, false, 0,
                DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp);
            int count = batch.TriangleVertices.Count;
            batch.QueueQuad(Vector2.Zero, ActualSize, 0f, Vector2.Zero, Vector2.One, GlobalColorTransform);
            batch.TransformTriangles(GlobalTransform, count);
            dc.PrimitivesRenderer2D.Flush();
            DrawAnnotations(dc);
        }

        private Vector2 Project(Vector3 position)
        {
            Vector3 clip = Vector3.Transform(position, Realm.Viewport.Camera.ViewProjectionMatrix);
            return new Vector2((clip.X + 1f) * ActualSize.X * 0.5f, (1f - clip.Y) * ActualSize.Y * 0.5f);
        }

        private void DrawAnnotations(DrawContext dc)
        {
            FlatBatch2D lines = dc.PrimitivesRenderer2D.FlatBatch(1);
            FontBatch2D font = dc.PrimitivesRenderer2D.FontBatch(LabelWidget.BitmapFont, 2);
            int lineStart = lines.LineVertices.Count;
            int triangleStart = lines.TriangleVertices.Count;
            int textStart = font.TriangleVertices.Count;
            foreach (RealmPonderAnnotation annotation in Annotations)
            {
                Vector2 anchor = Project(annotation.Position);
                if (anchor.X < 0 || anchor.Y < 0 || anchor.X > ActualSize.X || anchor.Y > ActualSize.Y) continue;
                Color color = annotation.Color * GlobalColorTransform;
                Vector2 textSize = LabelWidget.BitmapFont.MeasureText(annotation.Text, new Vector2(0.65f), Vector2.Zero);
                Vector2 boxSize = textSize + new Vector2(20f, 14f);
                Vector2 center = anchor + annotation.LabelOffset;
                center.X = Math.Clamp(center.X, boxSize.X / 2, Math.Max(boxSize.X / 2, ActualSize.X - boxSize.X / 2));
                center.Y = Math.Clamp(center.Y, boxSize.Y / 2, Math.Max(boxSize.Y / 2, ActualSize.Y - boxSize.Y / 2));
                Vector2 half = boxSize / 2;
                Vector2 lineEnd = new(center.X, center.Y + (anchor.Y >= center.Y ? half.Y : -half.Y));
                lines.QueueLine(anchor, lineEnd, 0f, color);
                lines.QueueDisc(anchor, new Vector2(3f), 0f, color);
                lines.QueueQuad(center - half, center + half, 0f, new Color(23, 26, 32, 240) * GlobalColorTransform);
                lines.QueueQuad(center - half, new Vector2(center.X + half.X, center.Y - half.Y + 2f), 0f, color);
                // A small footprint highlights the device without hiding its mesh.
                Vector3 p = annotation.Position;
                Vector3[] corners = [p + new Vector3(-0.52f, -0.12f, -0.52f), p + new Vector3(0.52f, -0.12f, -0.52f),
                    p + new Vector3(0.52f, -0.12f, 0.52f), p + new Vector3(-0.52f, -0.12f, 0.52f)];
                for (int i = 0; i < 4; i++) lines.QueueLine(Project(corners[i]), Project(corners[(i + 1) % 4]), 0f, color);
                font.QueueText(annotation.Text, center, 0f, color,
                    TextAnchor.HorizontalCenter | TextAnchor.VerticalCenter, new Vector2(0.65f));
            }
            lines.TransformLines(GlobalTransform, lineStart);
            lines.TransformTriangles(GlobalTransform, triangleStart);
            font.TransformTriangles(GlobalTransform, textStart);
        }

        public override void MeasureOverride(Vector2 parentAvailableSize)
        {
            IsDrawRequired = true;
            DesiredSize = Vector2.Min(Size, parentAvailableSize);
        }
    }
}
