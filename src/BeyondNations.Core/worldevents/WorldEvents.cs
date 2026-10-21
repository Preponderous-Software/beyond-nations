using System;
using System.Collections.Generic;
using System.Text.Json;

namespace beyondnations {

    /**
    * Throws the world events: droughts, sicknesses, bandits, claimants,
    * strangers at the gate. It is what makes two runs of a world diverge in
    * the player's hands while staying exactly reproducible for a seed.
    *
    * Two clocks, both on the events' own random stream so that nothing here
    * shifts the rest of a seeded world:
    *
    *   - the player's: about every PLAYER_EVENT_MIN_SECONDS to
    *     PLAYER_EVENT_MAX_SECONDS (10 to 15 minutes of play) an event is
    *     offered to the player, chosen by weight from the personal events and,
    *     if the player leads a nation, that nation's events. It waits as a
    *     pending choice; the host pauses the world while the choice sheet is
    *     up. Unanswered after CHOICE_TIMEOUT_SECONDS of ticks (only possible
    *     when nothing is drawing the sheet, as in a headless run) the event's
    *     default choice is taken.
    *   - the world's: every few minutes a nation not led by the player meets
    *     an event and its leader chooses at once. That is news in the
    *     chronicle, and it changes the world the player lives in.
    *
    * Some outcomes last (WorldEffect): a drought that stops a nation's farms,
    * a bandit camp that robs a settlement until it is dealt with. Pending
    * choices, effects and both clocks are saved.
    */
    public class WorldEvents {
        public const int PLAYER_EVENT_MIN_SECONDS = 600;
        public const int PLAYER_EVENT_MAX_SECONDS = 900;
        public const int FIRST_PLAYER_EVENT_MIN_SECONDS = 420;
        public const int FIRST_PLAYER_EVENT_MAX_SECONDS = 660;
        public const int WORLD_EVENT_MIN_SECONDS = 240;
        public const int WORLD_EVENT_MAX_SECONDS = 420;
        /** When nothing fits the player's situation, look again this much later. */
        public const int RETRY_SECONDS = 60;
        public const int CHOICE_TIMEOUT_SECONDS = 180;
        /** How often bandits rob the settlement they camp by. */
        public const int BANDIT_THEFT_SECONDS = 60;
        public const int BANDIT_THEFT_COINS = 5;
        public const int BANDIT_THEFT_GOODS = 5;

        // the stream is seeded from the world seed so a migrated save is reproducible too
        public const int StreamSalt = 0x3E7E47;

        private readonly WorldEventRegistry registry;
        private readonly RandomSource random;
        private int nextPlayerEventTick;
        private int nextWorldEventTick;
        private int numEvents;
        private int numPlayerEvents;
        private string lastPlayerEventId;
        private WorldEventInstance pending;
        private readonly List<WorldEffect> effects = new List<WorldEffect>();

        public WorldEvents(int worldSeed, int ticksPerSecond) : this(worldSeed, ticksPerSecond, WorldEventRegistry.createDefault()) {
        }

        public WorldEvents(int worldSeed, int ticksPerSecond, WorldEventRegistry registry) {
            this.registry = registry;
            this.random = new RandomSource(worldSeed ^ StreamSalt);
            // the clocks start on the first tick the world runs, so a world opened
            // part-way through its year (a starting season) does not meet an event at once
            nextPlayerEventTick = Unscheduled;
            nextWorldEventTick = Unscheduled;
        }

        /** A clock that has not started yet: it starts from the next tick the world runs. */
        public const int Unscheduled = -1;

        /** Sets both clocks counting from a tick: a new world, or a save made before world events. */
        private void scheduleFrom(int tick, int ticksPerSecond) {
            nextPlayerEventTick = tick + random.range(FIRST_PLAYER_EVENT_MIN_SECONDS, FIRST_PLAYER_EVENT_MAX_SECONDS + 1) * ticksPerSecond;
            nextWorldEventTick = tick + random.range(WORLD_EVENT_MIN_SECONDS, WORLD_EVENT_MAX_SECONDS + 1) * ticksPerSecond;
        }

