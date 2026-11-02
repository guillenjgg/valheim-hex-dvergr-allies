using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    /// <summary>
    /// Companions persistent state
    /// </summary>
    internal static class DvergrCompanionState
    {
        internal const string HiredKey = "HexDvergr_IsHired";
        internal const string OwnerKey = "HexDvergr_HiredBy";
        internal const string HomeKey = "HexDvergr_HomePosition";
        internal const string HasHomeKeyBeenSaved = "HexDvergr_HomeSaved";
        internal const string NameKey = "HexDvergr_Name";
        internal const string ModeKey = "HexDvergr_Mode";
        internal const string PendingModeTeleportKey = "HexDvergr_PendingModeTeleport";
        internal const string PendingUnhireKey = "HexDvergr_PendingUnhire";
        internal const string RecallPositionKey = "HexDvergr_RecallPosition";

        internal static bool IsHired(ZDO zdo)
        {
            return zdo.GetBool(HiredKey, false);
        }

        internal static bool IsHired(Character character)
        {
            if (!character)
            {
                return false;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return false;
            }

            return IsHired(nview.GetZDO());
        }

        internal static long GetOwnerPlayerId(ZDO zdo)
        {
            return zdo.GetLong(OwnerKey, 0L);
        }

        internal static bool HasHome(ZDO zdo)
        {
            return zdo.GetBool(HasHomeKeyBeenSaved, false);
        }

        internal static Vector3 GetHomePosition(ZDO zdo)
        {
            return zdo.GetVec3(HomeKey, zdo.GetPosition());
        }

        internal static DvergrCompanionMode GetMode(ZDO zdo)
        {
            return (DvergrCompanionMode)zdo.GetInt(ModeKey, (int)DvergrCompanionMode.Companion);
        }

        internal static bool IsUnhirePending(ZDO zdo)
        {
            return zdo.GetBool(PendingUnhireKey, false);
        }

        internal static string GetSnapshot(ZDO zdo)
        {
            return $"ZDO={zdo.m_uid}, " +
                   $"ZDOOwner={zdo.GetOwner()}, " +
                   $"IsHired={IsHired(zdo)}, " +
                   $"OwnerPlayerID={GetOwnerPlayerId(zdo)}, " +
                   $"HomeSaved={HasHome(zdo)}, " +
                   $"Home={GetHomePosition(zdo)}, " +
                   $"Position={zdo.GetPosition()}, " +
                   $"Mode={GetMode(zdo)}, " +
                   $"PendingModeTeleport={zdo.GetBool(PendingModeTeleportKey, false)}, " +
                   $"PendingUnhire={IsUnhirePending(zdo)}, " +
                   $"Name={zdo.GetString(NameKey, "")}";
        }
    }
}