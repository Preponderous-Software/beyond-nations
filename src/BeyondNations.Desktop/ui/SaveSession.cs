using System;
using System.Collections.Generic;
using System.Globalization;
using beyondnations;

namespace beyondnations.desktop.ui {

    /**
    * The host's side of saving (docs/SAVES.md), shared by the desktop and the
    * browser: which slot the world in play belongs to, when it was last
    * saved, the autosave clock, and the last thing to tell the player about
    * it. The format itself, and every integrity rule, is Core's (SaveGame,
    * SaveSlots); this only decides when to save and what to say.
    *
    * Every call that touches storage catches what it throws: a save that
    * fails is reported on the pause screen and the status line, never a crash.
    */
    public class SaveSession {
        /** Two minutes of play at the default 50 ticks per second. */
        public const int AutosaveTicks = 6000;
        public const string RunOverFeedback = "Your line has ended, so this world is not saved again; Load brings back the last save.";

        private readonly SaveSlots slots;
        private readonly string gameVersion;
        private readonly bool enabled;

        private string currentSlot;
        private int lastSaveTick;
        private string feedback;
        private bool feedbackIsError;
        private List<SaveSlotInfo> listing;
        // every save that reached the disk, manual or automatic (--playtest-out reports them)
        private int savesWritten;

        public SaveSession(ISaveStorage storage, string gameVersion, bool enabled) {
            this.slots = new SaveSlots(storage);
            this.gameVersion = gameVersion;
            this.enabled = enabled;
        }

        public bool isEnabled() {
            return enabled;
        }

        public string getCurrentSlot() {
            return currentSlot;
        }

        /** What the pause screen says about saving: the last save, or why it failed. Null before either. */
        public string getFeedback() {
            return feedback;
        }

        /** How many saves have reached the disk this run, autosaves included. */
        public int getSavesWritten() {
            return savesWritten;
        }

        public bool isFeedbackError() {
            return feedbackIsError;
        }

        /** A new world gets a slot of its own; nothing is written until it is first saved. */
        public void beginNewWorld(Simulation simulation, DateTime nowUtc) {
            currentSlot = SaveSlots.newSlotName(simulation.getRandom().getSeed(), nowUtc);
            lastSaveTick = simulation.getTickCounter().getTotalTicks();
            feedback = null;
            feedbackIsError = false;
        }

        /** Saves the world in play to its slot. Returns whether it worked; the reason is in getFeedback(). */
        public bool save(Simulation simulation, bool automatic) {
            if (!enabled || simulation == null || currentSlot == null) {
                return false;
            }
            // a run whose line has ended is never saved: the last save, from
            // before the death, is what Load brings back (realm/Succession)
            if (simulation.getRealm().isRunOver()) {
                feedback = RunOverFeedback;
                feedbackIsError = false;
                return false;
            }
            try {
                SaveHeader header = slots.write(currentSlot, SaveGame.encode(simulation, gameVersion));
                lastSaveTick = simulation.getTickCounter().getTotalTicks();
                savesWritten++;
                feedback = (automatic ? "Autosaved: " : "Saved: ") + header.describe() + ", "
                    + header.SavedAtUtc.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture) + ".";
                feedbackIsError = false;
                listing = null;
                Log.info("save: " + currentSlot + " (" + header.describe() + ", tick " + header.Tick + ")");
                return true;
            } catch (Exception e) {
                feedback = "Could not save: " + e.Message
                    + (e.Message.Contains("full") ? ". Delete an old world from Load to make room." : "");
                feedbackIsError = true;
                // try again in a while rather than every tick
                lastSaveTick = simulation.getTickCounter().getTotalTicks();
                Log.error("save failed: " + e);
                return false;
            }
        }

        /** Called every fixed step; saves once AutosaveTicks have passed since the last save. */
        public void onFixedStep(Simulation simulation) {
            if (!enabled || simulation == null || currentSlot == null) {
                return;
            }
            if (simulation.getTickCounter().getTotalTicks() - lastSaveTick >= AutosaveTicks) {
                if (save(simulation, true)) {
                    simulation.getPlayer().getStatus().update("Autosaved.");
                }
            }
        }

        /**
        * Loads a slot, or returns null with the reason in getFeedback(). On
        * success the loaded world becomes the one in play, saving to the same
        * slot.
        */
        public Simulation load(string slot) {
            try {
                SaveLoadResult result = slots.load(slot);
                currentSlot = slot;
                lastSaveTick = result.Simulation.getTickCounter().getTotalTicks();
                feedbackIsError = false;
                if (result.FromBackup) {
                    feedback = "The latest save was damaged (" + result.Problem + ") so the one before it was loaded.";
                    result.Simulation.getPlayer().getStatus().update("Loaded the previous save: the latest one was damaged.");
                } else {
                    feedback = null;
                    result.Simulation.getPlayer().getStatus().update("Welcome back. " + result.Header.describe() + ".");
                }
                Log.info("load: " + slot + (result.FromBackup ? " (from its backup)" : ""));
                return result.Simulation;
            } catch (Exception e) {
                feedback = e is SaveFormatException ? e.Message : "Could not load: " + e.Message;
                feedbackIsError = true;
                Log.error("load failed: " + e);
                return null;
            }
        }

        /** Every slot, newest first (read once, then again only after a save). */
        public List<SaveSlotInfo> list() {
            if (listing == null) {
                try {
                    listing = enabled ? slots.list() : new List<SaveSlotInfo>();
                } catch (Exception e) {
                    Log.error("could not list saves: " + e.Message);
                    listing = new List<SaveSlotInfo>();
                }
            }
            return listing;
        }

        /** Deletes a saved world, because the player asked to from the Load list. */
        public void delete(string slot) {
            try {
                slots.delete(slot);
                Log.info("save: deleted " + slot);
            } catch (Exception e) {
                feedback = "Could not delete: " + e.Message;
                feedbackIsError = true;
            }
            listing = null;
        }

        /** Forget the cached listing, so the next list() reads storage again. */
        public void refresh() {
            listing = null;
        }

        public SaveSlotInfo latest() {
            foreach (SaveSlotInfo info in list()) {
                if (info.isLoadable()) {
                    return info;
                }
            }
            return null;
        }

        /** One line for a slot in the Load list: "Day 3 - Xland  2026-10-18 14:22". */
        public static string describe(SaveSlotInfo info) {
            if (!info.isLoadable()) {
                return info.Slot + ": " + info.Problem;
            }
            string line = info.Header.describe() + "   " + info.Header.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            return info.FromBackup ? line + " (backup)" : line;
        }
    }
}
