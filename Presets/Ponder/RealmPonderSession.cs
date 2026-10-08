using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Owns the live Project. Backward seeking reconstructs it before replaying world callbacks and ticks.</summary>
    public sealed class RealmPonderSession : IDisposable
    {
        private readonly string m_id = "realmex-ponder-" + Guid.NewGuid().ToString("N");
        private readonly RealmPonderPlayer m_player;
        private readonly Dictionary<Point3, int> m_appliedBlocks = [];
        private int m_revision = -1;
        private bool m_disposed;
        public SandboxRealm Realm { get; private set; }
        public RealmPonderBlockPresenter Presenter { get; private set; }
        public RealmPonderWorld World { get; private set; }
        public event Action<SandboxRealm> RealmChanged;
        public RealmPonderSession(RealmPonderPlayer player)
        {
            ArgumentNullException.ThrowIfNull(player);
            if (player.HasSession) throw new InvalidOperationException("A Ponder player can own only one live session.");
            player.HasSession = true;
            m_player = player;
            player.Resetting += Reset; player.StateChanged += Apply;
            int tick = player.State.Tick;
            bool paused = player.IsPaused;
            try { player.Replay(); player.Seek(tick); player.IsPaused = paused; }
            catch { Dispose(); throw; }
        }
        private void Reset(RealmPonderState state)
        {
            RealmChanged?.Invoke(null); ReleaseRealm();
            m_appliedBlocks.Clear(); m_revision = -1;
            Realm = RealmHost.CreateRealm(m_id, projectTemplateName: m_player.Tutorial.ProjectTemplateName);
            if (Realm.Project.FindSubsystem<SubsystemTime>(true) is not Sandbox.PonderSubsystemTime)
                throw new InvalidOperationException("Ponder project templates must use PonderSubsystemTime for deterministic replay.");
            Realm.Profile.ParallelTick = false; Realm.Viewport.IsEnabled = true;
            World = new(Realm);
            Presenter = new(); Presenter.Attach(Realm, m_player);
            RealmChanged?.Invoke(Realm);
        }
        private void Apply(RealmPonderState state)
        {
            var terrain = Realm.Project.FindSubsystem<SubsystemTerrain>(true).Terrain;
            if (m_revision != state.GeometryRevision)
            {
                foreach (var cell in state.Blocks)
                {
                    if (m_appliedBlocks.TryGetValue(cell.Key, out int old) && old == cell.Value) continue;
                    Point3 p = cell.Key;
                    if (terrain.GetChunkAtCell(p.X, p.Z) == null) terrain.AllocateChunk(p.X >> 4, p.Z >> 4).State = TerrainChunkState.Valid;
                    Realm.Project.FindSubsystem<SubsystemTerrain>(true).ChangeCell(p.X, p.Y, p.Z, Terrain.ReplaceLight(cell.Value, 15));
                    m_appliedBlocks[p] = cell.Value;
                }
                m_revision = state.GeometryRevision; Presenter.Invalidate();
            }
            bool hadWorldInstructions = state.WorldInstructions.Count > 0;
            while (state.WorldInstructions.TryDequeue(out var action)) action(World);
            if (state.Tick > 0) Realm.Tick();
            if (hadWorldInstructions || m_player.Tutorial.ProjectTemplateName != RealmBootstrap.PonderProjectTemplateName)
            {
                foreach (Point3 p in state.Blocks.Keys.ToArray())
                {
                    int value = Terrain.ReplaceLight(terrain.GetCellValue(p.X, p.Y, p.Z), 0);
                    state.SetBlock(p, value); m_appliedBlocks[p] = value;
                }
                m_revision = state.GeometryRevision;
            }
            // Custom templates may animate devices and entities; mesh changes are observed at the same tick.
            if (hadWorldInstructions || m_player.Tutorial.ProjectTemplateName != RealmBootstrap.PonderProjectTemplateName) Presenter.Invalidate();
        }
        private void ReleaseRealm()
        {
            Presenter?.Dispose(); Presenter = null;
            if (Realm != null) { RealmHost.DestroyRealm(m_id); Realm = null; World = null; }
        }
        public void Dispose()
        {
            if (m_disposed) return; m_disposed = true;
            m_player.Resetting -= Reset; m_player.StateChanged -= Apply;
            m_player.HasSession = false;
            RealmChanged?.Invoke(null); ReleaseRealm();
        }
    }
}
