# save

How a world is written down and read back. What a save holds, how it is
checked and how the format is versioned are in [docs/SAVES.md](../../../docs/SAVES.md);
this is how the code is laid out.

```
callers                 service                 core                    ports            adapters
-------                 -------                 ----                    -----            --------
Game.cs (desktop) ──┐                                                ┌─ ISaveStorage ◄── FileSaveStorage          (Core, desktop)
                    ├─► SaveSession ──► SaveSlots ── SaveGame ───────┤                ◄── LocalStorageSaveStorage (Web, browser)
WebGame.cs (browser)┘   (Desktop/ui,    (integrity    (encode,       └─ IClock ◄─────── SystemClock              (Core)
                         shared by web)  rules)        decode, check)
```

The hosts are the composition roots: each builds a `SaveSession` with its own
storage, and the session uses `SystemClock` unless it is given another clock.
Dependencies point inward: `model`, `core` and `ports` never name an adapter,
and never touch a file, the clock or the environment themselves.
`TestSaveLayers` fails the build if they do.

| Folder | Holds |
| --- | --- |
| `model/` | plain values: `SaveHeader`, `SaveSlotInfo`, `SaveLoadResult`, `SaveFormatException` |
| `core/` | policy, no I/O: `SaveSlots` (slots, backups, the integrity rules), `SaveGame`, `SaveWriter`, `SaveReader`, `SaveMigrations`, `Crc32` |
| `ports/` | one interface per file: `ISaveStorage`, `IClock` |
| `adapters/` | one implementation per file: `FileSaveStorage`, `SystemClock` |

| Port | Question it answers | Adapters | Test fakes |
| --- | --- | --- | --- |
| `ISaveStorage` | Where do a save's bytes live, written all-or-nothing? | `FileSaveStorage` (desktop: a directory, fsync and rename), `LocalStorageSaveStorage` (browser: gzipped localStorage, in BeyondNations.Web) | `MemorySaveStorage` |
| `IClock` | What time is it, in UTC? (a save's `savedAt` and a new world's slot name) | `SystemClock` | `FixedClock` |

The fakes are in `tests/BeyondNations.Core.Tests/save/fakes/`, and Desktop.Tests
compiles them in too. `SaveStorageContract` is what every storage promises,
and it runs against both `FileSaveStorage` and `MemorySaveStorage`.

**To add a storage backend:** write one class that implements `ISaveStorage`
(in the host that can reach the storage), add a three-line
`SaveStorageContract` subclass for it if a test can build one, and pass it to
`new SaveSession(...)` in that host.

`SaveGame.encode(simulation, version)` without a time still reads the
machine's clock. Tests and the pacing tool call it, and `SaveSession` doesn't:
it stamps saves through `IClock`.
