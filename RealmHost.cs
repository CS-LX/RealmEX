using System;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace RealmEX
{
    /// <summary>
    /// 全局 Realm 调度入口。P2 将在此接入 SandboxRealm 生命周期与并行 tick。
    /// </summary>
    public static class RealmHost
    {
        private static readonly Dictionary<string, SandboxProject> m_sandboxes =
            new(StringComparer.OrdinalIgnoreCase);

        public static bool IsInitialized { get; private set; }

        public static IReadOnlyCollection<SandboxProject> ActiveSandboxes => m_sandboxes.Values.ToArray();

        public static void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;
            Engine.Log.Information("[RealmEX] RealmHost initialized; P1 sandbox bootstrap is available.");
        }

        public static SandboxProject CreateSandbox(string realmId, ValuesDictionary overrides = null)
        {
            Initialize();
            string normalizedId = NormalizeRealmId(realmId);
            if (m_sandboxes.ContainsKey(normalizedId))
            {
                throw new InvalidOperationException($"Realm \"{normalizedId}\" already exists.");
            }

            SandboxProject sandbox = RealmBootstrap.Create(normalizedId, overrides);
            m_sandboxes.Add(normalizedId, sandbox);
            return sandbox;
        }

        public static bool DestroySandbox(string realmId)
        {
            string normalizedId = NormalizeRealmId(realmId);
            if (!m_sandboxes.Remove(normalizedId, out SandboxProject sandbox))
            {
                return false;
            }

            sandbox.Dispose();
            return true;
        }

        public static void DisposeAll()
        {
            SandboxProject[] sandboxes = m_sandboxes.Values.ToArray();
            m_sandboxes.Clear();
            foreach (SandboxProject sandbox in sandboxes)
            {
                try
                {
                    sandbox.Dispose();
                }
                catch (Exception ex)
                {
                    Engine.Log.Error($"[RealmEX] Failed to dispose Realm \"{sandbox.RealmId}\": {ex}");
                }
            }
        }

        private static string NormalizeRealmId(string realmId)
        {
            if (string.IsNullOrWhiteSpace(realmId))
            {
                throw new ArgumentException("Realm id cannot be empty.", nameof(realmId));
            }
            return realmId.Trim();
        }
    }
}
