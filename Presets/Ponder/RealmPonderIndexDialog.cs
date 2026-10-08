using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Game;

namespace RealmEX.Presets.Ponder
{
    public sealed class RealmPonderIndexDialog : Dialog
    {
        private readonly RealmPonderRegistry m_registry;
        private readonly Action<RealmPonderTutorial> m_selected;
        private readonly Action m_closed;
        private readonly IReadOnlyList<RealmPonderTutorial> m_subjects;
        private readonly TextBoxWidget m_search;
        private readonly ListPanelWidget m_scenes;
        private readonly string m_language = LanguageControl.CurrentLanguageName;
        private string m_tag;
        private string m_query;
        private bool m_isClosed;
        public RealmPonderIndexDialog(RealmPonderRegistry registry, Action<RealmPonderTutorial> selected, Action closed, IReadOnlyList<RealmPonderTutorial> subjects = null)
        {
            m_registry = registry; m_selected = selected; m_closed = closed; m_subjects = subjects;
            HorizontalAlignment = VerticalAlignment = WidgetAlignment.Center;
            LoadContents(this, ContentManager.Get<XElement>("Dialogs/RealmPonderIndexDialog"));
            string Local(string key) => RealmPonderLocalization.BuiltIn.Text(key).Resolve(m_language);
            Children.Find<LabelWidget>("Index.Title").Text = Local("ui.index.title");
            Children.Find<LabelWidget>("Index.SearchLabel").Text = Local("ui.index.search");
            Children.Find<LabelWidget>("Index.Empty").Text = Local("ui.index.empty");
            m_search = Children.Find<TextBoxWidget>("Index.Search");
            m_scenes = Children.Find<ListPanelWidget>("Index.Scenes");
            m_scenes.ItemWidgetFactory = item =>
            {
                var tutorial = (RealmPonderTutorial)item;
                CanvasWidget row = new() { IsHitTestVisible = false };
                row.Children.Add(new RectangleWidget { Size = new(float.PositiveInfinity, 1), FillColor = new(24, 24, 24, 24), OutlineColor = Color.Transparent, VerticalAlignment = WidgetAlignment.Far, IsHitTestVisible = false });
                var subjects = registry.Entry(tutorial.Id).Subjects;
                if (subjects.Count > 0) row.Children.Add(new BlockIconWidget { Value = Terrain.ReplaceLight(subjects[0], 15), Size = new(56), Margin = new(8, 4), HorizontalAlignment = WidgetAlignment.Near, VerticalAlignment = WidgetAlignment.Center });
                row.Children.Add(new LabelWidget { Text = tutorial.Title.Resolve(m_language), FontScale = 1, WordWrap = true, Margin = new(subjects.Count > 0 ? 76 : 12, 6), VerticalAlignment = WidgetAlignment.Center, IsHitTestVisible = false });
                return row;
            };
            m_scenes.ItemClicked = item => { m_selected((RealmPonderTutorial)item); Close(); };
            var tags = Children.Find<ListPanelWidget>("Index.Tags");
            tags.ScrollPosition = 0; tags.ScrollSpeed = 0;
            tags.SelectionColor = new(24, 24, 24, 24);
            m_scenes.SelectionColor = new(24, 24, 24, 24);
            tags.AddItem(new RealmPonderTag("", RealmPonderLocalization.BuiltIn.Text("ui.index.all"), ""));
            tags.AddItems(registry.Tags.Cast<object>());
            tags.SelectedIndex = 0;
            tags.ItemWidgetFactory = item => new LabelWidget { Text = ((RealmPonderTag)item).Title.Resolve(m_language), FontScale = 1, WordWrap = true, HorizontalAlignment = WidgetAlignment.Center, VerticalAlignment = WidgetAlignment.Center };
            tags.ItemClicked = item => { m_tag = ((RealmPonderTag)item).Id; Reload(); };
            Reload();
        }
        private void Reload()
        {
            m_query = m_search.Text ?? "";
            m_scenes.ClearItems();
            var ids = m_subjects?.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var entry in m_registry.Search(m_query, string.IsNullOrEmpty(m_tag) ? null : m_tag, m_language, includeHidden: m_subjects != null))
                if (ids == null || ids.Contains(entry.Tutorial.Id)) m_scenes.AddItem(entry.Tutorial);
            m_scenes.ScrollPosition = 0;
            m_scenes.ScrollSpeed = 0;
            Children.Find<LabelWidget>("Index.Empty").IsVisible = m_scenes.Items.Count == 0;
        }
        public override void ChangeParent(ContainerWidget parentWidget)
        {
            base.ChangeParent(parentWidget);
            if (parentWidget != null && DialogsManager.m_animationData.TryGetValue(this, out var animation))
                animation.CoverWidget.FillColor = new Color(0, 0, 0, 48);
        }
        public override void MeasureOverride(Vector2 available)
        {
            Size = new(Math.Min(720, available.X - 32), Math.Min(640, available.Y - 32));
            Children.Find<CanvasWidget>("Index.Header").Size = new(Size.X - 40, 56);
            base.MeasureOverride(available);
        }
        public override void Update()
        {
            if (Input.Back || Input.Cancel || Children.Find<RealmPonderButtonWidget>("Index.Close").IsClicked)
            {
                Input.Clear(); Close(); return;
            }
            if ((m_search.Text ?? "") != m_query) Reload();
        }
        public void Close() { if (m_isClosed) return; m_isClosed = true; m_search.HasFocus = false; DialogsManager.HideDialog(this); m_closed(); }
    }
}
