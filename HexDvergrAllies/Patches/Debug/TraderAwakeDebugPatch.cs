
//using HarmonyLib;
//using System;
//using System.Collections;
//using System.Reflection;
//using System.Text;
//using UnityEngine;

//namespace HexDvergrAllies.Patches
//{
//    [HarmonyPatch(typeof(Trader), "Start")]
//    internal static class TraderAwakeDebugPatch
//    {
//        private static void Postfix(Trader __instance)
//        {
//            var logger = Plugin.Log;
//            var traderType = typeof(Trader);
//            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

//            logger.LogInfo($"[Trader Debug] Name={__instance.name}");

//            foreach (var field in traderType.GetFields(flags))
//            {
//                var value = field.GetValue(__instance);

//                if (value is IEnumerable collection && !(value is string))
//                {
//                    var index = 0;

//                    foreach (var item in collection)
//                    {
//                        logger.LogInfo($"[Trader Debug] {field.Name}[{index}] = {FormatValue(item)}");

//                        if (item != null)
//                        {
//                            foreach (var itemField in item.GetType().GetFields(flags))
//                                logger.LogInfo($"[Trader Debug]   {itemField.Name} = {FormatValue(itemField.GetValue(item))}");
//                        }

//                        index++;
//                    }

//                    logger.LogInfo($"[Trader Debug] {field.Name}.Count={index}");
//                }
//                else
//                {
//                    logger.LogInfo($"[Trader Debug] {field.Name} = {FormatValue(value)}");
//                }
//            }

//            foreach (var component in __instance.GetComponents<Component>())
//                logger.LogInfo($"[Trader Debug] Component={component.GetType().FullName}");

//            foreach (var method in traderType.GetMethods(flags | BindingFlags.DeclaredOnly))
//            {
//                var parameters = method.GetParameters();
//                var signature = new StringBuilder();

//                for (var i = 0; i < parameters.Length; i++)
//                {
//                    if (i > 0)
//                        signature.Append(", ");

//                    signature.Append(parameters[i].ParameterType.Name);
//                    signature.Append(' ');
//                    signature.Append(parameters[i].Name);
//                }

//                logger.LogInfo($"[Trader Debug] Method={method.ReturnType.Name} {method.Name}({signature})");
//            }
//        }

//        private static string FormatValue(object value)
//        {
//            if (value == null)
//                return "null";

//            if (value is GameObject gameObject)
//                return gameObject.name;

//            if (value is UnityEngine.Object unityObject)
//                return unityObject.name;

//            return value.ToString();
//        }
//    }
//}
