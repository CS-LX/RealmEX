using Engine;
using Engine.Graphics;
using Game;

namespace RealmEX.Core.Rendering
{
    /// <summary>
    /// Realm 离屏相机，不依赖玩家 GameWidget。
    /// </summary>
    public sealed class RealmCamera : Camera
    {
        private Vector3 m_viewPosition = new(8.5f, 8f, 18f);
        private Vector3 m_viewDirection = Vector3.Normalize(new Vector3(0f, -0.35f, -1f));
        private Vector3 m_viewUp = Vector3.UnitY;
        private Vector3 m_viewRight = Vector3.UnitX;
        private Vector2 m_viewportSize = new(512f, 512f);
        private Matrix m_projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathUtils.DegToRad(45f),
            1f,
            0.1f,
            2048f);
        private BoundingFrustum m_viewFrustum;

        public RealmCamera() : base(null)
        {
            RecalculateAxes();
        }

        public override Vector3 ViewPosition => m_viewPosition;

        public override Vector3 ViewDirection => m_viewDirection;

        public override Vector3 ViewUp => m_viewUp;

        public override Vector3 ViewRight => m_viewRight;

        public override Matrix ViewMatrix => Matrix.CreateLookAt(
            m_viewPosition,
            m_viewPosition + m_viewDirection,
            m_viewUp);

        public override Matrix InvertedViewMatrix => Matrix.Invert(ViewMatrix);

        public override Matrix ProjectionMatrix => m_projectionMatrix;

        public override Matrix ScreenProjectionMatrix => m_projectionMatrix;

        public override Matrix InvertedProjectionMatrix => Matrix.Invert(m_projectionMatrix);

        public override Matrix ViewProjectionMatrix => ViewMatrix * m_projectionMatrix;

        public override Vector2 ViewportSize => m_viewportSize;

        public override Matrix ViewportMatrix => Matrix.Identity;

        public override BoundingFrustum ViewFrustum
        {
            get
            {
                m_viewFrustum ??= new BoundingFrustum(ViewProjectionMatrix);
                m_viewFrustum.Matrix = ViewProjectionMatrix;
                return m_viewFrustum;
            }
        }

        public override bool UsesMovementControls => false;

        public override bool IsEntityControlEnabled => false;

        public void SetupPerspective(
            Vector3 position,
            Vector3 target,
            Vector3 up,
            Vector2 viewportSize,
            float fieldOfView = 45f)
        {
            m_viewPosition = position;
            m_viewDirection = Vector3.Normalize(target - position);
            m_viewUp = Vector3.Normalize(up);
            m_viewportSize = viewportSize;
            float aspectRatio = viewportSize.X > 0f && viewportSize.Y > 0f
                ? viewportSize.X / viewportSize.Y
                : 1f;
            m_projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathUtils.DegToRad(fieldOfView),
                aspectRatio,
                0.1f,
                2048f);
            RecalculateAxes();
        }

        public void SetupOrthographic(
            Vector3 position,
            Vector3 target,
            Vector3 up,
            Vector2 viewportSize,
            float worldHeight)
        {
            m_viewPosition = position;
            m_viewDirection = Vector3.Normalize(target - position);
            m_viewUp = Vector3.Normalize(up);
            m_viewportSize = viewportSize;
            float aspectRatio = viewportSize.X > 0f && viewportSize.Y > 0f
                ? viewportSize.X / viewportSize.Y
                : 1f;
            m_projectionMatrix = Matrix.CreateOrthographic(worldHeight * aspectRatio, worldHeight, 0.1f, 2048f);
            RecalculateAxes();
        }

        public override void Update(float dt)
        {
            _ = dt;
        }

        public override void PrepareForDrawing()
        {
            m_viewFrustum = null;
        }

        private void RecalculateAxes()
        {
            m_viewRight = Vector3.Normalize(Vector3.Cross(m_viewDirection, m_viewUp));
            m_viewUp = Vector3.Normalize(Vector3.Cross(m_viewRight, m_viewDirection));
            m_viewFrustum = null;
        }
    }
}
