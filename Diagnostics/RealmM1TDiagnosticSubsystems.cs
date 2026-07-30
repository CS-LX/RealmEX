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
}
