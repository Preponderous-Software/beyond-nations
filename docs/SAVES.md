# Saves

A Beyond Nations world can be saved and picked up again, on the desktop and
in the browser. This document covers what a save holds, how it is checked,
how the format is versioned and migrated, and where the files live.

The rule above every other: **a save must never be broken or lost by the
game**. A save that cannot be read is reported, never silently replaced, and
the save before the latest is always kept.

## Playing with saves

- **Every world is saved on its own.** Starting a world gives it a slot; the
  first save creates the file.
- **Autosave** every 6,000 ticks (two minutes of play at the default 50 ticks a
  second), and whenever the player leaves the world: *Main Menu* or *Quit* on
  the pause screen, Escape on the main menu, or closing the window. The browser
  build also saves when the tab is hidden (closed, switched away from, or a
  phone locked), which is the last moment a page can count on.
- **Save** on the pause screen saves at once, and the line under the buttons
  says what happened: `Saved: Day 3 - Xland, 14:22.`, or why it could not.
- **Continue** on the main menu loads the most recently saved world, and
  **Load** lists every saved world, newest first, with its day, the player's
  nation and when it was saved. A save that cannot be loaded is listed with
  the reason (damaged, or made by a newer version) rather than hidden. Each
  row has a **Delete** button that asks again (*Sure?*) before deleting.
  Continue and Load only appear once there is something to load, and *Start*
  stays the top button either way.
- `--continue` (`?continue` in the browser) opens straight into the latest
  save. `--no-saves` (`?no-saves`) turns saving off for a run: no autosave, no
  save on quit, no Continue or Load. A run that ends because of
  `--exit-after-frames` never saves on the way out.
- **A run that has ended is never saved.** When the player dies in a nation
  with no heir (docs/SYSTEMS.md, Rule), nothing more is written to the slot --
  no autosave, no save on leaving -- so *Load* brings back the last save, from
  before the death.

## Where saves live

| Build | Location |
| --- | --- |
| Linux | `$XDG_DATA_HOME/BeyondNations/Saves`, or `~/.local/share/BeyondNations/Saves` |
| Windows | `%AppData%\BeyondNations\Saves` |
| Browser | `localStorage`, keys `beyond-nations/saves/<file name>` |

Each world is `<slot>.bnsave`, with the save before it as `<slot>.bnsave.bak`.
A slot is named after the world's seed and when it began:
`world-42-20261018-142233` (a negative seed is written `m42`).

In the browser every file is gzipped and base64-encoded behind a `gz:` marker
(`SaveStorageCodec`), because the arcade serves every game from one origin and
`localStorage` gives that origin about 5 MB in all. The bytes read back are
exactly the bytes written, so the checksum below still holds. When storage is
full the save fails with a message that says so and suggests deleting an old
world from *Load*.

## The file

A save is UTF-8 text (in practice ASCII) in two parts:

```
{"magic":"beyond-nations-save","formatVersion":1,"gameVersion":"0.3.0-alpha","seed":42,"tick":2400,"day":1,"nation":null,"savedAt":"2026-10-18T12:00:00.0000000Z","bodyBytes":189217,"crc32":"b220a346"}
{"formatVersion":1,"gameVersion":"0.3.0-alpha","config":{...},"seed":42,"time":{...},"random":{...},...}
```

- **Line 1, the header.** Enough to list the save (`day`, `nation`, `savedAt`)
  and decide whether it can be read (`magic`, `formatVersion`) without reading
  the world. `bodyBytes` and `crc32` describe everything after the newline.
- **The body.** The whole world as one JSON object. Its own `formatVersion`
  and `seed` must agree with the header's.

`tests/BeyondNations.Core.Tests/save/fixtures/v1-seed42-tick2400.bnsave`,
`v2-seed42-tick2400.bnsave`, `v3-seed42-tick2400.bnsave`, `v4-seed42-tick2400.bnsave` and `v5-seed42-tick2400.bnsave` are real version 1 to 5 saves; open one to see every section. `.gitattributes` marks
`*.bnsave` as binary, since a line-ending conversion would break the checksum.

### What the body holds

Everything the simulation is made of, so that a loaded world is the same world
and goes on exactly as the saved one would have:

