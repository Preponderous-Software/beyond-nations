using System.Collections.Generic;
using System.Text.Json;
namespace beyondnations {

    /**
    * The transient message shown to the player, and when it expires.
    *
    * Status used to own a TextGameObject, which owned a Unity Canvas and Text
    * component, so a status message could not exist without a graphics context.
    * It is now the message and its expiry and nothing else; drawing it is the
    * host's job, and moves to ImGui under #221.
    *
    * There is one line, so a message used to replace whatever was set before
    * it on the same tick, before the host had drawn a frame: the era tracker
    * announces "A new era: ..." (with the Town era's siege warning) and the
    * objective tracker, evaluated next, completes "Reach the Town era" and
    * overwrote it, so no era announcement was ever seen. Messages said
    * during one simulation step (Simulation.fixedUpdate, between beginTick
    * and endTick) are now joined into one line, in the order they were said.
    * Anything else -- a later tick, or a command the player gives, even
    * while the world is paused under a screen and the tick stands still --
    * replaces the line as before, so repeated commands still count "(x2)".
    * The parts are not saved: a loaded line is one part, and the next
    * message replaces it.
    */
    public class Status {
        private TickCounter tickCounter;
        private int ticksToExpire;
        private string status;
        private int tickLastSet = 0;
        private int duplicateMessageCount = 0;
        // the messages making up the line, oldest first; empty when the line is
        // one message nobody has added to ("Game started.", a loaded line).
        // Transient, like stepping and joinable: they matter only within a step.
        private readonly List<string> parts = new List<string>();
        // inside a simulation step
        private bool stepping = false;
        // the line was said during this step, so the next message joins it
        private bool joinable = false;
        // the line is a note (note()), which any message may replace
        private bool showingNote = false;

        public Status(TickCounter tickCounter, int ticksToExpire) {
            this.tickCounter = tickCounter;
            this.ticksToExpire = ticksToExpire;
            this.status = "Game started.";
        }

        public void update(string status) {
            if (joinable) {
                // another message was said this step and has not been drawn yet: keep both
                if (!parts.Contains(status)) {
                    parts.Add(status);
                    this.status = string.Join(" ", parts);
                }
                return;
            }
            if (getStatus().Contains(status)) {
                duplicateMessageCount += 1;
                status += " (x" + (duplicateMessageCount + 1) + ")";
            }
            else {
                duplicateMessageCount = 0;
            }
            replaceLine(status);
        }

        /**
        * Says something only worth a glance, such as "Autosaved.": shown when
        * the line is empty or holds another note, and otherwise dropped, so it
        * never takes the place of a message that matters. The autosave on the
        * tick the player's nation fell put "Autosaved." where "Greenshire has
        * captured your village! ... You are without a nation." had been, and
        * the player saw only the HUD change (playtest 0.6.3 s01).
        */
        public void note(string status) {
            if (!string.IsNullOrEmpty(this.status) && !showingNote) {
                return;
            }
            duplicateMessageCount = 0;
            replaceLine(status);
            showingNote = true;
        }

        /**
        * Says a "press again to confirm" prompt: like update, but asking again
        * is never counted, since "Click again to confirm: Raid. (x2)" read as
        * "click twice" (0.6.2 playtest).
        */
        public void prompt(string status) {
            if (joinable) {
                update(status);
                return;
            }
            duplicateMessageCount = 0;
            replaceLine(status);
        }

        /**
        * Takes back a message that no longer holds, putting replacement in its
        * place -- but only while it is still the whole line, so a newer
        * message (or an expired, empty line) is left alone. Returns whether
        * it was taken back. Used for a confirm prompt whose window lapsed
        * (0.6.1/0.6.2 playtests: "Click War again..." outlived the armed
        * button).
        */
        public bool withdraw(string message, string replacement) {
            if (status != message) {
                return false;
            }
            duplicateMessageCount = 0;
            replaceLine(replacement);
            return true;
        }

        /**
        * Replaces only the latest message, keeping any said beside it, for a
        * message that grows (DeathNotice's count); the line counts as set now.
        */
        public void amend(string status) {
            if (parts.Count == 0) {
                replaceLine(status);
                return;
            }
            parts[parts.Count - 1] = status;
            this.status = string.Join(" ", parts);
            tickLastSet = tickCounter.getTotalTicks();
            joinable = stepping;
        }

        /** The message said last: the whole line unless several were said on one tick. */
        public string getLatest() {
            return parts.Count > 0 ? parts[parts.Count - 1] : status;
        }

        /** Called by Simulation as a step starts: messages said until endTick share the line. */
        public void beginTick() {
            stepping = true;
            joinable = false;
        }

        /** Called by Simulation as a step ends: the next message replaces the line. */
        public void endTick() {
            stepping = false;
            joinable = false;
        }

        private void replaceLine(string status) {
            showingNote = false;
            parts.Clear();
            parts.Add(status);
            this.status = status;
            tickLastSet = tickCounter.getTotalTicks();
            joinable = stepping;
        }

        public void clearStatusIfExpired() {
            if (tickCounter.getTotalTicks() - tickLastSet > ticksToExpire) {
                this.status = "";
                parts.Clear();
                joinable = false;
                showingNote = false;
            }
        }

        public int getTicksToExpire() {
            return ticksToExpire;
        }

        public int getTickLastSet() {
            return tickLastSet;
        }

        public string getStatus() {
            return status;
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("ticksToExpire", ticksToExpire);
            writer.write("text", status);
            writer.write("tickLastSet", tickLastSet);
            writer.write("duplicateMessageCount", duplicateMessageCount);
        }

        internal void load(JsonElement element) {
            ticksToExpire = SaveReader.getInt(element, "ticksToExpire");
            status = SaveReader.getString(element, "text");
            tickLastSet = SaveReader.getInt(element, "tickLastSet");
            duplicateMessageCount = SaveReader.getInt(element, "duplicateMessageCount");
            // a loaded line is one message, as "Game started." is
            parts.Clear();
            joinable = false;
        }
    }
}
