using System;
using System.Collections.Generic;

namespace RealmEX.Presets.Ponder
{
    public sealed class RealmPonderTutorial
    {
        private readonly List<RealmPonderStep> m_steps = [];

        public RealmPonderTutorial(string id, string title)
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
        }

        public string Id { get; }

        public string Title { get; }

        public IReadOnlyList<RealmPonderStep> Steps => m_steps;

        public RealmPonderTutorial AddStep(RealmPonderStep step)
        {
            m_steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
            return this;
        }
    }
}
