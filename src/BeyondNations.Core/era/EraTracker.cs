using System.Collections.Generic;
using System.Text.Json;

namespace beyondnations {

    /**
    * The arc of a run: which era the player's nation has reached, whether the
    * run has been won or lost, and the history the end-of-run screen tells.
    *
    * Every tick it applies the current era's rules to the nation the player
    * leads (which buildings its settlements may put up, how many settlements
    * it may hold), so a pawn building or founding for the player's nation
    * plays by them too. Once a second of game time it judges the nation:
    * advances the era when every requirement is met (one era at a time, so
    * each is announced), checks the victories, and counts down a collapse.
    * Eras never go back. Once the run has an outcome no other is recorded,
    * but a player who keeps playing after a victory still climbs the eras.
    *
    * Deterministic: it reads the world and draws no random numbers.
    */
    public class EraTracker {
        /** History lines kept for the end-of-run screen; the oldest go first. */
        public const int HistoryCapacity = 400;

        private Era era = Era.CAMP;
        // the tick each era was reached, -1 while it has not been
        private int[] reachedTicks = newReachedTicks();
        private RunOutcome outcome = RunOutcome.NONE;
        private int outcomeTick = -1;
        private bool outcomeAcknowledged = false;
        private bool everHeldSettlement = false;
        private int collapseSinceTick = -1;
        private string nationName = null;
        private int peakMembers = 0;
        private int peakSettlements = 0;
        private int peakFunds = 0;
        private List<HistoryEntry> history = new List<HistoryEntry>();
        private int lastChronicleSequence = -1;
        // the era each ambition was made in, by its index, so a save rebuilds the same list
        private List<Era> ambitionEras = new List<Era>();

        private static int[] newReachedTicks() {
            int[] ticks = new int[Eras.count()];
            for (int i = 0; i < ticks.Length; i++) {
                ticks[i] = -1;
            }
            ticks[0] = 0;
            return ticks;
        }

        public Era getEra() { return era; }
        public RunOutcome getOutcome() { return outcome; }
        public int getOutcomeTick() { return outcomeTick; }
        public bool hasEnded() { return outcome != RunOutcome.NONE; }
        public bool isOutcomeAcknowledged() { return outcomeAcknowledged; }
        /** The tick the era was reached, or -1. */
        public int getReachedTick(Era which) { return reachedTicks[(int) which]; }
        public int getCollapseSinceTick() { return collapseSinceTick; }
        /** The last name the player's nation went by, or null if the player never had one. */
        public string getNationName() { return nationName; }
        public int getPeakMembers() { return peakMembers; }
        public int getPeakSettlements() { return peakSettlements; }
        public int getPeakFunds() { return peakFunds; }
        public IReadOnlyList<HistoryEntry> getHistory() { return history; }

        /** True once, until acknowledged: the host shows the end-of-run screen. */
        public bool shouldShowOutcome() {
            return outcome != RunOutcome.NONE && !outcomeAcknowledged;
        }

        /**
        * "Continue playing" after a victory. A defeat cannot be played on
        * past, so it stays unacknowledged and the screen comes back.
        */
        public void acknowledgeOutcome() {
            if (VictoryRules.isVictory(outcome)) {
                outcomeAcknowledged = true;
            }
        }

        /** What the player's nation still needs to leave its era, measured now. */
        public List<EraRequirement> getRequirements(Simulation simulation) {
            return EraRules.requirementsToLeave(era, NationStanding.of(simulation), dayOf(simulation));
        }

        /** The in-game day, counted from 1, as the chronicle and the era requirements count it. */
        public static int dayOf(Simulation simulation) {
            return simulation.getTimeOfDay().getDay() + 1;
        }

        /** Makes the n-th ambition for the objective list, at the era it was first made in. */
        public Objective makeAmbition(int index) {
            Era madeIn;
            if (index < ambitionEras.Count) {
                madeIn = ambitionEras[index];
            } else {
                // ambitions are made strictly in order, so this is index == Count
                madeIn = era;
                ambitionEras.Add(madeIn);
            }
            return ObjectiveCatalog.ambition(index, madeIn);
        }

        /** Called by Simulation every tick. */
        public void update(Simulation simulation) {
            applyRules(simulation);
            warnOfSieges(simulation);
            // the player died with no heir (realm/Succession): the line, and the run, are over
            if (outcome == RunOutcome.NONE && simulation.getRealm().isRunOver()) {
                end(simulation, RunOutcome.DEFEAT);
            }
            TickCounter ticks = simulation.getTickCounter();
            if (ticks.getTotalTicks() % ticks.getTicksPerSecond() == 0) {
                evaluate(simulation);
            }
        }

