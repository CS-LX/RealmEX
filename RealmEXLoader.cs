using System;
using System.Collections.Generic;
using Game;

namespace RealmEX
{
    /// <summary>
    /// RealmEX 模组入口。当前为占位实现，后续在此注册并行 tick Hook 与 RealmHost 生命周期。
    /// </summary>
    public class RealmEXLoader : ModLoader
    {
        public override void __ModInitialize()
        {
            base.__ModInitialize();
            ModsManager.RegisterHook("OnLoadingFinished", this);
        }

        public override void OnLoadingFinished(List<Action> actions)
        {
            base.OnLoadingFinished(actions);
            RealmHost.Initialize();
        }
    }
}
