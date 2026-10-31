using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionShipManager
    {
        private const string HiredKey = "HexDvergr_IsHired";
        private const string OwnerKey = "HexDvergr_HiredBy";

        private const float BoardingRange = 30f;
        private const float BoardingDelay = 1f;
        private const float BoardingCheckInterval = 1f;

        private const float PassengerSpacing = 0.75f;
        private const float PassengerRowSpacing = 0.75f;
        private const float PassengerBowOffset = 5f;
        private const float BoardingHeightOffset = 0.1f;

        private const float PositionCorrectionDistance = 0.15f;

        private const float DisembarkDistance = 2f;
        private const float DisembarkRaycastHeight = 5f;
        private const float DisembarkRaycastDistance = 15f;

        private sealed class PassengerState
        {
            internal Ship Ship;
            internal Vector3 LocalPosition;
        }

        private static readonly Dictionary<Character, PassengerState> _passengers = new Dictionary<Character, PassengerState>();

        private static readonly List<Character> _removalBuffer = new List<Character>();

        private static Ship _currentShip;
        private static float _nextBoardingCheckTime;
        private static float _boardingReadyTime;
        private static int _nextPassengerIndex;
        private static bool _boardingPending;

        private static Vector3 _boardingOriginLocalPosition;
        private static Vector3 _boardingForwardLocalDirection;
        private static Vector3 _boardingRightLocalDirection;

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
                _nextPassengerIndex = 0;
                _nextBoardingCheckTime = 0f;
                _boardingPending = false;
                _boardingReadyTime = 0f;

                if (_currentShip)
                {
                    _boardingPending = true;
                    _boardingReadyTime = Time.time + BoardingDelay;

                    Plugin.Log.LogInfo($"[Dvergr Ship] Player entered ship: " + $"Ship={_currentShip.name}, " + $"BoardingDelay={BoardingDelay:F2}s, " + $"InitialPlayerPosition={player.transform.position}");
                }
            }

            if (!_currentShip)
            {
                return;
            }

            if (_boardingPending)
            {
                if (Time.time < _boardingReadyTime)
                {
                    return;
                }

                // Capture the player's position after the boarding delay,
                // rather than at the moment the ship is first detected.
                CaptureBoardingPosition(player, _currentShip);

                _boardingPending = false;

                Plugin.Log.LogInfo($"[Dvergr Ship] Boarding delay completed: " + $"Ship={_currentShip.name}, " + $"PlayerPosition={player.transform.position}");

                BoardCompanions(player, _currentShip);

                _nextBoardingCheckTime = Time.time + BoardingCheckInterval;
            }
            else if (Time.time >= _nextBoardingCheckTime)
            {
                BoardCompanions(player, _currentShip);

                _nextBoardingCheckTime = Time.time + BoardingCheckInterval;
            }

            UpdatePassengers(player);
        }

        internal static void RemoveCharacter(Character character)
        {
            _passengers.Remove(character);
        }

        private static void CaptureBoardingPosition(Player player, Ship ship)
        {
            _boardingOriginLocalPosition = ship.transform.InverseTransformPoint(player.transform.position);

            _boardingForwardLocalDirection = ship.transform.InverseTransformDirection(player.transform.forward);

            _boardingRightLocalDirection = ship.transform.InverseTransformDirection(player.transform.right);

            _boardingForwardLocalDirection.y = 0f;
            _boardingRightLocalDirection.y = 0f;

            if (_boardingForwardLocalDirection.sqrMagnitude < 0.001f)
            {
                _boardingForwardLocalDirection = Vector3.forward;
            }
            else
            {
                _boardingForwardLocalDirection.Normalize();
            }

            if (_boardingRightLocalDirection.sqrMagnitude < 0.001f)
            {
                _boardingRightLocalDirection = Vector3.right;
            }
            else
            {
                _boardingRightLocalDirection.Normalize();
            }

            Plugin.Log.LogInfo($"[Dvergr Ship] Boarding reference captured: " + $"Ship={ship.name}, " + $"LocalPosition={_boardingOriginLocalPosition}, " + $"LocalForward={_boardingForwardLocalDirection}, " + $"LocalRight={_boardingRightLocalDirection}");
        }

        private static void BoardCompanions(Player player, Ship ship)
        {
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

                float distance = Vector3.Distance(character.transform.position, player.transform.position);

                if (distance > BoardingRange)
                {
                    continue;
                }

                int passengerIndex = _nextPassengerIndex;

                Vector3 localPosition = GetPassengerLocalPosition(passengerIndex);

                Vector3 worldPosition = ship.transform.TransformPoint(localPosition);

                MonsterAI monsterAI = character.GetComponent<MonsterAI>();

                if (monsterAI)
                {
                    monsterAI.SetFollowTarget(null);
                    monsterAI.ResetPatrolPoint();
                    monsterAI.StopMoving();
                }

                _passengers.Add(character, new PassengerState { Ship = ship, LocalPosition = localPosition });

                MoveCharacter(character, worldPosition);

                _nextPassengerIndex++;

                Plugin.Log.LogInfo($"[Dvergr Ship] Boarded: " + $"Dvergr={character.name}, " + $"Ship={ship.name}, " + $"Position={worldPosition}, " + $"LocalPosition={localPosition}, " + $"PassengerIndex={passengerIndex}, " + $"Distance={distance:F2}");
            }
        }

        private static Vector3 GetPassengerLocalPosition(int passengerIndex)
        {
            int column = passengerIndex % 2;
            int row = passengerIndex / 2;

            float sideOffset = column == 0 ? -PassengerSpacing : PassengerSpacing;

            // PassengerBowOffset is an absolute ship-local Z coordinate.
            // Subsequent rows move toward the stern.
            float forwardPosition = PassengerBowOffset - PassengerRowSpacing * row;

            Vector3 localPosition = new Vector3(sideOffset, _boardingOriginLocalPosition.y + BoardingHeightOffset, forwardPosition);

            return localPosition;
        }

        private static void UpdatePassengers(Player player)
        {
            _removalBuffer.Clear();

            foreach (KeyValuePair<Character, PassengerState> entry in _passengers)
            {
                Character character = entry.Key;
                PassengerState state = entry.Value;

                if (!character || !state.Ship || state.Ship != _currentShip || !IsOwnedCompanion(character, player))
                {
                    _removalBuffer.Add(character);
                    continue;
                }

                Vector3 targetPosition = state.Ship.transform.TransformPoint(state.LocalPosition);

                float distance = Vector3.Distance(character.transform.position, targetPosition);

                if (distance > PositionCorrectionDistance)
                {
                    MoveCharacter(character, targetPosition);
                }
                else
                {
                    Rigidbody rigidbody = character.GetComponent<Rigidbody>();

                    if (rigidbody)
                    {
                        rigidbody.linearVelocity = Vector3.zero;
                        rigidbody.angularVelocity = Vector3.zero;
                    }
                }
            }

            foreach (Character character in _removalBuffer)
            {
                ReleasePassenger(character, player, false);
            }
        }

        private static void MoveCharacter(Character character, Vector3 position)
        {
            Rigidbody rigidbody = character.GetComponent<Rigidbody>();

            if (rigidbody)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.position = position;
            }
            else
            {
                character.transform.position = position;
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

            if (zdo == null)
            {
                return false;
            }

            return zdo.GetBool(HiredKey, false) && zdo.GetLong(OwnerKey, 0L) == player.GetPlayerID();
        }

        private static void DisembarkAll()
        {
            Player player = Player.m_localPlayer;

            _removalBuffer.Clear();

            foreach (Character character in _passengers.Keys)
            {
                _removalBuffer.Add(character);
            }

            int passengerIndex = 0;

            foreach (Character character in _removalBuffer)
            {
                ReleasePassenger(character, player, true, passengerIndex);

                passengerIndex++;
            }

            _nextPassengerIndex = 0;
        }

        private static void ReleasePassenger(Character character, Player player, bool relocate, int passengerIndex = 0)
        {
            _passengers.Remove(character);

            if (!character || !player || !IsOwnedCompanion(character, player))
            {
                return;
            }

            if (relocate)
            {
                Vector3 destination;

                if (TryGetDisembarkPosition(player, passengerIndex, out destination))
                {
                    MoveCharacter(character, destination);

                    Plugin.Log.LogInfo($"[Dvergr Ship] Relocated on disembark: " + $"Dvergr={character.name}, " + $"Destination={destination}");
                }
                else
                {
                    Plugin.Log.LogWarning($"[Dvergr Ship] Disembark relocation skipped: " + $"Dvergr={character.name}, " + $"Reason=No safe ground found");
                }
            }

            MonsterAI monsterAI = character.GetComponent<MonsterAI>();

            if (monsterAI)
            {
                monsterAI.ResetPatrolPoint();
                monsterAI.SetFollowTarget(player.gameObject);
            }

            Plugin.Log.LogInfo($"[Dvergr Ship] Disembarked: " + $"Dvergr={character.name}");
        }

        private static bool TryGetDisembarkPosition(Player player, int passengerIndex, out Vector3 destination)
        {
            destination = Vector3.zero;

            Vector3 offset = -player.transform.forward * DisembarkDistance + player.transform.right * ((passengerIndex % 2 == 0 ? -1f : 1f) * PassengerSpacing);

            Vector3 rayOrigin = player.transform.position + offset + Vector3.up * DisembarkRaycastHeight;

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, DisembarkRaycastDistance);

            float highestSurface = float.NegativeInfinity;
            bool foundSurface = false;

            foreach (RaycastHit hit in hits)
            {
                Collider collider = hit.collider;

                if (!collider || collider.isTrigger)
                {
                    continue;
                }

                if (collider.GetComponentInParent<Ship>())
                {
                    continue;
                }

                if (collider.GetComponentInParent<Character>())
                {
                    continue;
                }

                if (hit.point.y > highestSurface)
                {
                    highestSurface = hit.point.y;
                    destination = hit.point + Vector3.up * BoardingHeightOffset;

                    foundSurface = true;
                }
            }

            return foundSurface;
        }

        private static void ClearPassengers()
        {
            DisembarkAll();

            _currentShip = null;
            _nextPassengerIndex = 0;
            _nextBoardingCheckTime = 0f;
            _boardingReadyTime = 0f;
            _boardingPending = false;

            _boardingOriginLocalPosition = Vector3.zero;
            _boardingForwardLocalDirection = Vector3.zero;
            _boardingRightLocalDirection = Vector3.zero;
        }
    }
}