        /**
        * Puts the era's limits on the nation the player leads and lifts them
        * from every other nation, so a nation the player leaves or loses is
        * free again. The shelter from sieges, and the warning when it ends,
        * cover the player's nation whether the player leads it or joined it:
        * either way its village is the player's first home.
        */
        public void applyRules(Simulation simulation) {
            Player player = simulation.getPlayer();
            Nation home = ObjectiveCatalog.playerNation(simulation);
            Nation led = home != null && home.getLeaderId() == player.getId() ? home : null;
            bool sheltered = EraRules.isSheltered(era);
            bool forewarned = getSiegeWarningTicksLeft(simulation) > 0;
            EntityRepository entities = simulation.getEntityRepository();
            foreach (Nation nation in simulation.getNationRepository().getNations()) {
                bool limited = nation == led;
                nation.setSettlementLimit(limited ? EraRules.settlementLimit(era) : 0);
                nation.setSheltered(nation == home && sheltered);
                nation.setForewarned(nation == home && forewarned);
                foreach (EntityId settlementId in nation.getSettlements()) {
                    if (entities.getEntity(settlementId) is Settlement settlement) {
                        settlement.setBuildingEra(limited ? era : (Era?) null);
                    }
                }
            }
        }

        /**
        * Judges the nation now: history, era, victory, collapse. Public so a
        * test can run it without waiting for the second to come round.
        */
        public void evaluate(Simulation simulation) {
            collectHistory(simulation);
            NationStanding standing = NationStanding.of(simulation);
            if (standing != null) {
                nationName = standing.nation.getName();
                peakMembers = System.Math.Max(peakMembers, standing.members);
                peakSettlements = System.Math.Max(peakSettlements, standing.settlements);
                peakFunds = System.Math.Max(peakFunds, standing.funds);
                if (standing.settlements > 0) {
                    everHeldSettlement = true;
                }
            }

            if (standing != null && EraRules.allMet(EraRules.requirementsToLeave(era, standing, dayOf(simulation)))) {
                advance(simulation, standing);
            }

            if (outcome != RunOutcome.NONE) {
                return;
            }
            RunOutcome victory = VictoryRules.checkVictory(simulation, era, standing);
            if (victory != RunOutcome.NONE) {
                end(simulation, victory);
                return;
            }
            checkCollapse(simulation, standing);
        }

        private void advance(Simulation simulation, NationStanding standing) {
            Era next = Eras.next(era).Value;
            era = next;
            reachedTicks[(int) next] = simulation.getTickCounter().getTotalTicks();
            string name = Eras.getDisplayName(next);
            addMilestone(simulation, standing.nation.getName() + " entered the " + name + " era.");
            string message = "A new era: " + standing.nation.getName() + " is now a " + name + ".";
            List<string> unlocks = EraRules.describeUnlocks(next);
            if (unlocks.Count > 0) {
                message += " Unlocked: " + string.Join(", ", unlocks) + ".";
            }
            if (EraRules.isSheltered(era - 1) && !EraRules.isSheltered(next)) {
                int minutes = EraRules.SiegeWarningTicks / simulation.getTickCounter().getTicksPerSecond() / 60;
                message += " Its shelter is ending: hostile neighbours may besiege its villages in " + minutes + " minutes.";
            }
            simulation.getPlayer().getStatus().update(message);
            Log.info(message);
            // the new limits apply at once, not a tick later
            applyRules(simulation);
        }

        private void end(Simulation simulation, RunOutcome result) {
            outcome = result;
            outcomeTick = simulation.getTickCounter().getTotalTicks();
            string name = nationName ?? "Your nation";
            string text = VictoryRules.isVictory(result)
                ? name + " won a " + VictoryRules.getDisplayName(result) + " victory."
                : simulation.getRealm().isRunOver()
                    ? "With no heir, the line of " + name + "'s rulers ended, and its story with it."
                    : name + " collapsed, and its story ends.";
            addMilestone(simulation, text);
            if (!simulation.getRealm().isRunOver()) {
                // the realm has already said how the line ended
                simulation.getPlayer().getStatus().update(text);
            }
            Log.info("run ended: " + result + " on tick " + outcomeTick);
        }

