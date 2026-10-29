using HarmonyLib;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class DvergrDamageDebugPatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value || __instance.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            LogState(__instance, hit, "Before");
        }

        private static void Postfix(Character __instance, HitData hit)
        {
            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value || __instance.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            LogState(__instance, hit, "After");
        }

        private static void LogState(Character character, HitData hit, string phase)
        {
            var attacker = hit.GetAttacker();
            var ai = character.GetBaseAI();
            var statusEffect = character.GetSEMan().GetStatusEffect("SE_DvergrAlly".GetStableHashCode());

            Plugin.Log.LogInfo($"[Dvergr Damage - {phase}] " + $"Attacker={attacker?.name ?? "None"}, " + $"Player={attacker != null && attacker.IsPlayer()}, " + $"Damage={hit.GetTotalDamage():F1}, " + $"Health={character.GetHealth():F1}, " + $"Alerted={ai != null && ai.IsAlerted()}, " + $"Aggravated={ai != null && ai.IsAggravated()}, " + $"AllyEffect={statusEffect != null}, " + $"Immunity={HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value}");
        }
    }
}