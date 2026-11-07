# HexDvergrAllies

> **Warning: Always backup your world before installing any mod. Early development version.** Expect bugs and rough edges. Things will change as development continues. More features and improvements are on the way!

Sucks having to kill them or steal from them. Now they're your homies.

Hire Dvergr to fight beside you, trade with them, and set up temporary homesteads in their towns.

## Features

* Hire a Dvergr ally to fight beside you.
* Earn Blood Magic experience when your hired ally damages a creature. You can turn this off in the config.
* Trade for Dvergr extractors, soft tissue, blue jute, fallen brother trophies, and flametal.
* Adjust companion settings like follow distance, movement speed, health, attack speed, damage, and auto-healing.
* Your ally follows you through portals.
* They teleport back to you if they get stuck or fall too far behind.
* Walk through your ally. No more getting stuck in a doorway.
* They teleport aboard your ship when you board.
* Press `F9` to open the ally command UI. Switch modes, rename your ally, recall them, or break your pact. The keybind is configurable.

### Your First Dvergr

Each character gets a separate starter Dvergr camp in each world near the world's starting stones. Look for **Dvergr camp** on your map. It has a small roofed shelter, a fire, and a Dvergr offering that character one free pact. If the site has a usable coast, it also gets a shipwreck.

The server searches 60–200 meters from the starting stones and creates the encounter automatically. You don't have to approach it to trigger spawning. The marker appears after placement succeeds. Trees, rocks, player buildings, and protected locations are left alone; the search skips blocked sites. If no site fits within 200 meters, the log explains why instead of sending you farther away.

Hiring your starter ends the introduction and removes its camp marker. Completion is saved for that character in that world. Your starter stays in that world; entering another world begins a separate companion journey. Another character gets their own encounter and cannot hire your starter. You can still only have one companion at a time. Other Dvergr charge normally, and releasing your starter does not grant another free pact.

The starter only offers the introduction pact. They do not buy or sell items. Already have a hired Mistlands or Ashlands Dvergr? The camp still appears, but its pact stays blocked until you no longer own a companion—even if that companion is far away after a reload. Owning a normal Dvergr does not complete the introduction.

The starter is immune to damage until hired. After the first pact, normal companion damage rules apply, including after release. There are no replacement starters or repeat pacts after hiring.

Already-hired survivors from the old shared encounter count as a completed introduction. Existing camps and companions are left in place. Install the mod on the server and every player who needs its features.

Temporary testing buttons are available under **Debug** in BepInEx Configuration Manager: **Kill starter** kills your loaded starter; **Respawn starter (reset pact)** removes your starter and restores the free pact at the existing camp. Both work only for the local host. Reset affects only your character in the current world and keeps camp structures.

## Starter Balance

Edit starter stats directly in **Core/Features/Progression/DvergrStarterBalanceConfig.cs**, then rebuild. Each tier has named health, pierce and blunt values in one table. These values are code-only; BepInEx config does not override them. Movement, attack speed, healing, and damage reduction use the shared companion settings at every tier. Damage reduction defaults to 50%. Meadows starts at 200 health, 40 base pierce, and 15 base blunt. These replace general companion health and outgoing damage multipliers for starters. Normal Dvergr retain their existing settings. Vanilla attack variation, difficulty, armor and resistances still affect actual damage.

After hiring, starters advance automatically from the highest boss credited in their owning character's saved kill statistics, regardless of kill order: Eikthyr unlocks Black Forest, Elder unlocks Swamp, Bonemass unlocks Mountains, Moder unlocks Plains, Yagluth unlocks Mistlands, and Queen unlocks Ashlands. Ashlands is the current cap. World boss flags and other characters do not grant progress. The overhead name shows a compact level number from 1 (Meadows) to 7 (Ashlands). Open Valheim Compendium > Dvergr Companion for the current starter profile, all tier stats, and boss unlocks. The entry reads values directly from the code balance table and refreshes when the Compendium is reopened. A newly earned tier plays a level-up effect while hired; persisted tiers do not replay it after loading. Progress is checked every five seconds while the owner is connected; unloaded starters catch up when loaded. Health edits preserve current health percentage. Use the same mod build on the server and clients. The provisional curve scales from Meadows (200/40/15) to the Ashlands prefab reference (1000/210/70).

## New Mistlands Stock

Normal Mistlands Dvergr sell **2 blue jute for 500 coins** and **1 fallen brother trophy for 4 allowed enemy trophies**. You can mix trophies from the same allowed list used for extractors. This trophy purchase does not need Sap or change the world's extractor purchase count. The starter Dvergr does not offer this stock.

### Ally Modes

Press `Left Ctrl + P` to switch between Sentry and Companion mode. A center-screen message confirms the new mode. You can change the keybind under `SentryShortcut` in the config.

Press `O` for "Get over here" to recall your ally and switch them to Companion mode. You can change this under `RecallShortcut` in the config.

* **Companion:** Follows you around and fights beside you while you explore.
* **Sentry:** Guards the spot where you are standing when you activate the mode. Fights enemies within 20 meters of that spot.

