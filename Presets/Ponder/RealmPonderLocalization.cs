using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Game;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Immutable, pack-local translations. Missing locales/keys fall back to the required English catalog.</summary>
    public sealed class RealmPonderLocalization
    {
        private readonly Dictionary<string, Dictionary<string, string>> m_languages = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, RealmPonderText> m_staticText = new(StringComparer.Ordinal);
        private static RealmPonderLocalization m_builtin;
        public static RealmPonderLocalization BuiltIn => m_builtin ??= new(ContentManager.List("RealmEX/Lang")
            .Where(c => c.ContentSuffix == ".json").ToDictionary(c => Path.GetFileNameWithoutExtension(c.Filename), c =>
            { using var stream = c.Duplicate(); using StreamReader reader = new(stream); return reader.ReadToEnd(); }, StringComparer.OrdinalIgnoreCase));
        public RealmPonderLocalization(IReadOnlyDictionary<string, string> jsonFiles)
        {
            foreach (var file in jsonFiles)
            {
                string language = CultureInfo.GetCultureInfo(file.Key).Name;
                if (language.Length == 0) throw new FormatException("A locale name is required.");
                using JsonDocument json = JsonDocument.Parse(file.Value);
                Dictionary<string, string> values = new(StringComparer.Ordinal);
                foreach (var property in json.RootElement.EnumerateObject())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) || property.Value.ValueKind != JsonValueKind.String
                        || !values.TryAdd(property.Name, property.Value.GetString())) throw new FormatException($"Invalid or duplicate translation key '{property.Name}'.");
                }
                m_languages.Add(language, values);
            }
            if (!m_languages.ContainsKey("en-US")) throw new FormatException("A Ponder catalog requires en-US translations.");
        }
        public RealmPonderText Text(string key, params object[] arguments)
        {
            ArgumentNullException.ThrowIfNull(arguments);
            if (key != null && arguments.Length == 0 && m_staticText.TryGetValue(key, out var cached)) return cached;
            if (key == null || !m_languages["en-US"].ContainsKey(key)) throw new FormatException($"Missing Ponder translation '{key}'.");
            object[] snapshot = (object[])arguments.Clone();
            foreach (var language in m_languages.Values)
                if (language.TryGetValue(key, out string format)) _ = string.Format(CultureInfo.InvariantCulture, format, snapshot);
            var text = new RealmPonderText(this, key, snapshot);
            return snapshot.Length == 0 ? m_staticText.GetOrAdd(key, text) : text;
        }
        internal string Resolve(string key, string language, object[] arguments)
        {
            string format = null;
            try
            {
                var culture = CultureInfo.GetCultureInfo(language ?? "en-US");
                while (culture.Name.Length > 0)
                {
                    if (m_languages.TryGetValue(culture.Name, out var values) && values.TryGetValue(key, out format)) break;
                    culture = culture.Parent;
                }
            }
            catch (CultureNotFoundException) { }
            return string.Format(CultureInfo.InvariantCulture, format ?? m_languages["en-US"][key], arguments);
        }
    }
    public sealed class RealmPonderText
    {
        private readonly RealmPonderLocalization m_catalog;
        private readonly object[] m_arguments;
        private readonly string m_literal;
        public string Key { get; }
        internal RealmPonderText(RealmPonderLocalization catalog, string key, object[] arguments) { m_catalog = catalog; Key = key; m_arguments = arguments; }
        private RealmPonderText(string literal) { m_literal = literal; }
        public string Resolve(string language) => m_catalog == null ? m_literal : m_catalog.Resolve(Key, language, m_arguments);
        public static implicit operator RealmPonderText(string text) => new(text);
    }
}
