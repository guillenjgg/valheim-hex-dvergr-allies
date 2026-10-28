
using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch]
    internal static class DvergrCompanionPortalTeleportPatch
    {
        private const float CompanionRange = 30f;
        private const float ArrivalOffset = 2f;
        private const float MinimumTravelDistance = 30f;

        private sealed class TeleportState
        {
            internal Vector3 DeparturePosition;
            internal readonly List<ZDOID> Companions = new List<ZDOID>();
        }

        private static readonly Dictionary<Player, TeleportState> States =
            new Dictionary<Player, TeleportState>();

        [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
        [HarmonyPostfix]
        private static void TeleportToPostfix(Player __instance, bool __result)
        {
            if (!__result || __instance != Player.m_localPlayer)
            {
                return;
            }

            var state = new TeleportState
            {
                DeparturePosition = __instance.transform.position
            };

            foreach (var character in Character.GetAllCharacters())
            {
                if (!character || !DvergrCompanionManager.IsHired(character))
                {
                    continue;
                }

                var nview = character.GetComponent<ZNetView>();

                if (!nview || !nview.IsValid())
                {
                    continue;
                }

                var zdo = nview.GetZDO();

                if (zdo.GetLong("HexDvergr_HiredBy", 0L) != __instance.GetPlayerID())
                {
                    continue;
                }

                if (Vector3.Distance(
                    character.transform.position,
                    state.DeparturePosition) > CompanionRange)
                {
                    continue;
                }

                state.Companions.Add(zdo.m_uid);

                Plugin.Log.LogInfo(
                    $"[Dvergr Portal] Captured: Dvergr={character.name}, ZDO={zdo.m_uid}, NetworkOwner={nview.IsOwner()}");
            }

            States[__instance] = state;

            Plugin.Log.LogInfo(
                $"[Dvergr Portal] Teleport started: Companions={state.Companions.Count}, From={state.DeparturePosition}");
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPrefix]
        private static void UpdateTeleportPrefix(Player __instance, out bool __state)
        {
            __state = __instance.IsTeleporting();
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPostfix]
        private static void UpdateTeleportPostfix(Player __instance, bool __state)
        {
            if (!__state || __instance.IsTeleporting())
            {
                return;
            }

            if (!States.TryGetValue(__instance, out var state))
            {
                return;
            }

            States.Remove(__instance);

            var destination = __instance.transform.position;

            if (Vector3.Distance(state.DeparturePosition, destination) <
                MinimumTravelDistance)
            {
                Plugin.Log.LogInfo(
                    "[Dvergr Portal] Teleport completed without significant movement. Skipping companions.");
                return;
            }

            foreach (var companionId in state.Companions)
            {
                var companionObject = ZNetScene.instance.FindInstance(companionId);

                if (!companionObject)
                {
                    Plugin.Log.LogWarning(
                        $"[Dvergr Portal] Companion unloaded: ZDO={companionId}. Server relocation required.");
                    continue;
                }

                var nview = companionObject.GetComponent<ZNetView>();
                var character = companionObject.GetComponent<Character>();

                if (!nview || !nview.IsValid() || !nview.IsOwner() ||
                    !character || !DvergrCompanionManager.IsHired(character))
                {
                    Plugin.Log.LogWarning(
                        $"[Dvergr Portal] Companion not locally owned: ZDO={companionId}. Server relocation required.");
                    continue;
                }

                var zdo = nview.GetZDO();

                if (zdo.GetLong("HexDvergr_HiredBy", 0L) !=
                    __instance.GetPlayerID())
                {
                    continue;
                }

                var arrivalPosition = destination +
                    __instance.transform.right * ArrivalOffset;

                character.transform.position = arrivalPosition;
                zdo.SetPosition(arrivalPosition);

                Plugin.Log.LogInfo(
                    $"[Dvergr Portal] Companion relocated: ZDO={companionId}, Destination={arrivalPosition}");
            }
        }
    }
}
