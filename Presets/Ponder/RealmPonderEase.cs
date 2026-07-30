using System;

namespace RealmEX.Presets.Ponder
{
    public static class RealmPonderEase
    {
        public static float Linear(float t)
        {
            return Clamp01(t);
        }

        public static float SinInOut(float t)
        {
            t = Clamp01(t);
            return 0.5f - 0.5f * MathF.Cos(MathF.PI * t);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }
            if (value > 1f)
            {
                return 1f;
            }
            return value;
        }
    }
}
