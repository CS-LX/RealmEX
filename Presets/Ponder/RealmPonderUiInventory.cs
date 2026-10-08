using Game;
using GameEntitySystem;

namespace RealmEX.Presets.Ponder
{
    /// <summary>UI-only inventory. It is never connected to the player's Project or saved.</summary>
    internal sealed class RealmPonderUiInventory : ComponentInventoryBase
    {
        public RealmPonderUiInventory(int slots)
        {
            Project project = new();
            project.m_subsystems.Add(new SubsystemTerrain
            {
                SubsystemAnimatedTextures = new SubsystemAnimatedTextures
                {
                    m_subsystemBlocksTexture = new SubsystemBlocksTexture
                    {
                        BlocksTexture = GameManager.Project?.FindSubsystem<SubsystemAnimatedTextures>(false)?.AnimatedBlocksTexture
                            ?? BlocksTexturesManager.DefaultBlocksTexture
                    }
                }
            });
            m_entity = new Entity { m_project = project, m_components = [this] };
            for (int i = 0; i < slots; i++) m_slots.Add(new Slot());
        }
        public override int GetSlotCapacity(int slotIndex, int value) => 9999;
    }
}
