using System;
using RealmEX.Core.Scenes;
using RealmEX.Core.Storyboard;

namespace RealmEX.Presets.Ponder
{
    public static class RealmPonderPlayer
    {
        public static RealmStoryboard CreateStoryboard(
            RealmPonderTutorial tutorial,
            Action<RealmPonderStep> stepStarted = null)
        {
            ArgumentNullException.ThrowIfNull(tutorial);
            RealmStoryboard storyboard = new();
            storyboard.EnqueueAsync(async ctx =>
            {
                foreach (RealmPonderStep step in tutorial.Steps)
                {
                    await ctx.ApplyScene(new RealmScene(step.SceneName, step.TimeFactor));
                    if (step.BuildsWorld)
                    {
                        RealmPonderPumpkinLayouts.Apply(ctx.Realm, step.SceneName);
                    }

                    stepStarted?.Invoke(step);
                    if (step.WaitGameTimeSeconds > 0.0)
                    {
                        await ctx.WaitGameTime(step.WaitGameTimeSeconds);
                    }
                }
            });
            return storyboard;
        }
    }
}
