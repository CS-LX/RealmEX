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
        private static RealmPonderText Text(string key, params object[] args) => RealmPonderLocalization.BuiltIn.Text(key, args);
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
            registry.RegisterTag(new("realmex:circuits", Text("sample.circuits"), Text("sample.explore_switches_and_logic_gates")));
            registry.RegisterTag(new("realmex:farming", Text("sample.farming"), Text("sample.from_planting_to_harvest")));
            registry.Register(CreateAndGateTutorial(), ["realmex:circuits"], [AndGateBlock.Index, SwitchBlock.Index, LedBlock.Index], 0);
            registry.Register(CreateNotGateTutorial(), ["realmex:circuits"], [NotGateBlock.Index, SwitchBlock.Index], 1);
            registry.Register(CreatePumpkinTutorial(), ["realmex:farming"], [PumpkinBlock.Index, SoilBlock.Index, JackOLanternBlock.Index], 2);
            return registry;
        }
        public static RealmPonderTutorial CreateAndGateTutorial()
        {
            RealmPonderSceneBuilder b = new("realmex:and_gate", Text("sample.and_gate_both_inputs_together"), Load("AndGate"));
            b.ConfigureCamera(new(8.5f, 1.7f, 8.2f), 7.8f)
                .IndependentSection("gate", Cell(8, 8))
                .IndependentSection("inputs", Cell(6, 8).Add(Cell(7, 8)).Add(Cell(9, 8)).Add(Cell(10, 8)))
                .IndependentSection("output", Cell(8, 7).Add(Cell(8, 6)))
                .Keyframe(Text("sample.the_base_plate")).ShowSection("base", new(0, -0.5f, 0), 16)
                .Text("caption", Text("sample.start_with_a_wooden_platform_to_support"), new(8.5f, 1.5f, 9), 60).Idle(65)
                .Keyframe(Text("sample.the_and_gate")).ShowSection("gate", new(0, 1, 0), 20)
                .Text("caption", Text("sample.the_and_gate_only_sends_a_signal"), new(8.5f, 2.3f, 8.5f), 90)
                .Outline("focus", Cell(8, 8), 90, m_gold).Idle(95)
                .Keyframe(Text("sample.connect_the_inputs")).ShowSection("inputs", new(-0.6f, 0, 0), 20)
                .Text("caption", Text("sample.the_two_switches_control_inputs_a_and"), new(6.5f, 2.2f, 8.5f), 90)
                .Controls("input", RealmPonderInput.Interact, new(10.5f, 2.2f, 8.5f), 90).Idle(95)
                .Keyframe(Text("sample.both_off")).ShowSection("output", new(0, 0, -0.5f), 15);
            Truth(false, false);
            b.Keyframe(Text("sample.left_input_only")); Truth(true, false);
            b.Keyframe(Text("sample.right_input_only")); Truth(false, true);
            b.Keyframe(Text("sample.both_on")); Truth(true, true);
            b.Keyframe(Text("sample.explore_the_circuit")).RotateCamera(50, 55).MarkAsFinished()
                .Text("caption", Text("sample.both_inputs_must_be_on_use_the"), new(8.5f, 2.2f, 6.5f), 100).Idle(105);
            return b.Build();
            void Truth(bool a, bool c)
            {
                b.SetBlocks(Cell(6, 8), Switch(a)).SetBlocks(Cell(10, 8), Switch(c)).SetBlocks(Cell(8, 6), Led(a && c))
                    .Text("caption", Text("sample.and_result", a ? 1 : 0, c ? 1 : 0, a && c ? 1 : 0), new(8.5f, 2.2f, 6.5f), 80)
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
            RealmPonderSceneBuilder b = new("realmex:not_gate", Text("sample.not_gate_invert_the_input"), new(blocks));
            b.ConfigureCamera(new(8, 1.7f, 8), 7.8f).ShowSection("base", new(0, -0.5f, 0), 16)
                .Keyframe(Text("sample.invert_a_signal"))
                .Text("caption", Text("sample.a_not_gate_reverses_the_input_signal"), new(8.5f, 2.2f, 8.5f), 80).Idle(85)
                .Keyframe(Text("sample.input_off")).SetBlocks(Cell(6, 8), Switch(false)).SetBlocks(Cell(8, 6), Led(true))
                .Text("caption", Text("sample.input_0_gives_output_1"), new(8.5f, 2.2f, 6.5f), 80).Success(new(8.5f, 2.7f, 6.5f)).Idle(85)
                .Keyframe(Text("sample.input_on")).SetBlocks(Cell(6, 8), Switch(true)).SetBlocks(Cell(8, 6), Led(false))
                .Text("caption", Text("sample.input_1_gives_output_0"), new(8.5f, 2.2f, 6.5f), 80).Idle(85).MarkAsFinished();
            return b.Build();
        }
        public static RealmPonderTutorial CreatePumpkinTutorial()
        {
            RealmPonderSceneBuilder b = new("realmex:pumpkin", Text("sample.growing_pumpkins"), Load("Pumpkin"));
            b.ConfigureCamera(new(8.5f, 1.7f, 8.5f), 7.8f).IndependentSection("crop", Cell(8, 8))
                .Keyframe(Text("sample.prepare_the_field")).ShowSection("base", new(0, -0.5f, 0), 16)
                .Text("caption", Text("sample.prepare_a_field_with_room_above_it"), new(8.5f, 1.6f, 8.5f), 80).Idle(85)
                .Keyframe(Text("sample.plant_the_seed")).ShowSection("crop", new(0, 0.5f, 0), 15)
                .Text("caption", Text("sample.plant_pumpkin_seeds_and_watch_the_small"), new(8.5f, 2.2f, 8.5f), 80).Idle(85)
                .Keyframe(Text("sample.watch_it_grow")).SetBlocks(Cell(8, 8), Pumpkin(3))
                .Text("caption", Text("sample.the_pumpkin_grows_larger_over_time"), new(8.5f, 2.4f, 8.5f), 90).Idle(45)
                .SetBlocks(Cell(8, 8), Pumpkin(5)).Idle(50)
                .Keyframe(Text("sample.harvest_the_pumpkin")).SetBlocks(Cell(8, 8), Pumpkin(7))
                .Text("caption", Text("sample.an_orange_mature_pumpkin_is_ready_to"), new(8.5f, 2.5f, 8.5f), 80)
                .Success(new(8.5f, 3, 8.5f)).CreateItem("harvest", Pumpkin(7), new(8.5f, 3.3f, 8.5f), 0.65f)
                .MoveActor("harvest", new(1.7f, 0.8f, 0), 40).RotateActor("harvest", new(0, 180, 0), 70).Idle(85).RemoveActor("harvest")
                .Keyframe(Text("sample.make_a_lantern")).SetBlocks(Cell(8, 8), Terrain.MakeBlockValue(JackOLanternBlock.Index))
                .MoveSection("crop", new(0, 0.8f, 0), 20).RotateSection("crop", new(0, 360, 0), 90)
                .Text("caption", Text("sample.craft_a_jack_o_lantern_to_light"), new(8.5f, 3.2f, 8.5f), 90)
                .Particles("glow", new(8.5f, 2.8f, 8.5f), new(0, 0.4f, 0), 80, m_gold).Idle(90).MoveSection("crop", new(0, -0.8f, 0), 20).Idle(20).MarkAsFinished();
            return b.Build();
        }
    }
}
