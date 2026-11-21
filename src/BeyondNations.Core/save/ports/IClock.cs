using System;

namespace beyondnations {

    /**
    * What time it is, for stamping a save and naming a new world's slot
    * (docs/SAVES.md: `savedAt`, "world-42-20261018-142233").
    *
    * Contract: utcNow() returns a DateTime of kind Utc. It never blocks and
    * never throws, and may be called from any thread. Successive calls need
    * not move forward -- a test clock may stand still -- so nothing may rely
    * on two saves having different times.
    *
    * The simulation itself never reads this: game time is ticks
    * (TickCounter). This is wall-clock time, for the player's benefit only.
    */
    public interface IClock {
        DateTime utcNow();
    }
}
