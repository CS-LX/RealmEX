using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Input;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Presentation and transport are driven by the same player used by scripts and previews.</summary>
    public sealed class RealmPonderDialog : Dialog
    {
        private readonly RealmPonderRegistry m_registry;
        private IReadOnlyList<RealmPonderTutorial> m_sequence;
        private readonly RealmPonderWidget m_viewport;
        private readonly RealmPonderTimelineWidget m_timeline;
        private readonly Dictionary<string, RealmPonderButtonWidget> m_buttons = [];
        private readonly string m_language = LanguageControl.CurrentLanguageName;
        private RealmPonderSession m_session;
        private bool m_pausedBeforeInspect;
        private bool m_failed;
        private RealmPonderIndexDialog m_index;
        public RealmPonderPlayer Player { get; private set; }
        public bool IsCompleted => Player.IsCompleted;
        public bool IsClosed { get; private set; }
        public string TutorialId => Player.Tutorial.Id;
        public RealmPonderDialog(RealmPonderTutorial tutorial, RealmPonderRegistry registry = null, IReadOnlyList<RealmPonderTutorial> sequence = null)
        {
            m_registry = registry ?? new();
            if (registry == null) m_registry.Register(tutorial);
            m_sequence = m_registry.Sequence(tutorial.Id, sequence);
            HorizontalAlignment = VerticalAlignment = WidgetAlignment.Stretch;
            LoadContents(this, ContentManager.Get<XElement>("Dialogs/RealmPonderDialog"));
            m_viewport = Children.Find<RealmPonderWidget>("Ponder.Viewport");
            m_viewport.Language = m_language;
            m_viewport.SubjectSelected += OpenSubject;
            m_timeline = Children.Find<RealmPonderTimelineWidget>("Ponder.Timeline");
            m_timeline.SeekRequested += tick =>
            {
                try { Player.IsPaused = true; Player.Seek(tick); }
                catch (Exception ex) { PlaybackFailed(ex); }
                RefreshPresentation();
            };
            foreach (string id in new[] { "Index", "Close", "PreviousScene", "Previous", "Play", "Next", "NextScene", "Restart", "Inspect", "Reading", "ResetView" })
                m_buttons.Add(id, Children.Find<RealmPonderButtonWidget>("Ponder." + id));
            Children.Find<LabelWidget>("Ponder.Brand").Text = Local("ui.ponder");
            SelectTutorial(tutorial);
        }
        private string Local(string key) => RealmPonderLocalization.BuiltIn.Text(key).Resolve(m_language);
        public void SelectTutorial(RealmPonderTutorial tutorial)
        {
            bool comfy = Player?.ComfyReading ?? false;
            CleanupRealm(); m_failed = false;
            Player = new(tutorial) { ComfyReading = comfy }; m_timeline.Player = Player;
            m_viewport.InspectMode = false; m_viewport.Setup(null, Player);
            m_sequence = m_registry.Sequence(tutorial.Id, m_sequence);
            string series = m_registry.Entry(tutorial.Id).SeriesId;
            Children.Find<LabelWidget>("Ponder.Brand").Text = series == null ? Local("ui.ponder") : m_registry.GetSeries(series).Title.Resolve(m_language);
            Children.Find<LabelWidget>("Ponder.Title").Text = tutorial.Title.Resolve(m_language);
            RefreshPresentation();
        }
        public void RefreshPresentation()
        {
            try { m_viewport.RefreshPresentation(); }
            catch (Exception ex) { PlaybackFailed(ex); }
            int index = Player.KeyframeIndex;
            Children.Find<LabelWidget>("Ponder.Step").Text = m_failed ? Local("ui.could_not_open_the_tutorial_try_replaying")
                : RealmPonderLocalization.BuiltIn.Text("ui.step", index + 1, Player.Tutorial.Keyframes.Count, Player.Tutorial.Keyframes[index].Title.Resolve(m_language)).Resolve(m_language);
            m_buttons["Play"].Icon = Player.IsPaused || Player.IsCompleted ? RealmPonderIcon.Play : RealmPonderIcon.Pause;
            m_buttons["Previous"].IsEnabled = Player.State.Tick > 0;
            m_buttons["Next"].IsEnabled = index < Player.Tutorial.Keyframes.Count - 1;
            int scene = SequenceIndex();
            m_buttons["PreviousScene"].IsEnabled = scene > 0;
            m_buttons["NextScene"].IsEnabled = scene >= 0 && scene < m_sequence.Count - 1;
            m_buttons["NextScene"].IsActive = Player.Tutorial.NextUpEnabled && (Player.State.IsFinished || Player.IsCompleted);
            m_buttons["Inspect"].IsActive = m_viewport.InspectMode;
            m_buttons["Reading"].IsActive = Player.ComfyReading;
            string hovered = m_buttons.FirstOrDefault(p => p.Value.IsHovered).Key;
            Children.Find<CanvasWidget>("Ponder.Tooltip").IsVisible = hovered != null;
            Children.Find<LabelWidget>("Ponder.TooltipText").Text = hovered == null ? "" : ButtonHint(hovered);
            Children.Find<LabelWidget>("Ponder.Hint").Text = m_failed ? Local("ui.this_tutorial_could_not_be_opened") : m_viewport.InspectMode
                ? m_viewport.InspectedName ?? Local("ui.point_at_a_block_click_for_related")
                : hovered != null ? ButtonHint(hovered) : Player.IsCompleted ? Local("ui.finished_replay_or_continue_to_the_next")
                : Player.State.Ui != null ? Local("ui.drag_to_explore_the_interface_space_to")
                : Local("ui.drag_to_rotate_scroll_to_zoom_space");
        }
        private string ButtonHint(string id) => id switch
        {
            "Index" => Local("ui.tutorial_index"), "Close" => Local("ui.close"),
            "PreviousScene" => Local(m_registry.Entry(TutorialId).SeriesId == null ? "ui.previous_tutorial" : "ui.previous_chapter"),
            "NextScene" => Local(m_registry.Entry(TutorialId).SeriesId == null ? "ui.next_tutorial" : "ui.next_chapter"),
            "Previous" => Local("ui.previous_step_left_arrow"), "Next" => Local("ui.next_step_right_arrow"),
            "Play" => Player.IsPaused ? Local("ui.resume_space") : Local("ui.pause_space"),
            "Restart" => Local("ui.replay_from_the_start"), "ResetView" => Local("ui.reset_camera"),
            "Inspect" => Local("ui.inspect_blocks_and_related_tutorials"),
            "Reading" => Local("ui.comfy_reading_slow_down_while_text_is"), _ => ""
        };
        public override void ChangeParent(ContainerWidget parentWidget)
        {
            base.ChangeParent(parentWidget);
            // The full-screen tutorial already paints its own shade. Keep the host's input shield without double dimming.
            if (parentWidget != null && DialogsManager.m_animationData.TryGetValue(this, out var animation))
                animation.CoverWidget.FillColor = Color.Transparent;
        }
        public override void MeasureOverride(Vector2 available)
        {
            float width = Math.Max(1, Math.Min(1200, available.X - 32));
            float height = Math.Max(1, available.Y - 24);
            float headerHeight = height < 520 ? 64 : 88;
            var heading = Children.Find<CanvasWidget>("Ponder.HeadingFrame");
            heading.Size = new(Math.Max(1, width - 112), -1);
            Children.Find<LabelWidget>("Ponder.Brand").IsVisible = height >= 520;
            heading.Measure(new(heading.Size.X, float.PositiveInfinity));
            headerHeight = Math.Max(headerHeight, heading.ParentDesiredSize.Y + 12);
            Children.Find<CanvasWidget>("Ponder.Frame").Size = new(width, height);
            Children.Find<CanvasWidget>("Ponder.Header").Size = new(width, headerHeight);
            Children.Find<LabelWidget>("Ponder.Brand").IsVisible = height >= 520;
            Children.Find<LabelWidget>("Ponder.Hint").IsVisible = height >= 520;
            Children.Find<LabelWidget>("Ponder.Title").Size = new(Math.Max(1, width - 112), -1);
            foreach (var button in m_buttons.Where(p => p.Key is not "Index" and not "Close").Select(p => p.Value)) button.Size = new(Math.Min(48, (width - 18) / 9), 48);
            var footer = Children.Find<StackPanelWidget>("Ponder.Footer");
            Children.Find<LabelWidget>("Ponder.Step").Size = new(width, -1);
            Children.Find<LabelWidget>("Ponder.Hint").Size = new(width, -1);
            Children.Find<CanvasWidget>("Ponder.TimelineFrame").Size = new(Math.Min(450, width), 30);
            footer.Measure(new(width, height));
            var tooltip = Children.Find<CanvasWidget>("Ponder.Tooltip");
            var tooltipText = Children.Find<LabelWidget>("Ponder.TooltipText");
            tooltipText.Measure(new(Math.Min(450, width) - 24, float.PositiveInfinity));
            tooltip.Size = tooltipText.DesiredSize + new Vector2(24, 16);
            Children.Find<CanvasWidget>("Ponder.Frame").SetWidgetPosition(tooltip, new((width - tooltip.Size.X) / 2, Math.Max(0, height - footer.ParentDesiredSize.Y - tooltip.Size.Y - 8)));
            m_viewport.Size = new(width, Math.Max(80, height - headerHeight - footer.ParentDesiredSize.Y));
            base.MeasureOverride(available);
        }
        public override void Update()
        {
            if (IsClosed) return;
            if (Input.Cancel || Input.Back || m_buttons["Close"].IsClicked) { Close(); return; }
            if (m_index != null) return;
            try
            {
                if (m_session == null && !m_failed)
                {
                    m_session = new(Player);
                    m_session.RealmChanged += realm => m_viewport.Setup(realm, Player);
                    m_viewport.Setup(m_session.Realm, Player);
                }
                if (m_buttons["Restart"].IsClicked)
                {
                    if (m_failed) SelectTutorial(Player.Tutorial); else Player.Replay();
                    m_viewport.InspectMode = false;
                }
                else if (m_buttons["Play"].IsClicked || Input.IsKeyDownOnce(Key.Space))
                {
                    m_viewport.InspectMode = false;
                    if (Player.IsCompleted) Player.Replay(); else Player.IsPaused = !Player.IsPaused;
                }
                else if (m_buttons["Previous"].IsClicked || Input.Left) { Player.IsPaused = true; Player.SeekKeyframe(Player.KeyframeIndex - 1); }
                else if (m_buttons["Next"].IsClicked || Input.Right) { Player.IsPaused = true; Player.SeekKeyframe(Player.KeyframeIndex + 1); }
                else if (m_buttons["PreviousScene"].IsClicked) SelectTutorial(m_sequence[SequenceIndex() - 1]);
                else if (m_buttons["NextScene"].IsClicked) SelectTutorial(m_sequence[SequenceIndex() + 1]);
                if (m_buttons["ResetView"].IsClicked) m_viewport.ResetView();
                if (m_buttons["Reading"].IsClicked) Player.ComfyReading = !Player.ComfyReading;
                if (m_buttons["Inspect"].IsClicked)
                {
                    if (!m_viewport.InspectMode) { m_pausedBeforeInspect = Player.IsPaused; Player.IsPaused = true; m_viewport.InspectMode = true; }
                    else { m_viewport.InspectMode = false; Player.IsPaused = m_pausedBeforeInspect; }
                }
                if (m_buttons["Index"].IsClicked) OpenIndex();
                if (!m_failed) { Player.Advance(Math.Min(Time.FrameDuration, 0.25)); m_session?.Presenter.Synchronize(); }
            }
            catch (Exception ex)
            {
                PlaybackFailed(ex);
            }
            RefreshPresentation();
        }
        private int SequenceIndex() { for (int i = 0; i < m_sequence.Count; i++) if (m_sequence[i].Id == TutorialId) return i; return -1; }
        private void PlaybackFailed(Exception ex)
        {
            m_failed = true; Player.IsPaused = true; CleanupRealm();
            Engine.Log.Error($"[RealmEX/Ponder] UI=FAILED tutorial={TutorialId} exception={ex}");
        }
        private void OpenSubject(int contents)
        {
            var tutorials = m_registry.ForSubject(contents);
            if (tutorials.Count > 0) OpenIndex(tutorials);
        }
        private void OpenIndex(IReadOnlyList<RealmPonderTutorial> subjects = null)
        {
            bool paused = Player.IsPaused; Player.IsPaused = true;
            m_index = new(m_registry, tutorial =>
            {
                m_sequence = m_registry.Sequence(tutorial.Id, subjects);
                SelectTutorial(tutorial);
            }, () => { m_index = null; IsVisible = !IsClosed; Player.IsPaused = paused; }, subjects, subjects == null ? m_registry.Entry(TutorialId).SeriesId : null);
            DialogsManager.ShowDialog(ParentWidget ?? throw new InvalidOperationException("The tutorial must be attached before opening its index."), m_index);
            IsVisible = false;
        }
        public void Close()
        {
            if (IsClosed) return;
            IsClosed = true; m_index?.Close(); m_index = null;
            try { DialogsManager.HideDialog(this); } finally { CleanupRealm(); }
        }
        private void CleanupRealm()
        {
            m_viewport?.Setup(null); m_session?.Dispose(); m_session = null;
        }
    }
}
