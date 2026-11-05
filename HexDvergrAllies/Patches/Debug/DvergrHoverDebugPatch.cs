
//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using HarmonyLib;
//using HexDvergrAllies.Core.Managers;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
//    internal static class DvergrHoverDebugPatch
//    {
//        private static bool _hasLogged;

//        [HarmonyPostfix]
//        private static void Postfix(Player player)
//        {
//            if (_hasLogged || !player)
//            {
//                return;
//            }

//            GameObject hoverObject = player.GetHoverObject();

//            if (!hoverObject)
//            {
//                return;
//            }

//            Trader trader = hoverObject.GetComponentInParent<Trader>();

//            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
//            {
//                return;
//            }

//            _hasLogged = true;

//            Plugin.Log.LogInfo(
//                "[Dvergr Hover Debug] Starting Harmony patch inventory.");

//            LogPatches(
//                AccessTools.Method(typeof(Character), "GetHoverText"));

//            LogPatches(
//                AccessTools.Method(typeof(Hud), "UpdateCrosshair"));

//            LogPatches(
//                AccessTools.Method(typeof(Trader), "GetHoverText"));

//            Plugin.Log.LogInfo(
//                "[Dvergr Hover Debug] Harmony patch inventory complete.");
//        }

//        private static void LogPatches(MethodBase method)
//        {
//            if (method == null)
//            {
//                Plugin.Log.LogWarning(
//                    "[Dvergr Hover Debug] Target method not found.");

//                return;
//            }

//            Plugin.Log.LogInfo(
//                "[Dvergr Hover Debug] Target=" +
//                method.DeclaringType.FullName + "." + method.Name);

//            HarmonyLib.Patches patches = Harmony.GetPatchInfo(method);

//            if (patches == null)
//            {
//                Plugin.Log.LogInfo(
//                    "[Dvergr Hover Debug] No Harmony patches registered.");

//                return;
//            }

//            LogPatchCollection("Prefix", patches.Prefixes);
//            LogPatchCollection("Postfix", patches.Postfixes);
//            LogPatchCollection("Transpiler", patches.Transpilers);
//            LogPatchCollection("Finalizer", patches.Finalizers);
//        }

//        private static void LogPatchCollection(
//            string patchType,
//            IEnumerable<Patch> patches)
//        {
//            foreach (Patch patch in patches)
//            {
//                string methodName = "<unknown>";

//                if (patch.PatchMethod != null)
//                {
//                    methodName =
//                        patch.PatchMethod.DeclaringType.FullName +
//                        "." +
//                        patch.PatchMethod.Name;
//                }

//                string before = patch.before != null
//                    ? string.Join(", ", patch.before)
//                    : string.Empty;

//                string after = patch.after != null
//                    ? string.Join(", ", patch.after)
//                    : string.Empty;

//                Plugin.Log.LogInfo(
//                    "[Dvergr Hover Debug] " +
//                    "Type=" + patchType +
//                    ", Owner=" + patch.owner +
//                    ", Priority=" + patch.priority +
//                    ", Index=" + patch.index +
//                    ", Method=" + methodName +
//                    ", Before=[" + before + "]" +
//                    ", After=[" + after + "]");
//            }
//        }
//    }
//}
