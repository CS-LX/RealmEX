using System;
using System.Collections.Generic;
using System.Linq;
using Game;

namespace RealmEX.Presets.Ponder
{
    public interface IRealmPonderPlugin
    {
        string Namespace { get; }
        void Register(RealmPonderRegistry registry);
    }
    public sealed record RealmPonderTag(string Id, RealmPonderText Title, RealmPonderText Description);
    public sealed record RealmPonderSeries(string Id, RealmPonderText Title, int Order = 0);
    public sealed record RealmPonderEntry(RealmPonderTutorial Tutorial, IReadOnlyList<string> Tags, IReadOnlyList<int> Subjects, bool ShowInIndex, int Order, string SeriesId = null);
    /// <summary>Ordered, namespaced registration shared by item lookup and the tutorial index.</summary>
    public sealed class RealmPonderRegistry
    {
        private readonly Dictionary<string, RealmPonderEntry> m_entries = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RealmPonderTag> m_tags = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RealmPonderSeries> m_series = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RealmPonderText> m_sharedText = new(StringComparer.Ordinal);
        private readonly HashSet<string> m_plugins = new(StringComparer.Ordinal);
        private string m_registeringNamespace;
        public IEnumerable<RealmPonderTag> Tags => m_tags.Values;
        public IEnumerable<RealmPonderSeries> Series => m_series.Values;
        public void RegisterPlugin(IRealmPonderPlugin plugin)
        {
            ArgumentNullException.ThrowIfNull(plugin);
            if (m_registeringNamespace != null || string.IsNullOrWhiteSpace(plugin.Namespace) || m_plugins.Contains(plugin.Namespace))
                throw new InvalidOperationException("Plugin already registered, nested, or missing its namespace.");
            m_registeringNamespace = plugin.Namespace;
            var entriesBefore = m_entries.Keys.ToHashSet(StringComparer.Ordinal);
            var tagsBefore = m_tags.Keys.ToHashSet(StringComparer.Ordinal);
            var seriesBefore = m_series.Keys.ToHashSet(StringComparer.Ordinal);
            var textsBefore = m_sharedText.Keys.ToHashSet(StringComparer.Ordinal);
            try { plugin.Register(this); m_plugins.Add(plugin.Namespace); }
            catch
            {
                foreach (string key in m_entries.Keys.Where(k => !entriesBefore.Contains(k)).ToArray()) m_entries.Remove(key);
                foreach (string key in m_tags.Keys.Where(k => !tagsBefore.Contains(k)).ToArray()) m_tags.Remove(key);
                foreach (string key in m_series.Keys.Where(k => !seriesBefore.Contains(k)).ToArray()) m_series.Remove(key);
                foreach (string key in m_sharedText.Keys.Where(k => !textsBefore.Contains(k)).ToArray()) m_sharedText.Remove(key);
                throw;
            }
            finally { m_registeringNamespace = null; }
        }
        public void Register(RealmPonderTutorial tutorial, IEnumerable<string> tags = null, IEnumerable<int> subjects = null, int order = 0, bool showInIndex = true, string series = null)
        {
            CheckId(tutorial.Id);
            if (series != null) { CheckId(series); if (!m_series.ContainsKey(series)) throw new ArgumentException("Unknown tutorial series.", nameof(series)); }
            m_entries.Add(tutorial.Id, new(tutorial, Array.AsReadOnly((tags ?? []).Distinct().ToArray()), Array.AsReadOnly((subjects ?? []).Distinct().ToArray()), showInIndex, order, series));
        }
        public void RegisterSeries(RealmPonderSeries series) { CheckId(series.Id); m_series.Add(series.Id, series); }
        public RealmPonderSeries GetSeries(string id) => m_series[id];
        public IReadOnlyList<RealmPonderTutorial> Sequence(string tutorialId, IReadOnlyList<RealmPonderTutorial> candidates = null)
        {
            string series = Entry(tutorialId).SeriesId;
            var ids = candidates?.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
            return m_entries.Values.Where(e => e.SeriesId == series && (e.ShowInIndex || e.Tutorial.Id == tutorialId || ids != null && ids.Contains(e.Tutorial.Id)))
                .Where(e => series != null || ids == null || ids.Contains(e.Tutorial.Id))
                .OrderBy(e => e.Order).ThenBy(e => e.Tutorial.Id, StringComparer.Ordinal).Select(e => e.Tutorial).ToArray();
        }
        public void RegisterTag(RealmPonderTag tag) { CheckId(tag.Id); m_tags.Add(tag.Id, tag); }
        public void RegisterSharedText(string id, RealmPonderText text) { CheckId(id); m_sharedText.Add(id, text); }
        public RealmPonderText SharedText(string id) => m_sharedText[id];
        public RealmPonderTutorial Get(string id) => m_entries[id].Tutorial;
        public RealmPonderEntry Entry(string id) => m_entries[id];
        public IReadOnlyList<RealmPonderTutorial> ForSubject(int blockValue) => m_entries.Values.Where(e => e.Subjects.Any(subject => SameSubject(subject, blockValue))).OrderBy(e => e.Order).ThenBy(e => e.Tutorial.Id, StringComparer.Ordinal).Select(e => e.Tutorial).ToArray();
        private static bool SameSubject(int subject, int value)
        {
            if (Terrain.ReplaceLight(subject, 0) == Terrain.ReplaceLight(value, 0)) return true;
            if (Terrain.ExtractContents(subject) != Terrain.ExtractContents(value)) return false;
            Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            string craftingId = block?.GetCraftingId(value);
            return !string.IsNullOrEmpty(craftingId) && string.Equals(craftingId, block.GetCraftingId(subject), StringComparison.Ordinal);
        }
        public IReadOnlyList<RealmPonderEntry> Search(string query = "", string tag = null, string language = "en-US", bool includeHidden = false) => m_entries.Values
            .Where(e => (includeHidden || e.ShowInIndex) && (tag == null || e.Tags.Contains(tag)) && (string.IsNullOrWhiteSpace(query)
                || e.Tutorial.Title.Resolve(language).Contains(query, StringComparison.OrdinalIgnoreCase)
                || e.SeriesId != null && m_series[e.SeriesId].Title.Resolve(language).Contains(query, StringComparison.OrdinalIgnoreCase)
                || e.Tags.Any(t => m_tags.TryGetValue(t, out var value) && value.Title.Resolve(language).Contains(query, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(e => e.Order).ThenBy(e => e.Tutorial.Id, StringComparer.Ordinal).ToArray();
        private void CheckId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.Contains(':') || m_registeringNamespace != null && !id.StartsWith(m_registeringNamespace + ":", StringComparison.Ordinal))
                throw new ArgumentException("Registration must use its plugin namespace.", nameof(id));
        }
    }
}