        public WorldEventRegistry getRegistry() { return registry; }
        public RandomSource getRandom() { return random; }
        public int getNextPlayerEventTick() { return nextPlayerEventTick; }
        public int getNextWorldEventTick() { return nextWorldEventTick; }
        /** Every world event that has happened, the player's and the nations'. */
        public int getNumEvents() { return numEvents; }
        public int getNumPlayerEvents() { return numPlayerEvents; }
        public IReadOnlyList<WorldEffect> getEffects() { return effects; }

        /** The event waiting for the player to choose, or null. */
        public WorldEventInstance getPending() { return pending; }

        public bool isAwaitingPlayer() { return pending != null; }

        /** Whether the nation's farms are dried up (Simulation skips their harvest). */
        public bool isDrought(NationId nationId) {
            return nationId != null && findEffect(WorldEffectKind.DROUGHT, nationId, null) != null;
        }

        public bool hasBandits(EntityId settlementId) {
            return settlementId != null && findEffect(WorldEffectKind.BANDITS, null, settlementId) != null;
        }

        public WorldEffect findEffect(WorldEffectKind kind, NationId nationId, EntityId settlementId) {
            foreach (WorldEffect effect in effects) {
                if (effect.getKind() != kind) {
                    continue;
                }
                if (nationId != null && !nationId.Equals(effect.getNationId())) {
                    continue;
                }
                if (settlementId != null && !settlementId.Equals(effect.getSettlementId())) {
                    continue;
                }
                return effect;
            }
            return null;
        }

        public void addEffect(WorldEffect effect) {
            effects.Add(effect);
        }

        public void removeEffect(WorldEffect effect) {
            effects.Remove(effect);
        }

        // --- the clock ------------------------------------------------------

        /** Called once a tick by the simulation. */
        public void update(Simulation simulation) {
            int tick = simulation.getTickCounter().getTotalTicks();
            if (nextPlayerEventTick == Unscheduled || nextWorldEventTick == Unscheduled) {
                scheduleFrom(tick, ticksPerSecond(simulation));
            }
            WorldEventContext context = new WorldEventContext(simulation, this);
            updateEffects(context, tick);

            if (pending != null) {
                if (tick >= pending.getDeadlineTick()) {
                    IWorldEvent worldEvent = registry.get(pending.getEventId());
                    int choice = worldEvent == null ? -1 : worldEvent.getDefaultChoice(context, pending);
                    resolvePending(context, choice);
                }
            }
            else if (tick >= nextPlayerEventTick) {
                if (startPlayerEvent(context)) {
                    nextPlayerEventTick = tick + random.range(PLAYER_EVENT_MIN_SECONDS, PLAYER_EVENT_MAX_SECONDS + 1) * ticksPerSecond(simulation);
                } else {
                    nextPlayerEventTick = tick + RETRY_SECONDS * ticksPerSecond(simulation);
                }
            }

            if (tick >= nextWorldEventTick) {
                runNationEvent(context);
                nextWorldEventTick = tick + random.range(WORLD_EVENT_MIN_SECONDS, WORLD_EVENT_MAX_SECONDS + 1) * ticksPerSecond(simulation);
            }
        }

        private static int ticksPerSecond(Simulation simulation) {
            return simulation.getTickCounter().getTicksPerSecond();
        }

        /**
        * Offers the player an event now, whatever the clock says: the host's
        * --stage-event flag, and tests. Prefers the event with the given id
        * when it can happen. Returns whether one was offered.
        */
        public bool startPlayerEventNow(Simulation simulation, string preferredId = null) {
            if (pending != null) {
                return false;
            }
            WorldEventContext context = new WorldEventContext(simulation, this);
            if (preferredId != null) {
                IWorldEvent preferred = registry.get(preferredId);
                Nation nation = preferred == null ? null : playerLedNation(context);
                if (preferred != null && preferred.getWeight(context, preferred.isPersonal() ? null : nation) > 0
                        && (preferred.isPersonal() || nation != null) && offer(context, preferred, preferred.isPersonal() ? null : nation)) {
                    return true;
                }
            }
            return startPlayerEvent(context);
        }

