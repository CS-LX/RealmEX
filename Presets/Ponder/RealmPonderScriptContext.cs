using System;
using System.Threading.Tasks;
using Engine;
using RealmEX.Core;
using RealmEX.Core.Scenes;
using RealmEX.Core.Storyboard;

namespace RealmEX.Presets.Ponder
{
    public sealed class RealmPonderScriptContext
    {
        private readonly RealmStoryboardContext m_storyboardContext;
        private readonly Action<RealmPonderStep> m_stepStarted;
        private readonly int m_startStep;
        private int m_stepIndex = -1;

        internal RealmPonderScriptContext(
            RealmStoryboardContext storyboardContext,
            Action<RealmPonderStep> stepStarted,
            int startStep = 0)
        {
            m_storyboardContext = storyboardContext ?? throw new ArgumentNullException(nameof(storyboardContext));
            m_stepStarted = stepStarted;
            m_startStep = startStep;
            Camera = new RealmPonderCameraActions(this);
        }

        public SandboxRealm Realm => m_storyboardContext.Realm;

        public RealmPonderCameraActions Camera { get; }

        internal bool IsSeeking => m_stepIndex < m_startStep;

        public async Task ShowScene(
            string sceneName,
            string caption,
            Action<SandboxRealm, string> layout = null,
            float timeFactor = 1f,
            Color? clearColor = null,
            string title = null,
            RealmPonderAnnotation[] annotations = null)
        {
            m_stepIndex++;
            RealmPonderStep step = new(sceneName, timeFactor, caption, 0.0, layout != null, title, annotations);
            await m_storyboardContext.ApplyScene(new RealmScene(sceneName, timeFactor));
            layout?.Invoke(Realm, sceneName);
            // 颜色由流程显式控制；未指定时保持透明，方块叠在 Dialog mask 上。
            Realm.Viewport.ClearColor = clearColor ?? Color.Transparent;
            Camera.Apply();
            if (!IsSeeking)
            {
                m_stepStarted?.Invoke(step);
            }
        }

        public async Task Hold(double seconds)
        {
            if (seconds > 0.0 && !IsSeeking)
            {
                await m_storyboardContext.WaitGameTime(seconds);
            }
        }

        internal async Task WaitFrame()
        {
            await m_storyboardContext.WaitFrames(1);
        }
    }
}
