using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BigBlackGoldDigger
{
    // Unique ID, display name, version
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "BigBlackGoldDigger";
        public const string Name = "Big Black Gold Digger";
        public const string Version = "1.3.0";

        internal static ManualLogSource Log;

        // Settings from BepInEx\config\BigBlackGoldDigger.cfg
        internal static ConfigEntry<bool> CollectTrees;
        internal static ConfigEntry<bool> CollectOres;
        internal static ConfigEntry<bool> CollectBushes;
        internal static ConfigEntry<bool> CollectChests;
        internal static ConfigEntry<bool> CollectFishingSpots;
        internal static ConfigEntry<bool> LogCollections;

        // Runs once when BepInEx loads the plugin
        private void Awake()
        {
            Log = Logger;

            // Bind(section, key, default value, description) creates the setting in the .cfg file
            // the first time, and reads your saved value every time after that.
            CollectTrees        = Config.Bind("Resources", "Trees", true, "Auto-collect golden trees (Woodcutting)");
            CollectOres         = Config.Bind("Resources", "Ores", true, "Auto-collect golden ores (Mining)");
            CollectBushes       = Config.Bind("Resources", "Bushes", true, "Auto-collect golden bushes (Foraging)");
            CollectChests       = Config.Bind("Resources", "Chests", true, "Auto-collect golden chests (Thieving)");
            CollectFishingSpots = Config.Bind("Resources", "FishingSpots", true, "Auto-collect golden fishing spots (Fishing)");
            LogCollections      = Config.Bind("Logging", "LogCollections", true, "Write a console message each time a golden resource is collected");

            new Harmony(Guid).PatchAll();
            Log.LogInfo($"{Name} v{Version} by BigBlackHawk is active.");
        }

        // Called by each ApplyGlow postfix. Waits one frame so the rest of
        // ReduceHealth() finishes first, then collects like a mouse hover would.
        internal static void QueueCollect(MonoBehaviour resource, ConfigEntry<bool> enabled, Action collect)
        {
            if (!enabled.Value)
                return;

            resource.StartCoroutine(CollectNextFrame(resource, collect));
        }

        private static IEnumerator CollectNextFrame(MonoBehaviour resource, Action collect)
        {
            yield return null;

            // Only collect if it is still glowing (it may have despawned or been hovered already)
            if (resource == null || !Traverse.Create(resource).Field<bool>("isGlowing").Value)
                yield break;

            collect();

            if (LogCollections.Value)
                Log.LogInfo($"Collected golden resource: {resource.name} ({resource.GetType().Name})");
        }
    }

    // One postfix per resource type: runs right after the game turns the glow on

    [HarmonyPatch(typeof(Trees), nameof(Trees.ApplyGlow))]
    static class TreesPatch
    {
        static void Postfix(Trees __instance) => Plugin.QueueCollect(__instance, Plugin.CollectTrees, __instance.OnGolden);
    }

    [HarmonyPatch(typeof(Ores), nameof(Ores.ApplyGlow))]
    static class OresPatch
    {
        static void Postfix(Ores __instance) => Plugin.QueueCollect(__instance, Plugin.CollectOres, __instance.OnGolden);
    }

    [HarmonyPatch(typeof(Bushes), nameof(Bushes.ApplyGlow))]
    static class BushesPatch
    {
        static void Postfix(Bushes __instance) => Plugin.QueueCollect(__instance, Plugin.CollectBushes, __instance.OnGolden);
    }

    [HarmonyPatch(typeof(Chests), nameof(Chests.ApplyGlow))]
    static class ChestsPatch
    {
        static void Postfix(Chests __instance) => Plugin.QueueCollect(__instance, Plugin.CollectChests, __instance.OnGolden);
    }

    [HarmonyPatch(typeof(FishingSpots), nameof(FishingSpots.ApplyGlow))]
    static class FishingSpotsPatch
    {
        static void Postfix(FishingSpots __instance) => Plugin.QueueCollect(__instance, Plugin.CollectFishingSpots, __instance.OnGoldenOpp);
    }
}
