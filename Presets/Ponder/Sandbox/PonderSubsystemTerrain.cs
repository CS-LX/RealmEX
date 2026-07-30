using Engine;
using Engine.Audio;
using Game;
using TemplatesDatabase;

namespace RealmEX.Presets.Ponder.Sandbox
{
    /// <summary>
    /// Ponder 沙箱 stub 子系统。
    /// 硬规则：凡继承宿主 Subsystem 且未完整初始化依赖的 stub，必须自行接管
    /// Load / Update / Draw / Dispose，禁止落到宿主实现里碰 GameWidget、Views、Sky 等。
    /// </summary>
    public sealed class PonderSubsystemTerrain : SubsystemTerrain
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            Terrain = new Terrain();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Terrain.AllocateChunk(i, j).State = TerrainChunkState.Valid;
                }
            }

            // 仅供 ChangeCell 降级邻域状态；禁止走宿主 TerrainUpdater.Update / PrepareForDrawing。
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

        public override void Draw(Camera camera, int drawOrder)
        {
            // 宿主 Draw → TerrainUpdater.PrepareForDrawing 需要 camera.GameWidget。
        }

        public override void Update(float dt)
        {
            // 宿主 Update → TerrainUpdater.Update 需要 SubsystemSky 等。
        }
    }

    public sealed class PonderSubsystemGameInfo : SubsystemGameInfo
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            WorldSettings = new WorldSettings();
            DirectoryName = "RealmEX-Ponder";
            TotalElapsedGameTime = 0.0;
            TotalElapsedGameTimeDelta = 0f;
            WorldSeed = 0;
        }

        public override void Update(float dt)
        {
            // 宿主 Update 会用 m_subsystemTime / m_subsystemTimeOfDay（本 stub 未装）。
            TotalElapsedGameTime += dt;
            TotalElapsedGameTimeDelta = m_lastTotalElapsedGameTime.HasValue
                ? (float)(TotalElapsedGameTime - m_lastTotalElapsedGameTime.Value)
                : 0f;
            m_lastTotalElapsedGameTime = TotalElapsedGameTime;
        }
    }

    public sealed class PonderSubsystemAudio : SubsystemAudio
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            // 故意不找 SubsystemGameWidgets：Ponder 沙箱没有 Views。
        }

        public override void Update(float dt)
        {
            // 宿主 Update 遍历 m_subsystemViews.GameWidgets → NRE。
        }

        public override void Dispose()
        {
            foreach (Sound sound in m_sounds)
            {
                sound.Dispose();
            }
        }
    }

    public sealed class PonderSubsystemMovingBlocks : SubsystemMovingBlocks
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            Buffers = [];
            // 故意不装 Sky / AnimatedTextures：Ponder 不跑移动方块仿真。
        }

        public override void Update(float dt)
        {
            // 宿主 Update 需要 Terrain / Time / Sky。
        }

        public override void Draw(Camera camera, int drawOrder)
        {
            // 宿主 Draw 需要 Sky / AnimatedTextures / shader。
        }

        public override void Dispose()
        {
        }
    }

    public sealed class PonderSubsystemBlockBehaviors : SubsystemBlockBehaviors
    {
        public override void Load(ValuesDictionary valuesDictionary)
        {
            m_blockBehaviorsByContents = new SubsystemBlockBehavior[BlocksManager.Blocks.Length][];
            for (int i = 0; i < m_blockBehaviorsByContents.Length; i++)
            {
                m_blockBehaviorsByContents[i] = [];
            }
        }
    }
}
