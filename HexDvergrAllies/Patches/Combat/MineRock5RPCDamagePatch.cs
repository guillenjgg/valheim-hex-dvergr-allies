
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(MineRock5), "RPC_Damage")]
    internal static class MineRock5RPCDamagePatch
    {
        private static ref List<PrivateArea> AllAreas =>
            ref AccessTools.StaticFieldRefAccess<List<PrivateArea>>(typeof(PrivateArea), "m_allAreas");

        private static readonly Func<PrivateArea, bool> IsAreaEnabled = AccessTools.MethodDelegate<Func<PrivateArea, bool>>(AccessTools.Method(typeof(PrivateArea), "IsEnabled"));

        private static readonly Func<PrivateArea, Vector3, float, bool> IsInsideArea = AccessTools.MethodDelegate<Func<PrivateArea, Vector3, float, bool>>(AccessTools.Method(typeof(PrivateArea), "IsInside"));

        private static void Prefix(MineRock5 __instance, HitData hit, out bool __state)
        {
            __state = false;

            if (!__instance.m_triggerPrivateArea || __instance.m_name != "$piece_giant_bone")
            {
                return;
            }

            if (!(hit.GetAttacker() is Player))
            {
                return;
            }

            foreach (var area in AllAreas)
            {
                if (!IsAreaEnabled(area) || !IsInsideArea(area, __instance.transform.position, 0f))
                {
                    continue;
                }

                if (area.m_ownerFaction == Character.Faction.Dverger)
                {
                    __instance.m_triggerPrivateArea = false;
                    __state = true;
                }

                break;
            }
        }

        private static Exception Finalizer(MineRock5 __instance, bool __state, Exception __exception)
        {
            if (__state)
            {
                __instance.m_triggerPrivateArea = true;
            }

            return __exception;
        }
    }
}
