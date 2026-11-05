//using HarmonyLib;
//using System.Collections.Generic;
//using System.Reflection;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.CheckAccess))]
//    internal static class PrivateAreaCheckAccessDebugPatch
//    {
//        private const float DebugRadius = 25f;
//        private static bool _patchesLogged;

//        private static readonly FieldInfo AllAreasField = AccessTools.Field(typeof(PrivateArea), "m_allAreas");

//        private static void Postfix(Vector3 point, float radius, bool flash, bool wardCheck, bool __result)
//        {
//            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
//                return;

//            if (!_patchesLogged)
//            {
//                LogHarmonyPatches();
//                _patchesLogged = true;
//            }

//            if (!flash)
//                return;

//            var player = Player.m_localPlayer;

//            if (Utils.DistanceXZ(player.transform.position, point) > DebugRadius)
//                return;

//            if (!PrivateArea.InsideFactionArea(point, Character.Faction.Dverger))
//                return;

//            var playerId = player.GetPlayerID();
//            var allAreas = (List<PrivateArea>)AllAreasField.GetValue(null);

//            Plugin.Log.LogInfo(
//                $"[Ward Access] Position={point}, " +
//                $"Radius={radius:F1}, " +
//                $"WardCheck={wardCheck}, " +
//                $"Allowed={__result}");

//            foreach (var ward in allAreas)
//            {
//                var nview = ward.GetComponent<ZNetView>();

//                if (!nview.IsValid())
//                    continue;

//                var zdo = nview.GetZDO();

//                if (!zdo.GetBool(ZDOVars.s_enabled, false))
//                    continue;

//                var distance = Utils.DistanceXZ(ward.transform.position, point);

//                if (distance >= ward.m_radius + radius)
//                    continue;

//                var piece = ward.GetComponent<Piece>();
//                var creatorId = piece.GetCreator();
//                var isCreator = piece.IsCreator();

//                var permittedCount = zdo.GetInt(ZDOVars.s_permitted, 0);
//                var isPermitted = false;

//                for (var i = 0; i < permittedCount; i++)
//                {
//                    var permittedId = zdo.GetLong("pu_id" + i, 0L);

//                    if (permittedId == playerId)
//                        isPermitted = true;
//                }

//                Plugin.Log.LogInfo(
//                    $"[Ward Detail] Object={ward.name}, " +
//                    $"Faction={ward.m_ownerFaction}, " +
//                    $"Distance={distance:F1}, " +
//                    $"Radius={ward.m_radius:F1}, " +
//                    $"CreatorId={creatorId}, " +
//                    $"IsCreator={isCreator}, " +
//                    $"PermittedCount={permittedCount}, " +
//                    $"IsPermitted={isPermitted}, " +
//                    $"HaveAccess={isCreator || isPermitted}");
//            }
//        }

//        private static void LogHarmonyPatches()
//        {
//            var method = AccessTools.Method(typeof(PrivateArea), nameof(PrivateArea.CheckAccess));
//            var patchInfo = Harmony.GetPatchInfo(method);

//            if (patchInfo == null)
//            {
//                Plugin.Log.LogInfo("[Ward Harmony] No patches registered.");
//                return;
//            }

//            foreach (var patch in patchInfo.Prefixes)
//                LogPatch("Prefix", patch);

//            foreach (var patch in patchInfo.Postfixes)
//                LogPatch("Postfix", patch);

//            foreach (var patch in patchInfo.Transpilers)
//                LogPatch("Transpiler", patch);

//            foreach (var patch in patchInfo.Finalizers)
//                LogPatch("Finalizer", patch);
//        }

//        private static void LogPatch(string type, Patch patch)
//        {
//            Plugin.Log.LogInfo(
//                $"[Ward Harmony] Type={type}, " +
//                $"Owner={patch.owner}, " +
//                $"Method={patch.PatchMethod.DeclaringType?.FullName}.{patch.PatchMethod.Name}, " +
//                $"Priority={patch.priority}");
//        }
//    }
//}