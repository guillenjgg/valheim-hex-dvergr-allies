using HarmonyLib;
using HexDvergrAllies.Core.Features.Progression;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HexDvergrAllies.Patches.Networking
{
    [HarmonyPatch]
    internal static class ZoneSystemStarterKeyMapPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ZoneSystem), "GlobalKeyAdd", new[] { typeof(string), typeof(bool) });
            yield return AccessTools.Method(typeof(ZoneSystem), "GlobalKeyRemove", new[] { typeof(string), typeof(bool) });
        }

        private static void Prefix(string __0, out IDisposable __state)
        {
            __state = DvergrStarterMapRefreshGuard.BeginKeyUpdate(__0);
        }

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), "RPC_GlobalKeys")]
    internal static class ZoneSystemStarterKeySyncMapPatch
    {
        private static void Prefix(ZoneSystem __instance, List<string> __1, out IDisposable __state)
        {
            __state = DvergrStarterMapRefreshGuard.BeginSync(__instance.GetGlobalKeys(), __1);
        }

        private static Exception Finalizer(Exception __exception, IDisposable __state)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.UpdateNoMap))]
    internal static class GameStarterKeyMapRefreshPatch
    {
        private static bool Prefix()
        {
            return DvergrStarterMapRefreshGuard.ShouldRefreshMap;
        }
    }
}
