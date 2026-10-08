using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Engine;
using Game;

namespace RealmEX.Presets.Ponder
{
    public enum RealmPonderUiAction { Point, Click, Drag, Scroll }
    public sealed record RealmPonderUiCue(string Target, string From, RealmPonderUiAction Action, int StartTick, int Duration)
    {
        public float Progress(int tick) => Math.Clamp((tick - StartTick) / (float)Math.Max(1, Duration), 0, 1);
    }
    /// <summary>A fresh, isolated widget tree is created for every show/replay. Never return a player's live UI.</summary>
    public sealed class RealmPonderUiDefinition
    {
        public RealmPonderText Title { get; }
        public Vector2 Size { get; }
        public Func<CanvasWidget> Create { get; }
        public RealmPonderUiDefinition(RealmPonderText title, Vector2 size, Func<CanvasWidget> create)
        {
            if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0 || size.Y <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            Title = title; Size = size; Create = create ?? throw new ArgumentNullException(nameof(create));
        }
        public static RealmPonderUiDefinition FromXml(string asset, RealmPonderText title, Vector2 size)
            => new(title, size, () => { CanvasWidget widget = new(); widget.LoadContents(widget, new XElement(ContentManager.Get<XElement>(asset))); return widget; });
    }
    public sealed class RealmPonderUiState
    {
        private readonly List<Action<CanvasWidget, string>> m_edits = [];
        public RealmPonderUiDefinition Definition { get; }
        public IReadOnlyList<Action<CanvasWidget, string>> Edits { get; }
        public RealmPonderUiCue Cue { get; internal set; }
        internal RealmPonderUiState(RealmPonderUiDefinition definition) { Definition = definition; Edits = new ReadOnlyCollection<Action<CanvasWidget, string>>(m_edits); }
        internal void Edit(Action<CanvasWidget, string> edit) => m_edits.Add(edit);
    }
}
