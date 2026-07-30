using System;
using Engine;
using Game;
using RealmEX.Core.Rendering;
using RealmEX.Core.Scenes;
using RealmEX.Core.Storyboard;

namespace RealmEX.Core
{
    /// <summary>
    /// 单个 Realm 的生命周期容器：Project + Profile + Storyboard + Viewport。
    /// </summary>
    public sealed class SandboxRealm : IDisposable
    {
        private SubsystemUpdate m_subsystemUpdate;
        private SubsystemDrawing m_subsystemDrawing;

        public SandboxRealm(SandboxProject project, RealmProfile profile = null)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Profile = profile ?? new RealmProfile { RealmId = project.RealmId };
            if (string.IsNullOrWhiteSpace(Profile.RealmId))
            {
                Profile.RealmId = project.RealmId;
            }
        }

        public string RealmId => Project.RealmId;

        public SandboxProject Project { get; }

        public RealmProfile Profile { get; }

        public RealmViewport Viewport { get; } = new();

        public RealmStoryboard Storyboard { get; set; }

        public bool IsDisposed => Project.IsDisposed;

        public SubsystemUpdate SubsystemUpdate => m_subsystemUpdate ??= Project.FindSubsystem<SubsystemUpdate>(true);

        public SubsystemDrawing SubsystemDrawing => m_subsystemDrawing ??= Project.FindSubsystem<SubsystemDrawing>(true);

        public void ApplyScene(RealmScene scene)
        {
            scene.Apply(this);
        }

        public void Tick()
        {
            Storyboard?.Update(this);
            SubsystemUpdate.Update();
        }

        public void Draw(Point2 size)
        {
            Viewport.DrawIfNeeded(this, size);
        }

        public void Dispose()
        {
            Viewport.Dispose();
            Project.Dispose();
        }
    }
}
