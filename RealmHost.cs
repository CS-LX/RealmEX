using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using TemplatesDatabase;

namespace RealmEX
{
    /// <summary>
    /// 全局 Realm 调度入口。P2 将在此接入 SandboxRealm 生命周期与并行 tick。
    /// </summary>
    public static class RealmHost
    {
        private static readonly Dictionary<string, SandboxRealm> m_realms =
            new(StringComparer.OrdinalIgnoreCase);
        private static bool m_isTicking;

        public static bool IsInitialized { get; private set; }

        public static IReadOnlyCollection<SandboxRealm> ActiveRealms => m_realms.Values.ToArray();

        public static IReadOnlyCollection<SandboxProject> ActiveSandboxes =>
            m_realms.Values.Select(realm => realm.Project).ToArray();

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
            return CreateRealm(realmId, null, overrides).Project;
        }

        public static SandboxRealm CreateRealm(
            string realmId,
            RealmProfile profile = null,
            ValuesDictionary overrides = null)
        {
            Initialize();
            string normalizedId = NormalizeRealmId(realmId);
            if (m_realms.ContainsKey(normalizedId))
            {
                throw new InvalidOperationException($"Realm \"{normalizedId}\" already exists.");
            }

            SandboxProject sandbox = RealmBootstrap.Create(normalizedId, overrides);
            RealmProfile realmProfile = profile ?? new RealmProfile();
            realmProfile.RealmId = normalizedId;
            SandboxRealm realm = new(sandbox, realmProfile);
            m_realms.Add(normalizedId, realm);
            return realm;
        }

        public static bool DestroySandbox(string realmId)
        {
            return DestroyRealm(realmId);
        }

        public static bool DestroyRealm(string realmId)
        {
            string normalizedId = NormalizeRealmId(realmId);
            if (!m_realms.Remove(normalizedId, out SandboxRealm realm))
            {
                return false;
            }

            realm.Dispose();
            return true;
        }

        public static int TickParallel(float mainWorldDt)
        {
            _ = mainWorldDt;
            if (m_isTicking || m_realms.Count == 0)
            {
                return 0;
            }

            int ticked = 0;
            m_isTicking = true;
            try
            {
                foreach (SandboxRealm realm in m_realms.Values.ToArray())
                {
                    if (realm.IsDisposed || !realm.Profile.ParallelTick)
                    {
                        continue;
                    }

                    try
                    {
                        realm.Tick();
                        ticked++;
                    }
                    catch (Exception ex)
                    {
                        Engine.Log.Error($"[RealmEX] Tick failed for Realm \"{realm.RealmId}\": {ex}");
                    }
                }
            }
            finally
            {
                m_isTicking = false;
            }
            return ticked;
        }

        public static void DisposeAll()
        {
            SandboxRealm[] realms = m_realms.Values.ToArray();
            m_realms.Clear();
            foreach (SandboxRealm realm in realms)
            {
                try
                {
                    realm.Dispose();
                }
                catch (Exception ex)
                {
                    Engine.Log.Error($"[RealmEX] Failed to dispose Realm \"{realm.RealmId}\": {ex}");
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
