using System;

namespace HexDvergrAllies.Core.Transactions
{
    internal enum PurchaseState
    {
        Idle,
        WaitingForServer
    }

    internal static class DvergrTradeTransaction
    {
        private static long _nextRequestId = DateTime.UtcNow.Ticks;

        internal static long RequestId { get; private set; }
        internal static PurchaseState State { get; private set; } = PurchaseState.Idle;
        internal static bool IsPending => State != PurchaseState.Idle;

        internal static bool TryBegin(out long requestId)
        {
            requestId = 0;

            if (IsPending)
            {
                return false;
            }

            RequestId = ++_nextRequestId;
            State = PurchaseState.WaitingForServer;
            requestId = RequestId;

            return true;
        }

        internal static bool TryComplete(long requestId)
        {
            if (!IsPending || RequestId != requestId)
            {
                return false;
            }

            Reset();
            return true;
        }

        internal static void Reset()
        {
            RequestId = 0;
            State = PurchaseState.Idle;
        }
    }
}