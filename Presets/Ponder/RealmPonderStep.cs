using System;
using Engine;

namespace RealmEX.Presets.Ponder
{
    public sealed record RealmPonderAnnotation(Vector3 Position, string Text, Color Color, Vector2 LabelOffset);

    public sealed class RealmPonderStep
    {
        public RealmPonderStep(
            string sceneName,
            float timeFactor,
            string caption,
            double waitGameTimeSeconds,
            bool buildsWorld = false,
            string title = null,
            RealmPonderAnnotation[] annotations = null)
        {
            SceneName = sceneName;
            TimeFactor = timeFactor;
            Caption = caption;
            WaitGameTimeSeconds = waitGameTimeSeconds;
            BuildsWorld = buildsWorld;
            Title = title ?? "观察场景";
            Annotations = annotations ?? Array.Empty<RealmPonderAnnotation>();
        }

        public string SceneName { get; }

        public float TimeFactor { get; }

        public string Caption { get; }

        public double WaitGameTimeSeconds { get; }

        public bool BuildsWorld { get; }

        public string Title { get; }

        public RealmPonderAnnotation[] Annotations { get; }
    }
}
