using HexDvergrAllies.Assets;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.HexLocalization;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal sealed class DvergrStarterEncounterManager : MonoBehaviour
    {
        internal const string RegisterRpc = "HexDvergrStarterRegister";
        internal const string AcceptedRpc = "HexDvergrStarterAccepted";
        private static DvergrStarterEncounterManager _instance;
        private readonly Dictionary<long, bool> _registered = new Dictionary<long, bool>();
        private readonly Dictionary<long, DvergrStarterEncounter> _encounters = new Dictionary<long, DvergrStarterEncounter>();
        private readonly List<ZDO> _legacyDvergr = new List<ZDO>();
        private readonly HashSet<long> _legacyCompleted = new HashSet<long>();
        private int _legacyIndex;
        private bool _legacyScanComplete;
        private ZoneSystem _world;
        private Minimap _map;
        private Minimap.PinData _pin;
        private long _pinCharacter;
        private float _nextRegistration;

        internal static bool IsStarter(Character character) => DvergrStarterCharacterState.IsStarter(character);
        internal static bool HasFreePact(Character character) => DvergrStarterCharacterState.HasFreePact(Player.m_localPlayer, character);
        internal static int PactCost(Character character) => HasFreePact(character) ? 0 : DvergrPricingConfig.ForgePactCost;
        internal static string PactPrice(Character character) => HasFreePact(character) ? HexLocalizationManager.Localize("$hex_dvergr_starter_free") : DvergrPricingConfig.ForgePactCostDisplay;

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            var world = ZoneSystem.instance;

            if (_world != world)
            {
                RemovePin();
                _world = world;
                _registered.Clear();
                _encounters.Clear();
                _legacyDvergr.Clear();
                _legacyCompleted.Clear();
                _legacyIndex = 0;
                _legacyScanComplete = false;
                _nextRegistration = 0f;
            }

            if (!world || !ZNet.instance || !ZNetScene.instance || !Game.instance || ZDOMan.instance == null || ZRoutedRpc.instance == null || WorldGenerator.instance == null)
            {
                return;
            }

            var player = Player.m_localPlayer;

            if (player && player.GetPlayerID() != 0L && Time.time >= _nextRegistration)
            {
                _nextRegistration = Time.time + 5f;
                var completed = DvergrStarterCharacterState.HasCompleted(player) || DvergrStarterCharacterState.HasAcceptedStarter(player);

                if (completed)
                {
                    DvergrStarterCharacterState.SaveLocalCompletion(player);
                }

                var view = player.GetComponent<ZNetView>();

                if (view && view.IsValid() && view.IsOwner())
                {
                    view.GetZDO().Set(DvergrStarterCharacterState.CompletedKey, completed);
                    ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), RegisterRpc, player.GetZDOID(), completed);
                }
            }

            UpdatePin(player);

            if (!ZNet.instance.IsServer() || !world.LocationsGenerated || _registered.Count == 0)
            {
                return;
            }

            // Upgrade already-hired starters from the previous shared encounter.
            // The installed API scans a bounded batch; spread migration over frames.
            if (!_legacyScanComplete)
            {
                for (var batch = 0; batch < 16 && !_legacyScanComplete; batch++)
                {
                    _legacyScanComplete = ZDOMan.instance.GetAllZDOsWithPrefabIterative("Dverger", _legacyDvergr, ref _legacyIndex);
                }

                if (!_legacyScanComplete)
                {
                    return;
                }

                foreach (var zdo in _legacyDvergr)
                {
                    if (zdo.GetBool(DvergrStarterCharacterState.StarterKey, false) && DvergrCompanionState.IsHired(zdo))
                    {
                        _legacyCompleted.Add(DvergrCompanionState.GetOwnerPlayerId(zdo));
                    }
                }
            }

            foreach (var character in _registered)
            {
                var doneKey = DvergrStarterCharacterState.DoneKey(character.Key);

                if (character.Value || _legacyCompleted.Contains(character.Key))
                {
                    if (!world.GetGlobalKey(doneKey))
                    {
                        world.SetGlobalKey(doneKey);
                    }
                }
                else if (!world.GetGlobalKey(doneKey) && !_encounters.ContainsKey(character.Key))
                {
                    _encounters.Add(character.Key, new DvergrStarterEncounter(character.Key));
                }
            }

            // Serialize placement so two characters cannot reserve the same space.
            foreach (var encounter in _encounters.Values)
            {
                if (!encounter.IsFinished)
                {
                    encounter.Tick();
                    break;
                }
            }
        }

        internal static void OnRegister(long sender, ZDOID playerId, bool completed)
        {
            if (!_instance || !ZNet.instance || !ZNet.instance.IsServer())
            {
                return;
            }

            var zdo = ZDOMan.instance.GetZDO(playerId);
            var characterId = ConnectedCharacterId(playerId);

            if (zdo == null || zdo.GetOwner() != sender || characterId == 0L)
            {
                return;
            }

            bool previous;
            _instance._registered.TryGetValue(characterId, out previous);
            _instance._registered[characterId] = previous || completed;
        }

        internal static void OnAccepted(long sender, ZDOID playerId, ZDOID companionId)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer())
            {
                return;
            }

            var characterId = ConnectedCharacterId(playerId);
            var companion = ZDOMan.instance.GetZDO(companionId);

            if (characterId == 0L || companion == null || companion.GetOwner() != sender || !companion.GetBool(DvergrStarterCharacterState.StarterKey, false) || !DvergrCompanionState.IsHired(companion))
            {
                return;
            }

            var intendedCharacter = companion.GetLong(DvergrStarterCharacterState.CharacterKey, 0L);

            if (DvergrCompanionState.GetOwnerPlayerId(companion) != characterId || (intendedCharacter != 0L && intendedCharacter != characterId))
            {
                return;
            }

            ZoneSystem.instance.SetGlobalKey(DvergrStarterCharacterState.DoneKey(characterId));
            Plugin.Log.LogInfo($"[Starter Encounter] Introduction completed for character {characterId} after accepted pact.");
        }

        private static long ConnectedCharacterId(ZDOID playerId)
        {
            var connected = Player.m_localPlayer && Player.m_localPlayer.GetZDOID() == playerId;

            foreach (var peer in ZNet.instance.GetPeers())
            {
                connected |= peer.IsReady() && peer.m_characterID == playerId;
            }

            var zdo = ZDOMan.instance.GetZDO(playerId);

            return connected && zdo != null ? zdo.GetLong(ZDOVars.s_playerID, 0L) : 0L;
        }

        private void UpdatePin(Player player)
        {
            if (_map != Minimap.instance || (player && _pinCharacter != player.GetPlayerID()))
            {
                RemovePin();
                _map = Minimap.instance;
            }

            if (!_map || !player || DvergrStarterCharacterState.HasCompleted(player))
            {
                RemovePin();
                return;
            }

            if (_pin != null)
            {
                return;
            }

            var characterId = player.GetPlayerID();
            var prefix = DvergrStarterCharacterState.WorldPrefix(characterId);

            if (!_world.GetGlobalKey(prefix + "built"))
            {
                return;
            }

            foreach (var key in _world.GetGlobalKeys())
            {
                if (!key.StartsWith(prefix + "pos_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = key.Substring((prefix + "pos_").Length).Split('_');
                float x, y, z;
                int direction;

                if (parts.Length != 5 || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z) || !int.TryParse(parts[3], out direction))
                {
                    continue;
                }

                var position = new Vector3(x, y, z) + new Vector3(Mathf.Cos(direction * Mathf.PI / 4f), 0f, Mathf.Sin(direction * Mathf.PI / 4f)) * 14f;
                _pin = _map.AddPin(position, Minimap.PinType.Icon3, HexLocalizationManager.Localize("$hex_dvergr_starter_camp"), false, false);
                _pinCharacter = characterId;

                if (AssetHelper.DvergrPortrait)
                {
                    _pin.m_icon = AssetHelper.DvergrPortrait;
                }

                return;
            }
        }

        private void RemovePin()
        {
            if (_map && _pin != null)
            {
                _map.RemovePin(_pin);
            }

            _pin = null;
            _map = null;
            _pinCharacter = 0L;
        }

        private void OnDestroy()
        {
            RemovePin();
            _instance = null;
        }
    }
}
