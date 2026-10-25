using System.Collections.Generic;

namespace beyondnations {

    /**
    * Which chronicle entries are "your news": the ones about the player, the
    * player's nation or its neighbours, and the few that shape the whole
    * world. The HUD lists only these unless the player asks for every nation.
    *
    * Why: in the 0.6.2 playtest (docs/playtests/0.6.2, s04) the chronicle had
    * 495 entries by day 2, nearly all AI nations' rivalries and raids ("...
    * have become rivals over their border"), and the player's own lines were
    * buried among them.
    *
    * Worked out at draw time from what every entry already carries (the
    * nations and entities it names) and the world as it stands now, so it
    * needs nothing new in the save and old saves filter like new ones; and,
    * like ChronicleEntry.concerns, a nation that becomes a neighbour brings
    * its past lines with it.
    *
    * A neighbour of the player's nation is any other nation that:
    *  - borders it (Diplomacy.areNeighbours: settlements within
    *    Territory.NEIGHBOUR_DISTANCE), or
    *  - stands anywhere but Neutral with it (Allied, Friendly, Rival or
    *    Hostile; raiding needs hostility, so raiders are covered), or
    *  - is at war with it (Realm), or has denounced it or been denounced.
    *
    * World news, shown to everyone: a nation founded or disbanded, and any
    * line that names no nation and no one at all (a world-wide event). An
    * entry that names nobody is also how an entry with no metadata reads,
    * so such a line is shown rather than hidden.
    *
    * With no nation, the player's news is the lines about the player and
    * world news: there are no neighbours to follow yet, and the foundings
    * say which nations there are to join.
    */
    public class ChronicleFocus {
        private readonly EntityId playerId;
        private readonly NationId nationId;
        private readonly HashSet<NationId> neighbours;
        // every nation the player has founded or joined (Chronicle.getPlayerNations)
        private readonly HashSet<NationId> playerNations;

        public ChronicleFocus(EntityId playerId, NationId nationId, IEnumerable<NationId> neighbours, IEnumerable<NationId> playerNations = null) {
            this.playerId = playerId;
            this.nationId = nationId;
            this.neighbours = neighbours == null ? new HashSet<NationId>() : new HashSet<NationId>(neighbours);
            this.playerNations = playerNations == null ? new HashSet<NationId>() : new HashSet<NationId>(playerNations);
            if (nationId != null) {
                this.playerNations.Add(nationId);
            }
        }

        /** The focus for the simulation's player as the world stands now. */
        public static ChronicleFocus of(Simulation simulation) {
            Player player = simulation.getPlayer();
            if (player == null) {
                return new ChronicleFocus(null, null, null);
            }
            NationRepository nationRepository = simulation.getNationRepository();
            Nation own = player.getNationId() == null ? null : nationRepository.getNation(player.getNationId());
            Chronicle chronicle = simulation.getChronicle();
            return new ChronicleFocus(player.getId(), player.getNationId(),
                findNeighbours(own, nationRepository, simulation.getDiplomacy(), simulation.getRealm()),
                chronicle == null ? null : chronicle.getPlayerNations(player));
        }

        /** The nations whose news counts as the player's (see the class comment). Empty with no nation. */
        public static HashSet<NationId> findNeighbours(Nation own, NationRepository nationRepository, Diplomacy diplomacy, Realm realm) {
            HashSet<NationId> found = new HashSet<NationId>();
            if (own == null || nationRepository == null) {
                return found;
            }
            NationRelations relations = nationRepository.getRelations();
            foreach (Nation other in nationRepository.getNations()) {
                if (other == null || other.getId().Equals(own.getId())) {
                    continue;
                }
                bool related = relations != null
                    && (relations.getState(own, other) != NationRelationState.NEUTRAL
                        || relations.hasDenounced(own, other) || relations.hasDenounced(other, own));
                if (related
                    || (realm != null && realm.isAtWar(own, other))
                    || (diplomacy != null && diplomacy.areNeighbours(own, other))) {
                    found.Add(other.getId());
                }
            }
            return found;
        }

        /** World-shaping lines everyone sees: foundings, disbandings and lines that name no one. */
        public static bool isWorldNews(ChronicleEntry entry) {
            if (entry == null) {
                return false;
            }
            if (entry.getEventType() == EventType.NationCreation || entry.getEventType() == EventType.NationDisband) {
                return true;
            }
            return entry.getNationIds().Count == 0 && entry.getEntityIds().Count == 0;
        }

        public bool isNeighbour(NationId id) {
            return id != null && neighbours.Contains(id);
        }

        /**
        * Whether the entry is the player's own: about them, or about a nation
        * they have founded or joined, even one since lost (playtest 0.6.3 s07:
        * once their nation fell, its siege, raids and capture left "your
        * news").
        */
        public bool isPlayers(ChronicleEntry entry) {
            if (entry == null) {
                return false;
            }
            if (entry.involvesEntity(playerId)) {
                return true;
            }
            foreach (NationId id in entry.getNationIds()) {
                if (playerNations.Contains(id)) {
                    return true;
                }
            }
            return false;
        }

        /** Whether the entry is the player's news. */
        public bool includes(ChronicleEntry entry) {
            if (entry == null) {
                return false;
            }
            if (isWorldNews(entry) || isPlayers(entry)) {
                return true;
            }
            foreach (NationId id in entry.getNationIds()) {
                if (isNeighbour(id)) {
                    return true;
                }
            }
            return false;
        }

        /** The entries that are the player's news, oldest first. */
        public List<ChronicleEntry> select(IReadOnlyList<ChronicleEntry> entries) {
            List<ChronicleEntry> selected = new List<ChronicleEntry>();
            if (entries == null) {
                return selected;
            }
            foreach (ChronicleEntry entry in entries) {
                if (includes(entry)) {
                    selected.Add(entry);
                }
            }
            return selected;
        }

        /** The last count entries that are the player's news, oldest first, as Chronicle.getRecent. */
        public List<ChronicleEntry> recent(IReadOnlyList<ChronicleEntry> entries, int count) {
            List<ChronicleEntry> picked = new List<ChronicleEntry>();
            if (entries == null || count <= 0) {
                return picked;
            }
            for (int i = entries.Count - 1; i >= 0 && picked.Count < count; i--) {
                if (includes(entries[i])) {
                    picked.Add(entries[i]);
                }
            }
            picked.Reverse();
            return picked;
        }
    }
}
