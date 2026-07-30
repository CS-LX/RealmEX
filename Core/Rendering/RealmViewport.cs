using System;

namespace RealmEX.Core.Rendering
{
    /// <summary>
    /// Realm 离屏渲染占位外壳。真实 RenderTarget / Camera 接入留给 P3 渲染子阶段。
    /// </summary>
    public sealed class RealmViewport : IDisposable
    {
        public bool IsEnabled { get; set; }

        public int DrawCount { get; private set; }

        public bool DrawIfNeeded()
        {
            if (!IsEnabled)
            {
                return false;
            }

            DrawCount++;
            return true;
        }

        public void Dispose()
        {
            IsEnabled = false;
        }
    }
}
