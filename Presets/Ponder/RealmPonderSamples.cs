using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Engine;
using Game;

namespace RealmEX.Presets.Ponder
{
    public static class RealmPonderSamples
    {
        private static readonly Color m_gold = new(240, 200, 110);
        private static readonly Color m_green = new(130, 225, 160);
        private static RealmPonderSelection Cell(int x, int z) => RealmPonderSelection.At(x, 2, z);
        private static RealmPonderText Text(string zh, string en) => new(zh, en);
        private static int Switch(bool on) => Terrain.MakeBlockValue(SwitchBlock.Index, 0, 8 | (on ? 1 : 0));
        private static int Led(bool on) => Terrain.MakeBlockValue(LedBlock.Index, 0, LedBlock.SetMountingFace(LedBlock.SetColor(0, on ? 5 : 0), 4));
        private static int Pumpkin(int size) => Terrain.MakeBlockValue(PumpkinBlock.Index, 0, BasePumpkinBlock.SetSize(BasePumpkinBlock.SetIsDead(0, false), size));
        private static RealmPonderSchematic Load(string asset) => RealmPonderSchematic.Load(ContentManager.Get<XElement>("RealmEX/Ponder/" + asset), name => name switch
        {
            "planks" => Terrain.MakeBlockValue(PlanksBlock.Index), "and_gate" => Terrain.MakeBlockValue(AndGateBlock.Index, 0, 16),
            "switch" => Switch(false), "wire" => WireBlock.SetWireFacesBitmask(Terrain.MakeBlockValue(WireBlock.Index), 16), "led" => Led(false),
            "dirt" => Terrain.MakeBlockValue(DirtBlock.Index), "soil" => Terrain.MakeBlockValue(SoilBlock.Index), "pumpkin" => Pumpkin(0),
            _ => throw new FormatException($"Unknown Ponder palette entry '{name}'.")
        });
        public static RealmPonderRegistry CreateRegistry()
        {
            RealmPonderRegistry registry = new();
            registry.RegisterTag(new("realmex:circuits", Text("电路", "Circuits"), Text("了解开关与逻辑元件", "Explore switches and logic gates")));
            registry.RegisterTag(new("realmex:farming", Text("种植", "Farming"), Text("从播种到收获", "From planting to harvest")));
            registry.Register(CreateAndGateTutorial(), ["realmex:circuits"], [AndGateBlock.Index, SwitchBlock.Index, LedBlock.Index], 0);
            registry.Register(CreateNotGateTutorial(), ["realmex:circuits"], [NotGateBlock.Index, SwitchBlock.Index], 1);
            registry.Register(CreatePumpkinTutorial(), ["realmex:farming"], [PumpkinBlock.Index, SoilBlock.Index, JackOLanternBlock.Index], 2);
            return registry;
        }
        public static RealmPonderTutorial CreateAndGateTutorial()
        {
            RealmPonderSceneBuilder b = new("realmex:and_gate", Text("与门：两个条件同时成立", "AND gate: both inputs together"), Load("AndGate"));
            b.ConfigureCamera(new(8.5f, 1.7f, 8.2f), 7.8f)
                .IndependentSection("gate", Cell(8, 8))
                .IndependentSection("inputs", Cell(6, 8).Add(Cell(7, 8)).Add(Cell(9, 8)).Add(Cell(10, 8)))
                .IndependentSection("output", Cell(8, 7).Add(Cell(8, 6)))
                .Keyframe(Text("演示台", "The base plate")).ShowSection("base", new(0, -0.5f, 0), 16)
                .Text("caption", Text("先准备一块木板，电路元件将安装在上面。", "Start with a wooden platform to support the circuit."), new(8.5f, 1.5f, 9), 60).Idle(65)
                .Keyframe(Text("认识与门", "The AND gate")).ShowSection("gate", new(0, 1, 0), 20)
                .Text("caption", Text("中间是与门。两个输入同时接通时，它才输出信号。", "The AND gate only sends a signal when both inputs are on."), new(8.5f, 2.3f, 8.5f), 90)
                .Outline("focus", Cell(8, 8), 90, m_gold).Idle(95)
                .Keyframe(Text("连接输入", "Connect the inputs")).ShowSection("inputs", new(-0.6f, 0, 0), 20)
                .Text("caption", Text("两侧开关分别控制输入 A 和 B，导线把它们接到与门。", "The two switches control inputs A and B. Wires connect them to the gate."), new(6.5f, 2.2f, 8.5f), 90)
                .Controls("input", RealmPonderInput.Interact, new(10.5f, 2.2f, 8.5f), 90).Idle(95)
                .Keyframe(Text("都关闭", "Both off")).ShowSection("output", new(0, 0, -0.5f), 15);
            Truth(false, false, "0 与 0 → 0", "0 AND 0 → 0");
            b.Keyframe(Text("只开左侧", "Left input only")); Truth(true, false, "1 与 0 → 0", "1 AND 0 → 0");
            b.Keyframe(Text("只开右侧", "Right input only")); Truth(false, true, "0 与 1 → 0", "0 AND 1 → 0");
            b.Keyframe(Text("同时开启", "Both on")); Truth(true, true, "1 与 1 → 1", "1 AND 1 → 1");
            b.Keyframe(Text("观察结构", "Explore the circuit")).RotateCamera(50, 55).MarkAsFinished()
                .Text("caption", Text("两边都开，输出才开。点放大镜可以查看元件与相关教程。", "Both inputs must be on. Use the magnifier to inspect blocks and related tutorials."), new(8.5f, 2.2f, 6.5f), 100).Idle(105);
            return b.Build();
            void Truth(bool a, bool c, string zh, string en)
            {
                b.SetBlocks(Cell(6, 8), Switch(a)).SetBlocks(Cell(10, 8), Switch(c)).SetBlocks(Cell(8, 6), Led(a && c))
                    .Text("caption", Text(zh, en), new(8.5f, 2.2f, 6.5f), 80)
                    .Outline("focus", Cell(8, 6), 80, a && c ? m_green : m_gold);
                if (a && c) b.Success(new(8.5f, 2.7f, 6.5f));
                b.Idle(85);
            }
        }
        public static RealmPonderTutorial CreateNotGateTutorial()
        {
            Dictionary<Point3, int> blocks = new(Load("AndGate").Blocks);
            blocks[new(8, 2, 8)] = Terrain.MakeBlockValue(NotGateBlock.Index, 0, 16);
            blocks.Remove(new(9, 2, 8)); blocks.Remove(new(10, 2, 8));
            RealmPonderSceneBuilder b = new("realmex:not_gate", Text("非门：反转输入", "NOT gate: invert the input"), new(blocks));
            b.ConfigureCamera(new(8, 1.7f, 8), 7.8f).ShowSection("base", new(0, -0.5f, 0), 16)
                .Keyframe(Text("信号反转", "Invert a signal"))
                .Text("caption", Text("非门的输出始终与输入相反。", "A NOT gate reverses the input signal."), new(8.5f, 2.2f, 8.5f), 80).Idle(85)
                .Keyframe(Text("关闭输入", "Input off")).SetBlocks(Cell(6, 8), Switch(false)).SetBlocks(Cell(8, 6), Led(true))
                .Text("caption", Text("输入为 0 时，输出为 1。", "Input 0 gives output 1."), new(8.5f, 2.2f, 6.5f), 80).Success(new(8.5f, 2.7f, 6.5f)).Idle(85)
                .Keyframe(Text("接通输入", "Input on")).SetBlocks(Cell(6, 8), Switch(true)).SetBlocks(Cell(8, 6), Led(false))
                .Text("caption", Text("输入为 1 时，输出为 0。", "Input 1 gives output 0."), new(8.5f, 2.2f, 6.5f), 80).Idle(85).MarkAsFinished();
            return b.Build();
        }
        public static RealmPonderTutorial CreatePumpkinTutorial()
        {
            RealmPonderSceneBuilder b = new("realmex:pumpkin", Text("南瓜的生长", "Growing pumpkins"), Load("Pumpkin"));
            b.ConfigureCamera(new(8.5f, 1.7f, 8.5f), 7.8f).IndependentSection("crop", Cell(8, 8))
                .Keyframe(Text("准备田地", "Prepare the field")).ShowSection("base", new(0, -0.5f, 0), 16)
                .Text("caption", Text("准备田地，给上方留出足够空间。", "Prepare a field with room above it for the crop."), new(8.5f, 1.6f, 8.5f), 80).Idle(85)
                .Keyframe(Text("种下瓜苗", "Plant the seed")).ShowSection("crop", new(0, 0.5f, 0), 15)
                .Text("caption", Text("种下南瓜种子，观察中央的小瓜苗。", "Plant pumpkin seeds and watch the small seedling."), new(8.5f, 2.2f, 8.5f), 80).Idle(85)
                .Keyframe(Text("观察生长", "Watch it grow")).SetBlocks(Cell(8, 8), Pumpkin(3))
                .Text("caption", Text("南瓜逐渐长大。生长需要时间。", "The pumpkin grows larger over time."), new(8.5f, 2.4f, 8.5f), 90).Idle(45)
                .SetBlocks(Cell(8, 8), Pumpkin(5)).Idle(50)
                .Keyframe(Text("收获南瓜", "Harvest the pumpkin")).SetBlocks(Cell(8, 8), Pumpkin(7))
                .Text("caption", Text("橙色的成熟南瓜可以采收。", "An orange, mature pumpkin is ready to harvest."), new(8.5f, 2.5f, 8.5f), 80)
                .Success(new(8.5f, 3, 8.5f)).CreateItem("harvest", Pumpkin(7), new(8.5f, 3.3f, 8.5f), 0.65f)
                .MoveActor("harvest", new(1.7f, 0.8f, 0), 40).RotateActor("harvest", new(0, 180, 0), 70).Idle(85).RemoveActor("harvest")
                .Keyframe(Text("制作南瓜灯", "Make a lantern")).SetBlocks(Cell(8, 8), Terrain.MakeBlockValue(JackOLanternBlock.Index))
                .MoveSection("crop", new(0, 0.8f, 0), 20).RotateSection("crop", new(0, 360, 0), 90)
                .Text("caption", Text("成熟南瓜还能做成南瓜灯，为周围照明。", "Craft a jack-o'-lantern to light up the surroundings."), new(8.5f, 3.2f, 8.5f), 90)
                .Particles("glow", new(8.5f, 2.8f, 8.5f), new(0, 0.4f, 0), 80, m_gold).Idle(90).MoveSection("crop", new(0, -0.8f, 0), 20).Idle(20).MarkAsFinished();
            return b.Build();
        }
    }
}
