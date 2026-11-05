# HexDvergrAllies

> **Warning: Always backup your world before installing any mod. Early development version.** Expect bugs and rough edges. Things will change as development continues. More features and improvements are on the way!

Sucks having to kill them or steal from them. Now they're your homies.

Hire Dvergr to fight beside you, trade with them, and set up temporary homesteads in their towns.

## Features

* Hire a Dvergr ally to fight beside you.
* Earn Blood Magic experience when your hired ally damages a creature. You can turn this off in the config.
* Trade for Dvergr extractors, soft tissue, and flametal.
* Adjust companion settings like follow distance, movement speed, health, attack speed, damage, and auto-healing.
* Your ally follows you through portals.
* They teleport back to you if they get stuck or fall too far behind.
* Walk through your ally. No more getting stuck in a doorway.
* They teleport aboard your ship when you board.
* Press `F9` to open the ally command UI. Switch modes, rename your ally, recall them, or break your pact. The keybind is configurable.

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

Changes apply without restarting. Use Configuration Manager or edit the config file; the file watcher reloads saved changes.

### Hotkeys

| Setting | Default | What it does |
| --- | --- | --- |
| `UIShortcut` | `F9` | Opens or closes the ally command UI. |
| `SentryShortcut` | `Left Ctrl + P` | Switches between Sentry and Companion mode. |
| `RecallShortcut` | `O` | "Get over here": recalls your ally and switches them to Companion mode. |

### Companion Settings

* **FollowDistance:** Defaults to 2 meters. Controls how close your ally gets before they stop following. This doesn't limit how far they can move during combat.
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