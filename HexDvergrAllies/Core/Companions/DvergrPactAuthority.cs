using HexDvergrAllies.Core;
using HexDvergrAllies.Core.Features.Progression;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrPactAuthority
    {
        internal const string RequestRpc = "HexDvergrPactRequest";
        internal const string CommitRpc = "HexDvergrPactCommit";
        internal const string AcknowledgeRpc = "HexDvergrPactAcknowledge";
        internal const string ResultRpc = "HexDvergrPactResult";
        private static readonly Dictionary<long, Grant> Grants = new Dictionary<long, Grant>();
        private static ZDOMan _world;

        private sealed class Grant
        {
            internal long CharacterId;
            internal bool Starter;
            internal ZDOID Player;
            internal ZDOID Companion;
            internal long Request;
            internal long Peer;
            internal long Owner;
            internal long Expires;
            internal bool Accepted;
            internal float LastProbe;
        }

        private static void ResetIfNeeded()
        {
            if (!ReferenceEquals(_world, ZDOMan.instance))
            {
                _world = ZDOMan.instance;
                Grants.Clear();
            }
        }

        internal static void Update()
        {
            if (!ZNet.instance || !ZNet.instance.IsServer())
            {
                return;
            }

            ResetIfNeeded();

            if (Grants.Count == 0)
            {
                return;
            }

            var expired = new List<KeyValuePair<long, Grant>>();

            foreach (var entry in Grants)
            {
                if (entry.Value.Expires < ZNet.instance.GetTime().Ticks)
                {
                    expired.Add(entry);
                }
            }

            foreach (var entry in expired)
            {
                var grant = entry.Value;

                if (grant.Accepted)
                {
                    Grants.Remove(entry.Key);
                    continue;
                }

                var companion = ZDOMan.instance.GetZDO(grant.Companion);

                if (companion != null && DvergrCompanionState.IsHired(companion) && DvergrCompanionState.GetOwnerPlayerId(companion) == entry.Key)
                {
                    RecordAccepted(grant);
                    Reply(grant.Peer, grant.Player, grant.Companion, grant.Request, true);
                    Grants.Remove(entry.Key);
                    continue;
                }

                var ownerConnected = grant.Owner == DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance) || ZNet.instance.GetPeer(grant.Owner) != null;

                if (!ownerConnected)
                {
                    Reply(grant.Peer, grant.Player, grant.Companion, grant.Request, false);
                    Grants.Remove(entry.Key);
                }
                else if (Time.time - grant.LastProbe >= 2f)
                {
                    // Reconcile an expired grant instead of refunding a possibly
                    // committed hire. The owner may acknowledge an existing hire,
                    // but an expired grant cannot create a new one.
                    grant.LastProbe = Time.time;
                    ZRoutedRpc.instance.InvokeRoutedRPC(grant.Owner, CommitRpc, grant.Player, grant.Companion, grant.Request, grant.Expires);
                }
            }
        }

        internal static long AuthenticatePlayer(long sender, ZDOID playerId)
        {
            var player = ZDOMan.instance.GetZDO(playerId);
            var connected = Player.m_localPlayer && Player.m_localPlayer.GetZDOID() == playerId;

            foreach (var peer in ZNet.instance.GetPeers())
            {
                connected |= peer.IsReady() && peer.m_characterID == playerId;
            }

            return connected && player != null && player.GetOwner() == sender ? player.GetLong(ZDOVars.s_playerID, 0L) : 0L;
        }

        internal static bool HasCompanionOrPendingPact(long characterId)
        {
            ResetIfNeeded();
            Grant grant;

            if (Grants.TryGetValue(characterId, out grant))
            {
                // Unresolved requests remain reserved until reconciliation.
                if (!grant.Accepted || grant.Expires >= ZNet.instance.GetTime().Ticks)
                {
                    return true;
                }

                Grants.Remove(characterId);
            }

            return DvergrPersistentOwnership.HasCompanion(characterId);
        }

        internal static void Request(Player player, Character companion, long requestId)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), RequestRpc, player.GetZDOID(), companion.GetComponent<ZNetView>().GetZDO().m_uid, requestId);
        }

        internal static void OnRequest(long sender, ZDOID playerId, ZDOID companionId, long requestId)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer())
            {
                return;
            }

            ResetIfNeeded();
            var characterId = AuthenticatePlayer(sender, playerId);
            var player = ZDOMan.instance.GetZDO(playerId);
            var companion = ZDOMan.instance.GetZDO(companionId);
            var prefab = companion != null ? ZNetScene.instance.GetPrefab(companion.GetPrefab()) : null;
            var dvergr = prefab ? prefab.GetComponent<Character>() : null;

            Grant previous;

            if (characterId != 0L && Grants.TryGetValue(characterId, out previous) && previous.Peer == sender && previous.Player == playerId && previous.Request == requestId && previous.Companion == companionId)
            {
                if (previous.Accepted)
                {
                    Reply(sender, playerId, companionId, requestId, true);
                }

                return;
            }

            if (characterId == 0L || companion == null || !dvergr || dvergr.GetFaction() != Character.Faction.Dverger || companion.GetOwner() == 0L || DvergrCompanionState.IsHired(companion) || Vector3.Distance(player.GetPosition(), companion.GetPosition()) > Trading.DvergrTraderManager.InteractionDistance + 2f)
            {
                Reply(sender, playerId, companionId, requestId, false);
                return;
            }

            try
            {
                if (HasCompanionOrPendingPact(characterId) || (companion.GetBool(DvergrStarterCharacterState.StarterKey, false) && !DvergrStarterEligibility.CanHire(characterId, companion.GetLong(DvergrStarterCharacterState.CharacterKey, 0L), ZoneSystem.instance.GetGlobalKey(DvergrStarterCharacterState.DoneKey(characterId)), companion.GetBool(DvergrStarterCharacterState.ConsumedKey, false))))
                {
                    Reply(sender, playerId, companionId, requestId, false);
                    return;
                }
            }
            catch (Exception exception)
            {
                HexLog.LogError($"[Dvergr Pact] Persistent ownership validation failed: {exception}");
                Reply(sender, playerId, companionId, requestId, false);
                return;
            }

            var grant = new Grant { CharacterId = characterId, Starter = companion.GetBool(DvergrStarterCharacterState.StarterKey, false), Player = playerId, Companion = companionId, Request = requestId, Peer = sender, Owner = companion.GetOwner(), Expires = ZNet.instance.GetTime().AddSeconds(15).Ticks };
            Grants[characterId] = grant;
            ZRoutedRpc.instance.InvokeRoutedRPC(grant.Owner, CommitRpc, playerId, companionId, requestId, grant.Expires);
        }

        internal static void OnCommit(long sender, ZDOID playerId, ZDOID companionId, long requestId, long expires)
        {
            if (sender != DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance))
            {
                return;
            }

            var instance = ZNetScene.instance.FindInstance(companionId);
            var character = instance ? instance.GetComponent<Character>() : null;
            var view = character ? character.GetComponent<ZNetView>() : null;
            var success = false;

            if (view && view.IsValid() && view.IsOwner())
            {
                var player = ZDOMan.instance.GetZDO(playerId);
                var zdo = view.GetZDO();
                var characterId = player != null ? player.GetLong(ZDOVars.s_playerID, 0L) : 0L;
                success = characterId != 0L && DvergrCompanionState.IsHired(zdo) && DvergrCompanionState.GetOwnerPlayerId(zdo) == characterId;

                if (!success && expires >= ZNet.instance.GetTime().Ticks)
                {
                    success = DvergrForgePactManager.CommitApproved(character, playerId);
                }
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, AcknowledgeRpc, playerId, companionId, requestId, success);
        }

        internal static void OnAcknowledge(long sender, ZDOID playerId, ZDOID companionId, long requestId, bool success)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer())
            {
                return;
            }

            Grant grant = null;

            foreach (var pending in Grants.Values)
            {
                if (pending.Owner == sender && pending.Request == requestId && pending.Companion == companionId && pending.Player == playerId)
                {
                    grant = pending;
                    break;
                }
            }

            if (grant == null || grant.Accepted)
            {
                return;
            }

            var characterId = grant.CharacterId;

            if (success)
            {
                // Keep a short reservation while the owning peer's ZDO changes
                // reach the server; a second simultaneous hire must stay blocked.
                grant.Accepted = true;
                grant.Expires = ZNet.instance.GetTime().AddSeconds(15).Ticks;
                RecordAccepted(grant);
            }
            else
            {
                Grants.Remove(characterId);
            }

            Reply(grant.Peer, playerId, companionId, requestId, success);
        }

        private static void RecordAccepted(Grant grant)
        {
            DvergrPersistentOwnership.Track(grant.CharacterId, grant.Companion);

            if (grant.Starter)
            {
                DvergrStarterEncounterManager.RecordCompletion(grant.CharacterId);
            }
        }

        private static void Reply(long peer, ZDOID playerId, ZDOID companionId, long requestId, bool success)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResultRpc, playerId, companionId, requestId, success);
        }
    }
}
