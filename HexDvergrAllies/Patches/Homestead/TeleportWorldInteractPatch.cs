using HarmonyLib;
using HexDvergrAllies.Core;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Interact))]
    internal static class TeleportWorldInteractPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(TeleportWorld __instance, Humanoid human, bool hold, ref bool __result)
        {
            if (hold || !HexDvergrAlliesConfiguration.AllowTemporaryHomestead.Value)
            {
                return true;
            }

            var position = __instance.transform.position;

            if (PrivateArea.CheckAccess(position, 0f, false, false))
            {
                return true;
            }

            if (!PrivateArea.InsideFactionArea(position, Character.Faction.Dverger))
            {
                return true;
            }

            var prefabName = Utils.GetPrefabName(__instance.gameObject);

            if (!PrefabHelper.AllowedHomesteadPieces.Contains(prefabName))
            {
                return true;
            }

            if (TextInput.instance == null)
            {
                return true;
            }

            TextInput.instance.RequestText(__instance, "$piece_portal_tag", 10);
            
            __result = true;

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Homestead] Portal interaction allowed: Portal={prefabName}, Position={position}");
            }

            return false;
        }
    }
}