using System;
using System.Collections.Generic;
using Engine.Input;
using Game;
using GameEntitySystem;
using RealmEX.Core;
using RealmEX.Diagnostics;

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
            if (!ReferenceEquals(subsystemUpdate.Project, GameManager.Project))
            {
                return;
            }

            RealmHost.TickParallel(dt);
            RealmM2Diagnostics.Update(subsystemUpdate);
            RealmM3Diagnostics.Update(subsystemUpdate);

            if (Keyboard.IsKeyDownOnce(Key.F8))
            {
                RealmM1Diagnostics.Run();
            }
            if (Keyboard.IsKeyDownOnce(Key.F9))
            {
                RealmM2Diagnostics.Start();
            }
            if (Keyboard.IsKeyDownOnce(Key.F10))
            {
                RealmM3Diagnostics.Start();
            }
        }

        public override void OnProjectLoaded(Project project)
        {
            Engine.Log.Information("[RealmEX/M1] READY scope=P1Core action=PressF8");
            Engine.Log.Information("[RealmEX/M2] READY scope=P2Tick action=PressF9");
            Engine.Log.Information("[RealmEX/M3] READY scope=P3Scene action=PressF10");
            base.OnProjectLoaded(project);
        }

        public override void OnProjectDisposed()
        {
            RealmM2Diagnostics.Cancel();
            RealmM3Diagnostics.Cancel();
            RealmHost.DisposeAll();
            base.OnProjectDisposed();
        }
    }
}
