using System;
using Game;

namespace RealmEX
{
    /// <summary>
    /// 可替换的 Realm 场景数据。P3 核心先验证切换场景无需改 Host/Bootstrap。
    /// </summary>
    public sealed class RealmScene
    {
        public RealmScene(string name, float timeFactor)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Scene name cannot be empty.", nameof(name));
            }
            if (timeFactor < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(timeFactor), "Scene time factor cannot be negative.");
            }

            Name = name.Trim();
            TimeFactor = timeFactor;
        }

        public string Name { get; }

        public float TimeFactor { get; }

        public void Apply(SandboxRealm realm)
        {
            ArgumentNullException.ThrowIfNull(realm);
            SubsystemTime time = realm.Project.FindSubsystem<SubsystemTime>(true);
            time.BasicGameTimeFactor = TimeFactor;
            time.GameTimeFactor = TimeFactor;
            time.GameMenuDialogTimeFactor = TimeFactor;
            realm.Profile.SceneName = Name;
            realm.Profile.TimeFactor = TimeFactor;
        }
    }
}
