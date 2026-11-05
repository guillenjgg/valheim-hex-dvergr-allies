//using HarmonyLib;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList))]
//    internal static class StoreGuiHierarchyDebugPatch
//    {
//        private static bool _hierarchyLogged;

//        private static void Postfix(StoreGui __instance)
//        {
//            if (_hierarchyLogged)
//            {
//                return;
//            }

//            _hierarchyLogged = true;

//            Plugin.Log.LogInfo("[StoreGui Debug] ===== HIERARCHY START =====");

//            LogHierarchy(__instance.transform, 0);

//            Plugin.Log.LogInfo("[StoreGui Debug] ===== HIERARCHY END =====");
//        }

//        private static void LogHierarchy(Transform transform, int depth)
//        {
//            var gameObject = transform.gameObject;
//            var indentation = new string(' ', depth * 2);
//            var components = gameObject.GetComponents<Component>();

//            Plugin.Log.LogInfo(
//                $"[StoreGui Debug] {indentation}{gameObject.name} " +
//                $"[Active={gameObject.activeSelf}, ActiveInHierarchy={gameObject.activeInHierarchy}]");

//            foreach (var component in components)
//            {
//                if (component)
//                {
//                    Plugin.Log.LogInfo(
//                        $"[StoreGui Debug] {indentation}  - {component.GetType().Name}");
//                }
//            }

//            for (var i = 0; i < transform.childCount; i++)
//            {
//                LogHierarchy(transform.GetChild(i), depth + 1);
//            }
//        }
//    }
//}
