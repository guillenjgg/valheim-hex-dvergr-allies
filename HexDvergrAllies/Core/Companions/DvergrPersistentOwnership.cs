using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrPersistentOwnership
    {
        // Verified private in the installed assembly. Read synchronously on the
        // game thread; do not hold a dictionary enumerator across frames.
        private static readonly FieldInfo ObjectsField = AccessTools.Field(typeof(ZDOMan), "m_objectsByID");
        private static readonly HashSet<int> DvergrPrefabs = new HashSet<int>();
        private static ZNetScene _scene;
        private static ZDOMan _indexedWorld;
        private static readonly Dictionary<long, List<ZDOID>> HiresByCharacter = new Dictionary<long, List<ZDOID>>();

        private static IEnumerable<ZDO> GetRecords()
        {
            if (!ZNet.instance || !ZNet.instance.IsServer() || ZDOMan.instance == null || ObjectsField == null)
            {
                throw new InvalidOperationException("Persistent companion ownership requires the server's world records.");
            }

            if (_scene != ZNetScene.instance)
            {
                _scene = ZNetScene.instance;
                DvergrPrefabs.Clear();

                foreach (var prefab in _scene.m_prefabs)
                {
                    var character = prefab ? prefab.GetComponent<Character>() : null;

                    if (character && character.GetFaction() == Character.Faction.Dverger)
                    {
                        DvergrPrefabs.Add(prefab.name.GetStableHashCode());
                    }
                }
            }

            var objects = ObjectsField.GetValue(ZDOMan.instance) as Dictionary<ZDOID, ZDO>;

            if (objects == null)
            {
                throw new InvalidOperationException("Valheim world record dictionary is unavailable.");
            }

            return objects.Values;
        }

        internal static bool HasCompanion(long characterId)
        {
            if (!ReferenceEquals(_indexedWorld, ZDOMan.instance))
            {
                HiresByCharacter.Clear();

                foreach (var zdo in GetRecords())
                {
                    if (zdo.IsValid() && DvergrPrefabs.Contains(zdo.GetPrefab()) && DvergrCompanionState.IsHired(zdo))
                    {
                        Track(DvergrCompanionState.GetOwnerPlayerId(zdo), zdo.m_uid);
                    }
                }

                _indexedWorld = ZDOMan.instance;
            }

            List<ZDOID> companions;

            if (!HiresByCharacter.TryGetValue(characterId, out companions))
            {
                return false;
            }

            foreach (var id in companions)
            {
                var zdo = ZDOMan.instance.GetZDO(id);

                // Always revalidate the live persistent record: release/death and
                // ownership changes must take effect without rebuilding the index.
                if (zdo != null && DvergrCompanionState.IsHired(zdo) && DvergrCompanionState.GetOwnerPlayerId(zdo) == characterId)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void Track(long characterId, ZDOID companionId)
        {
            List<ZDOID> companions;

            if (!HiresByCharacter.TryGetValue(characterId, out companions))
            {
                companions = new List<ZDOID>();
                HiresByCharacter.Add(characterId, companions);
            }

            if (!companions.Contains(companionId))
            {
                companions.Add(companionId);
            }
        }

        internal static bool HasLivingStarter(long characterId)
        {
            foreach (var zdo in GetRecords())
            {
                if (zdo.IsValid() && DvergrPrefabs.Contains(zdo.GetPrefab()) && zdo.GetBool(Features.Progression.DvergrStarterCharacterState.StarterKey, false) && zdo.GetLong(Features.Progression.DvergrStarterCharacterState.CharacterKey, 0L) == characterId && zdo.GetFloat(ZDOVars.s_health, 1f) > 0f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
