using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace RealmEX
{
    /// <summary>
    /// 与主世界隔离的 Realm 工程。P1 仅支持内存态，不参与宿主存档流程。
    /// </summary>
    public sealed class SandboxProject : Project
    {
        internal SandboxProject(GameDatabase gameDatabase, ProjectData projectData, string realmId)
        {
            RealmId = realmId;
            LoadSandbox(gameDatabase, projectData);
        }

        public string RealmId { get; }

        public bool IsDisposed { get; private set; }

        public override ProjectData Save()
        {
            throw new InvalidOperationException(
                $"Realm \"{RealmId}\" is memory-only. Persistent Realm saves are not available before P4.");
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Exception firstError = null;
            foreach (Entity entity in m_entities.Keys.ToArray())
            {
                try
                {
                    entity.Dispose();
                    GC.SuppressFinalize(entity);
                }
                catch (Exception ex)
                {
                    firstError ??= ex;
                }
            }
            foreach (Subsystem subsystem in m_subsystems)
            {
                try
                {
                    subsystem.Dispose();
                    GC.SuppressFinalize(subsystem);
                }
                catch (Exception ex)
                {
                    firstError ??= ex;
                }
            }

            m_entities.Clear();
            m_subsystems.Clear();
            IsDisposed = true;
            GC.SuppressFinalize(this);
            if (firstError != null)
            {
                throw firstError;
            }
        }

        private void LoadSandbox(GameDatabase gameDatabase, ProjectData projectData)
        {
            ArgumentNullException.ThrowIfNull(gameDatabase);
            ArgumentNullException.ThrowIfNull(projectData);
            if (projectData.ValuesDictionary?.DatabaseObject == null)
            {
                throw new ArgumentException("Project data must reference a project template.", nameof(projectData));
            }

            try
            {
                m_gameDatabase = gameDatabase;
                m_projectData = projectData;
                m_projectTemplate = projectData.ValuesDictionary.DatabaseObject;

                Dictionary<string, Subsystem> subsystemsByName = [];
                foreach (ValuesDictionary values in projectData.ValuesDictionary.Values.OfType<ValuesDictionary>())
                {
                    if (values.DatabaseObject?.Type != gameDatabase.MemberSubsystemTemplateType)
                    {
                        continue;
                    }

                    bool isOptional = values.GetValue("IsOptional", false);
                    string className = values.GetValue<string>("Class");
                    Type type = TypeCache.FindType(className, false, !isOptional);
                    if (type == null)
                    {
                        continue;
                    }

                    object instance;
                    try
                    {
#pragma warning disable IL2072
                        instance = Activator.CreateInstance(type);
#pragma warning restore IL2072
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    if (instance is not Subsystem subsystem)
                    {
                        throw new InvalidOperationException(
                            $"Type \"{className}\" cannot be used as a subsystem because it does not inherit from Subsystem.");
                    }

                    subsystem.Initialize(this, values);
                    subsystemsByName.Add(values.DatabaseObject.Name, subsystem);
                    m_subsystems.Add(subsystem);
                }

                List<Entity> entities = projectData.EntityDataList != null
                    ? InitializeEntities(projectData.EntityDataList)
                    : [];
                if (projectData.EntityDataList != null)
                {
                    NextEntityID = projectData.NextEntityID;
                    AddEntities(entities);
                }

                Dictionary<Subsystem, bool> loadedSubsystems = [];
                foreach (Subsystem subsystem in subsystemsByName.Values)
                {
                    LoadSubsystem(subsystem, subsystemsByName, loadedSubsystems, 0);
                }
                LoadEntities(projectData.EntityDataList, entities);
            }
            catch
            {
                try
                {
                    Dispose();
                }
                catch
                {
                    // Preserve the load failure; partially initialized members are already cleared.
                }
                throw;
            }
        }
    }
}
