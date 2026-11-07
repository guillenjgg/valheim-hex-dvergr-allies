# Changelog

## Unreleased
- Add owner-only starter tombstone revival at full health, preserving identity, name and progression; normal companions and player graves retain their existing behavior.
- Add a native Dvergr Companion Compendium entry with starter stats and boss unlocks; simplify overhead level display to a number.
- Add code-defined starter biome balance profiles with a 200-health, 40-pierce Meadows baseline, isolated from normal companion stat multipliers. Advance from the owning character's highest credited boss and show the starter level, with a level-up effect for new hired tiers.
- Give the starter a pact-only store with no buying or selling, plus a localized explanation when another companion blocks hiring.
- Validate hires against persistent server ownership, including unloaded companions, and serialize simultaneous pact requests.
- Protect the starter from damage until hired. Recover missing starters without duplicating camp pieces; completed introductions stay completed.
- Add 2 blue jute for 500 coins and 1 fallen brother trophy for 4 allowed enemy trophies to normal Mistlands stores.
- Give each character a starter camp 60–200 meters from the starting stones, with a shelter, fire, and personal Dvergr.
- Add a shipwreck when a nearby coastal site can support it; land camps do not need a coast.
- Save introduction completion per character, per world after an accepted free pact. Hide its camp marker and prevent repeat introductions in that world; starters do not travel between worlds.
- Use the Dvergr portrait for personal map/minimap pins. Keep the map open during encounter updates.
- Keep normal paid pacts, the one-companion rule, and existing companion behavior.

## v0.0.2
- Improvements

## v0.0.1
- Initial development release