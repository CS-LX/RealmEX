using RealmEX.Core.Scenes;

namespace RealmEX.Core.Storyboard
{
    public sealed class RealmSceneStep : IRealmStoryboardStep
    {
        private readonly RealmScene m_scene;

        public RealmSceneStep(RealmScene scene)
        {
            m_scene = scene;
        }

        public bool Update(SandboxRealm realm)
        {
            realm.ApplyScene(m_scene);
            return true;
        }
    }
}
