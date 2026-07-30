using System;
using Engine;
using Engine.Graphics;
using Game;
using GameEntitySystem;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// 可见 Ponder 页面：标题、Realm 视口、caption、步骤进度与关闭按钮。
    /// </summary>
    public sealed class RealmPonderDialog : Dialog
    {
        private const string RealmIdPrefix = "realmex-ponder-ui-";

        private readonly string m_realmId;
        private readonly RealmPonderTutorial m_tutorial;
        private readonly LabelWidget m_titleLabel;
        private readonly LabelWidget m_captionLabel;
        private readonly LabelWidget m_progressLabel;
        private readonly RealmPonderWidget m_viewportWidget;
        private readonly BevelledButtonWidget m_closeButton;

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

            Children.Add(new RectangleWidget
            {
                FillColor = new Color(0, 0, 0, 160),
                OutlineColor = Color.Transparent,
                HorizontalAlignment = WidgetAlignment.Stretch,
                VerticalAlignment = WidgetAlignment.Stretch
            });

            CanvasWidget panel = new()
            {
                Size = new Vector2(760f, 560f),
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center
            };
            Children.Add(panel);

            panel.Children.Add(new RectangleWidget
            {
                FillColor = new Color(28, 30, 38, 245),
                OutlineColor = new Color(110, 118, 140),
                HorizontalAlignment = WidgetAlignment.Stretch,
                VerticalAlignment = WidgetAlignment.Stretch
            });

            StackPanelWidget stack = new()
            {
                Direction = LayoutDirection.Vertical,
                HorizontalAlignment = WidgetAlignment.Stretch,
                VerticalAlignment = WidgetAlignment.Stretch
            };
            stack.MarginLeft = 18f;
            stack.MarginRight = 18f;
            stack.MarginTop = 16f;
            stack.MarginBottom = 16f;
            panel.Children.Add(stack);

            m_titleLabel = new LabelWidget
            {
                Text = m_tutorial.Title,
                Color = Color.White,
                FontScale = 1.15f,
                HorizontalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(0f, 8f)
            };
            stack.Children.Add(m_titleLabel);

            m_viewportWidget = new RealmPonderWidget
            {
                Size = new Vector2(720f, 360f),
                HorizontalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(0f, 8f)
            };
            stack.Children.Add(m_viewportWidget);

            m_captionLabel = new LabelWidget
            {
                Text = string.Empty,
                Color = new Color(220, 220, 230),
                WordWrap = true,
                TextAnchor = TextAnchor.HorizontalCenter,
                HorizontalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(8f, 6f)
            };
            stack.Children.Add(m_captionLabel);

            m_progressLabel = new LabelWidget
            {
                Text = string.Empty,
                Color = new Color(160, 170, 190),
                HorizontalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(0f, 8f)
            };
            stack.Children.Add(m_progressLabel);

            m_closeButton = new BevelledButtonWidget
            {
                Text = "关闭",
                Size = new Vector2(160f, 48f),
                HorizontalAlignment = WidgetAlignment.Center
            };
            stack.Children.Add(m_closeButton);

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

            if (Input.Cancel || Input.Back || m_closeButton.IsClicked)
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
            m_blockPresenter = new RealmPonderBlockPresenter();
            m_blockPresenter.Attach(m_realm);
            m_viewportWidget.Setup(m_realm);
            m_realm.Storyboard = RealmPonderPlayer.CreateStoryboard(
                m_tutorial,
                OnStepStarted);
            RefreshLabels();
            Engine.Log.Information(
                $"[RealmEX/Ponder] UI=OPEN tutorial={m_tutorial.Id} title=\"{m_tutorial.Title}\" steps={m_tutorial.Steps.Count}");
        }

        private void OnStepStarted(RealmPonderStep step)
        {
            m_currentStep = step;
            m_currentStepIndex++;
            m_blockPresenter?.Invalidate();
            RefreshLabels();
            Engine.Log.Information(
                $"[RealmEX/Ponder] STEP index={m_currentStepIndex + 1} scene={step.SceneName} caption=\"{step.Caption}\"");
        }

        private void RefreshLabels()
        {
            int total = Math.Max(1, m_tutorial.StepCount);
            int shown = m_currentStepIndex < 0 ? 0 : Math.Min(m_currentStepIndex + 1, total);
            m_captionLabel.Text = m_currentStep?.Caption ?? "准备播放…";
            m_progressLabel.Text = m_isCompleted
                ? $"完成 · {total}/{total}"
                : $"步骤 {shown}/{total}";
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
