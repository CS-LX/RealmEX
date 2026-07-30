using System;
using Engine;
using Game;
using GameEntitySystem;
using RealmEX.Core;
using RealmEX.Presets.Ponder;

namespace RealmEX.Diagnostics
{
    public static class RealmPonderDiagnostics
    {
        private const string TestRealmId = "realmex-ponder-not-gate";
        private const double MaxGameTimeSeconds = 8.0;

        private static bool m_isRunning;
        private static int m_frames;
        private static int m_startedSteps;
        private static double m_startMainGameTime;
        private static SandboxRealm m_realm;
        private static RealmPonderTutorial m_tutorial;

        public static void Start()
        {
            if (m_isRunning)
            {
                Engine.Log.Warning("[RealmEX/Ponder] START ignored=already-running");
                return;
            }

            string stage = "start";
            try
            {
                if (GameManager.Project == null)
                {
                    throw new InvalidOperationException("Main project is not loaded.");
                }

                stage = "create";
                m_tutorial = RealmPonderSamples.CreateNotGateTutorial();
                m_realm = RealmHost.CreateRealm(TestRealmId);
                m_realm.Viewport.IsEnabled = true;
                m_realm.Storyboard = RealmPonderPlayer.CreateStoryboard(
                    m_tutorial,
                    step =>
                    {
                        m_startedSteps++;
                        Engine.Log.Information(
                            $"[RealmEX/Ponder] STEP index={m_startedSteps} scene={step.SceneName} caption=\"{step.Caption}\"");
                    });

                m_frames = 0;
                m_startedSteps = 0;
                m_startMainGameTime = GameManager.Project.FindSubsystem<SubsystemTime>(true).GameTime;
                m_isRunning = true;
                Engine.Log.Information(
                    $"[RealmEX/Ponder] START tutorial={m_tutorial.Id} title=\"{m_tutorial.Title}\" steps={m_tutorial.Steps.Count}");
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] RESULT=FAIL tutorial=not-gate stage={stage} exception={ex}");
                Cleanup();
            }
        }

        public static void Update(SubsystemUpdate mainUpdate)
        {
            if (!m_isRunning)
            {
                return;
            }

            string stage = "update";
            try
            {
                if (!ReferenceEquals(mainUpdate.Project, GameManager.Project))
                {
                    return;
                }

                m_frames++;
                m_realm.Draw(new Point2(256, 256));
                SubsystemTime mainTime = mainUpdate.Project.FindSubsystem<SubsystemTime>(true);
                if (!m_realm.Storyboard.IsCompleted
                    && mainTime.GameTime - m_startMainGameTime < MaxGameTimeSeconds)
                {
                    return;
                }

                stage = "assert";
                Check(m_realm.Storyboard.IsCompleted, "tutorial-storyboard-completed");
                Check(m_startedSteps == m_tutorial.Steps.Count, "tutorial-all-steps-started");
                Check(m_realm.Profile.SceneName == "not-gate-recap", "tutorial-final-scene");
                Check(m_realm.Viewport.Texture != null, "tutorial-viewport-texture-created");
                Check(m_realm.Viewport.DrawCount > 0, "tutorial-viewport-rendered");

                stage = "destroy";
                Check(RealmHost.DestroyRealm(TestRealmId), "tutorial-realm-destroyed");
                m_realm = null;
                Engine.Log.Information("[RealmEX/Ponder] RESULT=PASS tutorial=not-gate");
                Reset();
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] RESULT=FAIL tutorial=not-gate stage={stage} exception={ex}");
                Cleanup();
            }
        }

        public static void Cancel()
        {
            if (m_isRunning)
            {
                Engine.Log.Warning("[RealmEX/Ponder] CANCEL reason=project-disposed");
            }
            Reset();
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Check failed: {name}");
            }
            Engine.Log.Information($"[RealmEX/Ponder] CHECK=PASS name={name}");
        }

        private static void Cleanup()
        {
            try
            {
                if (m_realm != null)
                {
                    RealmHost.DestroyRealm(TestRealmId);
                }
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/Ponder] CLEANUP=FAIL exception={ex}");
            }
            Reset();
        }

        private static void Reset()
        {
            m_isRunning = false;
            m_frames = 0;
            m_startedSteps = 0;
            m_startMainGameTime = 0.0;
            m_realm = null;
            m_tutorial = null;
        }
    }
}
