namespace RealmEX
{
    /// <summary>
    /// 全局 Realm 调度占位。正式实现将负责创建/销毁 SandboxRealm、并行 tick 与持久化索引。
    /// </summary>
    public static class RealmHost
    {
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) {
                return;
            }
            IsInitialized = true;
            Engine.Log.Information("[RealmEX] RealmHost placeholder initialized (v1.0.0.0-preview1).");
        }
    }
}
