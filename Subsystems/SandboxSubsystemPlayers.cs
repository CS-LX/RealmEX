using Game;

namespace RealmEX.Subsystems
{
    /// <summary>
    /// 允许 Realm 在没有玩家数据时继续运行，且不会切换宿主界面。
    /// </summary>
    public sealed class SandboxSubsystemPlayers : SubsystemPlayers
    {
        public override void Update(float dt)
        {
            foreach (PlayerData playerData in m_playersData)
            {
                playerData.Update();
            }
        }
    }
}
