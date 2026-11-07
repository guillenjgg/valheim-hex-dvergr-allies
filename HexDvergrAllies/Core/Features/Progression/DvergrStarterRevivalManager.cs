using HexDvergrAllies.Core;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.HexLocalization;
using System;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterRevivalManager
    {
        internal const string TombstoneKey = "hex_starter_tombstone";
        internal const string IdentityKey = "hex_starter_identity";
        private const string DeathRecordedKey = "hex_starter_death_recorded";
        private const string UsedKey = "hex_starter_revived";
        internal const string RequestRpc = "HexStarterRevive";
        internal const string ResultRpc = "HexStarterReviveResult";
        private static readonly Action<TombStone> PositionCheck = AccessTools.MethodDelegate<Action<TombStone>>(AccessTools.Method(typeof(TombStone), "PositionCheck"));

        internal static bool IsTombstone(Component component)
        {
            var view = component ? component.GetComponent<ZNetView>() : null;

            return view && view.IsValid() && view.GetZDO().GetBool(TombstoneKey, false);
        }

        internal static void OnDeath(Character character)
        {
            if (!DvergrStarterCharacterState.IsStarter(character))
            {
                return;
            }

            var view = character.GetComponent<ZNetView>();
            var source = view.GetZDO();
            var owner = DvergrCompanionState.GetOwnerPlayerId(source);

            if (!view.IsOwner() || !DvergrCompanionState.IsHired(source) || owner == 0L || source.GetBool(DeathRecordedKey, false))
            {
                return;
            }

            var prefab = ZNetScene.instance.GetPrefab("Player_tombstone");

            if (!prefab)
            {
                HexLog.LogError("[Starter Revival] Cannot create tombstone: Player_tombstone prefab unavailable.");
                return;
            }

            var identity = source.GetString(IdentityKey, "");

            if (string.IsNullOrEmpty(identity))
            {
                identity = Guid.NewGuid().ToString("N");
            }

            var name = DvergrCompanionNameManager.GetCompanionName(character);
            var instance = UnityEngine.Object.Instantiate(prefab, character.GetCenterPoint(), character.transform.rotation);
            var graveView = instance.GetComponent<ZNetView>();
            var grave = graveView.GetZDO();
            grave.Persistent = true;
            grave.Set(TombstoneKey, true);
            grave.Set(IdentityKey, identity);
            grave.Set(DvergrStarterCharacterState.CharacterKey, owner);
            grave.Set(DvergrCompanionState.NameKey, name);
            grave.Set(DvergrStarterBalanceService.TierKey, source.GetInt(DvergrStarterBalanceService.TierKey, 0));
            instance.GetComponent<TombStone>().Setup(name, owner);
            source.Set(IdentityKey, identity);
            source.Set(DeathRecordedKey, true);
            var drops = character.GetComponent<CharacterDrop>();

            if (drops)
            {
                drops.SetDropsEnabled(false);
            }

            HexLog.LogInfo($"[Starter Revival] Tombstone created: Character={owner}, Companion={source.m_uid}, Tombstone={grave.m_uid}, Identity={identity}, Tier={grave.GetInt(DvergrStarterBalanceService.TierKey, 0)}");
        }

        internal static bool SuppressDeathLoot(Component component)
        {
            var view = component ? component.GetComponent<ZNetView>() : null;

            return view && view.IsValid() && view.GetZDO().GetBool(DvergrStarterCharacterState.StarterKey, false) && view.GetZDO().GetBool(DeathRecordedKey, false);
        }

        internal static bool UpdateTombstone(TombStone tombstone)
        {
            if (!IsTombstone(tombstone))
            {
                return true;
            }

            if (tombstone.GetComponent<ZNetView>().IsOwner())
            {
                PositionCheck(tombstone);
            }

            // A starter grave is intentionally empty. Vanilla would grant the corpse
            // recovery buff and destroy it; keep it until a successful revival.
            return false;
        }

        internal static string HoverText(TombStone tombstone)
        {
            var zdo = tombstone.GetComponent<ZNetView>().GetZDO();
            var name = zdo.GetString(DvergrCompanionState.NameKey, "");
            var owner = Player.m_localPlayer && Player.m_localPlayer.GetPlayerID() == zdo.GetLong(DvergrStarterCharacterState.CharacterKey, 0L);
            var prompt = HexLocalizationManager.Localize(owner ? "$hex_dvergr_revive_prompt" : "$hex_dvergr_revive_owner_only");

            return name + "\n" + Localization.instance.Localize(prompt);
        }

        internal static void Interact(TombStone tombstone, Humanoid user, bool hold)
        {
            var player = user as Player;

            if (hold || !player || player != Player.m_localPlayer)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), RequestRpc, player.GetZDOID(), tombstone.GetComponent<ZNetView>().GetZDO().m_uid);
        }

        internal static void OnRequest(long sender, ZDOID playerId, ZDOID graveId)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer() || !ZNetScene.instance || ZDOMan.instance == null)
            {
                return;
            }

            var player = ZDOMan.instance.GetZDO(playerId);
            var grave = ZDOMan.instance.GetZDO(graveId);
            var connected = Player.m_localPlayer && Player.m_localPlayer.GetZDOID() == playerId;

            foreach (var peer in ZNet.instance.GetPeers())
            {
                connected |= peer.IsReady() && peer.m_characterID == playerId;
            }

            if (!connected || player == null || player.GetOwner() != sender || player.GetFloat(ZDOVars.s_health, 0f) <= 0f || grave == null || grave.GetPrefab() != "Player_tombstone".GetStableHashCode() || !grave.GetBool(TombstoneKey, false) || grave.GetBool(UsedKey, false) || Vector3.Distance(player.GetPosition(), grave.GetPosition()) > 6f)
            {
                Reply(sender, 0);
                return;
            }

            var characterId = player.GetLong(ZDOVars.s_playerID, 0L);

            if (characterId == 0L || grave.GetLong(DvergrStarterCharacterState.CharacterKey, 0L) != characterId)
            {
                Reply(sender, 0);
                return;
            }

            if (DvergrPactAuthority.HasCompanionOrPendingPact(characterId) || DvergrPersistentOwnership.HasLivingStarter(characterId))
            {
                Reply(sender, 2);
                return;
            }

            var prefab = ZNetScene.instance.GetPrefab("Dverger");

            if (!prefab)
            {
                Reply(sender, 0);
                return;
            }

            // Main-thread server commit: consume the marker before creating the body.
            // Requests for another pact observe the tracked persistent hire immediately.
            grave.SetOwner(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance));
            grave.Set(UsedKey, true);
            GameObject body = null;

            try
            {
                var position = grave.GetPosition() + new Vector3(1.5f, 0.5f, 0f);
                body = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                var character = body.GetComponent<Character>();
                var view = body.GetComponent<ZNetView>();

                if (!character || !view || !view.IsValid())
                {
                    throw new InvalidOperationException("Invalid revived Dvergr network instance.");
                }

                var zdo = view.GetZDO();
                zdo.Persistent = true;
                zdo.Set(DvergrStarterCharacterState.StarterKey, true);
                zdo.Set(DvergrStarterCharacterState.CharacterKey, characterId);
                zdo.Set(DvergrStarterCharacterState.ConsumedKey, true);
                zdo.Set(IdentityKey, grave.GetString(IdentityKey, ""));
                zdo.Set(DvergrStarterBalanceService.TierKey, grave.GetInt(DvergrStarterBalanceService.TierKey, 0));
                zdo.Set(DvergrCompanionState.NameKey, grave.GetString(DvergrCompanionState.NameKey, ""));
                zdo.Set(DvergrCompanionState.OwnerKey, characterId);
                zdo.Set(DvergrCompanionState.HiredKey, true);
                zdo.Set(DvergrCompanionState.HomeKey, position);
                zdo.Set(DvergrCompanionState.HasHomeKeyBeenSaved, true);
                zdo.Set(DvergrCompanionState.ModeKey, (int)DvergrCompanionMode.Companion);
                body.GetComponent<MonsterAI>().ResetPatrolPoint();
                DvergrCompanionManager.InitializeCompanion(character);
                DvergrStarterBalanceService.Update(character);
                character.SetHealth(character.GetMaxHealth());
                DvergrPersistentOwnership.Track(characterId, zdo.m_uid);
                DvergrCompanionManager.CacheHiredCompanion(characterId, zdo.m_uid);
                DvergrStarterEncounterManager.RecordCompletion(characterId);
                HexLog.LogInfo($"[Starter Revival] Revived: Character={characterId}, Tombstone={graveId}, Companion={zdo.m_uid}, Identity={zdo.GetString(IdentityKey, "")}, Health={character.GetHealth()}/{character.GetMaxHealth()}, Tier={zdo.GetInt(DvergrStarterBalanceService.TierKey, 0)}");
            }
            catch (Exception error)
            {
                if (body)
                {
                    var character = body.GetComponent<Character>();

                    if (character)
                    {
                        DvergrCompanionManager.OnCompanionDeath(character);
                    }

                    ZNetScene.instance.Destroy(body);
                }

                grave.Set(UsedKey, false);
                HexLog.LogError("[Starter Revival] Revival failed; tombstone retained: " + error);
                Reply(sender, 0);
                return;
            }

            // Delete only after the fully initialized, full-health hire exists.
            ZDOMan.instance.DestroyZDO(grave);
            Reply(sender, 1);
        }

        private static void Reply(long peer, int result)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, ResultRpc, result);
        }

        internal static void OnResult(long sender, int result)
        {
            if (sender != DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance) || !Player.m_localPlayer)
            {
                return;
            }

            var key = result == 1 ? "$hex_dvergr_revived" : result == 2 ? "$hex_dvergr_revive_blocked" : "$hex_dvergr_revive_failed";
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, HexLocalizationManager.Localize(key));
        }
    }
}
