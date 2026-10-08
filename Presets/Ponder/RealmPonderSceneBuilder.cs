using System;
using System.Collections.Generic;
using Engine;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Instructions at the cursor run concurrently; only Idle advances the cursor.</summary>
    public sealed partial class RealmPonderSceneBuilder
    {
        private readonly string m_id;
        private readonly RealmPonderText m_title;
        private readonly RealmPonderSchematic m_schematic;
        private readonly List<RealmPonderInstruction> m_instructions = [];
        private readonly List<RealmPonderKeyframe> m_keyframes = [];
        private int m_cursor;
        private int m_end;
        public bool NextUpEnabled { get; set; } = true;
        public string ProjectTemplateName { get; set; } = RealmBootstrap.PonderProjectTemplateName;
        public int CurrentTick => m_cursor;
        public RealmPonderSceneBuilder(string id, RealmPonderText title, RealmPonderSchematic schematic)
        { m_id = id; m_title = title; m_schematic = schematic ?? throw new ArgumentNullException(nameof(schematic)); }
        public RealmPonderSceneBuilder Idle(int ticks)
        { CheckDuration(ticks); m_cursor = checked(m_cursor + ticks); m_end = Math.Max(m_end, m_cursor); return this; }
        public RealmPonderSceneBuilder Keyframe(RealmPonderText title, bool lazy = false)
        {
            if (lazy && m_keyframes.Count > 0 && m_cursor - m_keyframes[^1].Tick < 20) return this;
            if (m_keyframes.Count > 0 && m_keyframes[^1].Tick == m_cursor) m_keyframes.RemoveAt(m_keyframes.Count - 1);
            m_keyframes.Add(new(m_cursor, title)); return this;
        }
        public RealmPonderSceneBuilder ConfigureCamera(Vector3 target, float viewHeight = 9, float yaw = 35, float radius = 12, float height = 10)
            => Schedule(s => { s.CameraTarget = target; s.ViewHeight = viewHeight; s.CameraYaw = yaw; s.CameraRadius = radius; s.CameraHeight = height; });
        public RealmPonderSceneBuilder RotateCamera(float degrees, int duration)
            => Schedule(s => { float from = s.CameraYaw; s.Animate("camera-yaw", duration, t => s.CameraYaw = from + degrees * t); }, duration);
        public RealmPonderSceneBuilder RemoveShadow() => Schedule(s => s.ShowShadow = false);
        public RealmPonderSceneBuilder MarkAsFinished() => Schedule(s => s.IsFinished = true);
        public RealmPonderSceneBuilder IndependentSection(string id, RealmPonderSelection selection) => Schedule(s => s.Split(id, selection));
        public RealmPonderSceneBuilder MergeSection(string source, string target) => Schedule(s => s.Merge(source, target));
        public RealmPonderSceneBuilder ShowSection(string id, Vector3 from = default, int duration = 10)
            => Schedule(s => { var section = s.Section(id); s.Animate(id + ":visibility", duration, t => { section.Opacity = t; section.RevealOffset = from * (1 - t); }); }, duration);
        public RealmPonderSceneBuilder HideSection(string id, Vector3 toward = default, int duration = 10)
            => Schedule(s => { var section = s.Section(id); float opacity = section.Opacity; s.Animate(id + ":visibility", duration, t => { section.Opacity = opacity * (1 - t); section.RevealOffset = toward * t; }); }, duration);
        public RealmPonderSceneBuilder MoveSection(string id, Vector3 offset, int duration)
            => Schedule(s => { var section = s.Section(id); Vector3 from = section.Offset; s.Animate(id + ":position", duration, t => section.Offset = from + offset * t); }, duration);
        public RealmPonderSceneBuilder RotateSection(string id, Vector3 degrees, int duration, Vector3? pivot = null)
            => Schedule(s => { var section = s.Section(id); if (pivot.HasValue) section.Pivot = pivot.Value; Vector3 from = section.Rotation; s.Animate(id + ":rotation", duration, t => section.Rotation = from + degrees * t); }, duration);
        public RealmPonderSceneBuilder SetBlocks(RealmPonderSelection selection, int value)
            => Schedule(s => { foreach (Point3 p in selection) s.SetBlock(p, value); });
        public RealmPonderSceneBuilder ModifyBlocks(RealmPonderSelection selection, Func<int, int> modify)
            => Schedule(s => { foreach (Point3 p in selection) s.SetBlock(p, modify(s.Blocks.TryGetValue(p, out int value) ? value : 0)); });
        public RealmPonderSceneBuilder RestoreBlocks(RealmPonderSelection selection)
            => Schedule(s => { foreach (Point3 p in selection) s.SetBlock(p, m_schematic.Blocks.TryGetValue(p, out int value) ? value : 0); });
        public RealmPonderSceneBuilder Text(string id, RealmPonderText text, Vector3 point, int duration, Color? color = null, RealmPonderTextPlacement placement = RealmPonderTextPlacement.Auto)
            => Schedule(s => s.Overlay(new(id, RealmPonderOverlayKind.Text, s.Tick, duration, point, default, text, color ?? new Color(225, 231, 240), Placement: placement)), duration);
        public RealmPonderSceneBuilder Controls(string id, RealmPonderInput input, Vector3 point, int duration, int itemValue = 0, bool sneak = false)
            => Schedule(s => s.Overlay(new(id, RealmPonderOverlayKind.Controls, s.Tick, duration, point, default, "", Color.White, Input: input, ItemValue: itemValue, Sneak: sneak)), duration);
        public RealmPonderSceneBuilder Outline(string id, RealmPonderSelection selection, int duration, Color color)
            => Schedule(s => s.Overlay(new(id, RealmPonderOverlayKind.Outline, s.Tick, duration, selection.Center, default, "", color, selection)), duration);
        public RealmPonderSceneBuilder RemoveOverlay(string id) => Schedule(s => s.RemoveOverlay(id));
        public RealmPonderSceneBuilder ChaseBounds(string id, BoundingBox bounds, int duration, int transition, Color color)
            => Schedule(s =>
            {
                CheckDuration(transition);
                Vector3 from = bounds.Min, to = bounds.Max;
                if (s.Overlays.TryGetValue(id, out var old) && old.Kind == RealmPonderOverlayKind.Bounds) { from = old.Position; to = old.End; }
                int start = s.Tick;
                s.Animate("overlay:" + id, Math.Min(duration, transition), t => s.Overlay(new(id, RealmPonderOverlayKind.Bounds, start, duration,
                    Vector3.Lerp(from, bounds.Min, t), Vector3.Lerp(to, bounds.Max, t), "", color)));
            }, duration);
        public RealmPonderSceneBuilder Line(string id, Vector3 from, Vector3 to, int duration, Color color)
            => Overlay(id, RealmPonderOverlayKind.Line, from, to, "", duration, color);
        public RealmPonderSceneBuilder Success(Vector3 point, int duration = 20)
            => Overlay("success", RealmPonderOverlayKind.Success, point, default, "", duration, new Color(130, 230, 160));
        public RealmPonderSceneBuilder Particles(string id, Vector3 point, Vector3 velocity, int duration, Color color)
            => Overlay(id, RealmPonderOverlayKind.Particle, point, velocity, "", duration, color);
        public RealmPonderSceneBuilder CreateItem(string id, int value, Vector3 position, float scale = 0.6f)
            => Schedule(s => s.AddActor(new(id, RealmPonderActorKind.Item, position, value, null, null) { Scale = scale }));
        public RealmPonderSceneBuilder CreateModel(string id, string model, string texture, Vector3 position, float scale = 1)
            => Schedule(s => s.AddActor(new(id, RealmPonderActorKind.Model, position, 0, model, texture) { Scale = scale }));
        public RealmPonderSceneBuilder MoveActor(string id, Vector3 offset, int duration)
            => Schedule(s => { var actor = s.Actor(id); Vector3 from = actor.Position; s.Animate("actor:" + id + ":position", duration, t => actor.Position = from + offset * t); }, duration);
        public RealmPonderSceneBuilder RotateActor(string id, Vector3 degrees, int duration)
            => Schedule(s => { var actor = s.Actor(id); Vector3 from = actor.Rotation; s.Animate("actor:" + id + ":rotation", duration, t => actor.Rotation = from + degrees * t); }, duration);
        public RealmPonderSceneBuilder RemoveActor(string id) => Schedule(s => s.RemoveActor(id));
        /// <summary>Runs against the isolated Project, including on replay/seek. Never capture the main Project here.</summary>
        public RealmPonderSceneBuilder World(Action<RealmPonderWorld> instruction)
        {
            ArgumentNullException.ThrowIfNull(instruction);
            return Schedule(s => s.WorldInstructions.Enqueue(instruction));
        }
        private RealmPonderSceneBuilder Overlay(string id, RealmPonderOverlayKind kind, Vector3 point, Vector3 end, RealmPonderText text, int duration, Color color)
            => Schedule(s => s.Overlay(new(id, kind, s.Tick, duration, point, end, text, color)), duration);
        public RealmPonderSceneBuilder Schedule(Action<RealmPonderState> instruction, int duration = 0)
        {
            ArgumentNullException.ThrowIfNull(instruction); CheckDuration(duration);
            m_instructions.Add(new(m_cursor, instruction)); m_end = Math.Max(m_end, checked(m_cursor + duration)); return this;
        }
        public RealmPonderTutorial Build()
        {
            if (m_keyframes.Count == 0 || m_keyframes[0].Tick != 0) m_keyframes.Insert(0, new(0, m_title));
            return new(m_id, m_title, m_schematic, m_instructions, m_keyframes, m_end, NextUpEnabled, ProjectTemplateName);
        }
        private static void CheckDuration(int ticks) { if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks)); }
    }
}
