using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Engine;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Portable SC snapshot. Runtime block IDs are resolved by the caller's palette.</summary>
    public sealed class RealmPonderSchematic
    {
        public IReadOnlyDictionary<Point3, int> Blocks { get; }
        public RealmPonderSelection Selection { get; }
        public RealmPonderSchematic(IEnumerable<KeyValuePair<Point3, int>> blocks)
        {
            Dictionary<Point3, int> copy = new(blocks);
            if (copy.Count > 65536 || copy.Keys.Any(p => p.Y < 0 || p.Y > 255))
                throw new ArgumentException("Ponder schematics support 65536 cells and heights 0..255.", nameof(blocks));
            Blocks = new ReadOnlyDictionary<Point3, int>(copy);
            Selection = new(copy.Keys);
        }
        public static RealmPonderSchematic Load(XElement xml, Func<string, int> resolveValue)
        {
            ArgumentNullException.ThrowIfNull(resolveValue);
            if (xml.Name != "PonderSchematic" || (int?)xml.Attribute("Version") != 1)
                throw new FormatException("Unsupported Ponder schematic version.");
            Dictionary<Point3, int> blocks = [];
            foreach (XElement cell in xml.Elements("Fill"))
            {
                Point3 from = Parse((string)cell.Attribute("From"));
                Point3 to = cell.Attribute("To") == null ? from : Parse((string)cell.Attribute("To"));
                int value = resolveValue((string)cell.Attribute("Block") ?? throw new FormatException("Missing Block."));
                foreach (Point3 point in RealmPonderSelection.Box(from, to)) blocks[point] = value;
            }
            return new(blocks);
        }
        private static Point3 Parse(string value)
        {
            string[] parts = (value ?? "").Split(',');
            if (parts.Length != 3) throw new FormatException("Expected x,y,z.");
            return new(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture), int.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }
}
