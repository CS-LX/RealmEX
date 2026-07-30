using System;
using Engine;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// Ponder 可见动画：按步骤切换清屏色与镜头，不改 Core 绘制管线。
    /// </summary>
    public static class RealmPonderVisuals
    {
        private static readonly Color[] StepClearColors =
        [
            new Color(24, 36, 56),
            new Color(28, 48, 40),
            new Color(56, 32, 32),
            new Color(40, 32, 56)
        ];

        public static void ApplyStep(SandboxRealm realm, RealmPonderStep step, int stepIndex, float orbitRadians = 0f)
        {
            ArgumentNullException.ThrowIfNull(realm);
            ArgumentNullException.ThrowIfNull(step);

            int colorIndex = Math.Abs(stepIndex) % StepClearColors.Length;
            realm.Viewport.ClearColor = StepClearColors[colorIndex];
            realm.Viewport.OrthographicWorldHeight = 14f + (stepIndex % 3);

            float baseAngle = stepIndex * 0.55f + orbitRadians;
            float radius = 10f;
            float height = 8f + (stepIndex % 2);
            Vector3 target = new(8.5f, 2f, 8.5f);
            realm.Viewport.LookTarget = target;
            realm.Viewport.LookPosition = new Vector3(
                target.X + MathF.Sin(baseAngle) * radius,
                height,
                target.Z + MathF.Cos(baseAngle) * radius);
            realm.Viewport.LookUp = Vector3.UnitY;
        }
    }
}
