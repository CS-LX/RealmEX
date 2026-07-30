using System;
using System.Runtime.CompilerServices;

namespace RealmEX.Core.Storyboard
{
    public readonly struct RealmStoryboardAwaitable
    {
        private readonly RealmStoryboardAwaitOperation m_operation;

        internal RealmStoryboardAwaitable(RealmStoryboardAwaitOperation operation)
        {
            m_operation = operation;
        }

        public RealmStoryboardAwaiter GetAwaiter()
        {
            return new RealmStoryboardAwaiter(m_operation);
        }

        public readonly struct RealmStoryboardAwaiter : INotifyCompletion
        {
            private readonly RealmStoryboardAwaitOperation m_operation;

            internal RealmStoryboardAwaiter(RealmStoryboardAwaitOperation operation)
            {
                m_operation = operation;
            }

            public bool IsCompleted => m_operation == null || m_operation.IsCompleted;

            public void OnCompleted(Action continuation)
            {
                if (m_operation == null)
                {
                    continuation?.Invoke();
                    return;
                }

                m_operation.SetContinuation(continuation);
            }

            public void GetResult()
            {
            }
        }
    }
}