| Section | Contents |
| --- | --- |
| `config` | every GameConfig setting the simulation reads (not the data directory, which is the machine's) |
| `seed` | the seed the world was actually built from, even when the config asked for 0 |
| `time` | total ticks, ticks per second and per day, and the time of day the world started at |
| `random` | the full state of each of the four random streams (`main`, `settlementLife`, `traits`, `population`): seed, the generator's 56 ints and its two indices |
| `simulation` | the death counters, the territory the player was last told about, the chunk world generation is centred on, and the names each name generator has handed out |
| `environment` | every chunk: its id, coordinates, size and stream, and each location's id, colour and entity list |
| `playerId`, `entities` | every entity in the repository, in repository order: id, name, position, velocity, visibility, deletion mark, inventory (every slot and every tool's wear), appearance (every part, colour and label), and what its kind adds -- a pawn's energy, raw metabolism, speed, nation, home, behaviour, target, mill target, relationships, conversation cooldowns, order, traits and settler flag; a settlement's colour, nation, residents, buildings, claim radius, siege text and its market with every stall and its prices (each item's pressure and last-round reference price, where they differ from the defaults); a caravan's nation, colour, home and market, whether it is outbound or returning, its escort, whether it is the player's, its takings, when it set out and which way it faces (its goods and purse are its inventory); the player's yaw, speeds, energy, metabolism, relationships, nation, home and status line; a sapling's planting tick and growth time; a chicken's wander state; a drop's stack |
| `orphanEntities` | entities something still holds that the repository no longer does, such as a pawn's target that was gathered and swept away the same tick; the pawn lets go on its next step, and must load holding it so that step happens the same way |
| `nations`, `orphanNations` | every nation in founding order (id, name, colour, leader, members, roles, settlements, stream), and any disbanded nation still recorded as laying a siege |
| `relations` | every pair's score, alliances and denunciations |
| `diplomacy` | the diplomacy round and every siege's progress and besieger |
| `population` | birth, immigration and settler cooldowns, the wanderer clock, the growth counters and the names growth has handed out |
| `events` | for each event type, how many there have been and the most recent `SavedEventsPerType` (100): description and date |
| `objectives` | where the player is in the objective list (current index, how many ambitions have been generated, the id of the current objective as a check), the progress shown on the HUD, and the last objective completed and when; the objectives themselves are code (`ObjectiveCatalog`) and are rebuilt to the saved length |
| `realm` | the law round; each nation's laws (setting, any pending change and when it was proclaimed, taxes collected, deserters); wars; truces and the tick each ends; the line of rulers (name, nation, start, end, cause); when the current reign began; whether the run is over; the latest succession notice and whether the host has shown it (format 2) |
| `worldEvents` | the world events' random stream, when the player's and the nations' next events are due, how many there have been and the player's last, the event waiting on the player's choice if any (its id, when it began and when it times out, and the nation, other nation, settlement, entity and number it was prepared with), and every running effect (drought, bandits) with its timers (format 3) |
| `chronicle` | every chronicle entry kept (up to 200: sequence, event type, tick, day, part of day, text, the nations and entities it names), how many have ever been recorded, nations founded, and the raids already told today |
| `trade` | when each nation last sent a caravan, the day the last notable haul was told, and how many caravans have been sent, have sold, have been robbed and have been escorted (format 4) |
| `eras` | (format 5) the era and the tick each was reached, the run's outcome, when it came and whether a victory has been played past, the collapse countdown, the nation's name and its peak members, settlements and funds, the history kept for the end-of-run screen (up to 400 lines) with the last chronicle entry copied into it, and the era each ambition was made in |

The player's meal and sale counters, which objectives read, are saved with the
player.

References between objects are by id (32 hex digits), and a load resolves
every reference to the one object it names. Entity and nation ids also
compare by value with `==` (`stall.getOwnerId() == pawn.getId()` holds for
two copies of one id), but entities and nations themselves are compared by
reference, so each must still load as one object.

Floats are written in their shortest round-trip form, so every value reads
back bit for bit. Enums are written by name, so adding a member never shifts
the meaning of an old save.

**Seasons and regions are derived, not saved** (no format change was needed
for them). The season is a pure function of the tick count (`SeasonCalendar`),
and a chunk's region a pure function of the world seed and the chunk's
coordinates (`RegionMap`), so a loaded world -- including any save written
before either existed -- rebuilds both exactly. That makes `RegionMap`'s noise
part of the save contract: changing it would change the regions of every saved
world. `TestRegionMap` pins seed 42's regions so it cannot change by accident.

What is not saved, because it is not world state: the wall-clock measurement
behind the debug overlay's ticks per second; the recording behind Share >
Export replay (`RunRecorder`, [SHARING.md](./SHARING.md)), which only reads the
world and starts afresh at the tick a world is loaded at; and the live objects a fresh event
points at (the pawn that died, the nation that formed). Loaded events keep
their type, description and date (`RestoredEvent`), and the per-type counts
stay whole even when older events are dropped.

### Random streams

`RandomSource` carries its own copy of the generator `System.Random` uses for a
seeded instance (Knuth's subtractive generator), so its state can be read and
written. It gives exactly the numbers `System.Random(seed)` does --
`TestRandomSource` checks 20,000 draws of every kind for seven seeds -- so no
seed's world changed when it was introduced.

## Integrity

Writing (`SaveSlots.write`):

1. The new save is checked (header, length, checksum) before anything is written.
2. If the slot holds a save made by a newer version, nothing is written and the
   player is told; that save is left alone.
3. The current save becomes the backup, but only if it is itself sound, so a
   damaged save never pushes out a good backup.
4. The new save is written to a temporary file, flushed to disk (`fsync`), and
   renamed over the old one. A crash at any point leaves the old save intact.
5. The written file is read back and compared byte for byte.

Reading (`SaveGame.decode`, `SaveSlots.load`):

- Not a save, damaged (wrong length, checksum mismatch), from a newer format,
  or a well-formed file whose world cannot be built: a `SaveFormatException`
  with a message for the player. Never a crash, and never a half-built world.
- A damaged save falls back to its backup, and the player is told the latest
  save was damaged and the one before it was loaded.
- Loading never writes anything.

## Versioning and migration

`SaveGame.CurrentFormatVersion` is the format this build writes; it is 5.

| Version | Change | Migration from the one before |
| --- | --- | --- |
| 1 | the first format | -- |
| 2 | adds `realm` (laws, wars and truces, succession) | add `realm` empty: no laws, wars, truces or past rulers, reign begun at tick 0, run not over, no notice, and the law round a world played to the saved tick would have reached (`totalTicks / 500`) |
| 3 | world events: the `worldEvents` section | add it empty: nothing pending or running; the events' clocks start from the first tick the loaded world runs, as a new world's do |
| 4 | trade caravans and living prices: the `trade` section, `prices` in every market, and caravan entities | adds `trade` with nothing sent, and every market's `prices` empty (every item at its plain supply-curve price, no trend yet); an old world has no caravans |
| 5 | eras and the end of a run: the `eras` section | add `eras` at the camp, with no outcome and an empty history; the era catches up with the nation one era a second as play resumes, and the chronicle's kept lines about the player are copied into the history |

- **Any change to what a save holds bumps the version**, including adding a
  section. A new section in an old save would otherwise be silently dropped by
  an older build that loaded it and saved again.
- **Every bump adds a migration step** to `SaveMigrations`: a function from the
  body of version N to the body of version N + 1 that sets `formatVersion`.
  Old saves are read by running every step from their version up, then
  decoding the result exactly as a save written today. For a new section the
  step is only "add it, empty".
- **Steps are never edited or removed once released**, and every version gets a
  fixture: when the version is current, run the tests once with
  `BN_WRITE_SAVE_FIXTURE=1` and commit the new `vN-seed42-tick2400.bnsave`.
  `TestSaveMigrations` fails if a version has no fixture or no step, and loads
  every fixture and plays it on.
- **A save from a newer version is refused**, with a message saying so, and the
  file is left as it is.

For example, a new system with state of its own goes in a new body section,
with a new format and a step that adds the section empty (or with whatever a
world that never had it should start with) -- as format 2 did for `realm`.

**Repairs on load.** A bug can leave a world in a state that the format
allows but the rules do not, and the autosave then carries it into every
load. Such a state is put right as the save is loaded, without a format bump:
the save itself is fine, only what it describes is not. Before 0.6.0 a serf
could buy a stall in a foreign market and become a merchant whose stall was
nowhere its nation lives, which crashed the game when it stood at home (the
0.6.0 release-candidate playtest, seed 102). On load (`StallOwnership`), a
stall kept by someone who is not of the settlement's nation is given up, and a
merchant left with no stall in any of its nation's settlements is a serf again.

## How it is tested

- **The round trip** (`TestSaveRoundTrip`, the key gate). A world is run for N
  ticks, saved, loaded into a fresh `Simulation`, and then the original and the
  copy are run side by side for M more ticks. They must be identical straight
  after the load and still identical after: `WorldComparer` walks every field
  of every object reachable from both simulations (every float bit for bit,
  every list in order, the same shape of shared references), and the
  determinism fingerprint (`TestSimulationDeterminism`'s, extended) must match
  too. It runs for seeds 42, 1, 7, 2026 and 9, from tick 0 to tick 15,000, and
  at the moments most likely to hide a forgotten field: mid-siege (the capture
  still lands on the same tick), mid-orders with crafted and worn tools,
  objectives part-way through with a chronicle written, a pawn holding a target
  that has been swept away, and a loaded world saved again.
  Because `Guid.NewGuid()` ids are the one thing no seed reproduces, after
  both worlds run on, ids from before the save must still be equal and ids made
  since need only correspond one to one.
- **Coverage** (`TestSaveCoverage`). Every field of every type a `Simulation`
  can hold, found by reflection, must be listed in `SaveCoverage` as saved,
  derived (rebuilt on load, and still compared by the round trip) or transient.
  A new field, or a new kind of entity, fails the test until someone decides
  how it is saved.
- **Integrity** (`TestSaveIntegrity`, `TestSaveSlots`): flipped bytes, truncated
  files, foreign files, newer versions, header and body that disagree, a world
  that checks out but cannot be built; backups, fallback to the backup, the
  newer-version slot left alone, atomic writes.
- **World events** (`TestWorldEvents`): a world with a pending choice, a bandit camp and a drought round-trips, and the original and the copy, given the same choice, run on the same; v1 and v2 saves load with the world events' clocks started from their first tick, and the v3 fixture loads and plays on.
- **Migration** (`TestSaveMigrations`): the committed v1 to v5 fixtures load,
  play 1,500 ticks on and save again; a save from before eras (v1 to v4) loads
  at the camp with no outcome; the migration chain runs steps in order and
  refuses a step that does not stamp its version.
- **The browser** (`smoke.mjs`): after playing, the smoke test pauses, presses
  Save, checks the save reached `localStorage`, goes to the main menu and
  presses Continue, and checks the world comes back at the tick it was saved.
