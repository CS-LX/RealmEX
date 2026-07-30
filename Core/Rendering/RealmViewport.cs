using System;
using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Core.Rendering
{
    /// <summary>
    /// Realm 离屏渲染入口：管理 RenderTarget，并把 SandboxProject 绘制到纹理。
    /// </summary>
    public sealed class RealmViewport : IDisposable
    {
        private RenderTarget2D m_renderTarget;

        public bool IsEnabled { get; set; }

        public int DrawCount { get; private set; }

        public RealmCamera Camera { get; } = new();

        public Color ClearColor { get; set; } = Color.Transparent;

        public Texture2D Texture => m_renderTarget;

        public Point2? RenderTargetSize =>
            m_renderTarget != null ? new Point2(m_renderTarget.Width, m_renderTarget.Height) : null;

        public bool DrawIfNeeded(SandboxRealm realm, Point2 size)
        {
            if (!IsEnabled)
            {
                return false;
            }

            Draw(realm, size);
            return true;
        }

        public void Draw(SandboxRealm realm, Point2 size)
        {
            ArgumentNullException.ThrowIfNull(realm);
            EnsureRenderTarget(size);

            Camera.SetupOrthographic(
                new Vector3(8.5f, 10f, 18f),
                new Vector3(8.5f, 2f, 8.5f),
                Vector3.UnitY,
                new Vector2(size.X, size.Y),
                16f);
            Camera.PrepareForDrawing(null);

            RenderTarget2D previousRenderTarget = Display.RenderTarget;
            try
            {
                Display.RenderTarget = m_renderTarget;
                Display.Clear(ClearColor, 1f, 0);
                realm.SubsystemDrawing.Draw(Camera);
            }
            finally
            {
                Display.RenderTarget = previousRenderTarget;
            }
            DrawCount++;
        }

        public void Dispose()
        {
            IsEnabled = false;
            Utilities.Dispose(ref m_renderTarget);
        }

        private void EnsureRenderTarget(Point2 size)
        {
            int width = Math.Max(1, size.X);
            int height = Math.Max(1, size.Y);
            if (m_renderTarget == null
                || m_renderTarget.Width != width
                || m_renderTarget.Height != height)
            {
                Utilities.Dispose(ref m_renderTarget);
                m_renderTarget = new RenderTarget2D(
                    width,
                    height,
                    1,
                    ColorFormat.Rgba8888,
                    DepthFormat.Depth24Stencil8);
            }
        }
    }
}