        /**
        * The settlements of the nation the player leads that have a bandit
        * camp outside them, in the nation's order: the camps the player can
        * still deal with (offerStandingBanditCamp). Empty when the player
        * leads no nation.
        */
        public List<Settlement> getStandingBanditCamps(Simulation simulation) {
            List<Settlement> camped = new List<Settlement>();
            WorldEventContext context = new WorldEventContext(simulation, this);
            foreach (Settlement settlement in context.settlementsOf(playerLedNation(context))) {
                if (hasBandits(settlement.getId())) {
                    camped.Add(settlement);
                }
            }
            return camped;
        }

        /**
        * Puts a bandit camp the player waited on back in front of them as a
        * choice: drive the bandits off, pay them, or keep the gates barred.
        * Barring the gates is not final; until the camp moves on, its leader
        * can still deal with it. Off the event clock and the event stream: it
        * draws no random numbers and does not count as a new event. Returns
        * whether the choice was offered (not when another choice is waiting,
        * there is no camp there, or the player does not lead its nation).
        */
        public bool offerStandingBanditCamp(Simulation simulation, EntityId settlementId) {
            if (pending != null || !hasBandits(settlementId)) {
                return false;
            }
            IWorldEvent bandits = registry.get(BanditCampEvent.Id);
            WorldEventContext context = new WorldEventContext(simulation, this);
            Settlement settlement = context.getSettlement(settlementId);
            Nation nation = settlement == null ? null : context.getNation(settlement.getNationId());
            if (bandits == null || !context.isLedByPlayer(nation)) {
                return false;
            }
            WorldEventInstance instance = new WorldEventInstance(BanditCampEvent.Id, numEvents, context.getTick(), true);
            instance.setNationId(nation.getId());
            instance.setSettlementId(settlementId);
            instance.setMagnitude(BanditCampEvent.StandingCamp);
            instance.setDeadlineTick(context.getTick() + context.ticks(CHOICE_TIMEOUT_SECONDS));
            pending = instance;
            return true;
        }

        private Nation playerLedNation(WorldEventContext context) {
            Nation nation = context.getNation(context.getPlayer().getNationId());
            return context.isLedByPlayer(nation) ? nation : null;
        }

        private bool startPlayerEvent(WorldEventContext context) {
            Nation nation = playerLedNation(context);
            List<IWorldEvent> candidates = new List<IWorldEvent>();
            List<int> weights = new List<int>();
            foreach (IWorldEvent worldEvent in registry.getAll()) {
                Nation target = worldEvent.isPersonal() ? null : nation;
                if (!worldEvent.isPersonal() && nation == null) {
                    continue;
                }
                int weight = worldEvent.getWeight(context, target);
                if (weight > 0) {
                    candidates.Add(worldEvent);
                    weights.Add(weight);
                }
            }
            // not the same event twice running, when there is anything else
            if (candidates.Count > 1) {
                int last = candidates.FindIndex(e => e.getId() == lastPlayerEventId);
                if (last >= 0) {
                    candidates.RemoveAt(last);
                    weights.RemoveAt(last);
                }
            }
            while (candidates.Count > 0) {
                int index = pickWeighted(weights);
                IWorldEvent chosen = candidates[index];
                if (offer(context, chosen, chosen.isPersonal() ? null : nation)) {
                    return true;
                }
                candidates.RemoveAt(index);
                weights.RemoveAt(index);
            }
            return false;
        }

