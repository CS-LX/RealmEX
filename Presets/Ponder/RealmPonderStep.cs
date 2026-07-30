namespace RealmEX.Presets.Ponder
{
    public sealed class RealmPonderStep
    {
        public RealmPonderStep(
            string sceneName,
            float timeFactor,
            string caption,
            double waitGameTimeSeconds,
            bool buildsWorld = false)
        {
            SceneName = sceneName;
            TimeFactor = timeFactor;
            Caption = caption;
            WaitGameTimeSeconds = waitGameTimeSeconds;
            BuildsWorld = buildsWorld;
        }

        public string SceneName { get; }

        public float TimeFactor { get; }

        public string Caption { get; }

        public double WaitGameTimeSeconds { get; }

        public bool BuildsWorld { get; }
    }
}
