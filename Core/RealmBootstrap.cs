using System;
using Game;
using GameEntitySystem;
using TemplatesDatabase;

namespace RealmEX.Core
{
    /// <summary>
    /// 从 RealmEX 专用模板创建内存沙箱，不接管 GameManager 的主世界引用。
    /// </summary>
    public static class RealmBootstrap
    {
        public const string SandboxProjectTemplateName = "RealmEXSandboxProject";

        public const string PonderProjectTemplateName = "RealmEXPonderProject";

        public static SandboxProject Create(string realmId, ValuesDictionary overrides = null)
        {
            return Create(realmId, SandboxProjectTemplateName, overrides);
        }

        public static SandboxProject Create(
            string realmId,
            string projectTemplateName,
            ValuesDictionary overrides = null)
        {
            Project mainProject = GameManager.Project;
            SandboxProject sandbox = Create(
                DatabaseManager.GameDatabase,
                realmId,
                projectTemplateName,
                overrides);
            if (!ReferenceEquals(GameManager.Project, mainProject))
            {
                sandbox.Dispose();
                throw new InvalidOperationException("Realm bootstrap changed GameManager.Project.");
            }
            return sandbox;
        }

        public static SandboxProject Create(
            GameDatabase gameDatabase,
            string realmId,
            ValuesDictionary overrides = null)
        {
            return Create(gameDatabase, realmId, SandboxProjectTemplateName, overrides);
        }

        public static SandboxProject Create(
            GameDatabase gameDatabase,
            string realmId,
            string projectTemplateName,
            ValuesDictionary overrides = null)
        {
            ArgumentNullException.ThrowIfNull(gameDatabase);
            if (string.IsNullOrWhiteSpace(realmId))
            {
                throw new ArgumentException("Realm id cannot be empty.", nameof(realmId));
            }
            if (string.IsNullOrWhiteSpace(projectTemplateName))
            {
                throw new ArgumentException("Project template name cannot be empty.", nameof(projectTemplateName));
            }

            DatabaseObject projectTemplate = gameDatabase.Database.FindDatabaseObject(
                projectTemplateName.Trim(),
                gameDatabase.ProjectTemplateType,
                true);
            ProjectData projectData = new(gameDatabase, projectTemplate, overrides);
            return new SandboxProject(gameDatabase, projectData, realmId.Trim());
        }
    }
}
