using System;
using Engine;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// 南瓜教程场景布局：把真实方块写入沙箱 Terrain（Create 式分步演示）。
    /// </summary>
    public static class RealmPonderPumpkinLayouts
    {
        private const int PlotMin = 6;
        private const int PlotMax = 10;
        private const int GroundY = 1;
        private const int CropY = 2;

        public static void Apply(SandboxRealm realm, string sceneName)
        {
            ArgumentNullException.ThrowIfNull(realm);
            SubsystemTerrain terrain = realm.Project.FindSubsystem<SubsystemTerrain>(true);
            ClearPlot(terrain);

            switch (sceneName)
            {
                case "pumpkin-soil":
                    PaintSoil(terrain, hydrated: false);
                    break;
                case "pumpkin-seedling":
                    PaintSoil(terrain, hydrated: true);
                    SetCrop(terrain, 8, 8, MakePumpkin(0));
                    break;
                case "pumpkin-growing":
                    PaintSoil(terrain, hydrated: true);
                    SetCrop(terrain, 7, 8, MakePumpkin(2));
                    SetCrop(terrain, 8, 8, MakePumpkin(4));
                    SetCrop(terrain, 9, 8, MakePumpkin(3));
                    break;
                case "pumpkin-mature":
                    PaintSoil(terrain, hydrated: true);
                    SetCrop(terrain, 7, 7, MakePumpkin(7));
                    SetCrop(terrain, 8, 8, MakePumpkin(7));
                    SetCrop(terrain, 9, 9, MakePumpkin(7));
                    break;
                case "pumpkin-lantern":
                    PaintSoil(terrain, hydrated: true);
                    SetCrop(terrain, 7, 8, MakePumpkin(7));
                    SetCrop(terrain, 8, 8, Terrain.MakeBlockValue(JackOLanternBlock.Index));
                    SetCrop(terrain, 9, 8, MakePumpkin(7));
                    break;
                default:
                    PaintSoil(terrain, hydrated: false);
                    break;
            }

            terrain.ProcessModifiedCells();
        }

        private static void ClearPlot(SubsystemTerrain terrain)
        {
            for (int x = PlotMin - 1; x <= PlotMax + 1; x++)
            {
                for (int z = PlotMin - 1; z <= PlotMax + 1; z++)
                {
                    for (int y = GroundY; y <= CropY + 1; y++)
                    {
                        terrain.ChangeCell(x, y, z, 0);
                    }
                }
            }
        }

        private static void PaintSoil(SubsystemTerrain terrain, bool hydrated)
        {
            int dirt = Terrain.MakeBlockValue(DirtBlock.Index);
            int soil = Terrain.MakeBlockValue(
                SoilBlock.Index,
                0,
                SoilBlock.SetHydration(0, hydrated));
            for (int x = PlotMin; x <= PlotMax; x++)
            {
                for (int z = PlotMin; z <= PlotMax; z++)
                {
                    bool isField = x >= PlotMin + 1 && x <= PlotMax - 1
                        && z >= PlotMin + 1 && z <= PlotMax - 1;
                    terrain.ChangeCell(x, GroundY, z, isField ? soil : dirt);
                }
            }
        }

        private static void SetCrop(SubsystemTerrain terrain, int x, int z, int value)
        {
            terrain.ChangeCell(x, CropY, z, value);
        }

        private static int MakePumpkin(int size)
        {
            int data = BasePumpkinBlock.SetSize(BasePumpkinBlock.SetIsDead(0, false), size);
            return Terrain.MakeBlockValue(PumpkinBlock.Index, 0, data);
        }
    }
}
