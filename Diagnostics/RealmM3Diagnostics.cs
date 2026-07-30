using System;
using Engine;
using Game;
using GameEntitySystem;
using RealmEX.Core;
using RealmEX.Core.Scenes;
using RealmEX.Core.Storyboard;

namespace RealmEX.Diagnostics
{
    /// <summary>
    /// P3 自定义场景诊断。按 F10 后通过 Storyboard 切换两个 RealmScene 并比较时间倍率。
    /// </summary>
    public static class RealmM3Diagnostics
    {
        private const string TestRealmId = "realmex-m3-scene";
        private const int RequiredFrames = 8;

        private static bool m_isRunning;
        private static int m_frames;
        private static SandboxRealm m_realm;
        private static SubsystemTime m_sandboxTime;
        private static double m_previousSandboxTime;
        private static double m_slowDeltaTotal;
        private static double m_fastDeltaTotal;
        private static int m_slowSamples;
        private static int m_fastSamples;

        public static void Start()
        {
            if (m_isRunning)
            {
                Engine.Log.Warning("[RealmEX/M3] START ignored=already-running");
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
                m_realm = RealmHost.CreateRealm(TestRealmId);
                m_realm.Viewport.IsEnabled = true;
                m_sandboxTime = m_realm.Project.FindSubsystem<SubsystemTime>(true);
                m_previousSandboxTime = m_sandboxTime.GameTime;

                RealmStoryboard storyboard = new();
                storyboard.EnqueueAsync(async ctx =>
                {
                    await ctx.ApplyScene(new RealmScene("m3-slow", 0.5f));
                    await ctx.WaitFrames(3);
                    await ctx.ApplyScene(new RealmScene("m3-fast", 2f));
                    await ctx.WaitFrames(3);
                });
                m_realm.Storyboard = storyboard;

                m_frames = 0;
                m_slowDeltaTotal = 0.0;
                m_fastDeltaTotal = 0.0;
                m_slowSamples = 0;
                m_fastSamples = 0;
                m_isRunning = true;

                Engine.Log.Information("[RealmEX/M3] START scope=P3Scene scenes=m3-slow,m3-fast");
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/M3] RESULT=FAIL scope=P3Scene stage={stage} exception={ex}");
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
                double delta = m_sandboxTime.GameTime - m_previousSandboxTime;
                m_previousSandboxTime = m_sandboxTime.GameTime;

                if (m_realm.Profile.SceneName == "m3-slow" && delta > 0.0)
                {
                    m_slowDeltaTotal += delta;
                    m_slowSamples++;
                }
                else if (m_realm.Profile.SceneName == "m3-fast" && delta > 0.0)
                {
                    m_fastDeltaTotal += delta;
                    m_fastSamples++;
                }

                Engine.Log.Information(
                    $"[RealmEX/M3] TICK frame={m_frames} scene={m_realm.Profile.SceneName} factor={m_realm.Profile.TimeFactor:F2} sandboxTime={m_sandboxTime.GameTime:F6} delta={delta:F6}");
                m_realm.Draw(new Point2(256, 256));

                if (m_frames < RequiredFrames)
                {
                    return;
                }

                stage = "assert";
                Check(m_slowSamples > 0, "slow-scene-applied");
                Check(m_fastSamples > 0, "fast-scene-applied");
                Check(m_fastDeltaTotal / m_fastSamples > m_slowDeltaTotal / m_slowSamples, "scene-time-factor-changed");
                Check(m_realm.Storyboard.IsCompleted, "storyboard-completed");
                Check(m_realm.Viewport.Texture != null, "viewport-texture-created");
                Check(m_realm.Viewport.DrawCount > 0, "viewport-rendered");

                stage = "destroy";
                Check(RealmHost.DestroyRealm(TestRealmId), "realm-destroyed");
                m_realm = null;

                Engine.Log.Information("[RealmEX/M3] RESULT=PASS scope=P3Scene");
                Reset();
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/M3] RESULT=FAIL scope=P3Scene stage={stage} exception={ex}");
                Cleanup();
            }
        }

        public static void Cancel()
        {
            if (m_isRunning)
            {
                Engine.Log.Warning("[RealmEX/M3] CANCEL reason=project-disposed");
            }
            Reset();
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Check failed: {name}");
            }
            Engine.Log.Information($"[RealmEX/M3] CHECK=PASS name={name}");
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
                Engine.Log.Error($"[RealmEX/M3] CLEANUP=FAIL exception={ex}");
            }
            Reset();
        }

        private static void Reset()
        {
            m_isRunning = false;
            m_frames = 0;
            m_realm = null;
            m_sandboxTime = null;
            m_previousSandboxTime = 0.0;
            m_slowDeltaTotal = 0.0;
            m_fastDeltaTotal = 0.0;
            m_slowSamples = 0;
            m_fastSamples = 0;
        }
    }
}
