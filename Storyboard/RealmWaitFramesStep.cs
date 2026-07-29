using System;

namespace RealmEX
{
    public sealed class RealmWaitFramesStep : IRealmStoryboardStep
    {
        private int m_remainingFrames;

        public RealmWaitFramesStep(int frames)
        {
            if (frames < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frames), "Frame count cannot be negative.");
            }
            m_remainingFrames = frames;
        }

        public bool Update(SandboxRealm realm)
        {
            _ = realm;
            if (m_remainingFrames <= 0)
            {
                return true;
            }

            m_remainingFrames--;
            return m_remainingFrames <= 0;
        }
    }
}