Use Sentry mode while you're harvesting soft tissue or flametal, for example.

**Note:** Hired Dvergr switch back to Companion mode whenever you teleport. Set them back to Sentry mode afterward if you want them to stay put.

### Dvergr Types

* **Mistlands:** Mages and arbalest users. No fire immunity.
* **Ashlands:** Arbalest users. Immune to lava, fire, and Ashlands water. The AI can be dumb and just sit in lava and die. No more!

![HexDvergrAllies](https://raw.githubusercontent.com/guillenjgg/valheim-hex-mod-images/main/hexdvergrallies/hexdvergrallies_1.png)
![HexDvergrAllies](https://raw.githubusercontent.com/guillenjgg/valheim-hex-mod-images/main/hexdvergrallies/hexdvergrallies_2.png)
![HexDvergrAllies](https://raw.githubusercontent.com/guillenjgg/valheim-hex-mod-images/main/hexdvergrallies/hexdvergrallies_4.png)
![HexDvergrAllies](https://raw.githubusercontent.com/guillenjgg/valheim-hex-mod-images/main/hexdvergrallies/hexdvergrallies_5.png)
![HexDvergrAllies](https://raw.githubusercontent.com/guillenjgg/valheim-hex-mod-images/main/hexdvergrallies/hexdvergrallies_6.png)

## Known Issues

* Localization is incomplete. Some text is still English only.
* Dvergr can run away from your base, which gets annoying. Put them in Sentry mode outside to stop that.
* Dvergr won't attack inside Sunken Crypts.


## Configuration

Your config is updated automatically when the mod starts. Existing values for current options are kept, new options get their defaults, and obsolete options are removed. Settings are saved in the current display order.

Changes apply without restarting. Use Configuration Manager or edit the config file; the file watcher reloads saved changes.

### Hotkeys

| Setting | Default | What it does |
| --- | --- | --- |
| `UIShortcut` | `F9` | Opens or closes the ally command UI. |
| `SentryShortcut` | `Left Ctrl + P` | Switches between Sentry and Companion mode. |
| `RecallShortcut` | `O` | "Get over here": recalls your ally and switches them to Companion mode. |

### Companion Settings

* **FollowDistance:** Defaults to 2 meters. Controls how close your ally gets before they stop following. This doesn't limit how far they can move during combat.
* **TargetScanInterval:** Defaults to 1 second. Controls how often hired Dvergr scan for enemies in Companion and Sentry modes. Adjustable from 0.1 to 10 seconds; lower values mean more frequent scans. Changes take effect at the next normal scan reset. Attack cooldowns and special chase delays stay the same.
* **EnableDungeonFollowRecovery:** Enabled by default. Recovers a following ally stuck behind dungeon walls or on another floor, even when they are less than 12 meters away. Sentry mode and outdoor following are unaffected.
* **DungeonFollowRecoveryDelay:** Defaults to 4 seconds without progress. Recovery waits for a clear spot on your floor.
* **EnableDungeonTargetFiltering:** Enabled by default. Keeps an actionable target in dungeons and skips heard-only enemies with no path, so your ally is less likely to chase noises behind walls or on other floors. A visible enemy already within weapon range can still be attacked. Reachability is checked every half second. Toggle this off to compare vanilla targeting.
* **EnableCombatCircling:** Disabled by default. Stops your hired Dvergr from taking periodic breaks to run around enemies. Enable it to restore their normal circling behavior. This setting can be changed while playing.
* **MovementSpeedMultiplier:** Defaults to 2.5x.
* **HealthMultiplier:** Defaults to 2x maximum health.
* **DamageTakenMultiplier:** Defaults to 0.5, meaning 50% less incoming damage.
* **DamageDealtMultiplier:** Defaults to 1.5x damage.
* **AttackSpeedMultiplier:** Defaults to 3x. Speeds up attack animations and cooldown recovery.
* **EnableBloodMagicExperience:** Enabled by default. Damaging hits from your hired Dvergr give you Blood Magic experience, including killing blows. Hits that do no health damage and hits against players or structures give no experience.
* **BloodMagicExperienceFactor:** Defaults to 1. Adjusts the experience per damaging hit using normal skill progression, rather than adding whole skill levels.
* **EnableAutoHealing:** Enabled by default. Your ally heals to full at 50% health, with a 60-second cooldown. Adjust these with `AutoHealThreshold` and `AutoHealCooldown`.

You can also toggle player damage protection for Dvergr and temporary homesteads in Dvergr towns. Debug logs are available under advanced settings.

ServerSync is not supported yet. Settings aren't synced between players.

## Multiplayer Compatibility

One Dvergr ally per player.

Extractor purchases are tracked across the whole world, so everyone shares the same pricing. The first two purchases don't require sap. After that, each extractor costs 10 sap. Each purchase also requires a Mistlands trophy.


## Support & Feedback

[Join the Discord](https://discord.gg/wU2FXD94v4)

## Source Control

[View the source on GitHub](https://github.com/guillenjgg/valheim-hex-dvergr-allies)