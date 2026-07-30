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
                    ponder.Camera.LookAt(new(8.5f, 1.6f, 8.5f), 11f);
                    ponder.Camera.SetOrbit(25f);

                    await ponder.ShowScene(
                        "pumpkin-soil",
                        "先翻出一块松软的田地，上面要留空。",
                        RealmPonderPumpkinLayouts.Apply);
                    await ponder.Camera.RotateBy(45f, 1.2, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-seedling",
                        "把南瓜种子点在田地上，会出现小小的瓜苗。",
                        RealmPonderPumpkinLayouts.Apply);
                    await ponder.Camera.RotateBy(55f, 1.25, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-growing",
                        "瓜苗需要足够的阳光；有水的耕地会长得更快。",
                        RealmPonderPumpkinLayouts.Apply);
                    await ponder.Camera.RotateBy(70f, 1.35, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-mature",
                        "最终结成饱满的橙色南瓜，可以采下来吃或煮汤。",
                        RealmPonderPumpkinLayouts.Apply);
                    await ponder.Camera.RotateBy(65f, 1.35, RealmPonderEase.SinInOut);

                    await ponder.ShowScene(
                        "pumpkin-lantern",
                        "成熟南瓜还能做成南瓜灯，放在稳固表面上就会亮着。",
                        RealmPonderPumpkinLayouts.Apply);
                    await ponder.Camera.RotateBy(90f, 1.5, RealmPonderEase.SinInOut);
                    await ponder.Hold(0.4);
                }, expectedStepCount: 5);
        }
    }
}
