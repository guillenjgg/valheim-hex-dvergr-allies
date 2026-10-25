
namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTradeTerminalCommands
    {
        internal static void Register()
        {
            new Terminal.ConsoleCommand("hex_dvergr_reset", "Reset the Dvergr trading purchase counter.", args =>
            {
                if (!ZNet.instance.IsServer())
                {
                    args.Context.AddString("Only the server or host can reset Dvergr purchases.");
                    return;
                }

                DvergrTradeRpcManager.ResetPurchaseCount();
                args.Context.AddString("Dvergr purchase counter reset. Next purchase: 1.");
            });
        }
    }
}
