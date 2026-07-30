using System;
using System.Threading.Tasks;
using Engine;
using Game;

namespace RealmEX.Presets.Ponder
{
    public sealed class RealmPonderCameraActions
    {
        private readonly RealmPonderScriptContext m_context;

        private float m_angleRadians = MathUtils.DegToRad(35f);
        private float m_radius = 11f;
        private float m_height = 7.5f;
        private Vector3 m_target = new(8.5f, 1.6f, 8.5f);
        private float m_worldHeight = 11f;

        internal RealmPonderCameraActions(RealmPonderScriptContext context)
        {
            m_context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void SetOrbit(float angleDegrees, float radius = 11f, float height = 7.5f)
        {
            m_angleRadians = MathUtils.DegToRad(angleDegrees);
            m_radius = radius;
            m_height = height;
            Apply();
        }

        public void LookAt(Vector3 target, float worldHeight = 11f)
        {
            m_target = target;
            m_worldHeight = worldHeight;
            Apply();
        }

        public async Task RotateBy(float degrees, double seconds, Func<float, float> ease = null)
        {
            if (seconds < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "Camera animation seconds cannot be negative.");
            }

            float start = m_angleRadians;
            float end = start + MathUtils.DegToRad(degrees);
            if (seconds == 0.0)
            {
                m_angleRadians = end;
                Apply();
                return;
            }

            ease ??= RealmPonderEase.Linear;
            SubsystemTime time = m_context.Realm.Project.FindSubsystem<SubsystemTime>(true);
            double startTime = time.GameTime;
            while (true)
            {
                double elapsed = time.GameTime - startTime;
                float t = (float)Math.Min(1.0, elapsed / seconds);
                m_angleRadians = MathUtils.Lerp(start, end, ease(t));
                Apply();
                if (t >= 1f)
                {
                    break;
                }
                await m_context.WaitFrame();
            }
        }

        internal void Apply()
        {
            Vector3 target = m_target;
            m_context.Realm.Viewport.LookTarget = target;
            m_context.Realm.Viewport.LookPosition = new Vector3(
                target.X + MathF.Sin(m_angleRadians) * m_radius,
                m_height,
                target.Z + MathF.Cos(m_angleRadians) * m_radius);
            m_context.Realm.Viewport.LookUp = Vector3.UnitY;
            m_context.Realm.Viewport.OrthographicWorldHeight = m_worldHeight;
        }
    }
}
