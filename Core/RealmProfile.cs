namespace RealmEX.Core
{
    /// <summary>
    /// Realm 运行策略。P3 先承载场景名与时间倍率，后续扩展 tick 预算与持久化策略。
    /// </summary>
    public sealed class RealmProfile
    {
        public string RealmId { get; set; } = string.Empty;

        public string SceneName { get; set; } = string.Empty;

        public float TimeFactor { get; set; } = 1f;

        public bool ParallelTick { get; set; } = true;
    }
}
