using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionShipManager
    {
        private const string HiredKey = "HexDvergr_IsHired";
        private const string OwnerKey = "HexDvergr_HiredBy";

        private const float BoardingRange = 30f;
        private const float DeckHeight = 1.25f;
        private const float PassengerSpacing = 1.25f;

        private sealed class PassengerState
        {
            internal Ship Ship;
            internal Vector3 LocalPosition;
        }

        private static readonly Dictionary<Character, PassengerState> _passengers =
            new Dictionary<Character, PassengerState>();

        private static readonly List<Character> _removalBuffer =
            new List<Character>();

        private static Ship _currentShip;

        internal static bool IsPassenger(Character character)
        {
            return character && _passengers.ContainsKey(character);
        }

        internal static void Update()
        {
            Player player = Player.m_localPlayer;

            if (!player)
            {
                ClearPassengers();
                return;
            }

            Ship ship = Ship.GetLocalShip();

            if (ship != _currentShip)
            {
                DisembarkAll();
                _currentShip = ship;

                if (_currentShip)
                {
                    BoardCompanions(player, _currentShip);
                }
            }

            if (_currentShip)
            {
                UpdatePassengers(player);
            }
        }

        private static void BoardCompanions(Player player, Ship ship)
        {
            int passengerIndex = 0;

            foreach (Character character in Character.GetAllCharacters())
            {
                if (!IsOwnedCompanion(character, player))
                {
                    continue;
                }

                if (_passengers.ContainsKey(character))
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    character.transform.position,
                    player.transform.position);

                if (distance > BoardingRange)
                {
                    continue;
                }

                Vector3 localPosition = new Vector3(
                    passengerIndex % 2 == 0
                        ? -PassengerSpacing
                        : PassengerSpacing,
                    DeckHeight,
                    -1f - (passengerIndex / 2) * PassengerSpacing);

                Vector3 worldPosition =
                    ship.transform.TransformPoint(localPosition);

                MonsterAI monsterAI = character.GetComponent<MonsterAI>();

                if (monsterAI)
                {
                    monsterAI.SetFollowTarget(null);
                    monsterAI.ResetPatrolPoint();
                }

                _passengers.Add(character, new PassengerState
                {
                    Ship = ship,
                    LocalPosition = localPosition
                });

                character.transform.position = worldPosition;

                Plugin.Log.LogInfo(
                    $"[Dvergr Ship] Boarded: Dvergr={character.name}, Position={worldPosition}, PassengerIndex={passengerIndex}");

                passengerIndex++;
            }
        }

        private static void UpdatePassengers(Player player)
        {
            _removalBuffer.Clear();

            foreach (KeyValuePair<Character, PassengerState> entry in _passengers)
            {
                Character character = entry.Key;
                PassengerState state = entry.Value;

                if (!character ||
                    !state.Ship ||
                    state.Ship != _currentShip ||
                    !IsOwnedCompanion(character, player))
                {
                    _removalBuffer.Add(character);
                    continue;
                }

                Vector3 targetPosition =
                    state.Ship.transform.TransformPoint(state.LocalPosition);

                character.transform.position = targetPosition;
            }

            foreach (Character character in _removalBuffer)
            {
                ReleasePassenger(character);
            }
        }

        private static bool IsOwnedCompanion(Character character, Player player)
        {
            if (!character || !player)
            {
                return false;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (!netView || !netView.IsValid() || !netView.IsOwner())
            {
                return false;
            }

            ZDO zdo = netView.GetZDO();

            return zdo.GetBool(HiredKey, false) &&
                   zdo.GetLong(OwnerKey, 0L) == player.GetPlayerID();
        }

        private static void DisembarkAll()
        {
            _removalBuffer.Clear();

            foreach (Character character in _passengers.Keys)
            {
                _removalBuffer.Add(character);
            }

            foreach (Character character in _removalBuffer)
            {
                ReleasePassenger(character);
            }
        }

        private static void ReleasePassenger(Character character)
        {
            _passengers.Remove(character);

            if (!character)
            {
                return;
            }

            MonsterAI monsterAI = character.GetComponent<MonsterAI>();
            Player player = Player.m_localPlayer;

            if (monsterAI && player && IsOwnedCompanion(character, player))
            {
                monsterAI.ResetPatrolPoint();
                monsterAI.SetFollowTarget(player.gameObject);
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Ship] Disembarked: Dvergr={character.name}");
        }

        private static void ClearPassengers()
        {
            DisembarkAll();
            _currentShip = null;
        }
    }
}