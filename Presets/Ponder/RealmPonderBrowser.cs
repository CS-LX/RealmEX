using System;
using System.Linq;
using System.Xml.Linq;
using Game;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Shared content catalog and player-owned entry into the tutorial index.</summary>
    public static class RealmPonderBrowser
    {
        private static RealmPonderRegistry m_registry;
        public static void AddMenuEntry(Dialog menu, ContainerWidget owner)
        {
            ArgumentNullException.ThrowIfNull(menu); ArgumentNullException.ThrowIfNull(owner);
            if (menu.Children.Find<ButtonWidget>("RealmEX.Ponder", false) != null) return;
            var more = menu.Children.Find<ButtonWidget>("More", false);
            if (more?.ParentWidget?.ParentWidget is not StackPanelWidget buttons) return;
            var button = (BevelledButtonWidget)Widget.LoadWidget(null, ContentManager.Get<XElement>("Widgets/RealmPonderMenuEntry"), null);
            button.Text = RealmPonderLocalization.BuiltIn.Text(button.Text).Resolve(LanguageControl.CurrentLanguageName);
            button.Update1 = () =>
            {
                button.Update();
                if (!button.IsClicked) return;
                DialogsManager.HideDialog(menu);
                Open(owner);
            };
            buttons.Children.Add(button);
        }
        public static void Open(ContainerWidget owner, int? subject = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (DialogsManager.Dialogs.Any(d => d.ParentWidget == owner && d is RealmPonderDialog or RealmPonderIndexDialog)) return;
            try
            {
                m_registry ??= RealmPonderContentLoader.Discover(ModsManager.ModList, message => Engine.Log.Warning("[RealmEX/Ponder] " + message));
                var registry = m_registry;
                var subjects = subject.HasValue ? registry.ForSubject(subject.Value) : null;
                var index = new RealmPonderIndexDialog(registry, tutorial =>
                    DialogsManager.ShowDialog(owner, new RealmPonderDialog(tutorial, registry, subjects)), () => { }, subjects);
                DialogsManager.ShowDialog(owner, index);
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] Could not open the tutorial catalog: {ex}");
                string Local(string key) => RealmPonderLocalization.BuiltIn.Text(key).Resolve(LanguageControl.CurrentLanguageName);
                DialogsManager.ShowDialog(owner, new MessageDialog(Local("ui.unavailable"), Local("ui.unavailable_detail"), LanguageControl.Ok, null, null));
            }
        }
        public static void Reset()
        {
            foreach (Dialog dialog in DialogsManager.Dialogs.ToArray())
                if (dialog is RealmPonderDialog tutorial) tutorial.Close();
                else if (dialog is RealmPonderIndexDialog index) index.Close();
            m_registry = null;
        }
    }
}
