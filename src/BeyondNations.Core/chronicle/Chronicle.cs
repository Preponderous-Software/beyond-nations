using System.Text.Json;
using System;
using System.Collections.Generic;

namespace beyondnations {

    /**
    * The world's history, told as it happens.
    *
    * The chronicle subscribes to the EventRepository and turns the events
    * worth telling into short lines with an in-game time stamp -- "Day 3, dusk
    * - Mira founded the nation of Ironvale." -- and keeps the most recent
    * ones. Noise is left out: chunk generation, pawn spawns and relationship
    * ticks never make an entry. In a populated world some events happen too
    * often to tell every time, so births and pawns joining a nation are told
    * only for the player's nation, as is drift between neutral and friendly
    * relations, and a raid between two other nations is told once a day per
    * raider and victim.
    *
    * Phrasing varies, but deterministically. Each line picks its template
    * from a hash of the world seed, the event and how many entries came
    * before it, so the same seed tells the same story. It deliberately does
    * not draw from the world's RandomSource: taking a number from it would
    * shift every roll after it and change the world itself, which a history
    * must never do.
    *
    * Lines are written when the event is published, inside the tick that
    * produced it, so they describe the world as it was at that moment.
    */
    public class Chronicle {
        public const int DefaultCapacity = 200;

        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly TimeOfDay timeOfDay;
        private readonly TickCounter tickCounter;
        private readonly int seed;
        private readonly int capacity;
        private readonly List<ChronicleEntry> entries = new List<ChronicleEntry>();
        private int totalRecorded;
        private int nationsFounded;
        // raider|victim pairs already told today, so a feud between two
        // nations is one line a day rather than one per raid
        private readonly HashSet<string> raidsToldToday = new HashSet<string>();
        private int raidDay = -1;

        public Chronicle(EventRepository eventRepository, EntityRepository entityRepository, NationRepository nationRepository,
                TimeOfDay timeOfDay, TickCounter tickCounter, int seed, int capacity = DefaultCapacity) {
            if (capacity <= 0) {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "capacity must be positive");
            }
            this.entityRepository = entityRepository;
            this.nationRepository = nationRepository;
            this.timeOfDay = timeOfDay;
            this.tickCounter = tickCounter;
            this.seed = seed;
            this.capacity = capacity;
            if (eventRepository != null) {
                eventRepository.subscribe(onEvent);
            }
        }

        /** Every kept entry, oldest first. At most getCapacity() of them. */
        public IReadOnlyList<ChronicleEntry> getEntries() {
            return entries;
        }

        /** The last count entries, oldest first. */
        public List<ChronicleEntry> getRecent(int count) {
            int start = Math.Max(0, entries.Count - Math.Max(0, count));
            return entries.GetRange(start, entries.Count - start);
        }

        public int getCount() { return entries.Count; }
        public int getCapacity() { return capacity; }
        /** Entries ever recorded, including those since dropped for capacity. */
        public int getTotalRecorded() { return totalRecorded; }

        /**
        * Writes the entry for an event, or returns null if the event is not
        * worth a line. Subscribed to the EventRepository; public so a test can
        * feed it events directly.
        */
        public ChronicleEntry record(Event e) {
            if (e == null) {
                return null;
            }
            List<NationId> nations = new List<NationId>();
            List<EntityId> entities = new List<EntityId>();
            string text = describe(e, nations, entities);
            if (text == null) {
                return null;
            }
            // Names come back lower-case for "you" and "a wanderer", so that
            // they read right mid-sentence; the sentence itself starts upper.
            text = char.ToUpperInvariant(text[0]) + text.Substring(1);

            ChronicleEntry entry = new ChronicleEntry(totalRecorded, e.getType(), currentTick(), currentDay(), currentPartOfDay(), text, nations, entities);
            totalRecorded++;
            entries.Add(entry);
            if (entries.Count > capacity) {
                entries.RemoveAt(0);
            }
            return entry;
        }

        private void onEvent(Event e) {
            record(e);
        }

        // --- time -----------------------------------------------------------

        private int currentTick() {
            return tickCounter != null ? tickCounter.getTotalTicks() : 0;
        }

        private int currentDay() {
            return timeOfDay != null ? timeOfDay.getDay() + 1 : 1;
        }

        private string currentPartOfDay() {
            return partOfDay(timeOfDay != null ? timeOfDay.getFraction() : TimeOfDay.DefaultStartFraction);
        }

