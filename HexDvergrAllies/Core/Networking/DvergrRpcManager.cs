using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Core.Networking
{
    internal static class DvergrRpcManager
    {
        internal const string HireRpc = "HexDvergrHire";
        internal const string HireResultRpc = "HexDvergrHireResult";
        internal const string UnhireRpc = "HexDvergrUnhire";
        internal const string RenameRpc = "HexDvergrRename";
        internal const string SetModeRpc = "HexDvergrSetMode";

        internal const string PriceRequestRpc = "HexDvergrPriceRequest";
        internal const string PriceResponseRpc = "HexDvergrPriceResponse";
        internal const string PurchaseRequestRpc = "HexDvergrBuyRequest";
        internal const string PurchaseResponseRpc = "HexDvergrBuyResponse";
        internal const string PurchaseCompleteRpc = "HexDvergrBuyComplete";
        internal const string PurchaseResultRpc = "HexDvergrBuyResult";

        internal const string RemoteUnhireRpc = "HexDvergrRemoteUnhire";
        internal const string RemoteUnhireUnloadedRpc = "HexDvergrRemoteUnhireUnloaded";
        internal const string ProcessRemoteUnhireRpc = "HexDvergrProcessRemoteUnhire";
        internal const string RemoteUnhireCompletedRpc = "HexDvergrRemoteUnhireCompleted";
        internal const string RemoteSetModeRpc = "HexDvergrRemoteSetMode";
        internal const string RemoteRecallRpc = "HexDvergrRemoteRecall";
        internal const string RemoteRenameRpc = "HexDvergrRemoteRename";

        internal static void RegisterRoutedRpcs()
        {
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RemoteSetModeRpc, DvergrCompanionManager.RPC_RemoteSetMode);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, Vector3>(RemoteRecallRpc, DvergrCompanionManager.RPC_RemoteRecall);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, string>(RemoteRenameRpc, DvergrCompanionNameManager.RPC_RemoteRename);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RemoteUnhireRpc, DvergrCompanionManager.RPC_RemoteUnhire);
            ZRoutedRpc.instance.Register<ZDOID>(ProcessRemoteUnhireRpc, DvergrCompanionManager.RPC_ProcessRemoteUnhire);
            ZRoutedRpc.instance.Register<ZDOID>(RemoteUnhireUnloadedRpc, DvergrCompanionManager.RPC_RemoteUnhireUnloaded);
            ZRoutedRpc.instance.Register<ZDOID>(RemoteUnhireCompletedRpc, DvergrCompanionManager.RPC_RemoteUnhireCompleted);

            ZRoutedRpc.instance.Register<ZDOID>(PriceRequestRpc, DvergrTradeRpcManager.OnPriceRequest);
            ZRoutedRpc.instance.Register<int, int>(PriceResponseRpc, DvergrTradeRpcManager.OnPriceResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, long>(PurchaseRequestRpc, DvergrTradeRpcManager.OnBuyRequest);
            ZRoutedRpc.instance.Register<ZDOID, int, int, int, long, long>(PurchaseResponseRpc, DvergrTradeRpcManager.OnBuyResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, int, bool, long, long>(PurchaseCompleteRpc, DvergrTradeRpcManager.OnBuyComplete);
            ZRoutedRpc.instance.Register<long, bool>(PurchaseResultRpc, DvergrTradeRpcManager.OnBuyResult);
        }

        internal static void RegisterCharacterRpcs(ZNetView nview, Character character)
        {
            nview.Register<ZDOID, long>(HireRpc, (sender, playerId, requestId) => DvergrForgePactManager.Hire(character, sender, playerId, requestId));
            nview.Register<ZDOID, long, bool>(HireResultRpc, (sender, playerId, requestId, success) => DvergrForgePactManager.OnHireResult(character, sender, playerId, requestId, success));
            nview.Register<ZDOID>(UnhireRpc, (sender, playerId) => DvergrCompanionManager.Unhire(character, sender, playerId));
            nview.Register<ZDOID, string>(RenameRpc, (sender, playerId, name) => DvergrCompanionNameManager.Rename(character, sender, playerId, name));
            nview.Register<ZDOID>(SetModeRpc, (sender, playerId) => DvergrCompanionManager.SetMode(character, sender, playerId));
        }
    }
}