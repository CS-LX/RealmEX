using Game;
using TemplatesDatabase;

namespace RealmEX.Subsystems
{
    /// <summary>
    /// 沙箱没有真实玩家界面，不能让原版空玩家菜单暂停逻辑冻结时间。
    /// </summary>
    public sealed class SandboxSubsystemTime : SubsystemTime
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            base.Load(valuesDictionary);
            GameMenuDialogTimeFactor = 1f;
        }
    }
}
