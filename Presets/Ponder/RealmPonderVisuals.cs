using Engine;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// Ponder 场景样式：只改背景色，不在步骤切换时重置摄像机。
    /// </summary>
    public static class RealmPonderVisuals
    {
        public static void ApplySceneStyle(SandboxRealm realm, string sceneName)
        {
            realm.Viewport.ClearColor = sceneName switch
            {
                "pumpkin-soil" => new Color(110, 160, 210),
                "pumpkin-seedling" => new Color(100, 155, 205),
                "pumpkin-growing" => new Color(120, 170, 215),
                "pumpkin-mature" => new Color(95, 150, 200),
                "pumpkin-lantern" => new Color(85, 140, 195),
                _ => new Color(110, 160, 210)
            };
        }
    }
}
