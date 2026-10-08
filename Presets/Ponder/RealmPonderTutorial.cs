using System;
using System.Collections.Generic;
using System.Linq;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    public sealed record RealmPonderKeyframe(int Tick, RealmPonderText Title);
    internal sealed record RealmPonderInstruction(int Tick, Action<RealmPonderState> Apply);
    /// <summary>Compiled scene. Duration and keyframes come from the authored schedule.</summary>
    public sealed class RealmPonderTutorial
    {
        public string Id { get; }
        public RealmPonderText Title { get; }
        public RealmPonderSchematic Schematic { get; }
        public IReadOnlyList<RealmPonderKeyframe> Keyframes { get; }
        public int Duration { get; }
        public bool NextUpEnabled { get; }
        public string ProjectTemplateName { get; }
        internal IReadOnlyList<RealmPonderInstruction> Instructions { get; }
        internal RealmPonderTutorial(string id, RealmPonderText title, RealmPonderSchematic schematic,
            IEnumerable<RealmPonderInstruction> instructions, IEnumerable<RealmPonderKeyframe> keyframes, int duration, bool nextUpEnabled, string projectTemplateName)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.Contains(':')) throw new ArgumentException("Use a namespaced tutorial ID.", nameof(id));
            Id = id; Title = title ?? throw new ArgumentNullException(nameof(title)); Schematic = schematic;
            Instructions = Array.AsReadOnly(instructions.OrderBy(i => i.Tick).ToArray());
            Keyframes = Array.AsReadOnly(keyframes.OrderBy(i => i.Tick).ToArray());
            Duration = Math.Max(1, duration); NextUpEnabled = nextUpEnabled;
            ProjectTemplateName = projectTemplateName;
        }
    }
}
