using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Engine;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    public enum RealmPonderOverlayKind { Text, Controls, Outline, Bounds, Line, Success, Particle }
    public enum RealmPonderTextPlacement { Auto, NearTarget, TopLeft, TopRight, BottomLeft, BottomRight }
    public enum RealmPonderInput { Interact, Use, Scroll, Move }
    public sealed record RealmPonderText(string Chinese, string English)
    {
        public string Resolve(string language) => language?.StartsWith("zh", StringComparison.OrdinalIgnoreCase) == true ? Chinese : English;
        public static implicit operator RealmPonderText(string text) => new(text, text);
    }
    public sealed record RealmPonderOverlay(string Id, RealmPonderOverlayKind Kind, int StartTick, int Duration,
        Vector3 Position, Vector3 End, RealmPonderText Text, Color Color, RealmPonderSelection Selection = null,
        RealmPonderInput Input = RealmPonderInput.Interact, int ItemValue = 0, bool Sneak = false, RealmPonderTextPlacement Placement = RealmPonderTextPlacement.Auto)
    {
        public float Opacity(int tick) => Math.Clamp(Math.Min((tick - StartTick + 1) / 5f, (StartTick + Duration - tick) / 5f), 0, 1);
    }
    public sealed class RealmPonderSection
    {
        public string Id { get; }
        public RealmPonderSelection Selection { get; internal set; }
        public Vector3 Offset { get; internal set; }
        public Vector3 RevealOffset { get; internal set; }
        public Vector3 Rotation { get; internal set; }
        public Vector3 Pivot { get; internal set; }
        public float Opacity { get; internal set; }
        public Matrix Transform => Matrix.CreateTranslation(-Pivot) * Matrix.CreateRotationX(MathUtils.DegToRad(Rotation.X))
            * Matrix.CreateRotationY(MathUtils.DegToRad(Rotation.Y)) * Matrix.CreateRotationZ(MathUtils.DegToRad(Rotation.Z))
            * Matrix.CreateTranslation(Pivot + Offset + RevealOffset);
        internal RealmPonderSection(string id, RealmPonderSelection selection) { Id = id; Selection = selection; Pivot = selection.Center; }
    }
    internal sealed record RealmPonderAnimation(string Channel, int Start, int Duration, Action<float> Apply);
    /// <summary>Replayable scene state, independent of GPU and global frame time.</summary>
    public sealed class RealmPonderState
    {
        private readonly Dictionary<Point3, int> m_blocks;
        private readonly Dictionary<string, RealmPonderSection> m_sections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RealmPonderOverlay> m_overlays = new(StringComparer.Ordinal);
        private readonly List<RealmPonderAnimation> m_animations = [];
        private readonly Dictionary<string, RealmPonderActor> m_actors = new(StringComparer.Ordinal);
        internal readonly Queue<Action<RealmPonderWorld>> WorldInstructions = [];
        public IReadOnlyDictionary<Point3, int> Blocks { get; }
        public IReadOnlyDictionary<string, RealmPonderSection> Sections { get; }
        public IReadOnlyDictionary<string, RealmPonderOverlay> Overlays { get; }
        public IReadOnlyDictionary<string, RealmPonderActor> Actors { get; }
        public int Tick { get; internal set; }
        public int GeometryRevision { get; private set; }
        public Vector3 CameraTarget { get; internal set; }
        public float CameraYaw { get; internal set; } = 35;
        public float CameraHeight { get; internal set; } = 10;
        public float CameraRadius { get; internal set; } = 12;
        public float ViewHeight { get; internal set; } = 9;
        public bool ShowShadow { get; internal set; } = true;
        public bool IsFinished { get; internal set; }
        public RealmPonderUiState Ui { get; internal set; }
        internal RealmPonderState(RealmPonderTutorial tutorial)
        {
            m_blocks = new(tutorial.Schematic.Blocks);
            Blocks = new ReadOnlyDictionary<Point3, int>(m_blocks);
            Sections = new ReadOnlyDictionary<string, RealmPonderSection>(m_sections);
            Overlays = new ReadOnlyDictionary<string, RealmPonderOverlay>(m_overlays);
            Actors = new ReadOnlyDictionary<string, RealmPonderActor>(m_actors);
            CameraTarget = tutorial.Schematic.Selection.Center;
            m_sections.Add("base", new("base", tutorial.Schematic.Selection));
        }
        public void SetBlock(Point3 point, int value)
        {
            if (point.Y < 0 || point.Y > 255) throw new ArgumentOutOfRangeException(nameof(point));
            if (m_blocks.TryGetValue(point, out int previous) && previous == value) return;
            m_blocks[point] = value;
            if (!m_sections.Values.Any(s => s.Selection.Contains(point)))
                m_sections["base"].Selection = m_sections["base"].Selection.Add(new([point]));
            GeometryRevision++;
        }
        internal RealmPonderSection Section(string id) => m_sections.TryGetValue(id, out var section) ? section : throw new InvalidOperationException($"Unknown section '{id}'.");
        internal void Split(string id, RealmPonderSelection selection)
        {
            if (m_sections.ContainsKey(id)) throw new InvalidOperationException($"Duplicate section '{id}'.");
            foreach (var section in m_sections.Values) section.Selection = section.Selection.Subtract(selection);
            m_sections.Add(id, new(id, selection));
            GeometryRevision++;
        }
        internal void Merge(string source, string target)
        {
            var from = Section(source); var to = Section(target);
            if (source == target) return;
            if (source == "base") throw new InvalidOperationException("The base section cannot be removed; merge independent sections into it.");
            to.Selection = to.Selection.Add(from.Selection); m_sections.Remove(source); GeometryRevision++;
        }
        internal void Overlay(RealmPonderOverlay overlay) => m_overlays[overlay.Id] = overlay;
        internal void RemoveOverlay(string id) { m_overlays.Remove(id); m_animations.RemoveAll(a => a.Channel == "overlay:" + id); }
        internal void AddActor(RealmPonderActor actor) => m_actors.Add(actor.Id, actor);
        internal RealmPonderActor Actor(string id) => m_actors[id];
        internal void RemoveActor(string id)
        {
            m_actors.Remove(id);
            m_animations.RemoveAll(a => a.Channel.StartsWith("actor:" + id + ":", StringComparison.Ordinal));
        }
        internal void Animate(string channel, int duration, Action<float> apply)
        {
            m_animations.RemoveAll(a => a.Channel == channel);
            if (duration == 0) apply(1);
            else { apply(0); m_animations.Add(new(channel, Tick, duration, apply)); }
        }
        internal void Advance(int tick)
        {
            Tick = tick;
            SampleAnimations(tick);
            m_animations.RemoveAll(a => tick >= a.Start + a.Duration);
            foreach (string id in m_overlays.Where(p => tick >= p.Value.StartTick + p.Value.Duration).Select(p => p.Key).ToArray()) m_overlays.Remove(id);
        }
        internal void SampleAnimations(float tick)
        {
            foreach (var animation in m_animations)
            {
                float t = Math.Clamp((tick - animation.Start) / (float)animation.Duration, 0, 1);
                animation.Apply(t * t * (3 - 2 * t));
            }
        }
        public (Point3 Position, int Value, float Distance)? Raycast(Ray3 ray)
        {
            (Point3 Position, int Value, float Distance)? result = null;
            foreach (var section in m_sections.Values.Where(s => s.Opacity > 0.2f))
            {
                Matrix inverse = Matrix.Invert(section.Transform);
                Ray3 local = new(Vector3.Transform(ray.Position, inverse), Vector3.TransformNormal(ray.Direction, inverse));
                foreach (Point3 point in section.Selection)
                {
                    if (!m_blocks.TryGetValue(point, out int value) || Game.Terrain.ExtractContents(value) == 0) continue;
                    Vector3 min = new(point.X, point.Y, point.Z);
                    float? distance = local.Intersection(new BoundingBox(min, min + Vector3.One));
                    if (distance.HasValue && (!result.HasValue || distance < result.Value.Distance)) result = (point, value, distance.Value);
                }
            }
            return result;
        }
    }
}
