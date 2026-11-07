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
        internal const string BloodMagicExperienceRpc = "HexDvergrBloodMagicExperience";

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
        internal const string RecallAcceptedRpc = "HexDvergrRecallAccepted";
        internal const string RemoteRenameRpc = "HexDvergrRemoteRename";

        internal static void RegisterRoutedRpcs()
        {
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(Features.Progression.DvergrStarterRevivalManager.RequestRpc, Features.Progression.DvergrStarterRevivalManager.OnRequest);
            ZRoutedRpc.instance.Register<int>(Features.Progression.DvergrStarterRevivalManager.ResultRpc, Features.Progression.DvergrStarterRevivalManager.OnResult);
            ZRoutedRpc.instance.Register<ZDOID, int>(Features.Progression.DvergrStarterProgressionService.ReportRpc, Features.Progression.DvergrStarterProgressionService.OnReport);
            ZRoutedRpc.instance.Register<ZDOID, long, int>(Features.Progression.DvergrStarterProgressionService.ApplyRpc, Features.Progression.DvergrStarterProgressionService.OnApply);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, long>(DvergrPactAuthority.RequestRpc, DvergrPactAuthority.OnRequest);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, long, long>(DvergrPactAuthority.CommitRpc, DvergrPactAuthority.OnCommit);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, long, bool>(DvergrPactAuthority.AcknowledgeRpc, DvergrPactAuthority.OnAcknowledge);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, long, bool>(DvergrPactAuthority.ResultRpc, DvergrForgePactManager.OnAuthoritativeResult);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(Features.Progression.DvergrStarterVendorService.QueryRpc, Features.Progression.DvergrStarterVendorService.OnQuery);
            ZRoutedRpc.instance.Register<ZDOID, int>(Features.Progression.DvergrStarterVendorService.StatusRpc, Features.Progression.DvergrStarterVendorService.OnStatus);
            ZRoutedRpc.instance.Register<ZDOID, long>(Features.Progression.DvergrStarterEncounterManager.DeathRpc, Features.Progression.DvergrStarterEncounterManager.OnDeathReported);
            ZRoutedRpc.instance.Register<ZDOID, bool>(Features.Progression.DvergrStarterEncounterManager.RegisterRpc, Features.Progression.DvergrStarterEncounterManager.OnRegister);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(Features.Progression.DvergrStarterEncounterManager.AcceptedRpc, Features.Progression.DvergrStarterEncounterManager.OnAccepted);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(BloodMagicExperienceRpc, DvergrCompanionDamageManager.RPC_BloodMagicExperience);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RemoteSetModeRpc, DvergrCompanionModeManager.RPC_RemoteSetMode);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, Vector3>(RemoteRecallRpc, DvergrCompanionRecallManager.RPC_RemoteRecall);
            ZRoutedRpc.instance.Register<ZDOID, bool>(RecallAcceptedRpc, DvergrCompanionRecallManager.RPC_RecallAccepted);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, string>(RemoteRenameRpc, DvergrCompanionNameManager.RPC_RemoteRename);
            ZRoutedRpc.instance.Register<ZDOID, ZDOID>(RemoteUnhireRpc, DvergrCompanionUnhireManager.RPC_RemoteUnhire);
            ZRoutedRpc.instance.Register<ZDOID>(ProcessRemoteUnhireRpc, DvergrCompanionUnhireManager.RPC_ProcessRemoteUnhire);
            ZRoutedRpc.instance.Register<ZDOID>(RemoteUnhireUnloadedRpc, DvergrCompanionUnhireManager.RPC_RemoteUnhireUnloaded);
            ZRoutedRpc.instance.Register<ZDOID>(RemoteUnhireCompletedRpc, DvergrCompanionUnhireManager.RPC_RemoteUnhireCompleted);

            ZRoutedRpc.instance.Register<ZDOID>(PriceRequestRpc, DvergrTradeRpcManager.OnPriceRequest);
            ZRoutedRpc.instance.Register<int, int>(PriceResponseRpc, DvergrTradeRpcManager.OnPriceResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, long>(PurchaseRequestRpc, DvergrTradeRpcManager.OnBuyRequest);
            ZRoutedRpc.instance.Register<ZDOID, int, int, int, long, long>(PurchaseResponseRpc, DvergrTradeRpcManager.OnBuyResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, int, bool, long, long>(PurchaseCompleteRpc, DvergrTradeRpcManager.OnBuyComplete);
            ZRoutedRpc.instance.Register<long, bool>(PurchaseResultRpc, DvergrTradeRpcManager.OnBuyResult);
        }

        internal static void RegisterCharacterRpcs(ZNetView nview, Character character)
        {
            nview.Register<ZDOID>(UnhireRpc, (sender, playerId) => DvergrCompanionUnhireManager.Unhire(character, sender, playerId));
            nview.Register<ZDOID, string>(RenameRpc, (sender, playerId, name) => DvergrCompanionNameManager.Rename(character, sender, playerId, name));
            nview.Register<ZDOID>(SetModeRpc, (sender, playerId) => DvergrCompanionModeManager.SetMode(character, sender, playerId));
        }
    }
}
