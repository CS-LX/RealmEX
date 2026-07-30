using System;
using Engine;
using Engine.Graphics;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// 基础 Ponder 视口控件：把 RealmViewport 的离屏纹理绘制到 Widget 区域。
    /// </summary>
    public sealed class RealmPonderWidget : Widget
    {
        public Vector2 Size { get; set; } = new(float.PositiveInfinity);

        public SandboxRealm Realm { get; private set; }

        public void Setup(SandboxRealm realm)
        {
            Realm = realm;
            if (Realm != null)
            {
                Realm.Viewport.IsEnabled = true;
            }
        }

        public override void Draw(DrawContext dc)
        {
            if (Realm == null || ActualSize.X <= 0f || ActualSize.Y <= 0f)
            {
                return;
            }

            Point2 renderSize = new(
                Math.Max(1, (int)MathF.Round(ActualSize.X)),
                Math.Max(1, (int)MathF.Round(ActualSize.Y)));
            Realm.Draw(renderSize);

            Texture2D texture = Realm.Viewport.Texture;
            if (texture == null)
            {
                return;
            }

            TexturedBatch2D batch = dc.PrimitivesRenderer2D.TexturedBatch(
                texture,
                false,
                0,
                DepthStencilState.None,
                RasterizerState.CullNoneScissor,
                BlendState.AlphaBlend,
                SamplerState.PointClamp);
            int count = batch.TriangleVertices.Count;
            batch.QueueQuad(Vector2.Zero, ActualSize, 0f, Vector2.Zero, Vector2.One, GlobalColorTransform);
            batch.TransformTriangles(GlobalTransform, count);
            dc.PrimitivesRenderer2D.Flush();
        }

        public override void MeasureOverride(Vector2 parentAvailableSize)
        {
            IsDrawRequired = true;
            DesiredSize = Size;
        }
    }
}
