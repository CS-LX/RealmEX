using System;
using Game;

namespace RealmEX.Core.Storyboard
{
    internal sealed class RealmStoryboardAwaitOperation
    {
        private readonly Func<SandboxRealm, bool> m_update;
        private Action m_continuation;

        public RealmStoryboardAwaitOperation(Func<SandboxRealm, bool> update)
        {
            m_update = update ?? throw new ArgumentNullException(nameof(update));
        }

        public bool IsCompleted { get; private set; }

        public bool Update(SandboxRealm realm)
        {
            if (IsCompleted)
            {
                return true;
            }
            if (!m_update(realm))
            {
                return false;
            }

            IsCompleted = true;
            return true;
        }

        public void SetContinuation(Action continuation)
        {
            if (IsCompleted)
            {
                continuation?.Invoke();
                return;
            }

            m_continuation = continuation;
        }

        public void Continue()
        {
            m_continuation?.Invoke();
        }

        public static RealmStoryboardAwaitOperation WaitFrames(int frames)
        {
            int remainingFrames = frames;
            return new RealmStoryboardAwaitOperation(_ =>
            {
                if (remainingFrames <= 0)
                {
                    return true;
                }

                remainingFrames--;
                return remainingFrames <= 0;
            });
        }

        public static RealmStoryboardAwaitOperation WaitGameTime(double targetGameTime)
        {
            return new RealmStoryboardAwaitOperation(realm =>
            {
                SubsystemTime time = realm.Project.FindSubsystem<SubsystemTime>(true);
                return time.GameTime >= targetGameTime;
            });
        }
    }
}
