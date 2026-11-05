//using HarmonyLib;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Awake))]
//    internal static class PrivateAreaWardDebugPatch
//    {
//        private static void Postfix(PrivateArea __instance, ZNetView ___m_nview)
//        {
//            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
//                return;

//            if (__instance.m_ownerFaction != Character.Faction.Dverger)
//                return;

//            var isValid = ___m_nview.IsValid();
//            var isEnabled = isValid && ___m_nview.GetZDO().GetBool(ZDOVars.s_enabled, false);

//            var piece = __instance.GetComponent<Piece>();
//            var creatorId = piece.GetCreator();
//            var playerId = Game.instance.GetPlayerProfile().GetPlayerID();
//            var isCreator = creatorId == playerId;

//            Plugin.Log.LogInfo(
//                $"[Dvergr Ward] Object={__instance.name}, " +
//                $"Faction={__instance.m_ownerFaction}, " +
//                $"Position={__instance.transform.position}, " +
//                $"Radius={__instance.m_radius:F1}, " +
//                $"IsValid={isValid}, " +
//                $"IsEnabled={isEnabled}, " +
//                $"CreatorId={creatorId}, " +
//                $"PlayerId={playerId}, " +
//                $"IsCreator={isCreator}");
//        }
//    }
//}
