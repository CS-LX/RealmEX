using Engine;

namespace RealmEX.Presets.Ponder
{
    public static class RealmPonderSamples
    {
        public static RealmPonderTutorial CreateNotGateTutorial()
        {
            return new RealmPonderTutorial("realmex:not_gate", "非门")
                .AddStep(new RealmPonderStep(
                    "not-gate-intro",
                    1f,
                    "非门会把输入信号反转。",
                    1.0))
                .AddStep(new RealmPonderStep(
                    "not-gate-input-off",
                    1f,
                    "输入为 0 时，输出为 1。",
                    1.0))
                .AddStep(new RealmPonderStep(
                    "not-gate-input-on",
                    1f,
                    "输入为 1 时，输出为 0。",
                    1.0))
                .AddStep(new RealmPonderStep(
                    "not-gate-recap",
                    1f,
                    "口诀：输出永远与输入相反。",
                    0.8));
        }

        public static RealmPonderTutorial CreatePumpkinTutorial()
        {
            return new RealmPonderTutorial("realmex:pumpkin", "南瓜")
                .SetScript(async ponder =>
                {
                    ponder.Realm.Viewport.ClearColor = Color.Transparent;
                    ponder.Camera.LookAt(new(8.5f, 1.6f, 8.5f), 11f);
                    ponder.Camera.SetOrbit(25f);

                    await ponder.ShowScene(
                        "pumpkin-soil",
                        "先翻出一块松软的田地，上面要留空。",
                        RealmPonderPumpkinLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(45f, 1.2, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-seedling",
                        "把南瓜种子点在田地上，会出现小小的瓜苗。",
                        RealmPonderPumpkinLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(55f, 1.25, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-growing",
                        "瓜苗需要足够的阳光；有水的耕地会长得更快。",
                        RealmPonderPumpkinLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(70f, 1.35, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-mature",
                        "最终结成饱满的橙色南瓜，可以采下来吃或煮汤。",
                        RealmPonderPumpkinLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(65f, 1.35, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-lantern",
                        "成熟南瓜还能做成南瓜灯，放在稳固表面上就会亮着。",
                        RealmPonderPumpkinLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(90f, 1.5, RealmPonderEase.SinInOut);
                    await ponder.Hold(0.4);
                }, expectedStepCount: 5);
        }

        public static RealmPonderTutorial CreateAndGateTutorial()
        {
            return new RealmPonderTutorial("realmex:and_gate", "与门")
                .SetScript(async ponder =>
                {
                    ponder.Realm.Viewport.ClearColor = Color.Transparent;
                    ponder.Camera.LookAt(new(8.5f, 1.8f, 8.0f), 12f);
                    ponder.Camera.SetOrbit(20f, radius: 12f, height: 8.5f);

                    await ponder.ShowScene(
                        "and-intro",
                        "与门有两个输入。只有两边都接通，输出才会亮。",
                        RealmPonderAndGateLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(40f, 1.1, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "and-00",
                        "输入 0 和 0：两边都断开，输出保持熄灭。",
                        RealmPonderAndGateLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(50f, 1.15, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "and-10",
                        "输入 1 和 0：只接通一边，输出仍然熄灭。",
                        RealmPonderAndGateLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(55f, 1.2, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "and-01",
                        "输入 0 和 1：另一边接通，输出还是熄灭。",
                        RealmPonderAndGateLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(60f, 1.2, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "and-11",
                        "输入 1 和 1：两边都接通，输出才亮起来。",
                        RealmPonderAndGateLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(70f, 1.35, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "and-recap",
                        "口诀：与门要两边都开，输出才会开。",
                        RealmPonderAndGateLayouts.Apply,
                        clearColor: Color.Transparent);
                    await ponder.Camera.RotateBy(80f, 1.4, RealmPonderEase.SinInOut);
                    await ponder.Hold(0.5);
                }, expectedStepCount: 6);
        }
    }
}
