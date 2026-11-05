//using HarmonyLib;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
//    internal static class DestructibleRPCDamagePatch
//    {
//        private static bool Prefix(Destructible __instance, HitData hit, ZNetView ___m_nview)
//        {
//            var attacker = hit.GetAttacker();

//            if (attacker == null || !attacker.IsPlayer())
//                return true;

//            var insideDvergrArea = PrivateArea.InsideFactionArea(__instance.transform.position, Character.Faction.Dverger);
//            var protectedObject = HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value && insideDvergrArea;

//            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
//            {
//                Plugin.Log.LogInfo(
//                    $"[Destructible Damage] Object={__instance.name}, " +
//                    $"Attacker={attacker.name}, " +
//                    $"Damage={hit.GetTotalDamage():F1}, " +
//                    $"IsOwner={___m_nview.IsOwner()}, " +
//                    $"InsideDvergrArea={insideDvergrArea}, " +
//                    $"Blocked={protectedObject}");
//            }

//            return !protectedObject;
//        }
//    }
//}
