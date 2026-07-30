using System.Collections.Generic;
using RealmEX.Core.Scenes;

namespace RealmEX.Core.Storyboard
{
    public sealed class RealmStoryboard
    {
        private readonly Queue<IRealmStoryboardStep> m_steps = [];
        private IRealmStoryboardStep m_currentStep;

        public bool IsCompleted => m_currentStep == null && m_steps.Count == 0;

        public void Enqueue(IRealmStoryboardStep step)
        {
            m_steps.Enqueue(step);
        }

        public void EnqueueScene(RealmScene scene)
        {
            Enqueue(new RealmSceneStep(scene));
        }

        public void EnqueueWaitFrames(int frames)
        {
            Enqueue(new RealmWaitFramesStep(frames));
        }

        public void Update(SandboxRealm realm)
        {
            while (true)
            {
                m_currentStep ??= m_steps.Count > 0 ? m_steps.Dequeue() : null;
                if (m_currentStep == null)
                {
                    return;
                }

                if (!m_currentStep.Update(realm))
                {
                    return;
                }
                m_currentStep = null;
            }
        }
    }
}
