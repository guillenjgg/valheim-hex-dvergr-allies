using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    // These methods are private in the installed game, even though the build
    // reference publicizes them. Invoke cached reflection handles at runtime.
    internal static class DvergrStarterWorldAccess
    {
        private static readonly MethodInfo PokeLocalZoneMethod = AccessTools.Method(typeof(ZoneSystem), "PokeLocalZone", new[] { typeof(Vector2s) });
        private static readonly MethodInfo GetLocationMethod = AccessTools.Method(typeof(ZoneSystem), "GetLocation", new[] { typeof(string) });
        private static readonly MethodInfo SpawnLocationMethod = AccessTools.Method(typeof(ZoneSystem), "SpawnLocation", new[] { typeof(ZoneSystem.ZoneLocation), typeof(int), typeof(Vector3), typeof(Quaternion), typeof(ZoneSystem.SpawnMode), typeof(List<GameObject>), typeof(bool) });

        internal static bool IsAvailable => PokeLocalZoneMethod != null && GetLocationMethod != null && SpawnLocationMethod != null;

        internal static void LoadZone(ZoneSystem world, Vector2s zone)
        {
            PokeLocalZoneMethod.Invoke(world, new object[] { zone });
        }

        internal static ZoneSystem.ZoneLocation GetWreckLocation(ZoneSystem world)
        {
            return (ZoneSystem.ZoneLocation)GetLocationMethod.Invoke(world, new object[] { "ShipWreck01" });
        }

        internal static void SpawnWreck(ZoneSystem world, ZoneSystem.ZoneLocation location, Vector3 position, Quaternion rotation)
        {
            SpawnLocationMethod.Invoke(world, new object[] { location, 193701, position, rotation, ZoneSystem.SpawnMode.Full, new List<GameObject>(), false });
        }
    }
}