        /**
        * Names a time of day, on TimeOfDay's scale: 0.25 sunrise, 0.5 noon,
        * 0.75 sunset.
        */
        public static string partOfDay(float fraction) {
            if (fraction < 0.2f) return "night";
            if (fraction < 0.3f) return "dawn";
            if (fraction < 0.45f) return "morning";
            if (fraction < 0.55f) return "midday";
            if (fraction < 0.7f) return "afternoon";
            if (fraction < 0.8f) return "dusk";
            return "night";
        }

        // --- wording --------------------------------------------------------

        private string describe(Event e, List<NationId> nations, List<EntityId> entities) {
            switch (e.getType()) {
                case EventType.NationCreation:
                    return describeNationCreation((NationCreationEvent) e, nations, entities);
                case EventType.NationJoin:
                    return describeNationJoin((NationJoinEvent) e, nations, entities);
                case EventType.NationLeave:
                    return describeNationLeave((NationLeaveEvent) e, nations, entities);
                case EventType.NationDisband:
                    return describeNationDisband((NationDisbandEvent) e, nations);
                case EventType.PawnDeath:
                    return describePawnDeath((PawnDeathEvent) e, nations, entities);
                case EventType.PlayerDeath:
                    return describePlayerDeath((PlayerDeathEvent) e, entities);
                case EventType.PlayerFallingIntoVoid:
                    return describeVoidFall(entities);
                case EventType.SettlementFounded:
                    return describeSettlementFounded((SettlementFoundedEvent) e, nations, entities);
                case EventType.StallBuilt:
                    return describeStallBuilt((StallBuiltEvent) e, nations, entities);
                case EventType.LeadershipChange:
                    return describeLeadershipChange((LeadershipChangeEvent) e, nations, entities);
                case EventType.PawnRelationshipIncrease:
                    return describeRelationshipMilestone((PawnRelationshipIncreaseEvent) e, nations, entities);
                case EventType.NationRelationChange:
                    return describeNationRelationChange((NationRelationChangeEvent) e, nations);
                case EventType.NationRaid:
                    return describeNationRaid((NationRaidEvent) e, nations, entities);
                case EventType.SettlementCapture:
                    return describeSettlementCapture((SettlementCaptureEvent) e, nations, entities);
                case EventType.PawnSpawn:
                    return describeBirth((PawnSpawnEvent) e, nations, entities);
                default:
                    // ChunkGenerate: noise.
                    return null;
            }
        }

        private string describeNationCreation(NationCreationEvent e, List<NationId> nations, List<EntityId> entities) {
            Nation nation = e.getNation();
            EntityId founderId = nation.getLeaderId();
            nations.Add(nation.getId());
            entities.Add(founderId);
            nationsFounded++;

            string n = nation.getName();
            string text;
            if (isPlayer(founderId)) {
                text = pick(e, n,
                    "You founded the nation of " + n + ".",
                    "You raised a banner and named your people " + n + ".",
                    "Under your hand, the nation of " + n + " was born.");
            }
            else {
                string f = nameOf(founderId);
                text = pick(e, n,
                    f + " founded the nation of " + n + ".",
                    f + " raised a banner and called it " + n + ".",
                    "The nation of " + n + " was born, with " + f + " at its head.",
                    f + " declared the nation of " + n + ", though none had yet sworn to it.");
            }
            if (nationsFounded == 1) {
                text += " It is the first nation this world has known.";
            }
            return text;
        }

        private string describeNationJoin(NationJoinEvent e, List<NationId> nations, List<EntityId> entities) {
            Nation nation = e.getNation();
            nations.Add(nation.getId());
            entities.Add(e.getEntityId());
            string n = nation.getName();
            if (isPlayer(e.getEntityId())) {
                return pick(e, n,
                    "You swore yourself to " + n + ".",
                    "You joined the nation of " + n + ".",
                    n + " took you in as one of its own.");
            }
            // Pawns join nations all the time in a populated world (immigrants
            // and settlers), so only a join to the player's own nation is told.
            if (!isPlayersNation(nation.getId())) {
                return null;
            }
            string p = nameOf(e.getEntityId());
            return pick(e, n + p,
                p + " joined " + n + ".",
                p + " swore fealty to " + n + ".",
                n + " welcomed " + p + " into its ranks.",
                p + " threw in their lot with " + n + ".");
        }

        private string describeNationLeave(NationLeaveEvent e, List<NationId> nations, List<EntityId> entities) {
            Nation nation = e.getNation();
            nations.Add(nation.getId());
            entities.Add(e.getEntityId());
            string n = nation.getName();
            if (isPlayer(e.getEntityId())) {
                return pick(e, n,
                    "You turned your back on " + n + ".",
                    "You left the nation of " + n + ".");
            }
            string p = nameOf(e.getEntityId());
            return pick(e, n + p,
                p + " left " + n + ".",
                p + " walked away from " + n + ".");
        }