        private void checkCollapse(Simulation simulation, NationStanding standing) {
            if (!everHeldSettlement || !VictoryRules.isCollapsed(standing)) {
                if (collapseSinceTick >= 0) {
                    collapseSinceTick = -1;
                    simulation.getPlayer().getStatus().update("Your nation stands again.");
                }
                return;
            }
            int now = simulation.getTickCounter().getTotalTicks();
            if (collapseSinceTick < 0) {
                collapseSinceTick = now;
                int seconds = VictoryRules.CollapseGraceTicks / simulation.getTickCounter().getTicksPerSecond();
                simulation.getPlayer().getStatus().update("Your nation is collapsing! Found a settlement or win a follower within "
                    + (seconds / 60) + " minutes, or your story ends.");
                addMilestone(simulation, (nationName ?? "Your nation") + " lost its last settlement.");
                return;
            }
            if (now - collapseSinceTick >= VictoryRules.CollapseGraceTicks) {
                end(simulation, RunOutcome.DEFEAT);
            }
        }

        /**
        * Puts the run straight at an era and an outcome, as EndingShowcase
        * does for --stage-victory and --stage-defeat screenshots: each era up
        * to the given one is recorded as reached now, with its milestone, and
        * the outcome is recorded as play would record it. Not used by play.
        */
        public void stageEnding(Simulation simulation, Era reach, RunOutcome result) {
            NationStanding standing = NationStanding.of(simulation);
            if (standing != null) {
                nationName = standing.nation.getName();
            }
            // a staged ending skips the calendar as well as the rest
            while (era < reach && standing != null) {
                advance(simulation, standing);
            }
            if (result != RunOutcome.NONE && outcome == RunOutcome.NONE) {
                end(simulation, result);
            }
        }

        /**
        * Ticks left of the warning the player is given when the Town era ends
        * the shelter (EraRules.SiegeWarningTicks), or -1 while the nation is
        * still sheltered or the warning is over. Worked out from the tick the
        * Town era was reached, which is saved, so it needs no state of its own.
        */
        public int getSiegeWarningTicksLeft(Simulation simulation) {
            int reached = reachedTicks[(int) Era.TOWN];
            if (EraRules.isSheltered(era) || reached < 0) {
                return -1;
            }
            int left = reached + EraRules.SiegeWarningTicks - simulation.getTickCounter().getTotalTicks();
            return left > 0 ? left : -1;
        }

        /** A minute before the warning runs out, and when it does, the status line says so. */
        private void warnOfSieges(Simulation simulation) {
            int reached = reachedTicks[(int) Era.TOWN];
            if (EraRules.isSheltered(era) || reached < 0 || ObjectiveCatalog.playerNation(simulation) == null) {
                return;
            }
            int now = simulation.getTickCounter().getTotalTicks();
            int lastMinute = 60 * simulation.getTickCounter().getTicksPerSecond();
            if (EraRules.SiegeWarningTicks > lastMinute && now == reached + EraRules.SiegeWarningTicks - lastMinute) {
                simulation.getPlayer().getStatus().update("In 1 minute hostile neighbours may besiege your villages. "
                    + "Ally, gift, recruit or grow them into towns to hold them.");
            }
            else if (now == reached + EraRules.SiegeWarningTicks) {
                simulation.getPlayer().getStatus().update("Your nation's shelter is over: hostile neighbours may now besiege its villages. "
                    + "Towns are safe behind their palisades.");
            }
        }

        /** Ticks left before a collapse ends the run, or -1 when none is under way. */
        public int getCollapseTicksLeft(Simulation simulation) {
            if (collapseSinceTick < 0 || outcome != RunOutcome.NONE) {
                return -1;
            }
            return System.Math.Max(0, VictoryRules.CollapseGraceTicks - (simulation.getTickCounter().getTotalTicks() - collapseSinceTick));
        }

        /** Copies the chronicle's new lines about the player or their nation. */
        private void collectHistory(Simulation simulation) {
            Chronicle chronicle = simulation.getChronicle();
            if (chronicle == null) {
                return;
            }
            Player player = simulation.getPlayer();
            foreach (ChronicleEntry entry in chronicle.getEntries()) {
                if (entry.getSequence() <= lastChronicleSequence) {
                    continue;
                }
                lastChronicleSequence = entry.getSequence();
                // the nation's own lines even once it has fallen: its capture
                // used to be left out of the Defeat screen's history (0.6.3 s07)
                if (chronicle.concernsPlayer(entry, player)) {
                    addHistory(HistoryEntry.of(entry));
                }
            }
        }

