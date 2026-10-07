using System;
using RealmEX.Core.Scenes;
using RealmEX.Core.Storyboard;

namespace RealmEX.Presets.Ponder
{
    public static class RealmPonderPlayer
    {
        public static RealmStoryboard CreateStoryboard(
            RealmPonderTutorial tutorial,
            Action<RealmPonderStep> stepStarted = null,
            int startStep = 0)
        {
            ArgumentNullException.ThrowIfNull(tutorial);
            if (startStep < 0 || startStep >= tutorial.StepCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startStep));
            }
            RealmStoryboard storyboard = new();
            storyboard.EnqueueAsync(async ctx =>
            {
                if (tutorial.Script != null)
                {
                    RealmPonderScriptContext ponder = new(ctx, stepStarted, startStep);
                    await tutorial.Script(ponder);
                    return;
                }

                for (int i = 0; i < tutorial.Steps.Count; i++)
                {
                    RealmPonderStep step = tutorial.Steps[i];
                    await ctx.ApplyScene(new RealmScene(step.SceneName, step.TimeFactor));
                    if (step.BuildsWorld)
                    {
                        RealmPonderPumpkinLayouts.Apply(ctx.Realm, step.SceneName);
                    }

                    if (i < startStep)
                    {
                        continue;
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
