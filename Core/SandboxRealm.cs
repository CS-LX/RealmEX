using System;
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

        public void ApplyScene(RealmScene scene)
        {
            scene.Apply(this);
        }

        public void Tick()
        {
            Storyboard?.Update(this);
            Project.FindSubsystem<SubsystemUpdate>(true).Update();
        }

        public void Draw()
        {
            Viewport.DrawIfNeeded();
        }

        public void Dispose()
        {
            Viewport.Dispose();
            Project.Dispose();
        }
    }
}