        private string describeNationDisband(NationDisbandEvent e, List<NationId> nations) {
            Nation nation = e.getNation();
            nations.Add(nation.getId());
            string n = nation.getName();
            int settlements = nation.getNumberOfSettlements();
            if (settlements > 0) {
                return pick(e, n,
                    n + " has fallen; its settlements lie empty.",
                    n + " is no more, and its halls stand silent.",
                    "The banners of " + n + " came down for the last time, leaving " + plural(settlements, "settlement") + " to the wind.");
            }
            return pick(e, n,
                n + " faded from memory before it ever built a home.",
                n + " dissolved, its name already half-forgotten.",
                n + " is no more; it never raised so much as a hut.");
        }

        private string describePawnDeath(PawnDeathEvent e, List<NationId> nations, List<EntityId> entities) {
            Pawn pawn = e.getPawn();
            entities.Add(pawn.getId());
            Nation nation = nationOf(pawn.getNationId());
            if (nation != null) {
                nations.Add(nation.getId());
            }
            string p = pawn.getName();
            string where = pawn.isCurrentlyInSettlement() ? "within the walls" : "in the wilds";

            if (nation != null && pawn.getId().Equals(nation.getLeaderId())) {
                string n = nation.getName();
                return pick(e, p,
                    n + " mourns its leader, " + p + ", who starved " + where + ".",
                    "Hunger claimed " + p + ", leader of " + n + ", " + where + ".");
            }
            string who = nation != null ? p + " of " + nation.getName() : p;
            return pick(e, p,
                who + " starved " + where + ".",
                "Hunger claimed " + who + " " + where + ".",
                who + " was found starved " + where + ".",
                who + " died of hunger " + where + ".");
        }

        private string describePlayerDeath(PlayerDeathEvent e, List<EntityId> entities) {
            entities.Add(e.getPlayer().getId());
            return pick(e, "",
                "Hunger brought you low, but you rose again.",
                "You starved, and awoke somewhere else.",
                "You collapsed from hunger, and the world went on without you for a while.");
        }

        private string describeVoidFall(List<EntityId> entities) {
            Entity player = findPlayer();
            if (player != null) {
                entities.Add(player.getId());
            }
            return pickBy(EventType.PlayerFallingIntoVoid, "",
                "You fell into the void and clawed your way back to the surface.",
                "The void swallowed you, then spat you back out.");
        }

        private string describeSettlementFounded(SettlementFoundedEvent e, List<NationId> nations, List<EntityId> entities) {
            Nation nation = e.getNation();
            nations.Add(nation.getId());
            entities.Add(e.getFounderId());
            string n = nation.getName();
            string f = nameOf(e.getFounderId());
            int count = nation.getNumberOfSettlements();
            if (count <= 1) {
                return pick(e, n,
                    f + " raised the first settlement of " + n + ".",
                    n + " put down roots: " + f + " founded its first settlement.",
                    f + " laid the first stones of " + n + ".");
            }
            return pick(e, n,
                n + " grew: " + f + " founded its " + ordinal(count) + " settlement.",
                f + " raised a " + ordinal(count) + " settlement for " + n + ".");
        }

        private string describeStallBuilt(StallBuiltEvent e, List<NationId> nations, List<EntityId> entities) {
            Nation nation = e.getNation();
            nations.Add(nation.getId());
            entities.Add(e.getBuilderId());
            string n = nation.getName();
            string b = nameOf(e.getBuilderId());
            string text = pick(e, n,
                b + " built a market stall in " + n + ".",
                "Trade came to " + n + ": " + b + " built a stall.",
                "A new stall opened in the market of " + n + ", built by " + b + ".");
            Market market = e.getSettlement() != null ? e.getSettlement().getMarket() : null;
            if (market != null && market.getNumStalls() >= market.getMaxNumStalls()) {
                text += " The market is now full.";
            }
            return text;
        }

        private string describeLeadershipChange(LeadershipChangeEvent e, List<NationId> nations, List<EntityId> entities) {
            Nation nation = e.getNation();
            nations.Add(nation.getId());
            if (e.getPreviousLeaderId() != null) {
                entities.Add(e.getPreviousLeaderId());
            }
            entities.Add(e.getNewLeaderId());
            string n = nation.getName();

            if (isPlayer(e.getNewLeaderId())) {
                return pick(e, n,
                    "You now lead " + n + ".",
                    "The people of " + n + " looked to you; you lead them now.");
            }
            string next = nameOf(e.getNewLeaderId());
            if (isPlayer(e.getPreviousLeaderId())) {
                return pick(e, n,
                    "You stepped down; " + next + " now leads " + n + ".",
                    "You handed the rule of " + n + " to " + next + ".");
            }
            string previous = e.getPreviousLeaderId() != null ? nameOf(e.getPreviousLeaderId()) : "its fallen leader";
            return pick(e, n,
                next + " now leads " + n + ".",
                n + " has a new leader: " + next + ".",
                "The rule of " + n + " passed from " + previous + " to " + next + ".");
        }

