namespace RealmEX.Presets.Ponder.Sandbox
{
    /// <summary>PonderSession calls Realm.Tick exactly once per tutorial tick.</summary>
    public sealed class PonderSubsystemTime : Game.SubsystemTime
    {
        public override float CalculateGameTimeDalta() => 1f / RealmPonderPlayer.TicksPerSecond;
        public override void Load(TemplatesDatabase.ValuesDictionary valuesDictionary)
        {
            base.Load(valuesDictionary);
            GameMenuDialogTimeFactor = 1;
        }
    }
}
