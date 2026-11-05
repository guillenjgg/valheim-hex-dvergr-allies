
//using HarmonyLib;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(InventoryGui), "Update")]
//    internal static class InventoryGuiHierarchyDebugPatch
//    {
//        private static bool _logged;
//        private static bool _updateConfirmed;

//        private static void Postfix(InventoryGui __instance)
//        {
//            if (!_updateConfirmed)
//            {
//                _updateConfirmed = true;
//                Plugin.Log.LogInfo("[Companion UI Debug] InventoryGui.Update patch is running.");
//            }

//            if (_logged || !InventoryGui.IsVisible())
//            {
//                return;
//            }

//            _logged = true;

//            Plugin.Log.LogInfo("[Companion UI Debug] Inventory is visible.");
//            Plugin.Log.LogInfo("[Companion UI Debug] ===== Inventory Hierarchy START =====");

//            LogHierarchy(__instance.transform, 0);

//            Plugin.Log.LogInfo("[Companion UI Debug] ===== Inventory Hierarchy END =====");
//        }

//        private static void LogHierarchy(Transform current, int depth)
//        {
//            var rect = current.GetComponent<RectTransform>();
//            var components = current.GetComponents<Component>();
//            var componentNames = "";

//            foreach (var component in components)
//            {
//                if (component)
//                {
//                    componentNames += component.GetType().Name + ", ";
//                }
//            }

//            var indentation = new string(' ', depth * 2);

//            Plugin.Log.LogInfo($"[Companion UI Debug] {indentation}{current.name} | Active={current.gameObject.activeSelf} | Components={componentNames}");

//            if (rect)
//            {
//                Plugin.Log.LogInfo($"[Companion UI Debug] {indentation}  Size={rect.rect.size} Anchors={rect.anchorMin}/{rect.anchorMax} Pivot={rect.pivot}");
//            }

//            for (var i = 0; i < current.childCount; i++)
//            {
//                var child = current.GetChild(i);

//                if (child.name.StartsWith("slot", System.StringComparison.OrdinalIgnoreCase))
//                {
//                    continue;
//                }

//                LogHierarchy(child, depth + 1);
//            }
//        }
//    }
//}
