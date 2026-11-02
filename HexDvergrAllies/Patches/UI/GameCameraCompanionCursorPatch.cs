using HarmonyLib;
using HexDvergrAllies.Core.UI;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class GameCameraCompanionCursorPatch
    {
        private static bool Prefix()
        {
            if (!DvergrCompanionUIController.IsVisible)
            {
                return true;
            }

            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
            return false;
        }
    }
}