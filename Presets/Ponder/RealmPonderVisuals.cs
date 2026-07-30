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
            new Color(110, 160, 210),
            new Color(100, 155, 205),
            new Color(120, 170, 215),
            new Color(95, 150, 200),
            new Color(85, 140, 195)
        ];

        public static void ApplyStep(SandboxRealm realm, RealmPonderStep step, int stepIndex, float orbitRadians = 0f)
        {
            ArgumentNullException.ThrowIfNull(realm);
            ArgumentNullException.ThrowIfNull(step);

            int colorIndex = Math.Abs(stepIndex) % StepClearColors.Length;
            realm.Viewport.ClearColor = StepClearColors[colorIndex];
            realm.Viewport.OrthographicWorldHeight = 11f;

            float baseAngle = MathUtils.DegToRad(35f) + stepIndex * 0.35f + orbitRadians * 0.25f;
            float radius = 11f;
            float height = 7.5f;
            Vector3 target = new(8.5f, 1.6f, 8.5f);
            realm.Viewport.LookTarget = target;
            realm.Viewport.LookPosition = new Vector3(
                target.X + MathF.Sin(baseAngle) * radius,
                height,
                target.Z + MathF.Cos(baseAngle) * radius);
            realm.Viewport.LookUp = Vector3.UnitY;
        }
    }
}