        private bool offer(WorldEventContext context, IWorldEvent worldEvent, Nation nation) {
            WorldEventInstance instance = new WorldEventInstance(worldEvent.getId(), numEvents, context.getTick(), true);
            instance.setNationId(nation == null ? null : nation.getId());
            if (!worldEvent.prepare(context, nation, instance)) {
                return false;
            }
            instance.setDeadlineTick(context.getTick() + context.ticks(CHOICE_TIMEOUT_SECONDS));
            pending = instance;
            numEvents++;
            numPlayerEvents++;
            lastPlayerEventId = worldEvent.getId();
            Log.info("world event for the player: " + worldEvent.getId());
            return true;
        }

        /**
        * A nation the player does not lead meets an event, and its leader
        * chooses. Returns the instance, or null if nothing happened.
        */
        public WorldEventInstance runNationEvent(Simulation simulation) {
            return runNationEvent(new WorldEventContext(simulation, this));
        }

        private WorldEventInstance runNationEvent(WorldEventContext context) {
            List<Nation> nations = new List<Nation>();
            foreach (Nation nation in context.getSimulation().getNationRepository().getNations()) {
                if (!context.isLedByPlayer(nation) && nation.getNumberOfSettlements() > 0) {
                    nations.Add(nation);
                }
            }
            if (nations.Count == 0) {
                return null;
            }
            Nation target = nations[random.range(0, nations.Count)];
            List<IWorldEvent> candidates = new List<IWorldEvent>();
            List<int> weights = new List<int>();
            foreach (IWorldEvent worldEvent in registry.getAll()) {
                if (worldEvent.isPersonal()) {
                    continue;
                }
                int weight = worldEvent.getWeight(context, target);
                if (weight > 0) {
                    candidates.Add(worldEvent);
                    weights.Add(weight);
                }
            }
            while (candidates.Count > 0) {
                int index = pickWeighted(weights);
                IWorldEvent chosen = candidates[index];
                WorldEventInstance instance = new WorldEventInstance(chosen.getId(), numEvents, context.getTick(), false);
                instance.setNationId(target.getId());
                if (chosen.prepare(context, target, instance)) {
                    numEvents++;
                    int choice = chosen.chooseForNation(context, instance);
                    List<WorldEventChoice> choices = chosen.getChoices(context, instance);
                    if (choice < 0 || choice >= choices.Count || !choices[choice].isAvailable()) {
                        choice = chosen.getDefaultChoice(context, instance);
                    }
                    apply(context, chosen, instance, choice);
                    return instance;
                }
                candidates.RemoveAt(index);
                weights.RemoveAt(index);
            }
            return null;
        }

        private int pickWeighted(List<int> weights) {
            int total = 0;
            foreach (int weight in weights) {
                total += weight;
            }
            int roll = random.range(0, total);
            for (int i = 0; i < weights.Count; i++) {
                roll -= weights[i];
                if (roll < 0) {
                    return i;
                }
            }
            return weights.Count - 1;
        }

        // --- choosing -------------------------------------------------------

        /** The pending event's kind, or null. */
        public IWorldEvent getPendingEvent() {
            return pending == null ? null : registry.get(pending.getEventId());
        }

        public string describePending(Simulation simulation) {
            IWorldEvent worldEvent = getPendingEvent();
            return worldEvent == null ? null : worldEvent.describe(new WorldEventContext(simulation, this), pending);
        }

        public List<WorldEventChoice> getPendingChoices(Simulation simulation) {
            IWorldEvent worldEvent = getPendingEvent();
            return worldEvent == null ? new List<WorldEventChoice>() : worldEvent.getChoices(new WorldEventContext(simulation, this), pending);
        }

        /**
        * The player takes a choice on the pending event. Returns the line
        * saying how it turned out, or null (changing nothing) if there is no
        * pending event or the choice cannot be taken.
        */
        public string choose(Simulation simulation, int choice) {
            if (pending == null) {
                return null;
            }
            WorldEventContext context = new WorldEventContext(simulation, this);
            List<WorldEventChoice> choices = getPendingChoices(simulation);
            if (choice < 0 || choice >= choices.Count || !choices[choice].isAvailable()) {
                return null;
            }
            return resolvePending(context, choice);
        }