        private void addMilestone(Simulation simulation, string text) {
            TimeOfDay time = simulation.getTimeOfDay();
            addHistory(new HistoryEntry(simulation.getTickCounter().getTotalTicks(), time.getDay() + 1,
                Chronicle.partOfDay(time.getFraction()), text, true));
        }

        private void addHistory(HistoryEntry entry) {
            history.Add(entry);
            if (history.Count > HistoryCapacity) {
                history.RemoveAt(0);
            }
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.writeEnum("era", era);
            writer.beginArray("reachedTicks");
            foreach (int tick in reachedTicks) {
                writer.writeValue(tick);
            }
            writer.endArray();
            writer.writeEnum("outcome", outcome);
            writer.write("outcomeTick", outcomeTick);
            writer.write("outcomeAcknowledged", outcomeAcknowledged);
            writer.write("everHeldSettlement", everHeldSettlement);
            writer.write("collapseSinceTick", collapseSinceTick);
            writer.write("nationName", nationName);
            writer.write("peakMembers", peakMembers);
            writer.write("peakSettlements", peakSettlements);
            writer.write("peakFunds", peakFunds);
            writer.write("lastChronicleSequence", lastChronicleSequence);
            writer.beginArray("history");
            foreach (HistoryEntry entry in history) {
                writer.beginObject();
                entry.save(writer);
                writer.endObject();
            }
            writer.endArray();
            writer.beginArray("ambitionEras");
            foreach (Era made in ambitionEras) {
                writer.writeValue(made.ToString());
            }
            writer.endArray();
        }

        /** Read before the objectives, which ask makeAmbition for every ambition already made. */
        internal void load(JsonElement element) {
            era = SaveReader.getEnum<Era>(element, "era");
            List<int> ticks = new List<int>();
            foreach (JsonElement value in SaveReader.property(element, "reachedTicks").EnumerateArray()) {
                ticks.Add(value.GetInt32());
            }
            if (ticks.Count != Eras.count()) {
                throw new SaveFormatException("the save's eras do not match this version's");
            }
            reachedTicks = ticks.ToArray();
            outcome = SaveReader.getEnum<RunOutcome>(element, "outcome");
            outcomeTick = SaveReader.getInt(element, "outcomeTick");
            outcomeAcknowledged = SaveReader.getBool(element, "outcomeAcknowledged");
            everHeldSettlement = SaveReader.getBool(element, "everHeldSettlement");
            collapseSinceTick = SaveReader.getInt(element, "collapseSinceTick");
            nationName = SaveReader.getString(element, "nationName");
            peakMembers = SaveReader.getInt(element, "peakMembers");
            peakSettlements = SaveReader.getInt(element, "peakSettlements");
            peakFunds = SaveReader.getInt(element, "peakFunds");
            lastChronicleSequence = SaveReader.getInt(element, "lastChronicleSequence");
            history = new List<HistoryEntry>();
            foreach (JsonElement saved in SaveReader.property(element, "history").EnumerateArray()) {
                history.Add(HistoryEntry.load(saved));
            }
            ambitionEras = new List<Era>();
            foreach (JsonElement saved in SaveReader.property(element, "ambitionEras").EnumerateArray()) {
                ambitionEras.Add(SaveReader.toEnum<Era>(saved));
            }
        }

        /**
        * The section a save from before eras (format 1) is given: the camp,
        * no outcome, an empty history. The chronicle's kept lines about the
        * player are copied in on the first evaluation, and the era catches up
        * with the nation one era a second.
        */
        internal static System.Text.Json.Nodes.JsonObject defaultSection() {
            System.Text.Json.Nodes.JsonArray ticks = new System.Text.Json.Nodes.JsonArray();
            foreach (int tick in newReachedTicks()) {
                ticks.Add(tick);
            }
            return new System.Text.Json.Nodes.JsonObject {
                ["era"] = Era.CAMP.ToString(),
                ["reachedTicks"] = ticks,
                ["outcome"] = RunOutcome.NONE.ToString(),
                ["outcomeTick"] = -1,
                ["outcomeAcknowledged"] = false,
                ["everHeldSettlement"] = false,
                ["collapseSinceTick"] = -1,
                ["nationName"] = null,
                ["peakMembers"] = 0,
                ["peakSettlements"] = 0,
                ["peakFunds"] = 0,
                ["lastChronicleSequence"] = -1,
                ["history"] = new System.Text.Json.Nodes.JsonArray(),
                ["ambitionEras"] = new System.Text.Json.Nodes.JsonArray(),
            };
        }
    }
}
