using System;
using Engine;
using Engine.Graphics;
using Game;
using GameEntitySystem;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// Create / SCIENEW 风格 Ponder：全屏半透明黑 mask + 居中方块视口，无面板框。
    /// </summary>
    public sealed class RealmPonderDialog : Dialog
    {
        private const string RealmIdPrefix = "realmex-ponder-ui-";

        private readonly string m_realmId;
        private readonly RealmPonderTutorial m_tutorial;
        private readonly LabelWidget m_captionLabel;
        private readonly RealmPonderWidget m_viewportWidget;

        private SandboxRealm m_realm;
        private RealmPonderBlockPresenter m_blockPresenter;
        private RealmPonderStep m_currentStep;
        private int m_currentStepIndex = -1;
        private bool m_isCompleted;
        private bool m_isClosed;

        public RealmPonderDialog(RealmPonderTutorial tutorial)
        {
            m_tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
            m_realmId = RealmIdPrefix + Guid.NewGuid().ToString("N")[..8];

            HorizontalAlignment = WidgetAlignment.Stretch;
            VerticalAlignment = WidgetAlignment.Stretch;

            // 唯一遮罩：20% 不透明度黑，用于和主世界区分。
            Children.Add(new RectangleWidget
            {
                FillColor = new Color(0, 0, 0, 51),
                OutlineColor = Color.Transparent,
                HorizontalAlignment = WidgetAlignment.Stretch,
                VerticalAlignment = WidgetAlignment.Stretch,
                IsHitTestVisible = true
            });

            StackPanelWidget content = new()
            {
                Direction = LayoutDirection.Vertical,
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center
            };
            Children.Add(content);

            m_viewportWidget = new RealmPonderWidget
            {
                Size = new Vector2(820f, 520f),
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center
            };
            content.Children.Add(m_viewportWidget);

            m_captionLabel = new LabelWidget
            {
                Text = string.Empty,
                Color = Color.White,
                FontScale = 0.95f,
                WordWrap = true,
                TextAnchor = TextAnchor.HorizontalCenter,
                HorizontalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(24f, 18f)
            };
            content.Children.Add(m_captionLabel);

            StartPlayback();
        }

        public bool IsCompleted => m_isCompleted;

        public bool IsClosed => m_isClosed;

        public string TutorialId => m_tutorial.Id;

        public override void Update()
        {
            if (m_isClosed)
            {
                return;
            }

            if (m_realm != null && !m_realm.IsDisposed)
            {
                if (!m_isCompleted && m_realm.Storyboard != null && m_realm.Storyboard.IsCompleted)
                {
                    m_isCompleted = true;
                    RefreshLabels();
                    Engine.Log.Information($"[RealmEX/Ponder] UI=COMPLETED tutorial={m_tutorial.Id}");
                }
            }

            if (Input.Cancel || Input.Back)
            {
                Close();
            }
        }

        public void Close()
        {
            if (m_isClosed)
            {
                return;
            }

            m_isClosed = true;
            try
            {
                DialogsManager.HideDialog(this);
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] UI hide failed exception={ex}");
            }

            CleanupRealm();
            Engine.Log.Information(
                $"[RealmEX/Ponder] UI=CLOSED tutorial={m_tutorial.Id} completed={m_isCompleted}");
        }

        private void StartPlayback()
        {
            m_realm = RealmHost.CreateRealm(
                m_realmId,
                projectTemplateName: RealmBootstrap.PonderProjectTemplateName);
            m_realm.Viewport.IsEnabled = true;
            m_realm.Viewport.ClearColor = Color.Transparent;
            m_blockPresenter = new RealmPonderBlockPresenter();
            m_blockPresenter.Attach(m_realm);
            m_viewportWidget.Setup(m_realm);
            m_realm.Storyboard = RealmPonderPlayer.CreateStoryboard(
                m_tutorial,
                OnStepStarted);
            RefreshLabels();
            Engine.Log.Information(
                $"[RealmEX/Ponder] UI=OPEN tutorial={m_tutorial.Id} title=\"{m_tutorial.Title}\" steps={m_tutorial.StepCount} style=create-mask");
        }

        private void OnStepStarted(RealmPonderStep step)
        {
            m_currentStep = step;
            m_currentStepIndex++;
            if (m_realm != null)
            {
                m_realm.Viewport.ClearColor = Color.Transparent;
            }
            m_blockPresenter?.Invalidate();
            RefreshLabels();
            Engine.Log.Information(
                $"[RealmEX/Ponder] STEP index={m_currentStepIndex + 1} scene={step.SceneName} caption=\"{step.Caption}\"");
        }

        private void RefreshLabels()
        {
            m_captionLabel.Text = m_currentStep?.Caption ?? string.Empty;
        }

        private void CleanupRealm()
        {
            try
            {
                m_blockPresenter?.Dispose();
                m_blockPresenter = null;
                if (m_realm != null)
                {
                    RealmHost.DestroyRealm(m_realmId);
                    m_realm = null;
                }
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] UI cleanup failed exception={ex}");
            }
        }

        public static ContainerWidget FindHostWidget()
        {
            Project project = GameManager.Project;
            if (project != null)
            {
                SubsystemPlayers players = project.FindSubsystem<SubsystemPlayers>(false);
                if (players != null && players.ComponentPlayers.Count > 0)
                {
                    ComponentPlayer player = players.ComponentPlayers[0];
                    if (player?.GuiWidget != null)
                    {
                        return player.GuiWidget;
                    }
                }
            }

            return ScreensManager.RootWidget
                ?? throw new InvalidOperationException("No dialog host widget available.");
        }
    }
}
