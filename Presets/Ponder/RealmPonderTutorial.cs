using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealmEX.Presets.Ponder
{
    public sealed class RealmPonderTutorial
    {
        private readonly List<RealmPonderStep> m_steps = [];

        public RealmPonderTutorial(string id, string title, int expectedStepCount = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Tutorial id cannot be empty.", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Tutorial title cannot be empty.", nameof(title));
            }

            Id = id.Trim();
            Title = title.Trim();
            ExpectedStepCount = expectedStepCount;
        }

        public string Id { get; }

        public string Title { get; }

        public IReadOnlyList<RealmPonderStep> Steps => m_steps;

        public int ExpectedStepCount { get; private set; }

        public int StepCount => ExpectedStepCount > 0 ? ExpectedStepCount : m_steps.Count;

        public Func<RealmPonderScriptContext, Task> Script { get; private set; }

        public RealmPonderTutorial AddStep(RealmPonderStep step)
        {
            m_steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }

        public RealmPonderTutorial SetScript(Func<RealmPonderScriptContext, Task> script, int expectedStepCount)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
            if (expectedStepCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedStepCount), "Expected step count must be positive.");
            }
            ExpectedStepCount = expectedStepCount;
            return this;
        }
    }
}
