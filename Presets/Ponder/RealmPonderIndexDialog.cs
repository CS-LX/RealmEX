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
        private string m_series;
        private string m_tag;
        private string m_query;
        private bool m_isClosed;
        public RealmPonderIndexDialog(RealmPonderRegistry registry, Action<RealmPonderTutorial> selected, Action closed, IReadOnlyList<RealmPonderTutorial> subjects = null, string series = null)
        {
            m_registry = registry; m_selected = selected; m_closed = closed; m_subjects = subjects; m_series = series;
            HorizontalAlignment = VerticalAlignment = WidgetAlignment.Center;
            LoadContents(this, ContentManager.Get<XElement>("Dialogs/RealmPonderIndexDialog"));
            Children.Find<LabelWidget>("Index.SearchLabel").Text = Local("ui.index.search");
            Children.Find<LabelWidget>("Index.Empty").Text = Local("ui.index.empty");
            m_search = Children.Find<TextBoxWidget>("Index.Search");
            m_scenes = Children.Find<ListPanelWidget>("Index.Scenes");
            m_scenes.ItemWidgetFactory = item =>
            {
                bool isSeries = item is RealmPonderSeries;
                var entry = isSeries ? Entries().First(e => e.SeriesId == ((RealmPonderSeries)item).Id) : registry.Entry(((RealmPonderTutorial)item).Id);
                string title = isSeries ? ((RealmPonderSeries)item).Title.Resolve(m_language) : entry.Tutorial.Title.Resolve(m_language);
                if (isSeries) title = RealmPonderLocalization.BuiltIn.Text("ui.index.series", title, Entries().Count(e => e.SeriesId == ((RealmPonderSeries)item).Id)).Resolve(m_language);
                CanvasWidget row = new() { IsHitTestVisible = false };
                row.Children.Add(new RectangleWidget { Size = new(float.PositiveInfinity, 1), FillColor = new(24, 24, 24, 24), OutlineColor = Color.Transparent, VerticalAlignment = WidgetAlignment.Far, IsHitTestVisible = false });
                if (entry.Subjects.Count > 0) row.Children.Add(new BlockIconWidget { Value = Terrain.ReplaceLight(entry.Subjects[0], 15), Size = new(56), Margin = new(8, 4), HorizontalAlignment = WidgetAlignment.Near, VerticalAlignment = WidgetAlignment.Center });
                row.Children.Add(new LabelWidget { Text = title, FontScale = 1, WordWrap = true, Margin = new(entry.Subjects.Count > 0 ? 76 : 12, 6), VerticalAlignment = WidgetAlignment.Center, IsHitTestVisible = false });
                return row;
            };
            m_scenes.ItemClicked = item =>
            {
                if (item is RealmPonderSeries chosen) { m_series = chosen.Id; Reload(); }
                else { m_selected((RealmPonderTutorial)item); Close(); }
            };
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
        private string Local(string key) => RealmPonderLocalization.BuiltIn.Text(key).Resolve(m_language);
        private IEnumerable<RealmPonderEntry> Entries()
        {
            var ids = m_subjects?.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            return m_registry.Search(m_query, string.IsNullOrEmpty(m_tag) ? null : m_tag, m_language, includeHidden: m_subjects != null)
                .Where(e => ids == null || ids.Contains(e.Tutorial.Id));
        }
        private void Reload()
        {
            m_query = m_search.Text ?? "";
            m_scenes.ClearItems();
            var entries = Entries().ToArray();
            if (m_series == null)
            {
                var items = entries.Where(e => e.SeriesId == null).Select(e => (Item: (object)e.Tutorial, e.Order, Id: e.Tutorial.Id))
                    .Concat(entries.Where(e => e.SeriesId != null).Select(e => e.SeriesId).Distinct().Select(id => m_registry.GetSeries(id))
                        .Select(s => (Item: (object)s, s.Order, s.Id)));
                foreach (var item in items.OrderBy(i => i.Order).ThenBy(i => i.Id, StringComparer.Ordinal)) m_scenes.AddItem(item.Item);
            }
            else foreach (var entry in entries.Where(e => e.SeriesId == m_series)) m_scenes.AddItem(entry.Tutorial);
            Children.Find<LabelWidget>("Index.Title").Text = m_series == null ? Local("ui.index.title") : m_registry.GetSeries(m_series).Title.Resolve(m_language);
            Children.Find<RealmPonderButtonWidget>("Index.Back").IsVisible = m_series != null;
            m_scenes.ScrollPosition = 0; m_scenes.ScrollSpeed = 0;
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
            var header = Children.Find<CanvasWidget>("Index.Header");
            var title = Children.Find<LabelWidget>("Index.Title");
            float left = m_series == null ? 0 : 56;
            title.Size = new(Math.Max(1, Size.X - 96 - left), -1);
            title.Measure(new(title.Size.X, float.PositiveInfinity));
            header.Size = new(Size.X - 40, Math.Max(56, title.DesiredSize.Y));
            header.SetWidgetPosition(title, new(left, Math.Max(0, (header.Size.Y - title.DesiredSize.Y) / 2)));
            base.MeasureOverride(available);
        }
        public override void Update()
        {
            if (Children.Find<RealmPonderButtonWidget>("Index.Close").IsClicked) { Close(); return; }
            if (Input.Back || Input.Cancel || Children.Find<RealmPonderButtonWidget>("Index.Back").IsClicked)
            {
                Input.Clear();
                if (m_series != null) { m_series = null; Reload(); } else Close();
                return;
            }
            if ((m_search.Text ?? "") != m_query) Reload();
        }
        public void Close() { if (m_isClosed) return; m_isClosed = true; m_search.HasFocus = false; DialogsManager.HideDialog(this); m_closed(); }
    }
}