        /**
        * Relationship ticks are noise, but a pawn coming to count the player a
        * friend, or becoming loyal, is a turn in the story. Acquaintance is
        * reached too easily to be worth a line.
        */
        private string describeRelationshipMilestone(PawnRelationshipIncreaseEvent e, List<NationId> nations, List<EntityId> entities) {
            Pawn pawn = e.getPawn1();
            Entity other = e.getEntity2();
            if (pawn == null || other == null || other.getType() != EntityType.PLAYER) {
                return null;
            }
            if (!pawn.getRelationships().TryGetValue(other.getId(), out int value)) {
                return null;
            }
            RelationshipTier before = RelationshipTiers.fromValue(value - e.getIncrease());
            RelationshipTier after = RelationshipTiers.fromValue(value);
            if (after <= before || after < RelationshipTier.FRIEND) {
                return null;
            }
            entities.Add(pawn.getId());
            entities.Add(other.getId());
            Nation nation = nationOf(pawn.getNationId());
            if (nation != null) {
                nations.Add(nation.getId());
            }
            string p = nation != null ? pawn.getName() + " of " + nation.getName() : pawn.getName();
            if (after == RelationshipTier.LOYAL) {
                return pick(e, pawn.getName(),
                    p + " is now loyal to you.",
                    p + " would follow you anywhere now.");
            }
            return pick(e, pawn.getName(),
                p + " now counts you a friend.",
                "You and " + p + " have become friends.");
        }

        /**
        * Relations only produce an event when two nations cross into another
        * named state, so each one is a line. The event's own wording already
        * tells an alliance from a rivalry or a cooling feud; alliances get a
        * little variety on top.
        */
        private string describeNationRelationChange(NationRelationChangeEvent e, List<NationId> nations) {
            // Drift between neutral and friendly is constant in a populated
            // world; it is told only when the player's nation is one of the pair.
            bool drift = e.getBefore() >= NationRelationState.NEUTRAL && e.getBefore() != NationRelationState.ALLIED
                && e.getAfter() >= NationRelationState.NEUTRAL && e.getAfter() != NationRelationState.ALLIED;
            if (drift && !isPlayersNation(e.getNation1().getId()) && !isPlayersNation(e.getNation2().getId())) {
                return null;
            }
            nations.Add(e.getNation1().getId());
            nations.Add(e.getNation2().getId());
            if (e.getAfter() == NationRelationState.ALLIED) {
                string a = e.getNation1().getName();
                string b = e.getNation2().getName();
                return pick(e, a + "|" + b,
                    a + " and " + b + " have formed an alliance.",
                    a + " and " + b + " swore an alliance.");
            }
            return e.getDescription();
        }

        private string describeNationRaid(NationRaidEvent e, List<NationId> nations, List<EntityId> entities) {
            // Hostile nations raid each other every few seconds. A raid on or by
            // the player's nation is always told; any other pair gets one line a day.
            if (!isPlayersNation(e.getRaider().getId()) && !isPlayersNation(e.getVictim().getId())) {
                int day = currentDay();
                if (day != raidDay) {
                    raidDay = day;
                    raidsToldToday.Clear();
                }
                if (!raidsToldToday.Add(e.getRaider().getId() + "|" + e.getVictim().getId())) {
                    return null;
                }
            }
            nations.Add(e.getRaider().getId());
            nations.Add(e.getVictim().getId());
            if (e.getSettlement() != null) {
                entities.Add(e.getSettlement().getId());
            }
            string raider = e.getRaider().getName();
            string victim = e.getVictim().getName();
            return pick(e, raider + "|" + victim,
                raider + " raided " + victim + " and carried off " + e.getLoot() + ".",
                "Raiders from " + raider + " struck " + victim + ", taking " + e.getLoot() + ".");
        }

        private string describeSettlementCapture(SettlementCaptureEvent e, List<NationId> nations, List<EntityId> entities) {
            nations.Add(e.getTo().getId());
            nations.Add(e.getFrom().getId());
            entities.Add(e.getSettlement().getId());
            string to = e.getTo().getName();
            string from = e.getFrom().getName();
            string place = SettlementTiers.getDisplayName(e.getSettlement().getTier()).ToLowerInvariant();
            return pick(e, to + "|" + from,
                to + " captured a " + place + " from " + from + ".",
                "After a siege, a " + place + " of " + from + " fell to " + to + ".");
        }