        private string resolvePending(WorldEventContext context, int choice) {
            WorldEventInstance instance = pending;
            pending = null;
            IWorldEvent worldEvent = registry.get(instance.getEventId());
            if (worldEvent == null) {
                // an event a later build no longer has: let it pass
                return null;
            }
            List<WorldEventChoice> choices = worldEvent.getChoices(context, instance);
            if (choice < 0 || choice >= choices.Count || !choices[choice].isAvailable()) {
                // the default became impossible (what it needed is gone): take the first that is not
                choice = choices.FindIndex(c => c.isAvailable());
                if (choice < 0) {
                    return null;
                }
            }
            string outcome = apply(context, worldEvent, instance, choice);
            if (outcome != null) {
                context.tellPlayer(outcome);
            }
            return outcome;
        }

        private string apply(WorldEventContext context, IWorldEvent worldEvent, WorldEventInstance instance, int choice) {
            string text = worldEvent.resolve(context, instance, choice);
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            text = char.ToUpperInvariant(text[0]) + text.Substring(1);
            record(context, worldEvent.getId(), text, instance);
            return text;
        }

        /** Writes a line to the chronicle, naming what the instance is about. */
        internal void record(WorldEventContext context, string eventId, string text, WorldEventInstance instance) {
            List<NationId> nations = new List<NationId>();
            List<EntityId> entities = new List<EntityId>();
            if (instance != null) {
                if (instance.getNationId() != null) nations.Add(instance.getNationId());
                if (instance.getOtherNationId() != null) nations.Add(instance.getOtherNationId());
                if (instance.getSettlementId() != null) entities.Add(instance.getSettlementId());
                if (instance.getEntityId() != null) entities.Add(instance.getEntityId());
                if (instance.isForPlayer()) entities.Add(context.getPlayer().getId());
            }
            text = char.ToUpperInvariant(text[0]) + text.Substring(1);
            context.getSimulation().getEventProducer().produceWorldEventEvent(eventId, text, nations, entities);
        }

        // --- lasting effects ------------------------------------------------

        private void updateEffects(WorldEventContext context, int tick) {
            if (effects.Count == 0) {
                return;
            }
            foreach (WorldEffect effect in new List<WorldEffect>(effects)) {
                switch (effect.getKind()) {
                    case WorldEffectKind.DROUGHT:
                        updateDrought(context, effect, tick);
                        break;
                    case WorldEffectKind.BANDITS:
                        updateBandits(context, effect, tick);
                        break;
                }
            }
        }

        private void updateDrought(WorldEventContext context, WorldEffect effect, int tick) {
            Nation nation = context.getNation(effect.getNationId());
            if (nation == null) {
                effects.Remove(effect);
                return;
            }
            if (tick < effect.getEndTick()) {
                return;
            }
            effects.Remove(effect);
            endEffect(context, "drought", "rain returned to " + nation.getName() + ", and its farms grew green again.", nation, null);
        }

        private void updateBandits(WorldEventContext context, WorldEffect effect, int tick) {
            Settlement settlement = context.getSettlement(effect.getSettlementId());
            Nation nation = settlement == null ? null : context.getNation(settlement.getNationId());
            if (settlement == null || settlement.isMarkedForDeletion() || nation == null) {
                effects.Remove(effect);
                return;
            }
            if (tick >= effect.getEndTick()) {
                effects.Remove(effect);
                endEffect(context, "bandits", "the bandits outside the " + settlement.getDisplayName() + " broke camp and moved on.", nation, settlement);
                return;
            }
            if (tick < effect.getNextTick()) {
                return;
            }
            effect.setNextTick(tick + context.ticks(BANDIT_THEFT_SECONDS));
            Inventory stores = settlement.getInventory();
            int coins = WorldEventContext.takeUpTo(stores, ItemType.COIN, BANDIT_THEFT_COINS);
            int wood = WorldEventContext.takeUpTo(stores, ItemType.WOOD, BANDIT_THEFT_GOODS);
            int stone = WorldEventContext.takeUpTo(stores, ItemType.STONE, BANDIT_THEFT_GOODS);
            if (context.isPlayersNation(nation) && coins + wood + stone > 0) {
                context.tellPlayer("Bandits robbed the " + settlement.getDisplayName() + " of " + describeLoot(coins, wood, stone) + ".");
            }
        }

