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

        internal RealmPonderScriptContext(
            RealmStoryboardContext storyboardContext,
            Action<RealmPonderStep> stepStarted)
        {
            m_storyboardContext = storyboardContext ?? throw new ArgumentNullException(nameof(storyboardContext));
            m_stepStarted = stepStarted;
            Camera = new RealmPonderCameraActions(this);
        }

        public SandboxRealm Realm => m_storyboardContext.Realm;

        public RealmPonderCameraActions Camera { get; }

        public async Task ShowScene(
            string sceneName,
            string caption,
            Action<SandboxRealm, string> layout = null,
            float timeFactor = 1f,
            Color? clearColor = null)
        {
            RealmPonderStep step = new(sceneName, timeFactor, caption, 0.0, layout != null);
            await m_storyboardContext.ApplyScene(new RealmScene(sceneName, timeFactor));
            layout?.Invoke(Realm, sceneName);
            // 颜色由流程显式控制；未指定时保持透明，方块叠在 Dialog mask 上。
            Realm.Viewport.ClearColor = clearColor ?? Color.Transparent;
            Camera.Apply();
            m_stepStarted?.Invoke(step);
        }

        public async Task Hold(double seconds)
        {
            if (seconds > 0.0)
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
