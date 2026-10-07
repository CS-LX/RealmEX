using Engine;

namespace RealmEX.Presets.Ponder
{
    public static class RealmPonderSamples
    {
        private static readonly Color m_gold = new(235, 193, 94);
        private static readonly Color m_on = new(137, 220, 155);
        private static readonly Color m_off = new(190, 199, 214);

        public static RealmPonderTutorial CreateNotGateTutorial()
        {
            return new RealmPonderTutorial("realmex:not_gate", "非门")
                .AddStep(new RealmPonderStep("not-gate-intro", 1f, "非门会把输入信号反转。", 4.0, title: "信号反转"))
                .AddStep(new RealmPonderStep("not-gate-input-off", 1f, "输入为 0 时，输出为 1。", 4.0, title: "关闭输入"))
                .AddStep(new RealmPonderStep("not-gate-input-on", 1f, "输入为 1 时，输出为 0。", 4.0, title: "接通输入"))
                .AddStep(new RealmPonderStep("not-gate-recap", 1f, "口诀：输出永远与输入相反。", 4.0, title: "记住规律"));
        }

        public static RealmPonderTutorial CreatePumpkinTutorial()
        {
            return new RealmPonderTutorial("realmex:pumpkin", "南瓜的生长")
                .SetScript(async ponder =>
                {
                    ponder.Camera.LookAt(new(8.5f, 1.6f, 8.5f), 8.5f);
                    ponder.Camera.SetOrbit(35f, height: 9.5f);
                    await ponder.ShowScene("pumpkin-soil", "先准备一块田地，给上方留出足够空间。",
                        RealmPonderPumpkinLayouts.Apply, title: "准备田地",
                        annotations: [new(new(8.5f, 2f, 8.5f), "田地", m_gold, new(-70, -65))]);
                    await ponder.Hold(4.5);
                    await ponder.ShowScene("pumpkin-seedling", "种下南瓜种子，观察田地中央的小瓜苗。",
                        RealmPonderPumpkinLayouts.Apply, title: "种下瓜苗",
                        annotations: [new(new(8.5f, 2.3f, 8.5f), "瓜苗", m_gold, new(0, -75))]);
                    await ponder.Hold(4.5);
                    await ponder.ShowScene("pumpkin-growing", "这些南瓜处在不同的生长阶段，个头会逐渐变大。",
                        RealmPonderPumpkinLayouts.Apply, title: "观察生长",
                        annotations: [new(new(8.5f, 2.5f, 8.5f), "生长中", m_gold, new(0, -75))]);
                    await ponder.Hold(5.0);
                    await ponder.ShowScene("pumpkin-mature", "橙色的成熟南瓜可以采收。拖动画面，从不同方向观察。",
                        RealmPonderPumpkinLayouts.Apply, title: "收获南瓜",
                        annotations: [new(new(8.5f, 2.5f, 8.5f), "成熟", m_on, new(0, -75))]);
                    await ponder.Hold(5.0);
                    await ponder.ShowScene("pumpkin-lantern", "成熟南瓜还能做成南瓜灯，为周围提供照明。",
                        RealmPonderPumpkinLayouts.Apply, title: "制作南瓜灯",
                        annotations: [new(new(8.5f, 2.5f, 8.5f), "南瓜灯", m_gold, new(0, -80))]);
                    await ponder.Hold(5.0);
                }, expectedStepCount: 5);
        }

        public static RealmPonderTutorial CreateAndGateTutorial()
        {
            return new RealmPonderTutorial("realmex:and_gate", "与门：两个条件同时成立")
                .SetScript(async ponder =>
                {
                    ponder.Camera.LookAt(new(8.5f, 1.8f, 8.5f), 8.5f);
                    ponder.Camera.SetOrbit(35f, radius: 12f, height: 10f);
                    await ponder.ShowScene("and-board", "先准备一块木板，接下来逐步放入电路元件。",
                        RealmPonderAndGateLayouts.Apply, title: "搭建演示台");
                    await ponder.Hold(3.0);
                    await ponder.ShowScene("and-intro", "中间是与门。它需要两个输入同时接通，才会输出信号。",
                        RealmPonderAndGateLayouts.Apply, title: "认识与门",
                        annotations: [new(new(8.5f, 2.2f, 8.5f), "与门", m_gold, new(0, -85))]);
                    await ponder.Hold(5.0);
                    await ponder.ShowScene("and-inputs", "左侧开关是输入 A，右侧是输入 B。导线将它们连接到与门。",
                        RealmPonderAndGateLayouts.Apply, title: "连接两个输入",
                        annotations: [InputA(false), InputB(false)]);
                    await ponder.Hold(5.5);
                    await ponder.ShowScene("and-00", "两个开关都关闭。输入为 0 和 0，输出保持关闭。",
                        RealmPonderAndGateLayouts.Apply, title: "都关闭：0 与 0",
                        annotations: [InputA(false), InputB(false), Output(false)]);
                    await ponder.Hold(5.0);
                    await ponder.ShowScene("and-10", "只接通 A 还不够。输入为 1 和 0，输出仍为 0。",
                        RealmPonderAndGateLayouts.Apply, title: "只开左侧：1 与 0",
                        annotations: [InputA(true), InputB(false), Output(false)]);
                    await ponder.Hold(5.0);
                    await ponder.ShowScene("and-01", "只接通 B 也一样。输入为 0 和 1，输出仍为 0。",
                        RealmPonderAndGateLayouts.Apply, title: "只开右侧：0 与 1",
                        annotations: [InputA(false), InputB(true), Output(false)]);
                    await ponder.Hold(5.0);
                    await ponder.ShowScene("and-11", "现在两个输入都为 1，输出终于接通，指示灯以绿色表示开启。",
                        RealmPonderAndGateLayouts.Apply, title: "同时开启：1 与 1",
                        annotations: [InputA(true), InputB(true), Output(true)]);
                    await ponder.Hold(6.0);
                    await ponder.ShowScene("and-recap", "记住：两边都开，输出才开。可以点击下方步骤，对照任意一组输入。",
                        RealmPonderAndGateLayouts.Apply, title: "两个条件，缺一不可",
                        annotations: [Output(true)]);
                    await ponder.Hold(5.0);
                }, expectedStepCount: 8);
        }

        private static RealmPonderAnnotation InputA(bool on) =>
            new(new(6.5f, 2.2f, 8.5f), on ? "A = 1" : "A = 0", on ? m_on : m_off, new(-55, 45));

        private static RealmPonderAnnotation InputB(bool on) =>
            new(new(10.5f, 2.2f, 8.5f), on ? "B = 1" : "B = 0", on ? m_on : m_off, new(65, 40));

        private static RealmPonderAnnotation Output(bool on) =>
            new(new(8.5f, 2.2f, 6.5f), on ? "输出 = 1" : "输出 = 0", on ? m_on : m_off, new(10, -80));
    }
}
