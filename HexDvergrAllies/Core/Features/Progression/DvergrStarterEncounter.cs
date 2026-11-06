using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    // World keys are saved by ZoneSystem and synchronized to joining clients.
    internal sealed class DvergrStarterEncounter
    {
        private readonly long _characterId;
        private string PositionPrefix => DvergrStarterCharacterState.WorldPrefix(_characterId) + "pos_";
        private string CompleteKey => DvergrStarterCharacterState.WorldPrefix(_characterId) + "built";
        private string SlotPrefix => DvergrStarterCharacterState.WorldPrefix(_characterId) + "slot_";
        private const string StarterKey = "hex_starter_dvergr";
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private ZoneSystem _world;
        private IEnumerator _search;
        private Vector3 _position;
        private Vector3 _inland;
        private bool _hasPosition;
        private bool _candidatePending;
        private bool _hasWreck;
        private bool _searched;
        private bool _spawnFailed;
        private string _positionKey;
        private readonly List<ZoneSystem.LocationInstance> _nearbyLocations = new List<ZoneSystem.LocationInstance>();
        private readonly Dictionary<int, int> _obstaclePrefabKinds = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _searchCounts = new Dictionary<string, int>();
        private readonly HashSet<string> _rejected = new HashSet<string>();
        private float _nextTick;
        internal DvergrStarterEncounter(long characterId)
        {
            _characterId = characterId;
        }

        internal bool IsFinished => _spawnFailed || (_world && (_world.GetGlobalKey(CompleteKey) || _world.GetGlobalKey(DvergrStarterCharacterState.DoneKey(_characterId)) || (_searched && _search == null && !_hasPosition)));

        internal void Tick()
        {
            var zones = ZoneSystem.instance;

            if (_world != zones)
            {
                _world = zones;
                _search = null;
                _searched = false;
                _spawnFailed = false;
                _hasPosition = false;
                _candidatePending = false;
                _positionKey = null;
                _rejected.Clear();
                _nearbyLocations.Clear();
                _obstaclePrefabKinds.Clear();
                _searchCounts.Clear();
                _nextTick = 0f;
            }

            if (!zones || !ZNet.instance || !ZNetScene.instance || !Game.instance || WorldGenerator.instance == null)
            {
                return;
            }

            if (zones.GetGlobalKey(DvergrStarterCharacterState.DoneKey(_characterId)))
            {
                _search = null;
                return;
            }

            if (_search != null && !_search.MoveNext())
            {
                _search = null;
            }

            if (Time.time < _nextTick)
            {
                return;
            }

            _nextTick = Time.time + 1f;
            ReadPosition();

            if (!ZNet.instance.IsServer() || !zones.LocationsGenerated)
            {
                return;
            }

            if (!_hasPosition && !_searched)
            {
                _searched = true;
                _search = FindSite();
            }

            if (!_hasPosition || _spawnFailed || zones.GetGlobalKey(CompleteKey))
            {
                return;
            }

            if (!DvergrStarterWorldAccess.IsAvailable)
            {
                Fail("Required ZoneSystem methods are unavailable in this game version.");
                return;
            }

            try
            {
                if (TerrainReady())
                {
                    SpawnEncounter();
                }
            }
            catch (Exception exception)
            {
                Fail($"Terrain preparation or placement failed. {exception.GetBaseException()}");
            }
        }

        private IEnumerator FindSite()
        {
            ZoneSystem.LocationInstance start;

            if (!_world.FindClosestLocation(Game.instance.m_StartLocation, Vector3.zero, out start))
            {
                _searched = false;
                yield break;
            }

            var origin = start.m_position;
            var generator = WorldGenerator.instance;
            var samples = 0;
            _searchCounts.Clear();
            RefreshNearbyLocations(origin);
            Plugin.Log.LogInfo($"[Starter Encounter] Searching Meadows camp sites within 200m of initial spawn {origin}.");

            // Search outward from the world's initial spawn, never a player's bed.
            for (var radius = 60f; radius <= 200f; radius += 5f)
            {
                var count = Mathf.CeilToInt(2f * Mathf.PI * radius / 5f);

                for (var i = 0; i < count; i++)
                {
                    if (++samples % 128 == 0)
                    {
                        yield return null;
                    }

                    var angle = i * 2f * Mathf.PI / count;
                    var point = origin + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    point.y = generator.GetHeight(point);

                    if (generator.GetBiome(point) != Heightmap.Biome.Meadows)
                    {
                        CountRejection("NotMeadows");
                        continue;
                    }

                    if (point.y < _world.m_waterLevel + 0.8f)
                    {
                        CountRejection("WetCamp");
                        continue;
                    }

                    // Prefer a coastal orientation at this distance, then accept
                    // a land-only orientation before considering a farther camp.
                    for (var attempt = 0; attempt < 16; attempt++)
                    {
                        var direction = attempt % 8;
                        var a = direction * Mathf.PI / 4f;
                        var inland = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        var root = point - inland * 14f;
                        root.y = generator.GetHeight(root);

                        if (_rejected.Contains(PointKey(root)))
                        {
                            continue;
                        }

                        _hasWreck = attempt < 8;

                        if (_hasWreck && (point.y > _world.m_waterLevel + 7f || generator.GetHeight(root - inland * 20f) >= _world.m_waterLevel - 0.5f))
                        {
                            continue;
                        }

                        string reason;

                        if (!SuitableFootprint(point - inland * 14f, inland, false, out reason))
                        {
                            CountRejection(reason);
                            continue;
                        }

                        // Most of the starting island already has persistent trees
                        // and rocks. Reject those sites before loading terrain or
                        // stopping the search to validate one candidate at a time.
                        if (!NetworkFootprintClear(point - inland * 14f, inland, out reason))
                        {
                            CountRejection("KnownNetworkObstacle");
                            continue;
                        }

                        // The layout root is 14m in front of the camp center.
                        _position = root;
                        _inland = inland;
                        _hasPosition = true;
                        var value = string.Join("_", root.x.ToString("R", Invariant), root.y.ToString("R", Invariant), root.z.ToString("R", Invariant), direction.ToString(Invariant), _hasWreck ? "1" : "0");
                        _positionKey = PositionPrefix + value;
                        _candidatePending = true;
                        Plugin.Log.LogInfo($"[Starter Encounter] Checking character {_characterId} camp at {point}, Wreck={_hasWreck}, {radius:0}m from initial spawn. Marker will appear after placement succeeds.");
                        // Resume this same search after a loaded candidate fails,
                        // rather than rescanning every inner ring from scratch.
                        do
                        {
                            yield return null;
                        }
                        while (_candidatePending && !_spawnFailed);

                        if (_spawnFailed || _hasPosition)
                        {
                            yield break;
                        }
                    }
                }
            }

            var counts = new List<string>();

            foreach (var entry in _searchCounts)
            {
                counts.Add(entry.Key + "=" + entry.Value);
            }

            Plugin.Log.LogWarning($"[Starter Encounter] No suitable Meadows camp within 200m of initial spawn. Samples={samples}, Rejections: {string.Join(", ", counts)}. No distant fallback or encounter was created.");
        }

        private void CountRejection(string reason)
        {
            int count;
            _searchCounts.TryGetValue(reason, out count);
            _searchCounts[reason] = count + 1;
        }

        private void RefreshNearbyLocations(Vector3 origin)
        {
            _nearbyLocations.Clear();

            foreach (var location in _world.GetLocationList())
            {
                if (location.m_location == null)
                {
                    continue;
                }

                var radius = Mathf.Max(location.m_location.m_exteriorRadius, location.m_location.m_interiorRadius) + 4f;

                if (HorizontalDistance(location.m_position, origin) <= 250f + radius)
                {
                    _nearbyLocations.Add(location);
                }
            }
        }

        private bool NearLocation(Vector3 point, float clearance = 0f)
        {
            foreach (var location in _nearbyLocations)
            {
                var radius = Mathf.Max(location.m_location.m_exteriorRadius, location.m_location.m_interiorRadius) + 4f + clearance;

                if (HorizontalDistance(location.m_position, point) < radius)
                {
                    return true;
                }
            }

            return false;
        }

        private bool SuitableFootprint(Vector3 point, Vector3 inland, bool loaded, out string reason)
        {
            var right = Vector3.Cross(Vector3.up, inland);
            Func<float, float, Vector3> position = (x, z) => point + inland * z + right * x;
            Func<float, float, float> height = (x, z) => loaded ? _world.GetGroundHeight(position(x, z)) : WorldGenerator.instance.GetHeight(position(x, z));

            if (!DvergrStarterTerrainRules.Evaluate(height, (x, z) => WorldGenerator.instance.GetBiome(position(x, z)) == Heightmap.Biome.Meadows, (x, z) => NearLocation(position(x, z)), _world.m_waterLevel, out reason, _hasWreck))
            {
                return false;
            }

            if (!loaded)
            {
                return true;
            }

            for (var z = _hasWreck ? -4f : 10f; z <= 18f; z += 2f)
            {
                var width = z <= 8f ? 4f : z >= 12f && z <= 16f ? 2f : 0f;

                for (var x = -width; x <= width; x += 2f)
                {
                    var sample = position(x, z);
                    sample.y = height(x, z) + 1.5f;

                    var blockers = Physics.OverlapSphere(sample, 1.2f, LayerMask.GetMask("Default", "static_solid", "piece"), QueryTriggerInteraction.Ignore);

                    foreach (var blocker in blockers)
                    {
                        // Terrain is checked by the height policy; moving creatures
                        // must not cause the permanent camp search to chase players.
                        if (blocker.GetComponentInParent<Heightmap>() || blocker.GetComponentInParent<Character>())
                        {
                            continue;
                        }

                        reason = $"LoadedObstacle: Object={blocker.transform.root.name}, Collider={blocker.name}, Layer={LayerMask.LayerToName(blocker.gameObject.layer)}, Sample={sample}";
                        return false;
                    }
                }
            }

            return true;
        }

        private void ReadPosition()
        {
            if (_candidatePending && ZNet.instance.IsServer())
            {
                return;
            }

            foreach (var key in _world.GetGlobalKeys())
            {
                if (!key.StartsWith(PositionPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = key.Substring(PositionPrefix.Length).Split('_');
                float x, y, z;
                int direction;

                if (parts.Length != 5 || !float.TryParse(parts[0], NumberStyles.Float, Invariant, out x) || !float.TryParse(parts[1], NumberStyles.Float, Invariant, out y) || !float.TryParse(parts[2], NumberStyles.Float, Invariant, out z) || !int.TryParse(parts[3], out direction))
                {
                    continue;
                }

                if (_positionKey != key)
                {
                    }

                _positionKey = key;
                _hasWreck = parts[4] == "1";
                _position = new Vector3(x, y, z);
                _inland = new Vector3(Mathf.Cos(direction * Mathf.PI / 4f), 0f, Mathf.Sin(direction * Mathf.PI / 4f));
                _hasPosition = true;
                return;
            }

            _hasPosition = false;
        }

        private static string PointKey(Vector3 point)
        {
            return point.x.ToString("R", Invariant) + ":" + point.z.ToString("R", Invariant);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private bool NetworkFootprintClear(Vector3 position, Vector3 inland, out string reason)
        {
            reason = null;

            // Remote zones may contain persistent objects without local colliders.
            var objects = new List<ZDO>();
            ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(position), new SimulationDistance(1, 0, false), objects, new List<ZDO>());
            var rotation = Quaternion.Inverse(Quaternion.LookRotation(inland));

            foreach (var zdo in objects)
            {
                var local = rotation * (zdo.GetPosition() - position);
                var inWreck = _hasWreck && Mathf.Abs(local.x) <= 7f && local.z >= -7f && local.z <= 11f;
                var inShelter = Mathf.Abs(local.x) <= 5f && local.z >= 9f && local.z <= 19f;
                var inFire = Mathf.Abs(local.x) <= 4f && local.z >= 15f && local.z <= 21f;
                var inCamp = inWreck || inShelter || inFire;
                var inSmoothingArea = _hasWreck && new Vector2(local.x - 22f, local.z + 21f).sqrMagnitude < 169f;

                if (!inCamp && !inSmoothingArea)
                {
                    continue;
                }

                var hash = zdo.GetPrefab();
                int kind;

                if (!_obstaclePrefabKinds.TryGetValue(hash, out kind))
                {
                    var prefab = ZNetScene.instance.GetPrefab(hash);

                    if (!prefab)
                    {
                        continue;
                    }

                    kind = prefab.GetComponent<Piece>() ? 1 : 0;

                    if (prefab.GetComponent<TreeBase>() || prefab.GetComponent<TreeLog>() || prefab.GetComponent<MineRock>() || prefab.GetComponent<MineRock5>() || prefab.GetComponent<Destructible>())
                    {
                        kind |= 2;
                    }

                    _obstaclePrefabKinds[hash] = kind;
                }

                if ((kind & 1) != 0 || (inCamp && (kind & 2) != 0))
                {
                    var prefab = ZNetScene.instance.GetPrefab(hash);
                    reason = $"NetworkObstacle: Prefab={prefab.name}, Position={zdo.GetPosition()}, ZDO={zdo.m_uid}";
                    return false;
                }
            }

            return true;
        }

        private bool TerrainReady()
        {
            var rotation = Quaternion.LookRotation(_inland);

            var visited = new HashSet<Vector2s>();

            // Includes the wreck's smoothing modifier at local (22, -21), radius 10.
            for (var x = _hasWreck ? -10 : -4; x <= (_hasWreck ? 34 : 4); x += 4)
            {
                for (var z = _hasWreck ? -34 : 8; z <= 22; z += 4)
                {
                    var point = _position + rotation * new Vector3(x, 0f, z);
                    var zone = ZoneSystem.GetZone(point);

                    if (!visited.Add(zone))
                    {
                        continue;
                    }

                    var loaded = _world.IsZoneLoaded(point);
                    DvergrStarterWorldAccess.LoadZone(_world, zone);

                    if (!loaded)
                    {
                        // Load at most one additional zone per tick, including on dedicated servers.
                        return false;
                    }
                }
            }

            return true;
        }

        private bool SmoothingAreaClear(out string reason)
        {
            reason = null;
            var center = _position + Quaternion.LookRotation(_inland) * new Vector3(22f, 0f, -21f);

            for (var x = -10; x <= 10; x += 4)
            {
                for (var z = -10; z <= 10; z += 4)
                {
                    var point = center + new Vector3(x, 0f, z);
                    point.y = _world.GetGroundHeight(point) + 1f;

                    if (NearLocation(point, 2f) || Physics.CheckSphere(point, 2f, LayerMask.GetMask("piece"), QueryTriggerInteraction.Ignore))
                    {
                        reason = $"ProtectedSmoothingArea: Position={point}";
                        return false;
                    }
                }
            }

            return true;
        }

        private void RejectShore(string reason)
        {
            Plugin.Log.LogInfo($"[Starter Encounter] Shore at {_position} rejected: {reason}; searching for another nearby camp.");
            _rejected.Add(PointKey(_position));

            if (_world.GetGlobalKey(_positionKey))
            {
                _world.RemoveGlobalKey(_positionKey);
            }

            _candidatePending = false;
            _hasPosition = false;
            _positionKey = null;
            // FindSite is paused at this candidate and continues on the next frame.
            _searched = _search != null;
        }

        private bool PreparedSiteClear(out string reason)
        {
            return SuitableFootprint(_position, _inland, true, out reason) && (!_hasWreck || SmoothingAreaClear(out reason)) && NetworkFootprintClear(_position, _inland, out reason);
        }

        private void SpawnEncounter()
        {
            // Never relocate or retry a partially created wreck.
            if (_hasWreck && _world.GetGlobalKey(SlotPrefix + "wreck_started") && !_world.GetGlobalKey(SlotPrefix + "wreck"))
            {
                Fail("An earlier wreck placement was interrupted. Automatic retry stopped to avoid duplicating partially created wreck pieces.");
                return;
            }

            // Validate everything before the first persistent object is created.
            var wreck = _hasWreck ? DvergrStarterWorldAccess.GetWreckLocation(_world) : null;
            var names = new[] { "wood_pole2", "wood_roof_45", "fire_pit", "Dverger" };

            if (_hasWreck && wreck == null)
            {
                Fail("ShipWreck01 location is unavailable.");
                return;
            }

            foreach (var name in names)
            {
                var prefab = ZNetScene.instance.GetPrefab(name);

                if (!prefab || !prefab.GetComponent<ZNetView>())
                {
                    Fail($"Required network prefab unavailable: {name}.");
                    return;
                }
            }

            // Completed slots are never recreated, even after death, hiring or destruction.
            if (!_world.GetGlobalKey(SlotPrefix + "site_prepared"))
            {
                RefreshNearbyLocations(_position);
                string reason;

                if (!PreparedSiteClear(out reason))
                {
                    if (!_hasWreck)
                    {
                        RejectShore(reason);
                        return;
                    }

                    // Keep the nearby camp when only the optional wreck is blocked.
                    _hasWreck = false;
                    var previousKey = _positionKey;
                    _positionKey = _positionKey.Substring(0, _positionKey.Length - 1) + "0";

                    if (_world.GetGlobalKey(previousKey))
                    {
                        _world.RemoveGlobalKey(previousKey);
                    }

                    Plugin.Log.LogInfo($"[Starter Encounter] Omitting optional wreck for character {_characterId}: {reason}.");

                    if (!PreparedSiteClear(out reason))
                    {
                        RejectShore(reason);
                        return;
                    }
                }
            }

            try
            {
                // Persist the accepted site before any objects, so interrupted
                // placement resumes here rather than selecting a second encounter.
                if (!_world.GetGlobalKey(_positionKey))
                {
                    _world.SetGlobalKey(_positionKey);
                }

                _world.SetGlobalKey(SlotPrefix + "site_prepared");
                _position.y = _world.GetGroundHeight(_position);
                var rotation = Quaternion.LookRotation(_inland);

                if (_hasWreck && !_world.GetGlobalKey(SlotPrefix + "wreck"))
                {
                    var randomState = UnityEngine.Random.state;

                    _world.SetGlobalKey(SlotPrefix + "wreck_started");

                    try
                    {
                        DvergrStarterWorldAccess.SpawnWreck(_world, wreck, _position, rotation);
                        _world.SetGlobalKey(SlotPrefix + "wreck");
                    }
                    finally
                    {
                        UnityEngine.Random.state = randomState;
                    }
                }

                // Lower posts extend the supports into uneven ground without leveling terrain.
                SpawnPiece("foundation0", "wood_pole2", new Vector3(-2f, -1f, 12f), rotation);
                SpawnPiece("foundation1", "wood_pole2", new Vector3(2f, -1f, 12f), rotation);
                SpawnPiece("foundation2", "wood_pole2", new Vector3(-2f, -1f, 16f), rotation);
                SpawnPiece("foundation3", "wood_pole2", new Vector3(2f, -1f, 16f), rotation);

                // Roof slopes along local Z; two mirrored rows meet at the ridge.
                SpawnPiece("pole0", "wood_pole2", new Vector3(-2f, 1f, 12f), rotation);
                SpawnPiece("pole1", "wood_pole2", new Vector3(2f, 1f, 12f), rotation);
                SpawnPiece("pole2", "wood_pole2", new Vector3(-2f, 1f, 16f), rotation);
                SpawnPiece("pole3", "wood_pole2", new Vector3(2f, 1f, 16f), rotation);
                SpawnPiece("roof0", "wood_roof_45", new Vector3(-1f, 3f, 15f), rotation);
                SpawnPiece("roof1", "wood_roof_45", new Vector3(1f, 3f, 15f), rotation);
                SpawnPiece("roof2", "wood_roof_45", new Vector3(-1f, 3f, 13f), rotation * Quaternion.Euler(0f, 180f, 0f));
                SpawnPiece("roof3", "wood_roof_45", new Vector3(1f, 3f, 13f), rotation * Quaternion.Euler(0f, 180f, 0f));
                SpawnPiece("fire", "fire_pit", new Vector3(0f, 0f, 18f), rotation);
                SpawnPiece("dvergr", "Dverger", new Vector3(0f, 0.3f, 11f), rotation);
                _world.SetGlobalKey(CompleteKey);
                _candidatePending = false;
                Plugin.Log.LogInfo($"[Starter Encounter] Created for character {_characterId} at {_position + _inland * 14f}, Wreck={_hasWreck}.");
            }
            catch (Exception exception)
            {
                Fail($"Placement interrupted; completed slots are retained. {exception}");
            }
        }

        private void SpawnPiece(string slot, string prefabName, Vector3 offset, Quaternion rotation)
        {
            var key = SlotPrefix + slot;

            if (_world.GetGlobalKey(key))
            {
                return;
            }

            var point = _position + Quaternion.LookRotation(_inland) * offset;
            // Roof and upper/lower posts share a plane above the highest support corner.
            var isShelter = slot.StartsWith("foundation", StringComparison.Ordinal) || slot.StartsWith("pole", StringComparison.Ordinal) || slot.StartsWith("roof", StringComparison.Ordinal);
            point.y = isShelter ? ShelterBaseHeight() + offset.y : _world.GetGroundHeight(point) + offset.y;
            var instance = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab(prefabName), point, rotation);
            var view = instance.GetComponent<ZNetView>();

            if (!view || !view.IsValid())
            {
                UnityEngine.Object.Destroy(instance);
                throw new InvalidOperationException($"Invalid network view for {prefabName}.");
            }

            view.GetZDO().Persistent = true;

            if (slot == "dvergr")
            {
                view.GetZDO().Set(StarterKey, true);
                view.GetZDO().Set(DvergrStarterCharacterState.CharacterKey, _characterId);
                instance.GetComponent<MonsterAI>().SetPatrolPoint();
            }

            _world.SetGlobalKey(key);
        }

        private float ShelterBaseHeight()
        {
            var rotation = Quaternion.LookRotation(_inland);
            var height = float.MinValue;

            for (var x = -2f; x <= 2f; x += 4f)
            {
                for (var z = 12f; z <= 16f; z += 4f)
                {
                    height = Mathf.Max(height, _world.GetGroundHeight(_position + rotation * new Vector3(x, 0f, z)));
                }
            }

            return height - 0.1f;
        }

        private void Fail(string reason)
        {
            _spawnFailed = true;
            Plugin.Log.LogWarning("[Starter Encounter] " + reason);
        }

    }
}
