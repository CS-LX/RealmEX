using System;
using System.Threading.Tasks;

namespace RealmEX.Core.Storyboard
{
    public sealed class RealmAsyncStoryboardStep : IRealmStoryboardStep
    {
        private readonly Func<RealmStoryboardContext, Task> m_script;
        private readonly RealmStoryboardContext m_context = new();
        private Task m_task;

        public RealmAsyncStoryboardStep(Func<RealmStoryboardContext, Task> script)
        {
            m_script = script ?? throw new ArgumentNullException(nameof(script));
        }

        public bool Update(SandboxRealm realm)
        {
            m_context.Attach(realm);
            m_task ??= m_script(m_context);
            m_context.Update();

            if (!m_task.IsCompleted)
            {
                return false;
            }
            if (m_task.IsFaulted)
            {
                throw new InvalidOperationException("Async storyboard script failed.", m_task.Exception);
            }
            if (m_task.IsCanceled)
            {
                throw new OperationCanceledException("Async storyboard script was canceled.");
            }

            return true;
        }
    }
}
