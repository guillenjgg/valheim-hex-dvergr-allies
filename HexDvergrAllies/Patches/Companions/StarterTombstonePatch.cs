using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using HexDvergrAllies.Core.Features.Progression;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class StarterRevivalLootPatch
    {
        private static bool Prefix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            if (!DvergrStarterRevivalManager.SuppressDeathLoot(__instance))
            {
                return true;
            }

            __result = new List<KeyValuePair<GameObject, int>>();
            return false;
        }
    }

    [HarmonyPatch(typeof(TombStone), "UpdateDespawn")]
    internal static class StarterTombstoneUpdatePatch
    {
        private static bool Prefix(TombStone __instance) => DvergrStarterRevivalManager.UpdateTombstone(__instance);
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.GetHoverText))]
    internal static class StarterTombstoneHoverPatch
    {
        private static bool Prefix(TombStone __instance, ref string __result)
        {
            if (!DvergrStarterRevivalManager.IsTombstone(__instance))
            {
                return true;
            }

            __result = DvergrStarterRevivalManager.HoverText(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    internal static class StarterTombstoneInteractPatch
    {
        private static bool Prefix(TombStone __instance, Humanoid __0, bool __1, ref bool __result)
        {
            if (!DvergrStarterRevivalManager.IsTombstone(__instance))
            {
                return true;
            }

            DvergrStarterRevivalManager.Interact(__instance, __0, __1);
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.TakeAll))]
    internal static class StarterTombstoneTakeAllPatch
    {
        private static bool Prefix(Container __instance, ref bool __result)
        {
            return StarterTombstoneContainerPatch.Prefix(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class StarterTombstoneContainerPatch
    {
        internal static bool Prefix(Container __instance, ref bool __result)
        {
            if (!DvergrStarterRevivalManager.IsTombstone(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
