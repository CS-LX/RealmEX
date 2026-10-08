using System;
using System.Collections.Generic;
using Engine;
using Game;
using GameEntitySystem;
using RealmEX.Core;
using TemplatesDatabase;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Entity links belong to one sandbox generation and cannot leak across replay or players.</summary>
    public sealed class RealmPonderWorld
    {
        private readonly Dictionary<string, Entity> m_entities = new(StringComparer.Ordinal);
        public SandboxRealm Realm { get; }
        public SandboxProject Project => Realm.Project;
        internal RealmPonderWorld(SandboxRealm realm) => Realm = realm;
        public Entity CreateEntity(string id, string template, ValuesDictionary overrides = null)
        {
            if (m_entities.ContainsKey(id)) throw new InvalidOperationException($"Entity link '{id}' already exists.");
            // Host CreateEntity(template, overrides) mutates its shared cached dictionary. Build a private one instead.
            ValuesDictionary values = new();
            values.PopulateFromDatabaseObject(Project.GameDatabase.Database.FindDatabaseObject(template, Project.GameDatabase.EntityTemplateType, true));
            if (overrides != null) values.ApplyOverrides(overrides);
            Entity entity = Project.CreateEntity(values);
            try { Project.AddEntity(entity); m_entities.Add(id, entity); }
            catch { entity.Dispose(); throw; }
            return entity;
        }
        public Entity Entity(string id) => m_entities[id];
        public void RemoveEntity(string id)
        {
            if (m_entities.Remove(id, out Entity entity)) Project.RemoveEntity(entity, true);
        }
        public void ModifyBlockEntity<T>(Point3 position, Action<T> modify) where T : Component
        {
            var block = Project.FindSubsystem<SubsystemBlockEntities>(true).GetBlockEntity(position.X, position.Y, position.Z)
                ?? throw new InvalidOperationException($"No block entity at {position}.");
            modify(block.Entity.FindComponent<T>(true));
        }
    }
}
