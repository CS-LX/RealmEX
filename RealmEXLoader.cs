using System;
using System.Collections.Generic;
using Engine.Input;
using Game;
using GameEntitySystem;

namespace RealmEX
{
    /// <summary>
    /// RealmEX 模组入口，负责 RealmHost 的初始化与主世界退出清理。
    /// </summary>
    public class RealmEXLoader : ModLoader
    {
        public override void __ModInitialize()
        {
            base.__ModInitialize();
            ModsManager.RegisterHook("OnLoadingFinished", this);
            ModsManager.RegisterHook("SubsystemUpdate", this);
            ModsManager.RegisterHook("OnProjectLoaded", this);
            ModsManager.RegisterHook("OnProjectDisposed", this);
        }

        public override void OnLoadingFinished(List<Action> actions)
        {
            base.OnLoadingFinished(actions);
            RealmHost.Initialize();
        }

        public override void SubsystemUpdate(SubsystemUpdate subsystemUpdate, float dt)
        {
            if (ReferenceEquals(subsystemUpdate.Project, GameManager.Project)
                && Keyboard.IsKeyDownOnce(Key.F8))
            {
                RealmM1Diagnostics.Run();
            }
        }

        public override void OnProjectLoaded(Project project)
        {
            Engine.Log.Information("[RealmEX/M1] READY scope=P1Core action=PressF8");
            base.OnProjectLoaded(project);
        }

        public override void OnProjectDisposed()
        {
            RealmHost.DisposeAll();
            base.OnProjectDisposed();
        }
    }
}