        public static string describeLoot(int coins, int wood, int stone) {
            List<string> parts = new List<string>();
            if (coins > 0) parts.Add(coins + " coins");
            if (wood > 0) parts.Add(wood + " wood");
            if (stone > 0) parts.Add(stone + " stone");
            return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
        }

        private void endEffect(WorldEventContext context, string eventId, string text, Nation nation, Settlement settlement) {
            WorldEventInstance about = new WorldEventInstance(eventId, numEvents, context.getTick(), false);
            about.setNationId(nation.getId());
            about.setSettlementId(settlement == null ? null : settlement.getId());
            record(context, eventId, text, about);
            if (context.isPlayersNation(nation)) {
                context.tellPlayer(char.ToUpperInvariant(text[0]) + text.Substring(1));
            }
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.beginObject("random");
            writer.write("seed", random.getSeed());
            writer.beginArray("state");
            foreach (int value in random.getState()) {
                writer.writeValue(value);
            }
            writer.endArray();
            writer.endObject();
            writer.write("nextPlayerEventTick", nextPlayerEventTick);
            writer.write("nextWorldEventTick", nextWorldEventTick);
            writer.write("numEvents", numEvents);
            writer.write("numPlayerEvents", numPlayerEvents);
            writer.write("lastPlayerEventId", lastPlayerEventId);
            if (pending == null) {
                writer.getJson().WriteNull("pending");
            } else {
                writer.beginObject("pending");
                pending.save(writer);
                writer.endObject();
            }
            writer.beginArray("effects");
            foreach (WorldEffect effect in effects) {
                writer.beginObject();
                effect.save(writer);
                writer.endObject();
            }
            writer.endArray();
        }

        /**
        * Reads the section back. A save from before world events (migrated
        * to format 2 with an empty section) starts both clocks from the first
        * tick it runs after loading, as a new world does.
        */
        internal void load(JsonElement element, SaveReader reader) {
            effects.Clear();
            pending = null;
            if (!SaveReader.has(element, "random")) {
                // the clocks start from the first tick the loaded world runs
                nextPlayerEventTick = Unscheduled;
                nextWorldEventTick = Unscheduled;
                return;
            }
            JsonElement saved = SaveReader.property(element, "random");
            if (SaveReader.getInt(saved, "seed") != random.getSeed()) {
                throw new SaveFormatException("the world events random stream was seeded differently");
            }
            List<int> values = new List<int>();
            foreach (JsonElement value in SaveReader.property(saved, "state").EnumerateArray()) {
                values.Add(value.GetInt32());
            }
            try {
                random.setState(values.ToArray());
            } catch (ArgumentException e) {
                throw new SaveFormatException("the world events random stream is corrupt", e);
            }
            nextPlayerEventTick = SaveReader.getInt(element, "nextPlayerEventTick");
            nextWorldEventTick = SaveReader.getInt(element, "nextWorldEventTick");
            numEvents = SaveReader.getInt(element, "numEvents");
            numPlayerEvents = SaveReader.getInt(element, "numPlayerEvents");
            lastPlayerEventId = SaveReader.getString(element, "lastPlayerEventId");
            JsonElement savedPending = SaveReader.property(element, "pending");
            if (savedPending.ValueKind != JsonValueKind.Null) {
                pending = WorldEventInstance.load(savedPending, reader);
            }
            foreach (JsonElement effect in SaveReader.property(element, "effects").EnumerateArray()) {
                effects.Add(WorldEffect.load(effect, reader));
            }
        }
    }
}
