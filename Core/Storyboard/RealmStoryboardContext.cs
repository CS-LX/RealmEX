using System;
using System.Collections.Generic;
using Game;
using RealmEX.Core.Scenes;

namespace RealmEX.Core.Storyboard
{
    public sealed class RealmStoryboardContext
    {
        private readonly List<RealmStoryboardAwaitOperation> m_operations = [];

        public SandboxRealm Realm { get; private set; }

        public RealmStoryboardAwaitable ApplyScene(RealmScene scene)
        {
            EnsureRealm();
            Realm.ApplyScene(scene);
            return default;
        }

        public RealmStoryboardAwaitable WaitFrames(int frames)
        {
            if (frames < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frames), "Frame count cannot be negative.");
            }
            if (frames == 0)
            {
                return default;
            }

            RealmStoryboardAwaitOperation operation = RealmStoryboardAwaitOperation.WaitFrames(frames);
            m_operations.Add(operation);
            return new RealmStoryboardAwaitable(operation);
        }

        public RealmStoryboardAwaitable WaitGameTime(double seconds)
        {
            if (seconds < 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), "Game time wait cannot be negative.");
            }
            if (seconds == 0.0)
            {
                return default;
            }

            EnsureRealm();
            SubsystemTime time = Realm.Project.FindSubsystem<SubsystemTime>(true);
            RealmStoryboardAwaitOperation operation =
                RealmStoryboardAwaitOperation.WaitGameTime(time.GameTime + seconds);
            m_operations.Add(operation);
            return new RealmStoryboardAwaitable(operation);
        }

        internal void Attach(SandboxRealm realm)
        {
            Realm = realm ?? throw new ArgumentNullException(nameof(realm));
        }

        internal void Update()
        {
            List<RealmStoryboardAwaitOperation> completedOperations = null;
            for (int i = m_operations.Count - 1; i >= 0; i--)
            {
                RealmStoryboardAwaitOperation operation = m_operations[i];
                if (!operation.Update(Realm))
                {
                    continue;
                }

                m_operations.RemoveAt(i);
                completedOperations ??= [];
                completedOperations.Add(operation);
            }

            if (completedOperations == null)
            {
                return;
            }

            for (int i = 0; i < completedOperations.Count; i++)
            {
                completedOperations[i].Continue();
            }
        }

        private void EnsureRealm()
        {
            if (Realm == null)
            {
                throw new InvalidOperationException("Storyboard context is not attached to a realm.");
            }
        }
    }
}
