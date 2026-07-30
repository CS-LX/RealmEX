using System;
using Game;
using GameEntitySystem;
using RealmEX.Core;
using RealmEX.Subsystems;

namespace RealmEX.Diagnostics
{
    /// <summary>
    /// P1 核心隔离的游戏内诊断入口。完整 Terrain / 设备级 M1 另行验收。
    /// </summary>
    public static class RealmM1Diagnostics
    {
        private const string TestRealmId = "realmex-m1-core";

        public static void Run()
        {
            string stage = "start";
            SandboxProject sandbox = null;
            int activeBefore = RealmHost.ActiveSandboxes.Count;
            Engine.Log.Information(
                $"[RealmEX/M1] START scope=P1Core activeBefore={activeBefore} deviceTerrain=NOT_TESTED");

            try
            {
                Project mainProject = GameManager.Project
                    ?? throw new InvalidOperationException("Main project is not loaded.");

                stage = "create";
                sandbox = RealmHost.CreateSandbox(TestRealmId);
                Check(!ReferenceEquals(sandbox, mainProject), "sandbox-project-distinct");
                Check(ReferenceEquals(GameManager.Project, mainProject), "main-project-reference-preserved");
                Check(ReferenceEquals(sandbox.GameDatabase, mainProject.GameDatabase), "game-database-shared-readonly");

                stage = "subsystems";
                SandboxSubsystemPlayers players = sandbox.FindSubsystem<SandboxSubsystemPlayers>(true);
                SubsystemTime sandboxTime = sandbox.FindSubsystem<SubsystemTime>(true);
                SubsystemUpdate sandboxUpdate = sandbox.FindSubsystem<SubsystemUpdate>(true);
                Check(!ReferenceEquals(players, mainProject.FindSubsystem<SubsystemPlayers>(true)), "players-isolated");
                Check(!ReferenceEquals(sandboxTime, mainProject.FindSubsystem<SubsystemTime>(true)), "time-isolated");
                Check(!ReferenceEquals(sandboxUpdate, mainProject.FindSubsystem<SubsystemUpdate>(true)), "update-isolated");

                stage = "empty-players";
                players.Update(0f);
                Check(players.PlayersData.Count == 0, "empty-players-no-screen-switch");

                stage = "save-guard";
                bool saveBlocked = false;
                try
                {
                    sandbox.Save();
                }
                catch (InvalidOperationException ex) when (
                    ex.Message.Contains("memory-only", StringComparison.Ordinal))
                {
                    saveBlocked = true;
                }
                Check(saveBlocked, "memory-only-save-guard");

                stage = "destroy";
                Check(RealmHost.DestroySandbox(TestRealmId), "sandbox-destroyed");
                sandbox = null;
                Check(RealmHost.ActiveSandboxes.Count == activeBefore, "active-count-restored");

                Engine.Log.Information(
                    "[RealmEX/M1] RESULT=PASS scope=P1Core deviceTerrain=NOT_TESTED");
            }
            catch (Exception ex)
            {
                Engine.Log.Error($"[RealmEX/M1] RESULT=FAIL scope=P1Core stage={stage} exception={ex}");
            }
            finally
            {
                if (sandbox != null)
                {
                    try
                    {
                        RealmHost.DestroySandbox(TestRealmId);
                    }
                    catch (Exception cleanupError)
                    {
                        Engine.Log.Error($"[RealmEX/M1] CLEANUP=FAIL exception={cleanupError}");
                    }
                }
            }
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Check failed: {name}");
            }
            Engine.Log.Information($"[RealmEX/M1] CHECK=PASS name={name}");
        }
    }
}
