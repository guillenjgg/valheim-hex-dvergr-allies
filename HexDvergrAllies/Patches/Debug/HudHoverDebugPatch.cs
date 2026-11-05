
//using System;
//using System.Diagnostics;
//using System.Reflection;
//using HarmonyLib;
//using HexDvergrAllies.Core.Managers;
//using TMPro;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
//    internal static class HudHoverDebugPatch
//    {
//        private static readonly FieldInfo HoverNameField =
//            AccessTools.Field(typeof(Hud), "m_hoverName");

//        private static string _lastHoverText;
//        private static int _lastHoverObjectId;

//        [HarmonyPostfix]
//        [HarmonyPriority(Priority.Last)]
//        private static void Postfix(Player player)
//        {
//            if (!player || HoverNameField == null)
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

//            TMP_Text hoverName = HoverNameField.GetValue(Hud.instance) as TMP_Text;

//            if (!hoverName)
//            {
//                return;
//            }

//            string text = hoverName.text ?? string.Empty;
//            int objectId = hoverObject.GetInstanceID();

//            if (_lastHoverObjectId == objectId &&
//                _lastHoverText == text)
//            {
//                return;
//            }

//            _lastHoverObjectId = objectId;
//            _lastHoverText = text;

//            StackTrace trace = new StackTrace(1, true);

//            Plugin.Log.LogInfo(
//                "[Dvergr HUD Debug] " +
//                "HoverObject=" + hoverObject.name +
//                ", FinalText=" + text.Replace("\n", " | ") +
//                Environment.NewLine +
//                "StackTrace:" + Environment.NewLine +
//                trace);
//        }
//    }
//}
