using System;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// 与门教程布局：开关 / 导线 / 与门 / LED 真值表分步（不跑完整电路仿真）。
    /// </summary>
    public static class RealmPonderAndGateLayouts
    {
        private const int BoardMin = 6;
        private const int BoardMax = 10;
        private const int BoardY = 1;
        private const int DeviceY = 2;
        private const int MountFace = 4;

        public static void Apply(SandboxRealm realm, string sceneName)
        {
            ArgumentNullException.ThrowIfNull(realm);
            SubsystemTerrain terrain = realm.Project.FindSubsystem<SubsystemTerrain>(true);
            ClearBoard(terrain);
            PaintBoard(terrain);

            switch (sceneName)
            {
                case "and-intro":
                    PlaceGate(terrain);
                    break;
                case "and-00":
                    PlaceCircuit(terrain, inputA: false, inputB: false, outputOn: false);
                    break;
                case "and-10":
                    PlaceCircuit(terrain, inputA: true, inputB: false, outputOn: false);
                    break;
                case "and-01":
                    PlaceCircuit(terrain, inputA: false, inputB: true, outputOn: false);
                    break;
                case "and-11":
                    PlaceCircuit(terrain, inputA: true, inputB: true, outputOn: true);
                    break;
                case "and-recap":
                    PlaceCircuit(terrain, inputA: true, inputB: true, outputOn: true);
                    break;
                default:
                    PlaceGate(terrain);
                    break;
            }

            terrain.ProcessModifiedCells();
        }

        private static void ClearBoard(SubsystemTerrain terrain)
        {
            for (int x = BoardMin - 1; x <= BoardMax + 1; x++)
            {
                for (int z = BoardMin - 1; z <= BoardMax + 1; z++)
                {
                    for (int y = BoardY; y <= DeviceY; y++)
                    {
                        terrain.ChangeCell(x, y, z, 0);
                    }
                }
            }
        }

        private static void PaintBoard(SubsystemTerrain terrain)
        {
            int plank = Terrain.MakeBlockValue(PlanksBlock.Index);
            for (int x = BoardMin; x <= BoardMax; x++)
            {
                for (int z = BoardMin; z <= BoardMax; z++)
                {
                    terrain.ChangeCell(x, BoardY, z, plank);
                }
            }
        }

        private static void PlaceGate(SubsystemTerrain terrain)
        {
            terrain.ChangeCell(8, DeviceY, 8, MakeAndGate());
        }

        private static void PlaceCircuit(SubsystemTerrain terrain, bool inputA, bool inputB, bool outputOn)
        {
            // A 开关 —— 左；B 开关 —— 右；与门居中；LED 朝前。
            // 木板顶面（face 4）上的地板导线 + 开关 / 与门 / LED，真值表只做视觉示意。
            terrain.ChangeCell(6, DeviceY, 8, MakeSwitch(inputA));
            terrain.ChangeCell(10, DeviceY, 8, MakeSwitch(inputB));
            terrain.ChangeCell(7, DeviceY, 8, MakeFloorWire());
            terrain.ChangeCell(9, DeviceY, 8, MakeFloorWire());
            terrain.ChangeCell(8, DeviceY, 8, MakeAndGate());
            terrain.ChangeCell(8, DeviceY, 7, MakeFloorWire());
            terrain.ChangeCell(8, DeviceY, 6, MakeLed(outputOn));
        }

        private static int MakeAndGate()
        {
            int data = MountFace << 2;
            return Terrain.MakeBlockValue(AndGateBlock.Index, 0, data);
        }

        private static int MakeSwitch(bool on)
        {
            int data = (MountFace << 1) | (on ? 1 : 0);
            return Terrain.MakeBlockValue(SwitchBlock.Index, 0, data);
        }

        private static int MakeLed(bool on)
        {
            // 0=白/暗态示意关，5=绿表示开。
            int color = on ? 5 : 0;
            int data = LedBlock.SetMountingFace(LedBlock.SetColor(0, color), MountFace);
            return Terrain.MakeBlockValue(LedBlock.Index, 0, data);
        }

        private static int MakeFloorWire()
        {
            // face 4 = 贴在下方支撑面（木板顶）上的导线。
            return WireBlock.SetWireFacesBitmask(Terrain.MakeBlockValue(WireBlock.Index), 1 << MountFace);
        }
    }
}
