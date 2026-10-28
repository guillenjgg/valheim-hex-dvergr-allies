
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

        private sealed class CompanionState
        {
            internal ZDOID ZdoId;
            internal Vector3 SourcePosition;
        }

        private sealed class TeleportState
        {
            internal Vector3 DeparturePosition;
            internal readonly List<CompanionState> Companions = new List<CompanionState>();
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

                var sourcePosition = character.transform.position;

                if (Vector3.Distance(sourcePosition, state.DeparturePosition) > CompanionRange)
                {
                    continue;
                }

                state.Companions.Add(new CompanionState
                {
                    ZdoId = zdo.m_uid,
                    SourcePosition = sourcePosition
                });

                Plugin.Log.LogInfo(
                    $"[Dvergr Portal] Captured: Dvergr={character.name}, ZDO={zdo.m_uid}, Source={sourcePosition}, NetworkOwner={nview.IsOwner()}");
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

            if (Vector3.Distance(state.DeparturePosition, destination) < MinimumTravelDistance)
            {
                Plugin.Log.LogInfo(
                    "[Dvergr Portal] Teleport completed without significant movement. Skipping companions.");
                return;
            }

            foreach (var companion in state.Companions)
            {
                var companionObject = ZNetScene.instance.FindInstance(companion.ZdoId);

                if (!companionObject)
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Portal] Companion unloaded. Requesting server relocation: ZDO={companion.ZdoId}");

                    DvergrCompanionPortalRelocationManager.RequestRelocation(
                        __instance,
                        companion.ZdoId,
                        companion.SourcePosition);

                    continue;
                }

                var nview = companionObject.GetComponent<ZNetView>();
                var character = companionObject.GetComponent<Character>();

                if (!nview || !nview.IsValid() || !nview.IsOwner() ||
                    !character || !DvergrCompanionManager.IsHired(character))
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Portal] Companion not locally owned. Requesting server relocation: ZDO={companion.ZdoId}");

                    DvergrCompanionPortalRelocationManager.RequestRelocation(
                        __instance,
                        companion.ZdoId,
                        companion.SourcePosition);

                    continue;
                }

                var zdo = nview.GetZDO();

                if (zdo.GetLong("HexDvergr_HiredBy", 0L) != __instance.GetPlayerID())
                {
                    continue;
                }

                var arrivalPosition = destination + __instance.transform.right * ArrivalOffset;

                character.transform.position = arrivalPosition;
                zdo.SetPosition(arrivalPosition);

                Plugin.Log.LogInfo(
                    $"[Dvergr Portal] Companion relocated locally: ZDO={companion.ZdoId}, Destination={arrivalPosition}");
            }
        }
    }
}
