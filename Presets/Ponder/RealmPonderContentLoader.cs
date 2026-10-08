using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Engine;
using Game;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Optional content packs are discovered from loaded mods; content mods need no RealmEX reference.</summary>
    public static class RealmPonderContentLoader
    {
        public static RealmPonderRegistry Discover(IEnumerable<ModEntity> mods, Action<string> report = null, RealmPonderRegistry registry = null)
        {
            registry ??= RealmPonderSamples.CreateRegistry();
            foreach (ModEntity mod in mods.Where(m => !m.IsDisabled).OrderBy(m => m.modInfo.PackageName, StringComparer.Ordinal))
            {
                List<(string Path, string Xml)> manifests = [];
                mod.GetFiles(".xml", (path, stream) =>
                {
                    if (!path.EndsWith(".ponder.xml", StringComparison.OrdinalIgnoreCase)) return;
                    try { manifests.Add((path, Read(stream))); }
                    catch (Exception ex) { report?.Invoke($"{mod.modInfo.PackageName}/{path}: {ex.Message}"); }
                });
                foreach (var manifest in manifests.OrderBy(m => m.Path, StringComparer.Ordinal))
                {
                    try
                    {
                        string ReadFile(string path)
                        {
                            string text = null;
                            Exception error = null;
                            // ModEntity and FastDebugModEntity return opposite booleans. The callback is authoritative.
                            mod.GetFile(path, stream => { try { text = Read(stream); } catch (Exception ex) { error = ex; } });
                            if (error != null) throw error;
                            return text ?? throw new FileNotFoundException($"Missing tutorial resource '{path}'.");
                        }
                        Load(registry, manifest.Path, manifest.Xml, ReadFile, ResolveCraftingId);
                    }
                    catch (Exception ex) { report?.Invoke($"{mod.modInfo.PackageName}/{manifest.Path}: {ex.Message}"); }
                }
            }
            return registry;
        }
        public static void Load(RealmPonderRegistry registry, string manifestPath, string xml, Func<string, string> readFile, Func<string, int> resolveCraftingId)
            => registry.RegisterPlugin(new Pack(manifestPath, ParseXml(xml), readFile, resolveCraftingId));
        private static string Read(Stream stream)
        {
            using StreamReader reader = new(stream, leaveOpen: true);
            char[] buffer = new char[1024 * 1024 + 1];
            int count = reader.ReadBlock(buffer, 0, buffer.Length);
            if (count == buffer.Length) throw new FormatException("Tutorial resources must be smaller than 1 MiB.");
            return new string(buffer, 0, count);
        }
        internal static XElement ParseXml(string text)
        {
            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 });
            return XElement.Load(reader);
        }
        public static int ResolveCraftingId(string id)
        {
            foreach (Block block in BlocksManager.Blocks.Where(b => b != null))
                foreach (int value in block.GetCreativeValues())
                    if (string.Equals(block.GetCraftingId(value), id, StringComparison.Ordinal)) return Terrain.ReplaceLight(value, 0);
            throw new FormatException($"Unknown crafting ID '{id}'.");
        }
        internal static RealmPonderText Text(XElement node) => new(Required(node, "Zh"), Required(node, "En"));
        internal static string Required(XElement node, string name) => (string)node.Attribute(name) is { Length: > 0 } value ? value : throw new FormatException($"Missing {node.Name}/{name}.");
        private sealed class Pack : IRealmPonderPlugin
        {
            private readonly XElement m_xml;
            private readonly string m_directory;
            private readonly Func<string, string> m_read;
            private readonly Func<string, int> m_resolve;
            public string Namespace { get; }
            public Pack(string path, XElement xml, Func<string, string> read, Func<string, int> resolve)
            {
                if (xml.Name != "PonderPack" || (int?)xml.Attribute("Version") != 1) throw new FormatException("Unsupported PonderPack version.");
                Namespace = Required(xml, "Namespace");
                if (Namespace.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '-' and not '.')) throw new FormatException("Invalid pack namespace.");
                m_directory = path.Contains('/') ? path[..(path.LastIndexOf('/') + 1)] : "";
                m_xml = xml; m_read = read; m_resolve = resolve;
            }
            private string Resource(string path)
            {
                if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.Contains(':') || path.StartsWith('/') || path.Split('/').Any(p => p is ".." or "." or ""))
                    throw new FormatException("Tutorial resources must be relative paths within their pack.");
                return m_directory + path;
            }
            private string Id(string name)
            {
                if (string.IsNullOrWhiteSpace(name) || name.Contains(':')) throw new FormatException("Pack-local IDs must be nonempty and unqualified.");
                return Namespace + ":" + name;
            }
            public void Register(RealmPonderRegistry registry)
            {
                Dictionary<string, int> palette = new(StringComparer.Ordinal);
                foreach (var block in m_xml.Element("Palette")?.Elements("Block") ?? [])
                {
                    int value = block.Attribute("Value") != null ? (int)block.Attribute("Value") : m_resolve(Required(block, "CraftingId"));
                    value = Terrain.ReplaceData(value, checked(Terrain.ExtractData(value) + ((int?)block.Attribute("DataOffset") ?? 0)));
                    palette.Add(Required(block, "Id"), value);
                }
                Dictionary<string, RealmPonderUiDefinition> interfaces = new(StringComparer.Ordinal);
                foreach (var ui in m_xml.Elements("Ui"))
                    interfaces.Add(Required(ui, "Id"), RealmPonderUiDefinition.FromXml(Required(ui, "Asset"), Text(ui),
                        new(float.Parse(Required(ui, "Width"), CultureInfo.InvariantCulture), float.Parse(Required(ui, "Height"), CultureInfo.InvariantCulture))));
                foreach (var tag in m_xml.Elements("Tag")) registry.RegisterTag(new(Id(Required(tag, "Id")), Text(tag), ""));
                var tags = m_xml.Elements("Tag").Select(t => Required(t, "Id")).ToHashSet(StringComparer.Ordinal);
                foreach (var tutorial in m_xml.Elements("Tutorial"))
                {
                    string script = Resource(Required(tutorial, "Script"));
                    // .js is globally auto-executed by SC even without RealmEX. .pjs contains ordinary JavaScript.
                    if (!script.EndsWith(".pjs", StringComparison.Ordinal)) throw new FormatException("Ponder scripts must use .pjs to avoid the host's global JavaScript loader.");
                    var schematic = tutorial.Attribute("Schematic") is { } source
                        ? RealmPonderSchematic.Load(ParseXml(m_read(Resource(source.Value))), id => palette[id]) : new RealmPonderSchematic([]);
                    var builder = new RealmPonderSceneBuilder(Id(Required(tutorial, "Id")), Text(tutorial), schematic);
                    RealmPonderScript.Compile(builder, m_read(script), script, palette, interfaces);
                    RealmPonderTutorial result = builder.Build();
                    new RealmPonderPlayer(result).Seek(result.Duration); // Validate section/UI ordering before publishing the pack.
                    string[] tutorialTags = ((string)tutorial.Attribute("Tags") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (tutorialTags.Any(t => !tags.Contains(t))) throw new FormatException("Tutorial references an unknown tag.");
                    int[] subjects = ((string)tutorial.Attribute("Subjects") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(id => palette[id]).ToArray();
                    registry.Register(result, tutorialTags.Select(Id), subjects, (int?)tutorial.Attribute("Order") ?? 0, (bool?)tutorial.Attribute("ShowInIndex") ?? true);
                }
                if (!m_xml.Elements("Tutorial").Any()) throw new FormatException("A Ponder pack must contain at least one tutorial.");
            }
        }
    }
}
