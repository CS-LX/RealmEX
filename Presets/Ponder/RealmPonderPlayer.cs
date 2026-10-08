using System;
using System.Linq;

namespace RealmEX.Presets.Ponder
{
    /// <summary>20 Hz tutorial clock. Seeking replays the same instructions and animations as playback.</summary>
    public sealed class RealmPonderPlayer
    {
        public const int TicksPerSecond = 20;
        private int m_instruction;
        private double m_accumulator;
        private float m_speed = 1;
        public RealmPonderTutorial Tutorial { get; }
        public RealmPonderState State { get; private set; }
        /// <summary>Fractional scene time for rendering between deterministic simulation ticks.</summary>
        public float PresentationTick { get; private set; }
        public bool IsPaused { get; set; }
        public bool ComfyReading { get; set; }
        public bool IsCompleted => State.Tick >= Tutorial.Duration;
        public int Generation { get; private set; }
        internal bool HasSession { get; set; }
        public event Action<RealmPonderState> Resetting;
        public event Action<RealmPonderState> StateChanged;
        public float Speed { get => m_speed; set { if (!float.IsFinite(value) || value < 0.25f || value > 4) throw new ArgumentOutOfRangeException(nameof(value)); m_speed = value; } }
        public int KeyframeIndex => Math.Max(0, Tutorial.Keyframes.TakeWhile(k => k.Tick <= State.Tick).Count() - 1);
        public RealmPonderPlayer(RealmPonderTutorial tutorial) { Tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial)); Replay(); }
        public void Replay()
        {
            Generation++; m_accumulator = 0; m_instruction = 0; PresentationTick = 0; IsPaused = false;
            State = new(Tutorial); Resetting?.Invoke(State); ApplyInstructions(); StateChanged?.Invoke(State);
        }
        public void Advance(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsPaused || IsCompleted) return;
            m_accumulator += seconds * TicksPerSecond * Speed;
            while (!IsCompleted)
            {
                double cost = ComfyReading && State.Overlays.Values.Any(o => o.Kind == RealmPonderOverlayKind.Text) ? 3 : 1;
                if (m_accumulator + 1e-8 < cost) break;
                m_accumulator -= cost; Step();
            }
            if (IsCompleted) { m_accumulator = 0; PresentationTick = State.Tick; }
            else
            {
                double cost = ComfyReading && State.Overlays.Values.Any(o => o.Kind == RealmPonderOverlayKind.Text) ? 3 : 1;
                PresentationTick = State.Tick + (float)(m_accumulator / cost);
                State.SampleAnimations(PresentationTick);
            }
        }
        public void Seek(int tick)
        {
            tick = Math.Clamp(tick, 0, Tutorial.Duration);
            bool paused = IsPaused;
            if (tick < State.Tick) Replay();
            while (State.Tick < tick) Step();
            m_accumulator = 0; PresentationTick = State.Tick; State.SampleAnimations(PresentationTick); IsPaused = paused;
        }
        public void SeekKeyframe(int index) => Seek(Tutorial.Keyframes[Math.Clamp(index, 0, Tutorial.Keyframes.Count - 1)].Tick);
        private void Step() { State.Advance(State.Tick + 1); ApplyInstructions(); StateChanged?.Invoke(State); }
        private void ApplyInstructions()
        {
            while (m_instruction < Tutorial.Instructions.Count && Tutorial.Instructions[m_instruction].Tick <= State.Tick)
                Tutorial.Instructions[m_instruction++].Apply(State);
        }
    }
}
