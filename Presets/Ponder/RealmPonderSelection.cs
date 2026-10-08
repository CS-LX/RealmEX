using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Immutable, inclusive block selection.</summary>
    public sealed class RealmPonderSelection : IReadOnlyCollection<Point3>
    {
        private readonly HashSet<Point3> m_points;
        public RealmPonderSelection(IEnumerable<Point3> points) => m_points = new(points);
        public int Count => m_points.Count;
        public bool Contains(Point3 point) => m_points.Contains(point);
        public Vector3 Center => Count == 0 ? Vector3.Zero : (new Vector3(m_points.Min(p => p.X), m_points.Min(p => p.Y), m_points.Min(p => p.Z))
            + new Vector3(m_points.Max(p => p.X) + 1, m_points.Max(p => p.Y) + 1, m_points.Max(p => p.Z) + 1)) / 2;
        public static RealmPonderSelection At(int x, int y, int z) => new([new Point3(x, y, z)]);
        public static RealmPonderSelection Box(Point3 from, Point3 to)
        {
            int minX = Math.Min(from.X, to.X), maxX = Math.Max(from.X, to.X);
            int minY = Math.Min(from.Y, to.Y), maxY = Math.Max(from.Y, to.Y);
            int minZ = Math.Min(from.Z, to.Z), maxZ = Math.Max(from.Z, to.Z);
            long sizeX = (long)maxX - minX + 1, sizeY = (long)maxY - minY + 1, sizeZ = (long)maxZ - minZ + 1;
            if (sizeX > 65536 || sizeY > 65536 || sizeZ > 65536 || sizeX * sizeY * sizeZ > 65536)
                throw new ArgumentOutOfRangeException(nameof(to), "A Ponder selection is limited to 65536 cells.");
            List<Point3> points = [];
            for (long x = minX; x <= maxX; x++)
                for (long y = minY; y <= maxY; y++)
                    for (long z = minZ; z <= maxZ; z++) points.Add(new((int)x, (int)y, (int)z));
            return new(points);
        }
        public RealmPonderSelection Add(RealmPonderSelection other) => new(m_points.Concat(other));
        public RealmPonderSelection Subtract(RealmPonderSelection other) => new(m_points.Where(p => !other.Contains(p)));
        public RealmPonderSelection Where(Func<Point3, bool> predicate) => new(m_points.Where(predicate));
        public RealmPonderSelection Layers(int first, int last) => Where(p => p.Y >= first && p.Y <= last);
        public IEnumerator<Point3> GetEnumerator() => m_points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
