using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Debug
{
    [HarmonyPatch(typeof(MonsterAI), "DoAttack")]
    internal static class MonsterAICompanionAttackDebugPatch
    {
        private static void Postfix(MonsterAI __instance, Character target, bool isFriend, bool __result)
        {
            var character = __instance.GetComponent<Character>();

            if (!character || !DvergrCompanionState.IsHired(character))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var humanoid = character as Humanoid;
            var weapon = humanoid.GetCurrentWeapon();
            var attack = weapon?.m_shared.m_attack;

            Plugin.Log.LogInfo(
                $"[Dvergr Attack Diagnostic] " +
                $"Dvergr={character.name}, " +
                $"Target={(target ? target.name : "None")}, " +
                $"IsFriend={isFriend}, " +
                $"Result={__result}, " +
                $"Weapon={(weapon != null ? weapon.m_shared.m_name : "None")}, " +
                $"RequiresReload={(attack != null && attack.m_requiresReload)}, " +
                $"WeaponLoaded={humanoid.IsWeaponLoaded()}, " +
                $"InMinorAction={character.InMinorAction()}, " +
                $"InAttack={character.InAttack()}");
        }
    }
}