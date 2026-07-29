using System;
using System.Linq;
using Game;
using GameEntitySystem;

namespace RealmEX
{
    /// <summary>
    /// P2 并行 tick 的游戏内诊断入口。按 F9 后跨数帧检查主世界与沙箱同时推进。
    /// </summary>
    public static class RealmM2Diagnostics
    {
        private const string TestRealmId = "realmex-m2-tick";
        private const int RequiredFrames = 5;

        private static bool m_isRunning;
        private static int m_frames;
        private static SandboxProject m_sandbox;
        private static SubsystemTime m_mainTime;
        private static SubsystemTime m_sandboxTime;
        private static double m_initialMainTime;
        private static double m_initialSandboxTime;
        private static double m_previousMainTime;
        private static double m_previousSandboxTime;
        private static int m_mainAdvancedFrames;
        private static int m_sandboxAdvancedFrames;

        public static void Start()
        {
            if (m_isRunning)
            {
                Engine.Log.Warning("[RealmEX/M2] START ignored=already-running");
                return;
            }

            string stage = "start";
            try
            {
                Project mainProject = GameManager.Project
                    ?? throw new InvalidOperationException("Main project is not loaded.");

                stage = "create";
                m_sandbox = RealmHost.CreateSandbox(TestRealmId);
                m_mainTime = mainProject.FindSubsystem<SubsystemTime>(true);
                m_sandboxTime = m_sandbox.FindSubsystem<SubsystemTime>(true);
                m_initialMainTime = m_mainTime.GameTime;
                m_initialSandboxTime = m_sandboxTime.GameTime;
                m_previousMainTime = m_initialMainTime;
                m_previousSandboxTime = m_initialSandboxTime;
                m_mainAdvancedFrames = 0;
                m_sandboxAdvancedFrames = 0;
                m_frames = 0;
                m_isRunning = true;

                Engine.Log.Information(
                    $"[RealmEX/M2] START scope=P2Tick frames={RequiredFrames} active={RealmHost.ActiveSandboxes.Count}");
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/M2] RESULT=FAIL scope=P2Tick stage={stage} exception={ex}");
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
                if (m_mainTime.GameTime > m_previousMainTime)
                {
                    m_mainAdvancedFrames++;
                }
                if (m_sandboxTime.GameTime > m_previousSandboxTime)
                {
                    m_sandboxAdvancedFrames++;
                }
                Engine.Log.Information(
                    $"[RealmEX/M2] TICK frame={m_frames} mainTime={m_mainTime.GameTime:F6} sandboxTime={m_sandboxTime.GameTime:F6}");
                m_previousMainTime = m_mainTime.GameTime;
                m_previousSandboxTime = m_sandboxTime.GameTime;

                if (m_frames < RequiredFrames)
                {
                    return;
                }

                stage = "assert";
                Check(m_mainTime.GameTime > m_initialMainTime, "main-time-advanced");
                Check(m_sandboxTime.GameTime > m_initialSandboxTime, "sandbox-time-advanced");
                Check(m_mainAdvancedFrames >= RequiredFrames - 1, "main-time-advanced-across-frames");
                Check(m_sandboxAdvancedFrames >= RequiredFrames - 1, "sandbox-time-advanced-across-frames");
                Check(RealmHost.ActiveSandboxes.Any(sandbox => ReferenceEquals(sandbox, m_sandbox)), "sandbox-owned-by-host");

                stage = "destroy";
                Check(RealmHost.DestroySandbox(TestRealmId), "sandbox-destroyed");
                m_sandbox = null;

                Engine.Log.Information("[RealmEX/M2] RESULT=PASS scope=P2Tick");
                Reset();
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/M2] RESULT=FAIL scope=P2Tick stage={stage} exception={ex}");
                Cleanup();
            }
        }

        public static void Cancel()
        {
            if (m_isRunning)
            {
                Engine.Log.Warning("[RealmEX/M2] CANCEL reason=project-disposed");
            }
            Reset();
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Check failed: {name}");
            }
            Engine.Log.Information($"[RealmEX/M2] CHECK=PASS name={name}");
        }

        private static void Cleanup()
        {
            try
            {
                if (m_sandbox != null)
                {
                    RealmHost.DestroySandbox(TestRealmId);
                }
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/M2] CLEANUP=FAIL exception={ex}");
            }
            Reset();
        }

        private static void Reset()
        {
            m_isRunning = false;
            m_frames = 0;
            m_sandbox = null;
            m_mainTime = null;
            m_sandboxTime = null;
            m_initialMainTime = 0.0;
            m_initialSandboxTime = 0.0;
            m_previousMainTime = 0.0;
            m_previousSandboxTime = 0.0;
            m_mainAdvancedFrames = 0;
            m_sandboxAdvancedFrames = 0;
        }
    }
}
