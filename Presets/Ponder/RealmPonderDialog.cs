using System;
using System.Xml.Linq;
using Engine;
using Engine.Input;
using Game;
using GameEntitySystem;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Ponder presentation and playback controls, entirely owned by the preset.</summary>
    public sealed class RealmPonderDialog : Dialog
    {
        private readonly string m_realmId = "realmex-ponder-ui-" + Guid.NewGuid().ToString("N")[..8];
        private readonly RealmPonderTutorial m_tutorial;
        private readonly CanvasWidget m_header;
        private readonly CanvasWidget m_note;
        private readonly StackPanelWidget m_stage;
        private readonly StackPanelWidget m_footer;
        private readonly LabelWidget m_captionLabel;
        private readonly LabelWidget m_stepLabel;
        private readonly LabelWidget m_stepTitle;
        private readonly RealmPonderWidget m_viewportWidget;
        private readonly RealmPonderButtonWidget m_close;
        private readonly RealmPonderButtonWidget m_previous;
        private readonly RealmPonderButtonWidget m_play;
        private readonly RealmPonderButtonWidget m_next;
        private readonly RealmPonderButtonWidget m_restart;
        private readonly RealmPonderButtonWidget m_resetView;
        private readonly RealmPonderButtonWidget[] m_chapters;
        private readonly RealmPonderButtonWidget[] m_controls;

        private SandboxRealm m_realm;
        private RealmPonderBlockPresenter m_blockPresenter;
        private RealmPonderStep m_currentStep;
        private int m_currentStepIndex = -1;
        private bool m_pauseAtTarget;
        private bool m_isPaused;
        private bool m_isCompleted;
        private bool m_isClosed;

        public RealmPonderDialog(RealmPonderTutorial tutorial)
        {
            m_tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
            HorizontalAlignment = WidgetAlignment.Stretch;
            VerticalAlignment = WidgetAlignment.Stretch;
            // DialogsManager supplies the background cover; do not darken it a second time.
            LoadContents(this, ContentManager.Get<XElement>("Dialogs/RealmPonderDialog"));
            m_header = Children.Find<CanvasWidget>("Ponder.Header");
            m_note = Children.Find<CanvasWidget>("Ponder.Note");
            m_stage = Children.Find<StackPanelWidget>("Ponder.Stage");
            m_footer = Children.Find<StackPanelWidget>("Ponder.Footer");
            m_captionLabel = Children.Find<LabelWidget>("Ponder.Caption");
            m_stepLabel = Children.Find<LabelWidget>("Ponder.Step");
            m_stepTitle = Children.Find<LabelWidget>("Ponder.StepTitle");
            m_viewportWidget = Children.Find<RealmPonderWidget>("Ponder.Viewport");
            m_close = Children.Find<RealmPonderButtonWidget>("Ponder.Close");
            m_previous = Children.Find<RealmPonderButtonWidget>("Ponder.Previous");
            m_play = Children.Find<RealmPonderButtonWidget>("Ponder.Play");
            m_next = Children.Find<RealmPonderButtonWidget>("Ponder.Next");
            m_restart = Children.Find<RealmPonderButtonWidget>("Ponder.Restart");
            m_resetView = Children.Find<RealmPonderButtonWidget>("Ponder.ResetView");
            m_controls = [m_previous, m_play, m_next, m_restart, m_resetView];
            Children.Find<LabelWidget>("Ponder.Title").Text = tutorial.Title;
            StackPanelWidget chapters = Children.Find<StackPanelWidget>("Ponder.Chapters");
            m_chapters = new RealmPonderButtonWidget[tutorial.StepCount];
            for (int i = 0; i < m_chapters.Length; i++)
            {
                m_chapters[i] = new RealmPonderButtonWidget { Text = (i + 1).ToString(), Margin = new Vector2(3, 0) };
                chapters.Children.Add(m_chapters[i]);
            }
            RefreshLabels();
        }

        public bool IsCompleted => m_isCompleted;
        public bool IsClosed => m_isClosed;
        public string TutorialId => m_tutorial.Id;

        public override void MeasureOverride(Vector2 parentAvailableSize)
        {
            float width = Math.Max(1f, Math.Min(1120f, parentAvailableSize.X - 40f));
            float height = Math.Max(1f, parentAvailableSize.Y - 32f);
            m_header.Size = new Vector2(width, 76f);
            LabelWidget title = Children.Find<LabelWidget>("Ponder.Title");
            title.Size = new Vector2(Math.Max(1f, width - 110f), -1f);
            title.FontScale = width < 760f ? 0.95f : 1.2f;
            Children.Find<LabelWidget>("Ponder.Hint").IsVisible = height >= 440f;
            for (int i = 0; i < m_chapters.Length; i++)
            {
                m_chapters[i].Size = new Vector2(Math.Max(1f, (width - 6f * m_chapters.Length) / Math.Max(1, m_chapters.Length)), 48f);
            }
            float controlWidth = Math.Min(80f, Math.Max(1f, (width - 30f) / 5f));
            foreach (RealmPonderButtonWidget control in m_controls)
                control.Size = new Vector2(controlWidth, 48f);
            m_footer.Measure(new Vector2(width, height));
            float stageHeight = Math.Max(1f, height - 76f - m_footer.ParentDesiredSize.Y);
            bool wide = width >= 760f || (width >= 560f && height < 560f) || (width >= 400f && height < 440f);
            m_stage.Direction = wide ? LayoutDirection.Horizontal : LayoutDirection.Vertical;
            float noteWidth = wide ? (width >= 760f ? 280f : Math.Min(220f, width * 0.4f)) : width;
            StackPanelWidget noteContent = Children.Find<StackPanelWidget>("Ponder.NoteContent");
            noteContent.Measure(new Vector2(Math.Max(1, noteWidth - 40f), float.PositiveInfinity));
            float maxNoteHeight = wide ? stageHeight - 32f : Math.Min(180f, stageHeight * 0.4f);
            m_note.Size = new Vector2(noteWidth, Math.Max(1f, Math.Min(noteContent.ParentDesiredSize.Y + 32f, maxNoteHeight)));
            float noteHeight = m_note.Size.Y + m_note.MarginVerticalSum;
            m_viewportWidget.Size = new Vector2(wide ? width - noteWidth : width,
                Math.Max(1f, wide ? stageHeight : stageHeight - noteHeight));
            base.MeasureOverride(parentAvailableSize);
        }

        public override void Update()
        {
            if (m_isClosed) return;
            if (Input.Cancel || Input.Back || m_close.IsClicked)
            {
                Close();
                return;
            }
            if (m_realm == null) StartPlayback();
            if (m_isClosed) return;
            if (!m_isCompleted && m_realm?.Storyboard?.IsCompleted == true)
            {
                m_isCompleted = true;
                m_realm.Profile.ParallelTick = false;
                RefreshLabels();
            }
            if (m_restart.IsClicked) StartPlayback();
            else if (m_play.IsClicked || Input.IsKeyDownOnce(Key.Space))
            {
                if (m_isCompleted) StartPlayback();
                else SetPaused(!m_isPaused);
            }
            else if ((m_previous.IsClicked || Input.Left) && m_currentStepIndex > 0)
                StartPlayback(m_currentStepIndex - 1, true);
            else if ((m_next.IsClicked || Input.Right) && m_currentStepIndex < m_tutorial.StepCount - 1)
                StartPlayback(m_currentStepIndex + 1, true);
            else
            {
                for (int i = 0; i < m_chapters.Length; i++)
                {
                    if (m_chapters[i].IsClicked)
                    {
                        StartPlayback(i, true);
                        break;
                    }
                }
            }
            if (m_resetView.IsClicked) m_viewportWidget.ResetView();
        }

        public void Close()
        {
            if (m_isClosed) return;
            m_isClosed = true;
            try
            {
                DialogsManager.HideDialog(this);
            }
            finally
            {
                CleanupRealm();
            }
            Engine.Log.Information($"[RealmEX/Ponder] UI=CLOSED tutorial={m_tutorial.Id} completed={m_isCompleted}");
        }

        private void StartPlayback(int startStep = 0, bool pauseAtTarget = false)
        {
            CleanupRealm();
            m_currentStep = null;
            m_currentStepIndex = startStep - 1;
            m_isCompleted = false;
            m_isPaused = false;
            m_pauseAtTarget = pauseAtTarget;
            try
            {
                m_realm = RealmHost.CreateRealm(m_realmId, projectTemplateName: RealmBootstrap.PonderProjectTemplateName);
                m_realm.Viewport.IsEnabled = true;
                m_realm.Viewport.ClearColor = Color.Transparent;
                m_blockPresenter = new RealmPonderBlockPresenter();
                m_blockPresenter.Attach(m_realm);
                m_viewportWidget.Setup(m_realm);
                m_realm.Storyboard = RealmPonderPlayer.CreateStoryboard(m_tutorial, OnStepStarted, startStep);
                RefreshLabels();
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] UI=FAILED tutorial={m_tutorial.Id} exception={ex}");
                Close();
            }
        }

        private void OnStepStarted(RealmPonderStep step)
        {
            m_currentStep = step;
            m_currentStepIndex++;
            m_viewportWidget.Annotations = step.Annotations;
            m_blockPresenter?.Invalidate();
            if (m_pauseAtTarget)
            {
                m_pauseAtTarget = false;
                SetPaused(true);
            }
            RefreshLabels();
        }

        private void SetPaused(bool paused)
        {
            m_isPaused = paused;
            if (m_realm != null) m_realm.Profile.ParallelTick = !paused && !m_isCompleted;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            m_captionLabel.Text = m_currentStep?.Caption ?? string.Empty;
            m_stepTitle.Text = m_currentStep?.Title ?? "准备开始";
            m_stepLabel.Text = $"{Math.Max(1, m_currentStepIndex + 1):00} / {m_tutorial.StepCount:00}"
                + (m_isCompleted ? "  ·  演示结束" : m_isPaused ? "  ·  已暂停" : "  ·  演示中");
            m_play.Text = m_isCompleted ? "重播" : m_isPaused ? "继续" : "暂停";
            m_previous.IsEnabled = m_currentStepIndex > 0;
            m_next.IsEnabled = m_currentStepIndex < m_tutorial.StepCount - 1;
            for (int i = 0; i < m_chapters.Length; i++)
            {
                m_chapters[i].IsActive = i == m_currentStepIndex;
                m_chapters[i].IsVisited = i < m_currentStepIndex;
            }
        }

        private void CleanupRealm()
        {
            // Hide animations can still draw the dialog after the Realm is disposed.
            m_viewportWidget.Setup(null);
            m_blockPresenter?.Dispose();
            m_blockPresenter = null;
            if (m_realm != null)
            {
                RealmHost.DestroyRealm(m_realmId);
                m_realm = null;
            }
        }

        public static ContainerWidget FindHostWidget()
        {
            Project project = GameManager.Project;
            SubsystemPlayers players = project?.FindSubsystem<SubsystemPlayers>(false);
            if (players != null && players.ComponentPlayers.Count > 0 && players.ComponentPlayers[0]?.GuiWidget != null)
                return players.ComponentPlayers[0].GuiWidget;
            return ScreensManager.RootWidget ?? throw new InvalidOperationException("No dialog host widget available.");
        }
    }
}
