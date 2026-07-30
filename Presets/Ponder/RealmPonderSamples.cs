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
    }
}
