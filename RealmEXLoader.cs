using System;
using System.Collections.Generic;
using Game;
using RealmEX.Core;
using RealmEX.Presets.Ponder;

namespace RealmEX
{
    public class RealmEXLoader : ModLoader
    {
        public override void __ModInitialize()
        {
            base.__ModInitialize();
            ModsManager.RegisterHook("OnLoadingFinished", this);
            ModsManager.RegisterHook("SubsystemUpdate", this);
            ModsManager.RegisterHook("OnShowDialog", this);
            ModsManager.RegisterHook("OnProjectDisposed", this);
        }
        public override void OnLoadingFinished(List<Action> actions)
        {
            base.OnLoadingFinished(actions);
            RealmPonderBrowser.Reset();
            RealmHost.Initialize();
        }
        public override void SubsystemUpdate(SubsystemUpdate subsystemUpdate, float dt)
        {
            if (ReferenceEquals(subsystemUpdate.Project, GameManager.Project)) RealmHost.TickParallel(dt);
        }
        public override void OnShowDialog(ref ContainerWidget parentWidget, ref Dialog dialog)
        {
            if (dialog is GameMenuDialog menu && parentWidget != null) RealmPonderBrowser.AddMenuEntry(menu, parentWidget);
        }
        public override void OnProjectDisposed()
        {
            RealmPonderBrowser.Reset();
            RealmHost.DisposeAll();
            base.OnProjectDisposed();
        }
    }
}
