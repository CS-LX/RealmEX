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
                .AddStep(new RealmPonderStep(
                    "pumpkin-soil",
                    1f,
                    "先翻出一块松软的田地，上面要留空。",
                    1.4,
                    buildsWorld: true))
                .AddStep(new RealmPonderStep(
                    "pumpkin-seedling",
                    1f,
                    "把南瓜种子点在田地上，会出现小小的瓜苗。",
                    1.4,
                    buildsWorld: true))
                .AddStep(new RealmPonderStep(
                    "pumpkin-growing",
                    1f,
                    "瓜苗需要足够的阳光；有水的耕地会长得更快。",
                    1.6,
                    buildsWorld: true))
                .AddStep(new RealmPonderStep(
                    "pumpkin-mature",
                    1f,
                    "最终结成饱满的橙色南瓜，可以采下来吃或煮汤。",
                    1.6,
                    buildsWorld: true))
                .AddStep(new RealmPonderStep(
                    "pumpkin-lantern",
                    1f,
                    "成熟南瓜还能做成南瓜灯，放在稳固表面上就会亮着。",
                    1.8,
                    buildsWorld: true));
        }
    }
}
