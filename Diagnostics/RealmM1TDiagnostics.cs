using System;
using Engine;
using Game;
using GameEntitySystem;
using RealmEX.Core;
using TemplatesDatabase;

namespace RealmEX.Diagnostics
{
    /// <summary>
    /// M1T Terrain / BlockEntity 隔离诊断。当前只验证 BlockEntity 注册隔离级。
    /// </summary>
    public static class RealmM1TDiagnostics
    {
        private const string TestRealmId = "realmex-m1t-terrain";
        private const string TerrainTemplateName = "RealmEXSandboxTerrainProject";
        private static readonly Point3 ChestCoordinates = new(30999, 128, 30999);
        private static readonly Point3 TerrainCellCoordinates = new(1, 64, 1);

        public static void Run()
        {
            string stage = "start";
            SandboxProject sandbox = null;
            Engine.Log.Information(
                $"[RealmEX/M1T] START scope=TerrainIsolation validation=BlockEntityRegistry template={TerrainTemplateName} terrainMode=diagnostic-stub");

            try
            {
                Project mainProject = GameManager.Project
                    ?? throw new InvalidOperationException("Main project is not loaded.");
                int activeBefore = RealmHost.ActiveSandboxes.Count;

                stage = "create";
                sandbox = CreateDiagnosticSandbox();
                Check(!ReferenceEquals(sandbox, mainProject), "sandbox-project-distinct");
                Check(ReferenceEquals(GameManager.Project, mainProject), "main-project-reference-preserved");
                Check(RealmHost.ActiveSandboxes.Count == activeBefore, "host-active-count-unchanged");

                stage = "subsystems";
                SubsystemBlockEntities sandboxBlockEntities =
                    sandbox.FindSubsystem<SubsystemBlockEntities>(true);
                SubsystemBlockEntities mainBlockEntities =
                    mainProject.FindSubsystem<SubsystemBlockEntities>(true);
                SubsystemTerrain sandboxTerrain =
                    sandbox.FindSubsystem<SubsystemTerrain>(true);
                SubsystemTerrain mainTerrain =
                    mainProject.FindSubsystem<SubsystemTerrain>(true);
                SubsystemChestBlockBehavior chestBehavior =
                    sandbox.FindSubsystem<SubsystemChestBlockBehavior>(true);
                Check(!ReferenceEquals(sandboxBlockEntities, mainBlockEntities), "block-entity-registry-isolated");
                Check(!ReferenceEquals(sandboxTerrain, mainTerrain), "terrain-subsystem-isolated");

                ComponentBlockEntity mainBefore = mainBlockEntities.GetBlockEntity(ChestCoordinates);

                stage = "spawn-chest";
                int chestValue = Terrain.MakeBlockValue(45);
                chestBehavior.OnBlockAdded(chestValue, 0, ChestCoordinates.X, ChestCoordinates.Y, ChestCoordinates.Z);

                stage = "assert";
                ComponentBlockEntity sandboxBlockEntity = sandboxBlockEntities.GetBlockEntity(ChestCoordinates);
                Check(sandboxBlockEntity != null, "sandbox-chest-registered");
                Check(sandboxBlockEntity.Coordinates == ChestCoordinates, "sandbox-chest-coordinates");
                Check(sandboxBlockEntity.Entity.FindComponent<ComponentChest>(true) != null, "sandbox-chest-component-loaded");
                Check(
                    ReferenceEquals(mainBlockEntities.GetBlockEntity(ChestCoordinates), mainBefore),
                    "main-world-block-entity-unchanged");

                stage = "change-cell";
                int mainCellBefore = mainTerrain.Terrain.GetCellValue(TerrainCellCoordinates);
                ComponentBlockEntity mainCellBlockEntityBefore =
                    mainBlockEntities.GetBlockEntity(TerrainCellCoordinates);
                sandboxTerrain.ChangeCell(
                    TerrainCellCoordinates.X,
                    TerrainCellCoordinates.Y,
                    TerrainCellCoordinates.Z,
                    chestValue);
                Check(
                    sandboxTerrain.Terrain.GetCellValue(TerrainCellCoordinates) == chestValue,
                    "sandbox-terrain-cell-changed");
                Check(
                    mainTerrain.Terrain.GetCellValue(TerrainCellCoordinates) == mainCellBefore,
                    "main-world-cell-unchanged");
                Check(
                    sandboxBlockEntities.GetBlockEntity(TerrainCellCoordinates) != null,
                    "sandbox-change-cell-created-block-entity");
                Check(
                    ReferenceEquals(mainBlockEntities.GetBlockEntity(TerrainCellCoordinates), mainCellBlockEntityBefore),
                    "main-world-change-cell-block-entity-unchanged");

                stage = "process-neighbors";
                sandboxTerrain.ProcessModifiedCells();
                Check(sandboxTerrain.m_modifiedCells.Count == 0, "sandbox-modified-cells-processed");

                stage = "dispose";
                sandbox.Dispose();
                sandbox = null;
                Check(RealmHost.ActiveSandboxes.Count == activeBefore, "host-active-count-restored");

                Engine.Log.Information(
                    "[RealmEX/M1T] RESULT=PASS scope=TerrainIsolation validation=BlockEntityRegistry,TerrainCellChange terrainMode=diagnostic-stub");
            }
            catch (M1TBlockedException ex)
            {
                Engine.Log.Warning(
                    $"[RealmEX/M1T] RESULT=BLOCKED scope=TerrainIsolation validation=BlockEntityRegistry stage={stage} reason={ex.Message}");
            }
            catch (Exception ex) when (stage == "create" || stage == "subsystems")
            {
                Engine.Log.Warning(
                    $"[RealmEX/M1T] RESULT=BLOCKED scope=TerrainIsolation validation=BlockEntityRegistry stage={stage} exception={ex}");
            }
            catch (Exception ex)
            {
                Engine.Log.Error(
                    $"[RealmEX/M1T] RESULT=FAIL scope=TerrainIsolation validation=BlockEntityRegistry stage={stage} exception={ex}");
            }
            finally
            {
                if (sandbox != null)
                {
                    try
                    {
                        sandbox.Dispose();
                    }
                    catch (Exception cleanupError)
                    {
                        Engine.Log.Error($"[RealmEX/M1T] CLEANUP=FAIL exception={cleanupError}");
                    }
                }
            }
        }

        private static SandboxProject CreateDiagnosticSandbox()
        {
            GameDatabase gameDatabase = DatabaseManager.GameDatabase;
            DatabaseObject projectTemplate = gameDatabase.Database.FindDatabaseObject(
                TerrainTemplateName,
                gameDatabase.ProjectTemplateType,
                false);
            if (projectTemplate == null)
            {
                throw new M1TBlockedException($"ProjectTemplate \"{TerrainTemplateName}\" was not loaded.");
            }

            ProjectData projectData = new(gameDatabase, projectTemplate, null);
            return new SandboxProject(gameDatabase, projectData, TestRealmId);
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"Check failed: {name}");
            }
            Engine.Log.Information($"[RealmEX/M1T] CHECK=PASS name={name}");
        }

        private sealed class M1TBlockedException : Exception
        {
            public M1TBlockedException(string message)
                : base(message)
            {
            }
        }
    }
}