        /**
        * Spawns are noise (world generation, wanderers, immigrants), except a
        * child born into the player's nation: a newborn is the only pawn that
        * spawns already belonging to a nation.
        */
        private string describeBirth(PawnSpawnEvent e, List<NationId> nations, List<EntityId> entities) {
            Pawn pawn = e.getPawn();
            if (pawn == null || pawn.getNationId() == null || !isPlayersNation(pawn.getNationId())) {
                return null;
            }
            Nation nation = nationOf(pawn.getNationId());
            if (nation == null) {
                return null;
            }
            nations.Add(nation.getId());
            entities.Add(pawn.getId());
            string n = nation.getName();
            return pick(e, pawn.getName(),
                pawn.getName() + " was born in " + n + ".",
                n + " welcomed a newborn, " + pawn.getName() + ".");
        }

        // --- helpers --------------------------------------------------------

        private bool isPlayersNation(NationId id) {
            Player player = findPlayer() as Player;
            return id != null && player != null && player.getNationId() != null && player.getNationId().Equals(id);
        }

        private string pick(Event e, string key, params string[] options) {
            return pickBy(e.getType(), key, options);
        }

        private string pickBy(EventType type, string key, params string[] options) {
            uint hash = stableHash(seed + "|" + (int) type + "|" + key + "|" + totalRecorded);
            return options[(int) (hash % (uint) options.Length)];
        }

        /**
        * FNV-1a. string.GetHashCode() is randomised per process, which would
        * tell a different story every run.
        */
        public static uint stableHash(string text) {
            uint hash = 2166136261;
            foreach (char c in text) {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }

        private bool isPlayer(EntityId id) {
            if (id == null || entityRepository == null) {
                return false;
            }
            Entity entity = entityRepository.getEntity(id);
            return entity != null && entity.getType() == EntityType.PLAYER;
        }

        private Entity findPlayer() {
            if (entityRepository == null) {
                return null;
            }
            List<Entity> players = entityRepository.getEntitiesOfType(EntityType.PLAYER);
            return players.Count > 0 ? players[0] : null;
        }

        private string nameOf(EntityId id) {
            Entity entity = id != null && entityRepository != null ? entityRepository.getEntity(id) : null;
            if (entity == null) {
                return "a wanderer";
            }
            if (entity.getType() == EntityType.PLAYER) {
                return "you";
            }
            return entity.getName();
        }

        private Nation nationOf(NationId id) {
            if (id == null || nationRepository == null) {
                return null;
            }
            return nationRepository.getNation(id);
        }

        private static string plural(int count, string noun) {
            return count == 1 ? "one " + noun : count + " " + noun + "s";
        }

        public static string ordinal(int n) {
            switch (n) {
                case 1: return "first";
                case 2: return "second";
                case 3: return "third";
                case 4: return "fourth";
                case 5: return "fifth";
                case 6: return "sixth";
                case 7: return "seventh";
                case 8: return "eighth";
                case 9: return "ninth";
                case 10: return "tenth";
            }
            int lastTwo = n % 100;
            string suffix = lastTwo >= 11 && lastTwo <= 13 ? "th"
                : (n % 10) == 1 ? "st"
                : (n % 10) == 2 ? "nd"
                : (n % 10) == 3 ? "rd"
                : "th";
            return n + suffix;
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.beginArray("entries");
            foreach (ChronicleEntry entry in entries) {
                writer.beginObject();
                entry.save(writer);
                writer.endObject();
            }
            writer.endArray();
            writer.write("totalRecorded", totalRecorded);
            writer.write("nationsFounded", nationsFounded);
            writer.writeStringList("raidsToldToday", raidsToldToday);
            writer.write("raidDay", raidDay);
        }

        internal void load(JsonElement element, SaveReader reader) {
            entries.Clear();
            foreach (JsonElement entry in SaveReader.property(element, "entries").EnumerateArray()) {
                entries.Add(ChronicleEntry.load(entry, reader));
            }
            totalRecorded = SaveReader.getInt(element, "totalRecorded");
            nationsFounded = SaveReader.getInt(element, "nationsFounded");
            raidsToldToday.Clear();
            raidsToldToday.UnionWith(SaveReader.getStringList(element, "raidsToldToday"));
            raidDay = SaveReader.getInt(element, "raidDay");
        }
    }
}
