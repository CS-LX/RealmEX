using Engine;
using Game;
using TemplatesDatabase;

namespace RealmEX.Diagnostics
{
    /// <summary>
    /// M1T 诊断专用 Terrain。它只满足 BlockEntity 注册路径的类型依赖，
    /// 不代表完整宿主 Terrain 方块变更验收。
    /// </summary>
    public sealed class DiagnosticSubsystemTerrain : SubsystemTerrain
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            Terrain = new Terrain();
            Terrain.AllocateChunk(0, 0).State = TerrainChunkState.Valid;
            TerrainUpdater = new TerrainUpdater
            {
                m_terrain = Terrain
            };
            m_subsystemBlockBehaviors = Project.FindSubsystem<SubsystemBlockBehaviors>(true);
        }

        public override void Dispose()
        {
            Terrain?.Dispose();
        }
    }

    public sealed class DiagnosticSubsystemGameInfo : SubsystemGameInfo
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            WorldSettings = new WorldSettings();
            DirectoryName = "RealmEX-M1T";
            TotalElapsedGameTime = 0.0;
            TotalElapsedGameTimeDelta = 0f;
            WorldSeed = 0;
        }
    }

    public sealed class DiagnosticSubsystemAudio : SubsystemAudio
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
        }
    }

    public sealed class DiagnosticSubsystemMovingBlocks : SubsystemMovingBlocks
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            Buffers = [];
        }

        public override void Dispose()
        {
        }
    }

    public sealed class DiagnosticSubsystemBlockBehaviors : SubsystemBlockBehaviors
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            m_blockBehaviorsByContents = new SubsystemBlockBehavior[BlocksManager.Blocks.Length][];
            for (int i = 0; i < m_blockBehaviorsByContents.Length; i++)
            {
                m_blockBehaviorsByContents[i] = [];
            }

            SubsystemChestBlockBehavior chestBehavior =
                Project.FindSubsystem<SubsystemChestBlockBehavior>(true);
            m_blockBehaviorsByContents[45] = [chestBehavior];
            m_blockBehaviors.Add(chestBehavior);
        }
    }
}
